
using Azure.Core;
using Azure.Identity;
using NimbusBridge.Azure.EventHubs.Services;
using NimbusBridge.Core.Models;
using NimbusBridge.Core.Services;

namespace NimbusBridgeApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Get configuration from Azure KeyVault
            var azureKeyVaultEndpoint = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_ENDPOINT");
            if(string.IsNullOrEmpty(azureKeyVaultEndpoint))
            {
                throw new InvalidOperationException("AZURE_KEY_VAULT_ENDPOINT environment variable is not set");
            }

            TokenCredential azureTokenCredential = new DefaultAzureCredential();

            var nimbusBridgeUserAssignedIdentityId = Environment.GetEnvironmentVariable("NIMBUS_BRIDGE_USER_ASSIGNED_IDENTITY_ID");
            if (!string.IsNullOrEmpty(nimbusBridgeUserAssignedIdentityId))
            {
                azureTokenCredential = new ManagedIdentityCredential(clientId: nimbusBridgeUserAssignedIdentityId);
            }

            builder.Configuration.AddAzureKeyVault(new Uri(azureKeyVaultEndpoint), azureTokenCredential);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            string? checkpointBlobContainerUrl = builder.Configuration.GetValue<string>("CheckpointBlobContainerUrl");
            if (string.IsNullOrEmpty(checkpointBlobContainerUrl))
            {
                throw new InvalidOperationException("CheckpointBlobContainerUrl configuration is not set");
            }

            string? eventHubsNamespaceFqdn = builder.Configuration.GetValue<string>("EventHubsNamespaceFqdn");
            if (string.IsNullOrEmpty(eventHubsNamespaceFqdn))
            {
                throw new InvalidOperationException("EventHubsNamespaceFqdn configuration is not set");
            }

            var serverBrokerService = new EventHubsServerBrokerService(checkpointBlobContainerUrl, eventHubsNamespaceFqdn, azureTokenCredential);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Task listeningTask = serverBrokerService.StartListeningAsync(cancellationTokenSource.Token);

            builder.Services.AddSingleton<IServerBrokerService>(serverBrokerService);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            var summaries = new[]
            {
                "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
            };

            const string SampleTenantId = "55CDA25F-2555-4E53-B06C-8B61D1C45C79";
            app.MapGet("/weatherforecast", async (HttpContext httpContext, IServerBrokerService serverBrokerService) =>
            {
                var command = new BrokerCommand(SampleTenantId, "GetWeatherForecast");
                return await serverBrokerService.SendCommandAsync(command, httpContext.RequestAborted);
            })
            .WithName("GetWeatherForecast")
            .WithOpenApi();

            app.Run();
            cancellationTokenSource.Cancel();
        }
    }
}
