using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace aspire1.WeatherService.Services;

/// <summary>
/// Provides weather forecast data with a Redis-backed cache-aside pattern.
/// Falls back gracefully to generating fresh data when the cache is unavailable.
/// </summary>
public class CachedWeatherService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedWeatherService> _logger;
    private const string CacheKeyPrefix = "api:weather:forecast";

    /// <summary>
    /// Initializes a new instance of <see cref="CachedWeatherService"/>.
    /// </summary>
    /// <param name="cache">The distributed cache used to store and retrieve forecast data.</param>
    /// <param name="logger">The logger instance for cache hit/miss and error diagnostics.</param>
    public CachedWeatherService(IDistributedCache cache, ILogger<CachedWeatherService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Returns an array of weather forecasts, serving from cache when available
    /// and writing through to cache on a miss (5-minute TTL).
    /// </summary>
    /// <param name="maxItems">The maximum number of forecast entries to return. Defaults to 10.</param>
    /// <param name="cancellationToken">A token to cancel the async operation.</param>
    /// <returns>An array of <see cref="WeatherForecast"/> records.</returns>
    public async Task<WeatherForecast[]> GetWeatherForecastAsync(
        int maxItems = 10,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}:{maxItems}";

        try
        {
            // Try cache first
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (cachedData != null)
            {
                _logger.LogInformation("Cache HIT for weather forecast (maxItems={MaxItems})", maxItems);
                ApplicationMetrics.CacheHits.Add(1,
                    new KeyValuePair<string, object?>("entity", "weather"));
                return JsonSerializer.Deserialize<WeatherForecast[]>(cachedData)!;
            }

            _logger.LogInformation("Cache MISS for weather forecast (maxItems={MaxItems})", maxItems);
            ApplicationMetrics.CacheMisses.Add(1,
                new KeyValuePair<string, object?>("entity", "weather"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache read failed, falling back to generation");
        }

        // Generate fresh data
        var forecasts = GenerateForecasts(maxItems);

        try
        {
            // Store in cache with 5-minute TTL
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(forecasts),
                options,
                cancellationToken);

            _logger.LogInformation("Cached weather forecast for 5 minutes");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache write failed, continuing without cache");
        }

        return forecasts;
    }

    private static WeatherForecast[] GenerateForecasts(int count)
    {
        // Generates randomized forecast data for demonstration purposes
        var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

        return Enumerable.Range(1, count).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)],
                Random.Shared.Next(20, 95) // Generate humidity between 20% and 94% (inclusive)
            ))
            .ToArray();
    }
}

/// <summary>
/// Represents a single-day weather forecast returned by the WeatherService API.
/// </summary>
/// <param name="Date">The forecast date.</param>
/// <param name="TemperatureC">Temperature in Celsius.</param>
/// <param name="Summary">A short human-readable summary (e.g., "Warm", "Freezing").</param>
/// <param name="Humidity">Relative humidity as a percentage (0–100).</param>
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary, int Humidity)
{
    /// <summary>
    /// Gets the temperature in Fahrenheit, derived from <see cref="TemperatureC"/>.
    /// </summary>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
