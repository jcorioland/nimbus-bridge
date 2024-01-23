using System.Text.Json;
using System.Text.Json.Serialization;

namespace NimbusBridge.Core.Models;

/// <summary>
/// Defines the broker command.
/// </summary>
/// <remarks>
/// Creates a new instance of the <see cref="BrokerCommand"/>
/// </remarks>
/// <param name="tenantId">The unique identifier of the tenant.</param>
/// <param name="commandName">The name of the command.</param>
public class BrokerCommand(string tenantId, string commandName) : BrokerCommandResponseBase(tenantId)
{

    /// <summary>
    /// Gets or sets the name of the command.
    /// </summary>
    public string CommandName { get; set; } = commandName;
}
