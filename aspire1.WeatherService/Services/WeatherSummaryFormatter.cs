namespace aspire1.WeatherService.Services;

/// <summary>
/// Provides deterministic, human-readable labels for weather conditions
/// based on temperature and humidity values.
/// </summary>
/// <remarks>
/// All methods are pure functions with no side effects or I/O.
/// Temperature bands align with <c>ApplicationMetrics.GetTemperatureRange</c>
/// in <c>aspire1.ServiceDefaults</c>.
/// </remarks>
public static class WeatherSummaryFormatter
{
    /// <summary>
    /// Returns a human-readable weather summary based on the given temperature.
    /// </summary>
    /// <param name="temperatureC">Temperature in degrees Celsius.</param>
    /// <returns>
    /// One of: <c>Freezing</c> (&lt; 0 °C), <c>Cold</c> (0–15 °C),
    /// <c>Mild</c> (16–25 °C), <c>Warm</c> (26–35 °C), or <c>Hot</c> (&gt; 35 °C).
    /// </returns>
    public static string GetSummary(int temperatureC) => temperatureC switch
    {
        < 0 => "Freezing",
        <= 15 => "Cold",
        <= 25 => "Mild",
        <= 35 => "Warm",
        _ => "Hot"
    };

    /// <summary>
    /// Returns a human-readable humidity description based on the given relative humidity.
    /// </summary>
    /// <param name="humidity">Relative humidity as an integer percentage (0–100).</param>
    /// <returns>
    /// One of: <c>dry</c> (&lt; 30 %), <c>comfortable</c> (30–60 %), or <c>humid</c> (&gt; 60 %).
    /// </returns>
    public static string GetHumidityDescription(int humidity) => humidity switch
    {
        < 30 => "dry",
        <= 60 => "comfortable",
        _ => "humid"
    };
}
