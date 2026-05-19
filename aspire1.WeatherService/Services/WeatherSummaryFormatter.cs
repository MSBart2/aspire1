namespace aspire1.WeatherService.Services;

public static class WeatherSummaryFormatter
{
    public static string GetSummary(int temperatureC, int humidity)
    {
        var baseLabel = temperatureC switch
        {
            < -10 => "Freezing",
            < 0   => "Bracing",
            < 10  => "Chilly",
            < 16  => "Cool",
            < 22  => "Mild",
            < 28  => "Warm",
            < 32  => "Balmy",
            < 38  => "Hot",
            < 45  => "Sweltering",
            _     => "Scorching"
        };

        if (humidity >= 70)
            return $"{baseLabel} and Humid";
        if (humidity < 30)
            return $"{baseLabel} and Dry";

        return baseLabel;
    }
}
