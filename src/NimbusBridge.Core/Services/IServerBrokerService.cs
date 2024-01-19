using NimbusBridge.Core.Models;

namespace NimbusBridge.Core.Services;

public interface IServerBrokerService<TCommand> where TCommand : BrokerCommand
{
    Task StartListeningAsync(CancellationToken cancellationToken);
    Task<TResponse> SendCommandAsync<TResponse>(TCommand command, CancellationToken cancellationToken) where TResponse: BrokerResponseBase;
}
