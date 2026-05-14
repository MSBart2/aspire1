namespace aspire1.Contracts;

/// <summary>
/// Shared weather forecast DTO — the single source of truth for the API contract between
/// <c>aspire1.WeatherService</c> and <c>aspire1.Web</c>. Any schema change here is a
/// compile-time error in both projects.
/// </summary>
/// <param name="Date">The forecast date.</param>
/// <param name="TemperatureC">Temperature in degrees Celsius.</param>
/// <param name="Summary">Short human-readable weather description (e.g. "Sunny", "Freezing").</param>
/// <param name="Humidity">Relative humidity percentage (0–100).</param>
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary, int Humidity)
{
    /// <summary>Gets the temperature converted to degrees Fahrenheit.</summary>
    public int TemperatureF => (int)Math.Round(TemperatureC * 1.8 + 32);
}
