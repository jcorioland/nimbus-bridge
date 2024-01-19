using NimbusBridge.Core.Models;

namespace NimbusBridge.Azure.EventHubs.Models;

/// <summary>
/// Defines a broker command that can be sent to the NimbusBridge client, through EventHubs broker.
/// </summary>
/// <param name="tenantId">The tenant identifier.</param>
/// <param name="commandName">The name of the command to send.</param>
public class EventHubsBrokerCommand(string tenantId, string commandName) : BrokerCommand(tenantId, commandName)
{
    /// <summary>
    /// The list of partitions that can be used to send the response to this command.
    /// </summary>
    public List<string> Partitions { get; set; } = new List<string>();
}
