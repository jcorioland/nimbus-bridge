using Microsoft.Extensions.Configuration;
using NimbusBridge.Azure.EventHubs.Services;
using NimbusBridge.Core.Models;

internal class Program
{
    private static EventHubsClientBrokerService? clientBrokerService;
    private static readonly SemaphoreSlim semaphoreSlim = new(1, 1);

    private static async Task Main()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        IConfigurationRoot configuration = builder.Build();

        string? checkpointStoreBlobContainerUrl = configuration["NIMBUS_BRIDGE_CHECKPOINT_BLOB_CONTAINER_URL"];
        if (string.IsNullOrEmpty(checkpointStoreBlobContainerUrl))
        {
            throw new InvalidOperationException("The configuration setting NIMBUS_BRIDGE_CHECKPOINT_BLOB_CONTAINER_URL must be set.");
        }

        string? eventHubsNamespaceFqdn = configuration["NIMBUS_BRIDGE_EVENTHUBS_NAMESPACE_FQDN"];
        if(string.IsNullOrEmpty(eventHubsNamespaceFqdn))
        {
            throw new InvalidOperationException("The configuration setting variable NIMBUS_BRIDGE_EVENTHUBS_NAMESPACE_FQDN must be set.");
        }

        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) => {
            e.Cancel = true;
            cts.Cancel();
        };

        Console.WriteLine("Please enter the name of the tenant to simulate (contoso or adventureworks)");
        string tenantName = Console.ReadLine() ?? string.Empty;

        if(tenantName != "contoso" && tenantName != "adventureworks")
        {
            tenantName = "contoso";
            Console.WriteLine("The tenant name is invalid. The tenant name has been set to contoso.");
        }

        clientBrokerService = new EventHubsClientBrokerService(checkpointStoreBlobContainerUrl, eventHubsNamespaceFqdn, tenantName);
        clientBrokerService.CommandReceivedAsync += OnCommandReceivedAsync;
        await clientBrokerService.StartListeningAsync(cts.Token);
    }

    private static async Task OnCommandReceivedAsync(BrokerCommand command)
    {
        Console.WriteLine("New command received.");
        if(clientBrokerService == null)
        {
            throw new InvalidOperationException("The client broker service is not initialized.");
        }

        if (command.CommandName == "GetWeatherForecast")
        {
            Console.WriteLine("Sending response to command GetWeatherForecast.");
            var summaries = new[]
            {
                "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
            };

            var response = new GetWeatherForecastResponse(command)
            {
                Summary = summaries[new Random().Next(summaries.Length)],
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                TemperatureC = new Random().Next(-10, 10)
            };

            await semaphoreSlim.WaitAsync();
            try
            {
                await clientBrokerService.SendResponseAsync(response, CancellationToken.None);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
    }
}