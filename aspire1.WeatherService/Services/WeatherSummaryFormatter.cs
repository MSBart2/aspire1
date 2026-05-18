namespace aspire1.WeatherService.Services;

/// <summary>
/// Provides deterministic human-readable weather summary labels based on temperature.
/// </summary>
/// <remarks>
/// Maps a Celsius temperature to one of five fixed labels:
/// Freezing (&lt;0°C), Cold (&lt;16°C), Mild (&lt;26°C), Warm (&lt;=40°C), Hot (&gt;40°C).
/// This class is stateless and thread-safe; use <see cref="GetSummary"/> directly as a pure function.
/// </remarks>
public static class WeatherSummaryFormatter
{
    /// <summary>
    /// Returns a weather summary label for the given temperature in Celsius.
    /// </summary>
    /// <param name="temperatureC">Temperature in degrees Celsius.</param>
    /// <returns>
    /// One of five deterministic labels: <c>"Freezing"</c>, <c>"Cold"</c>, <c>"Mild"</c>,
    /// <c>"Warm"</c>, or <c>"Hot"</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// string label = WeatherSummaryFormatter.GetSummary(-5);  // "Freezing"
    /// string label = WeatherSummaryFormatter.GetSummary(20);  // "Mild"
    /// string label = WeatherSummaryFormatter.GetSummary(45);  // "Hot"
    /// </code>
    /// </example>
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
