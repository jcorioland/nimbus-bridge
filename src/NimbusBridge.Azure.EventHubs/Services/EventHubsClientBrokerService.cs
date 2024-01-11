using NimbusBridge.Core.Models;
using NimbusBridge.Core.Services;

namespace NimbusBridge.Azure.EventHubs.Services;

public class EventHubsClientBrokerService : IClientBrokerService
{
    public Task<BrokerResponseBase> HandleCommandAsync(BrokerCommand command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task StartListeningAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
