using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using NimbusBridge.Core.Models;
using NimbusBridge.Core.Services;
using System.Collections.Concurrent;
using System.Diagnostics;
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
    private readonly EventHubProducerClient _commandsEventHubProducerClient;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<BrokerResponseBase>> _callbacks;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubsServerBrokerService"/> class.
    /// </summary>
    /// <param name="checkpointStoreBlobContainerUrl">The url of the blob container that is used for EventHubs checkpointing.</param>
    /// <param name="eventHubsNamespaceFqdn">The fqdn of the EventHubs namespace used to exchange commands and responses messages.</param>
    /// <param name="tokenCredential">The <see cref="TokenCredential"/> used to authenticate with Azure.</param>
    public EventHubsServerBrokerService(string checkpointStoreBlobContainerUrl, string eventHubsNamespaceFqdn, TokenCredential tokenCredential)
    {
        ArgumentNullException.ThrowIfNull(checkpointStoreBlobContainerUrl, nameof(checkpointStoreBlobContainerUrl));
        ArgumentNullException.ThrowIfNull(eventHubsNamespaceFqdn, nameof(eventHubsNamespaceFqdn));

        var checkpointStore = new BlobContainerClient(new Uri(checkpointStoreBlobContainerUrl), tokenCredential);

        _responsesEventProcessorClient = new EventProcessorClient(
            checkpointStore,
            EventHubConsumerClient.DefaultConsumerGroupName,
            eventHubsNamespaceFqdn,
            ResponsesEventHubName,
            tokenCredential
        );

        _commandsEventHubProducerClient = new EventHubProducerClient(
            eventHubsNamespaceFqdn,
            CommandsEventHubName,
            tokenCredential
        );

        _callbacks = new ConcurrentDictionary<string, TaskCompletionSource<BrokerResponseBase>>();
    }

    public async Task<BrokerResponseBase> SendCommandAsync(BrokerCommand command, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<BrokerResponseBase>(cancellationToken);
        if(!_callbacks.TryAdd(command.CorrelationId, tcs))
        {
            throw new InvalidOperationException("A callback for the given correlation id already exists.");
        }

        var dataBatch = await _commandsEventHubProducerClient.CreateBatchAsync(cancellationToken);
        dataBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(command.GetJson())));

        await _commandsEventHubProducerClient.SendAsync(dataBatch, cancellationToken);
        return await tcs.Task;
    }

    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        _responsesEventProcessorClient.ProcessEventAsync += OnProcessEventAsync;
        await _responsesEventProcessorClient.StartProcessingAsync(cancellationToken);
    }

    private Task OnProcessEventAsync(ProcessEventArgs args)
    {
        var jsonResponse = Encoding.UTF8.GetString(args.Data.Body.ToArray());
        var brokeredResponse = JsonSerializer.Deserialize<BrokerResponseBase>(jsonResponse);

        if (brokeredResponse == null)
        {
            return Task.CompletedTask;
        }

        if (!string.IsNullOrEmpty(brokeredResponse.CorrelationId))
        {
            TaskCompletionSource<BrokerResponseBase>? tcs;
            if (_callbacks.TryGetValue(brokeredResponse.CorrelationId, out tcs))
            {
                tcs.SetResult(brokeredResponse);
                _callbacks.Remove(brokeredResponse.CorrelationId, out _);
            }
            else
            {
                // todo: log
            }
        }

        return Task.CompletedTask;
    }
}
