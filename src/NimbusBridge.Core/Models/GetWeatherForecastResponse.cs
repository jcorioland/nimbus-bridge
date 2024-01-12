using System.Text.Json.Serialization;

namespace NimbusBridge.Core.Models;

[JsonSerializable(typeof(GetWeatherForecastResponse))]
public class GetWeatherForecastResponse : BrokerResponseBase
{
    public GetWeatherForecastResponse()
    {
    }

    public GetWeatherForecastResponse(BrokerCommand command) : base(command)
    {
    }

    public GetWeatherForecastResponse(string correlationId, string tenantId) : base(correlationId, tenantId)
    {
    }

    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}
