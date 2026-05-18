namespace aspire1.WeatherService.Services;

public static class WeatherSummaryFormatter
{
    public static string GetSummary(int temperatureC) =>
        temperatureC switch
        {
            < 0 => "Freezing",
            < 16 => "Cold",
            < 26 => "Mild",
            <= 40 => "Warm",
            _ => "Hot"
        };
}
