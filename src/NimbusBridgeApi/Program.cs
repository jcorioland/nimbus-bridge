
using Azure.Core;
using Azure.Identity;
using NimbusBridge.Azure.EventHubs.Models;
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

            // for this sample, we use the Azure CLI credential as we support a flow that uses the Azure Developer CLI to deploy the sample (see README.md)
            // This is useful if you want to run the NimbusBridgeApi locally from your developer machine
            TokenCredential azureTokenCredential = new AzureCliCredential();

            // when deployed in the cloud, we are using a user-assigned identity
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

            // in this example, we support two tenants: contoso and northwind
            // in a production use case, the list of tenants would be retrieved from a database and each request / user tenant would be deducted from an authentication token
            List<string> tenantIdentifiers = ["contoso", "northwind"];
            
            var serverBrokerService = new EventHubsServerBrokerService(checkpointBlobContainerUrl, eventHubsNamespaceFqdn, azureTokenCredential, tenantIdentifiers);
            CancellationTokenSource cancellationTokenSource = new();
            Task listeningTask = serverBrokerService.StartListeningAsync(cancellationTokenSource.Token);

            builder.Services.AddSingleton<IServerBrokerService<EventHubsBrokerCommand>>(serverBrokerService);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapGet("/weatherforecast", async (HttpContext httpContext, IServerBrokerService<EventHubsBrokerCommand> serverBrokerService) =>
            {
                string tenantId = httpContext.Request.Query["tenantId"].ToString();
                if(string.IsNullOrEmpty(tenantId))
                {
                    throw new HttpRequestException(message: "No tenantId parameter found.", inner: null, statusCode: System.Net.HttpStatusCode.BadRequest);
                }

                var command = new EventHubsBrokerCommand(tenantId, "GetWeatherForecast");
                return await serverBrokerService.SendCommandAsync<GetWeatherForecastResponse>(command, httpContext.RequestAborted);
            })
            .WithName("GetWeatherForecast")
            .WithOpenApi();

            app.Run();
            cancellationTokenSource.Cancel();
        }
    }
}
