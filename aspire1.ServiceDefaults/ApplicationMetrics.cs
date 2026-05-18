using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Custom application metrics for Application Insights telemetry.
/// Provides counters and histograms for tracking business-specific metrics.
/// </summary>
public static class ApplicationMetrics
{
    private const string MeterName = "aspire1.metrics";
    private static readonly Meter Meter = new(MeterName, "1.0.0");

    /// <summary>
    /// Tracks counter button clicks on the Counter page.
    /// Tags: page, range (0-10, 11-50, 51-100, 100+)
    /// </summary>
    public static readonly Counter<long> CounterClicks = Meter.CreateCounter<long>(
        "counter.clicks",
        unit: "clicks",
        description: "Number of counter button clicks");

    /// <summary>
    /// Tracks total weather API calls.
    /// Tags: endpoint, feature_enabled
    /// </summary>
    public static readonly Counter<long> WeatherApiCalls = Meter.CreateCounter<long>(
        "weather.api.calls",
        unit: "calls",
        description: "Total number of weather API calls");

    /// <summary>
    /// Tracks forecasts with "Sunny" in the summary.
    /// Tags: temperature_range (<0, 0-15, 16-25, >25)
    /// </summary>
    public static readonly Counter<long> SunnyForecasts = Meter.CreateCounter<long>(
        "weather.sunny.count",
        unit: "forecasts",
        description: "Number of sunny weather forecasts");

    /// <summary>
    /// Tracks cache hits.
    /// Tags: entity (e.g., "weather")
    /// </summary>
    public static readonly Counter<long> CacheHits = Meter.CreateCounter<long>(
        "cache.hits",
        unit: "hits",
        description: "Number of cache hits");

    /// <summary>
    /// Tracks cache misses.
    /// Tags: entity (e.g., "weather")
    /// </summary>
    public static readonly Counter<long> CacheMisses = Meter.CreateCounter<long>(
        "cache.misses",
        unit: "misses",
        description: "Number of cache misses");

    /// <summary>
    /// Tracks API call duration in milliseconds.
    /// Tags: endpoint, success (true/false)
    /// </summary>
    public static readonly Histogram<double> ApiCallDuration = Meter.CreateHistogram<double>(
        "api.call.duration",
        unit: "ms",
        description: "API call duration in milliseconds");

    /// <summary>
    /// Helper method to categorize count into ranges for reduced cardinality.
    /// </summary>
    /// <param name="count">The current count value</param>
    /// <returns>Range string: "0-10", "11-50", "51-100", "100+"</returns>
    public static string GetCountRange(int count)
    {
        return count switch
        {
            <= 10 => "0-10",
            <= 50 => "11-50",
            <= 100 => "51-100",
            _ => "100+"
        };
    }

    /// <summary>
    /// Helper method to categorize temperature into ranges.
    /// </summary>
    /// <param name="temperatureC">Temperature in Celsius</param>
    /// <returns>Range string: "<0", "0-15", "16-25", ">25"</returns>
    public static string GetTemperatureRange(int temperatureC)
    {
        return temperatureC switch
        {
            < 0 => "<0",
            <= 15 => "0-15",
            <= 25 => "16-25",
            _ => ">25"
        };
    }
}
