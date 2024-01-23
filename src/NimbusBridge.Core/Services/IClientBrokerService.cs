using NimbusBridge.Core.Models;

namespace NimbusBridge.Core.Services;

/// <summary>
/// Defines an interface for the client broker service.
/// </summary>
public interface IClientBrokerService<TCommand> where TCommand: BrokerCommand
{
    /// <summary>
    /// Starts listening to the broker for commands.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that is used to stop the listening.</param>
    /// <returns>A task that can be awaited until the listening has been cancelled.</returns>
    Task StartListeningAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Raised when a command has been received from the broker.
    /// </summary>
    event Func<TCommand, Task>? CommandReceivedAsync;

    /// <summary>
    /// Sends a response to the broker.
    /// </summary>
    /// <param name="response">The response to send.</param>
    /// <param name="cancellationToken">A cancellation token that allows to cancel the operation.</param>
    /// <returns>A task that can be awaited until the response has been sent</returns>
    Task SendResponseAsync(BrokerResponseBase response, CancellationToken cancellationToken);
}
