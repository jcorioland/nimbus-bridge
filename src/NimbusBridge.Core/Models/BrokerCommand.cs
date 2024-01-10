namespace NimbusBridge.Core.Models;

/// <summary>
/// Defines the broker command.
/// </summary>
public class BrokerCommand : BrokerCommandResponseBase
{
    /// <summary>
    /// Creates a new instance of the <see cref="BrokerCommand"/>
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="commandName">The name of the command.</param>
    public BrokerCommand(string tenantId, string commandName)
        : base(tenantId)
    {
        CommandName = commandName;
    }

    /// <summary>
    /// Gets or sets the name of the command.
    /// </summary>
    public string CommandName { get; set; }
}
