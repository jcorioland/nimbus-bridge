using System.Text.Json;
using System.Text.Json.Serialization;

namespace NimbusBridge.Core.Models;

/// <summary>
/// Defines the base class for broker commands and responses.
/// </summary>
public abstract class BrokerCommandResponseBase
{
    /// <summary>
    /// Defines a default constructor for broker commands and responses.
    /// </summary>
    public BrokerCommandResponseBase()
    {
        CorrelationId = string.Empty;
        TenantId = string.Empty;
    }

    /// <summary>
    /// Defines a constructor for broker commands and responses.
    /// </summary>
    /// <param name="correlationId">The correlation id that helps to correlate commands and responses and ensure that a client cannot send random response to any tenant.</param>
    /// <param name="tenantId">The unique identifier of the tenant</param>
    public BrokerCommandResponseBase(string correlationId, string tenantId)
    {
        CorrelationId = correlationId;
        TenantId = tenantId;
    }

    /// <summary>
    /// Defines a constructor for broker commands and responses.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant</param>
    public BrokerCommandResponseBase(string tenantId)
    {
        CorrelationId = Guid.NewGuid().ToString();
        TenantId = tenantId;
    }

    /// <summary>
    /// The correlation id that helps to correlate commands and responses.
    /// It's important that this correlation id is unique and used only to correlate the command and its response, and also
    /// that is not possible for a client to send random response to any tenant.
    /// </summary>
    public string CorrelationId { get; set; }

    /// <summary>
    /// The unique identifier of the tenant for which the commands and responses are intended.
    /// </summary>
    public string TenantId { get; set; }
}
