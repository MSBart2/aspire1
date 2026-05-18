namespace aspire1.WeatherService.Services;

/// <summary>
/// Produces consistent human-readable weather summary text from forecast data.
/// Use this single place for all forecast display text — cards, logs, tooltips, all of it.
/// </summary>
public static class WeatherSummaryFormatter
{
    /// <summary>
    /// Returns a temperature band label for the given temperature in Celsius.
    /// Bands align with WeatherCard.razor CSS classes.
    /// </summary>
    public static string GetTemperatureLabel(int temperatureC) =>
        temperatureC switch
        {
            < 0 => "Freezing",
            < 16 => "Cold",
            < 26 => "Mild",
            <= 40 => "Hot",
            _ => "Scorching"
        };

    /// <summary>
    /// Formats a concise weather summary from temperature and optional humidity.
    /// When humidity is greater than zero, a humidity note is appended.
    /// </summary>
    public static string FormatSummary(int temperatureC, int humidity = 0)
    {
        var label = GetTemperatureLabel(temperatureC);
        return humidity > 0 ? $"{label}. Humidity: {humidity}%" : label;
    }
}
