namespace aspire1.WeatherService.Services;

public static class WeatherSummaryFormatter
{
    public static string FormatSummary(int temperatureC, int humidity = 0)
    {
        var band = temperatureC switch
        {
            < 0 => "Freezing",
            < 10 => "Cold",
            < 20 => "Mild",
            < 30 => "Warm",
            _ => "Hot"
        };

        return humidity > 0 ? $"{band} and humid" : band;
    }
}
