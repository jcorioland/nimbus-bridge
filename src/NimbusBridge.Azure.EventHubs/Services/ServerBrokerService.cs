using NimbusBridge.Core.Models;
using NimbusBridge.Core.Services;

namespace NimbusBridge.Azure.EventHubs.Services;

public class ServerBrokerService : IServerBrokerService
{
    public Task<BrokerResponse<TResponse>> SendCommandAsync<TResponse>(BrokerCommand command, CancellationToken cancellationToken) where TResponse : class, new()
    {
        throw new NotImplementedException();
    }

    public Task StartListeningAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
