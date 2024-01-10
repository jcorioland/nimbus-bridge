using NimbusBridge.Core.Models;

namespace NimbusBridge.Core.Services;

public interface IServerBrokerService
{
    Task StartListeningAsync(CancellationToken cancellationToken);
    Task<BrokerResponse<TResponse>> SendCommandAsync<TResponse>(BrokerCommand command, CancellationToken cancellationToken) where TResponse: class, new();
}
