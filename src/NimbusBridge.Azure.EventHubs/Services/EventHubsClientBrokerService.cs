using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.EventHubs;
using NimbusBridge.Core.Models;
using NimbusBridge.Core.Services;
using Azure.Storage.Blobs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Identity;
using Azure.Messaging.EventHubs.Processor;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using NimbusBridge.Azure.EventHubs.Models;
using System.Collections.Concurrent;

namespace NimbusBridge.Azure.EventHubs.Services;

/// <summary>
/// Defines an implementation of <see cref="IClientBrokerService"/> that uses Azure Event Hubs as the underlying transport."/>
/// </summary>
public class EventHubsClientBrokerService : IClientBrokerService<EventHubsBrokerCommand>
{
    private const string CommandsEventHubName = "commands";
    private const string ResponsesEventHubName = "responses";
    private readonly EventProcessorClient _commandsEventProcessorClient;
    private readonly EventHubProducerClient _responsesEventHubProducerClient;
    private readonly string _tenantName;
    private readonly ConcurrentDictionary<string, List<string>> _responsePartitions = new ConcurrentDictionary<string, List<string>>();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubsClientBrokerService"/> class.
    /// </summary>
    /// <param name="checkpointStoreBlobContainerUrl">The url of the blob container that is used for EventHubs checkpointing.</param>
    /// <param name="eventHubsNamespaceFqdn">The fqdn of the EventHubs namespace used to exchange commands and responses messages.</param>
    /// <param name="tenantName">The name of the tenant.</param>
    public EventHubsClientBrokerService(string checkpointStoreBlobContainerUrl, string eventHubsNamespaceFqdn, string tenantName)
    {
        ArgumentNullException.ThrowIfNull(checkpointStoreBlobContainerUrl, nameof(checkpointStoreBlobContainerUrl));
        ArgumentNullException.ThrowIfNull(eventHubsNamespaceFqdn, nameof(eventHubsNamespaceFqdn));
        ArgumentNullException.ThrowIfNull(tenantName, nameof(tenantName));

        // for this sample, we use the Azure CLI credential as we support a flow that uses the Azure Developer CLI to deploy the sample (see README.md)
        // it assumes this sample NimbusBridge client will be run locally from your developer machine
        // if you need to change this behavior, you can use any of the Azure.Identity credential types (see https://docs.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme#defaultazurecredential)
        var tokenCredential = new AzureCliCredential();
        var checkpointStore = new BlobContainerClient(new Uri(checkpointStoreBlobContainerUrl), tokenCredential);

        _tenantName = tenantName;

        string tenantCommandsHubName = $"{tenantName}-{CommandsEventHubName}";
        _commandsEventProcessorClient = new EventProcessorClient(
            checkpointStore,
            EventHubConsumerClient.DefaultConsumerGroupName,
            eventHubsNamespaceFqdn,
            tenantCommandsHubName,
            tokenCredential
        );

        _responsesEventHubProducerClient = new EventHubProducerClient(
            eventHubsNamespaceFqdn,
            ResponsesEventHubName,
            tokenCredential
        );
    }

    /// <summary>
    /// Raised when a command has been received from the broker.
    /// </summary>
    public event Func<EventHubsBrokerCommand, Task>? CommandReceivedAsync;

    /// <summary>
    /// Starts listening to the broker for commands.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that is used to stop the listening.</param>
    /// <returns>A task that can be awaited until the listening has been cancelled.</returns>
    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        _commandsEventProcessorClient.ProcessEventAsync += ProcessCommandEventAsync;
        _commandsEventProcessorClient.ProcessErrorAsync += ProcessErrorAsync;
        await _commandsEventProcessorClient.StartProcessingAsync(cancellationToken);
        Console.WriteLine($"Started listening to the broker for commands on {_tenantName}-commands hub.");
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    /// <summary>
    /// Sends a response to the broker.
    /// </summary>
    /// <param name="response">The response to send.</param>
    /// <param name="cancellationToken">A cancellation token that allows to cancel the operation.</param>
    /// <returns>A task that can be awaited until the response has been sent</returns>
    public async Task SendResponseAsync(BrokerResponseBase response, CancellationToken cancellationToken)
    {
        if (!_responsePartitions.TryGetValue(response.CorrelationId, out var partitions) || !partitions.Any())
        {
            throw new InvalidOperationException($"Unable to retrieve the list of partitions to send the response to for correlation id {response.CorrelationId}.");
        }

        var createBatchOptions = new CreateBatchOptions
        {
            PartitionId = partitions.First()
        };

        var dataBatch = await _responsesEventHubProducerClient.CreateBatchAsync(createBatchOptions, cancellationToken);
        string json = JsonSerializer.Serialize(response);
        dataBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(json)));

        await _responsesEventHubProducerClient.SendAsync(dataBatch, cancellationToken);
    }

    private async Task ProcessCommandEventAsync(ProcessEventArgs args)
    {
        Console.WriteLine($"Received a new command on {_tenantName}-commands hub.");
        try
        {
            if (args.Data == null)
            {
                return;
            }

            var jsonCommand = Encoding.UTF8.GetString(args.Data.Body.ToArray());
            var command = JsonSerializer.Deserialize<EventHubsBrokerCommand>(jsonCommand) ?? throw new InvalidOperationException("Cannot deserialize the brokered command.");
            _responsePartitions.TryAdd(command.CorrelationId, command.Partitions);
            CommandReceivedAsync?.Invoke(command);
        }
        finally
        {
            await args.UpdateCheckpointAsync();
        }
    }


    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        Console.WriteLine(args.Exception.Message);
        return Task.CompletedTask;
    }
}
