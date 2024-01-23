using System.Text.Json.Serialization;

namespace NimbusBridge.Core.Models;

[JsonDerivedType(typeof(GetWeatherForecastResponse))]
public abstract class BrokerResponseBase : BrokerCommandResponseBase
{
    public BrokerResponseBase()
        : base()
    {
    }

    public BrokerResponseBase(string correlationId, string tenantId)
        : base(correlationId, tenantId)
    {
    }

    public BrokerResponseBase(BrokerCommand command)
        : base(command.CorrelationId, command.TenantId)
    {
    }

    public bool HasError { get; set; } = false;
}
