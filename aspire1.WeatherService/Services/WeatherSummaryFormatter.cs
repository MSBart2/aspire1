namespace aspire1.WeatherService.Services;

/// <summary>
/// Produces deterministic, temperature-derived weather summary strings.
/// Temperature governs the base label; humidity governs the optional suffix.
/// </summary>
public static class WeatherSummaryFormatter
{
    /// <summary>
    /// Returns a human-readable weather summary based on temperature and humidity.
    /// </summary>
    /// <param name="temperatureC">Temperature in degrees Celsius.</param>
    /// <param name="humidity">Relative humidity percentage (0–100).</param>
    /// <returns>A summary string such as "Mild", "Hot and Humid", or "Bracing and Dry".</returns>
    public static string GetSummary(int temperatureC, int humidity)
    {
        var baseLabel = temperatureC switch
        {
            < -10 => "Freezing",
            < 0   => "Bracing",
            < 10  => "Chilly",
            < 18  => "Cool",
            < 24  => "Mild",
            < 30  => "Warm",
            < 38  => "Hot",
            _     => "Scorching"
        };

        if (humidity >= 80)
            return $"{baseLabel} and Humid";

        if (humidity <= 30)
            return $"{baseLabel} and Dry";

        return baseLabel;
    }
}
