using NimbusBridge.Core.Models;

namespace NimbusBridge.Core.Services;

/// <summary>
/// Defines an interface for the client broker service.
/// </summary>
public interface IClientBrokerService
{
    /// <summary>
    /// Starts listening to the broker for commands.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that is used to stop the listening.</param>
    /// <returns>A task that can be awaited until the listening has been cancelled.</returns>
    Task StartListeningAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Handles a broker command on the client side and returns the response.
    /// </summary>
    /// <typeparam name="TResponse">The underlying type of the response.</typeparam>
    /// <param name="command">The command to be handled.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A broker response that contains the results of the command.</returns>
    Task<BrokerResponse<TResponse>> HandleCommandAsync<TResponse>(BrokerCommand command, CancellationToken cancellationToken) where TResponse : class, new();
}
