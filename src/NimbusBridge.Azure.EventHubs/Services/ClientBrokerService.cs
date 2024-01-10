using NimbusBridge.Core.Models;
using NimbusBridge.Core.Services;

namespace NimbusBridge.Azure.EventHubs.Services;

public class ClientBrokerService : IClientBrokerService
{
    public Task<BrokerResponse<TResponse>> HandleCommandAsync<TResponse>(BrokerCommand command, CancellationToken cancellationToken) where TResponse : class, new()
    {
        throw new NotImplementedException();
    }

    public Task StartListeningAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
