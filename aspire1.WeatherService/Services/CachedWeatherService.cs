using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace aspire1.WeatherService.Services;

public class CachedWeatherService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedWeatherService> _logger;
    private const string CacheKeyPrefix = "api:weather:forecast";

    public CachedWeatherService(IDistributedCache cache, ILogger<CachedWeatherService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

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

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary, int Humidity)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
