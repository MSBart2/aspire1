using aspire1.Contracts;

namespace aspire1.WeatherService.Services;

public sealed record CachedWeatherResult(
    WeatherForecast[] Forecasts,
    string CacheStatus,
    string Source);
