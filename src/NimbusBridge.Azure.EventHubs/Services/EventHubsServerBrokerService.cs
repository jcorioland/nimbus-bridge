using Azure.Core;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using NimbusBridge.Core.Models;
using NimbusBridge.Core.Services;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace NimbusBridge.Azure.EventHubs.Services;

/// <summary>
/// Defines an implementation of <see cref="IServerBrokerService"/> that uses Azure Event Hubs as the underlying transport."/>
/// </summary>
public class EventHubsServerBrokerService : IServerBrokerService
{
    private const string CommandsEventHubName = "commands";
    private const string ResponsesEventHubName = "responses";
    private readonly EventProcessorClient _responsesEventProcessorClient;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<BrokerResponseBase>> _callbacks;
    private readonly ConcurrentDictionary<string, EventHubProducerClient> _producers;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubsServerBrokerService"/> class.
    /// </summary>
    /// <param name="checkpointStoreBlobContainerUrl">The url of the blob container that is used for EventHubs checkpointing.</param>
    /// <param name="eventHubsNamespaceFqdn">The fqdn of the EventHubs namespace used to exchange commands and responses messages.</param>
    /// <param name="tokenCredential">The <see cref="TokenCredential"/> used to authenticate with Azure.</param>
    /// <param name="tenantIdentifiers">The names of the tenants.</param>
    public EventHubsServerBrokerService(string checkpointStoreBlobContainerUrl, string eventHubsNamespaceFqdn, TokenCredential tokenCredential, List<string> tenantIdentifiers)
    {
        ArgumentNullException.ThrowIfNull(checkpointStoreBlobContainerUrl, nameof(checkpointStoreBlobContainerUrl));
        ArgumentNullException.ThrowIfNull(eventHubsNamespaceFqdn, nameof(eventHubsNamespaceFqdn));
        ArgumentNullException.ThrowIfNull(tokenCredential, nameof(tokenCredential));
        ArgumentNullException.ThrowIfNull(tenantIdentifiers, nameof(tenantIdentifiers));

        // initialize the checkpoint store that's used by the event processor client to checkpoint event processing into Azure Blob Storage
        var checkpointStore = new BlobContainerClient(new Uri(checkpointStoreBlobContainerUrl), tokenCredential);

        // initialize the event processor client to process events from the responses event hub
        // all the tenants NimbusBridge clients will send their responses to this event hub
        // the response will contain the correlation id that will be used to correlate the response with the command
        _responsesEventProcessorClient = new EventProcessorClient(
            checkpointStore,
            EventHubConsumerClient.DefaultConsumerGroupName,
            eventHubsNamespaceFqdn,
            ResponsesEventHubName,
            tokenCredential
        );

        // initialize the event hub producer clients for each tenant
        _producers = new ConcurrentDictionary<string, EventHubProducerClient>();

        // each tenant will have its own event hub to send commands to the nimbus bridge client
        foreach(var tenantId in tenantIdentifiers)
        {
            var tenantCommandsEventHubProducerClient = new EventHubProducerClient(
                eventHubsNamespaceFqdn,
                $"{tenantId}-{CommandsEventHubName}", // the hub name is composed of the tenant id and the commands event hub name, ex: contoso-commands
                tokenCredential
            );

            // add the producer to the dictionary of producers so it can be retrieved when a command has to be sent
            _producers.TryAdd(tenantId, tenantCommandsEventHubProducerClient);
        }

        // initialize the dictionary of callbacks of task completion source that will be used to put the http request on hold until the response is received
        // and correlate the response with the command
        _callbacks = new ConcurrentDictionary<string, TaskCompletionSource<BrokerResponseBase>>();
    }

    /// <summary>
    /// Sends a command to the broker.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">A cancellation token that allows to interrupt the operation.</param>
    /// <exception cref="InvalidOperationException">If the callback already exists for the same command correlation id or if no EventHubProducerClient is found for the tenant</exception>
    public async Task<BrokerResponseBase> SendCommandAsync(BrokerCommand command, CancellationToken cancellationToken)
    {
        // create a task completion source that will be used to put the http request on hold until the response is received
        var tcs = new TaskCompletionSource<BrokerResponseBase>(cancellationToken);
        if(!_callbacks.TryAdd(command.CorrelationId, tcs))
        {
            throw new InvalidOperationException("A callback for the given correlation id already exists.");
        }

        // serialize the command to json
        var jsonCommand = JsonSerializer.Serialize(command);
        
        // retrieve the producer for the tenant
        if(!_producers.TryGetValue(command.TenantId, out var tenantEventHubProducerClient))
        {
            throw new InvalidOperationException($"No producer found for tenant {command.TenantId}.");
        }

        // create the event to send to the event hub
        var dataBatch = await tenantEventHubProducerClient.CreateBatchAsync(cancellationToken);
        dataBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(jsonCommand)));

        // send the event to the event hub
        await tenantEventHubProducerClient.SendAsync(dataBatch, cancellationToken);
        
        // this is where the http request is put on hold until the response is received
        // the task completion source will be completed in the OnProcessEventAsync method,
        // once a response with the same correlation id is received
        return await tcs.Task;
    }

    /// <summary>
    /// Starts listening to the broker for responses.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that allows to interrupt the operation.</param>
    /// <returns></returns>
    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        _responsesEventProcessorClient.ProcessEventAsync += OnProcessEventAsync;
        _responsesEventProcessorClient.ProcessErrorAsync += ProcessErrorAsync;
        await _responsesEventProcessorClient.StartProcessingAsync(cancellationToken);
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        Console.WriteLine(args.Exception.Message);
        return Task.CompletedTask;
    }

    private async Task OnProcessEventAsync(ProcessEventArgs args)
    {
        try
        {
            if(args.Data == null)
            {
                return;
            }

            // deserialize the response
            // in the case of the sample, we only support the GetWeatherForecastResponse but it might be extended to more strongly typed responses
            var jsonResponse = Encoding.UTF8.GetString(args.Data.Body.ToArray());
            var brokeredResponse = JsonSerializer.Deserialize<GetWeatherForecastResponse>(jsonResponse);

            if (brokeredResponse == null)
            {
                return;
            }


            if (!string.IsNullOrEmpty(brokeredResponse.CorrelationId))
            {
                // retrieve the task completion source that was created when the command was sent
                if (_callbacks.TryGetValue(brokeredResponse.CorrelationId, out TaskCompletionSource<BrokerResponseBase>? tcs))
                {
                    // this is where we complete the task completion source that put the http request on hold in the SendCommandAsync method
                    // by getting the tcs using the correlation id and setting the result, the http request is unblocked and the response is sent back to the client
                    tcs.SetResult(brokeredResponse);

                    // remove the callback from the dictionary
                    _callbacks.Remove(brokeredResponse.CorrelationId, out _);
                }
                else
                {
                    Console.WriteLine($"No callback found for correlation id {brokeredResponse.CorrelationId} and tenant {brokeredResponse.TenantId}.");
                }
            }
        }
        finally
        {
            // update the checkpoint so the event processor client knows where to start processing the next time it starts
            await args.UpdateCheckpointAsync();
        }
    }
}
