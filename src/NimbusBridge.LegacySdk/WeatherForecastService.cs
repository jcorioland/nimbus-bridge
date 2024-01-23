namespace NimbusBridge.LegacySdk;

/// <summary>
/// This class simulates a legacy SDK that is used to communicate with the NimbusBridge legacy software.
/// This service allows to get the weather forecast. You can imagine that it connects to various system / database that run on premise, for example.
/// </summary>
public class WeatherForecastService
{
    public WeatherForecast GetWeatherForecast(DateOnly date)
    {
        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        var weatherForecast = new WeatherForecast
        {
            Summary = summaries[new Random().Next(summaries.Length)],
            Date = date,
            TemperatureC = new Random().Next(-10, 10)
        };

        return weatherForecast;
    }
}
