namespace aspire1.Contracts;

/// <summary>
/// Shared weather API envelope containing forecast data plus optional diagnostics metadata.
/// </summary>
/// <param name="Forecasts">Forecast items for the requested period.</param>
/// <param name="Diagnostics">Optional developer-facing diagnostics metadata for the response.</param>
public sealed record WeatherForecastResponse(
    WeatherForecast[] Forecasts,
    WeatherDiagnostics? Diagnostics);
