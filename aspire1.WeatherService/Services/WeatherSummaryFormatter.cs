namespace aspire1.WeatherService.Services;

/// <summary>
/// Produces a concise, human-readable summary from weather forecast data.
/// Suitable for cards, logs, and tooltips — consistent in one place, every time.
/// </summary>
public static class WeatherSummaryFormatter
{
    /// <summary>
    /// Formats a weather summary from temperature and optional humidity.
    /// </summary>
    /// <param name="temperatureC">Temperature in degrees Celsius.</param>
    /// <param name="humidity">Optional relative humidity percentage (0–100). When zero or null, humidity text is omitted.</param>
    /// <returns>A short human-readable summary string.</returns>
    public static string Format(int temperatureC, int? humidity = null)
    {
        var label = temperatureC switch
        {
            < 0 => "Freezing",
            >= 0 and < 16 => "Cold",
            >= 16 and < 26 => "Mild",
            >= 26 and <= 40 => "Warm",
            _ => "Hot"
        };

        return humidity.HasValue && humidity.Value > 0
            ? $"{label} – humidity {humidity.Value}%"
            : label;
    }
}
