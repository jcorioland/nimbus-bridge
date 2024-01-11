using NimbusBridge.Core.Models;

namespace NimbusBridge.Core.Services;

public interface IServerBrokerService
{
    Task StartListeningAsync(CancellationToken cancellationToken);
    Task<BrokerResponseBase> SendCommandAsync(BrokerCommand command, CancellationToken cancellationToken);
}
