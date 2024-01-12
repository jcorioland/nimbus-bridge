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

namespace NimbusBridge.Azure.EventHubs.Services;

/// <summary>
/// Defines an implementation of <see cref="IClientBrokerService"/> that uses Azure Event Hubs as the underlying transport."/>
/// </summary>
public class EventHubsClientBrokerService : IClientBrokerService
{
    private const string CommandsEventHubName = "commands";
    private const string ResponsesEventHubName = "responses";
    private readonly EventProcessorClient _commandsEventProcessorClient;
    private readonly EventHubProducerClient _responsesEventHubProducerClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubsClientBrokerService"/> class.
    /// </summary>
    /// <param name="checkpointStoreBlobContainerUrl">The url of the blob container that is used for EventHubs checkpointing.</param>
    /// <param name="eventHubsNamespaceFqdn">The fqdn of the EventHubs namespace used to exchange commands and responses messages.</param>
    public EventHubsClientBrokerService(string checkpointStoreBlobContainerUrl, string eventHubsNamespaceFqdn)
    {
        ArgumentNullException.ThrowIfNull(checkpointStoreBlobContainerUrl, nameof(checkpointStoreBlobContainerUrl));
        ArgumentNullException.ThrowIfNull(eventHubsNamespaceFqdn, nameof(eventHubsNamespaceFqdn));
        
        var tokenCredential = new DefaultAzureCredential();
        var checkpointStore = new BlobContainerClient(new Uri(checkpointStoreBlobContainerUrl), tokenCredential);

        _commandsEventProcessorClient = new EventProcessorClient(
            checkpointStore,
            EventHubConsumerClient.DefaultConsumerGroupName,
            eventHubsNamespaceFqdn,
            CommandsEventHubName,
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
    public event Func<BrokerCommand, Task>? CommandReceivedAsync;

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
        Console.WriteLine("Started listening to the broker for commands.");
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
        var dataBatch = await _responsesEventHubProducerClient.CreateBatchAsync(cancellationToken);
        string json = JsonSerializer.Serialize(response);
        dataBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(json)));

        await _responsesEventHubProducerClient.SendAsync(dataBatch, cancellationToken);
    }

    private async Task ProcessCommandEventAsync(ProcessEventArgs args)
    {
        Console.WriteLine("Received a new command.");
        try
        {
            if (args.Data == null)
            {
                return;
            }

            var jsonResponse = Encoding.UTF8.GetString(args.Data.Body.ToArray());
            var command = JsonSerializer.Deserialize<BrokerCommand>(jsonResponse);
            if (command == null)
            {
                throw new InvalidOperationException("Cannot deserialize the brokered command.");
            }

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
