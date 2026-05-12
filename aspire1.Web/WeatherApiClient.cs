namespace aspire1.Web;

/// <summary>
/// Encapsulates the result of a weather forecast API call, distinguishing between
/// a service-unavailable response (feature flag disabled or transient error) and a
/// legitimately empty 200 response (no forecast data available).
/// </summary>
/// <param name="Forecasts">The weather forecasts returned by the API. Empty when <see cref="IsUnavailable"/> is <see langword="true"/>.</param>
/// <param name="IsUnavailable">
/// <see langword="true"/> when the API returned 503 Service Unavailable (feature flag disabled) or an
/// <see cref="HttpRequestException"/> was caught; <see langword="false"/> when the API responded successfully.
/// </param>
public sealed record WeatherApiResult(WeatherForecast[] Forecasts, bool IsUnavailable = false);

/// <summary>
/// Typed HTTP client for the aspire1 WeatherService API.
/// Handles 503 Service Unavailable responses gracefully — returning an empty result instead of
/// throwing — so the Blazor UI can distinguish "feature disabled" from a real failure.
/// </summary>
public class WeatherApiClient(HttpClient httpClient, ILogger<WeatherApiClient> logger)
{
    // Constants for telemetry to avoid string allocations
    private const string SuccessTrue = "true";
    private const string SuccessFalse = "false";

    /// <summary>
    /// Fetches weather forecast data from the WeatherService API.
    /// </summary>
    /// <param name="maxItems">Maximum number of forecast entries to return. Defaults to 10.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="WeatherApiResult"/> containing the forecast array and an availability flag.
    /// Returns <c>IsUnavailable = true</c> with an empty array when the API responds with 503
    /// (feature flag disabled) or when an <see cref="HttpRequestException"/> is caught.
    /// </returns>
    /// <remarks>
    /// A 503 response is treated as intentional behavior (the WeatherForecast feature flag is
    /// disabled on the API side) and is not retried. All other HTTP errors are caught and logged
    /// at Warning level before returning an empty graceful result.
    /// </remarks>
    public async Task<WeatherApiResult> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var success = false;

        try
        {
            var response = await httpClient.GetAsync("/weatherforecast", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                // Feature flag is disabled on the API side — not an error, just temporarily unavailable.
                // Mark success=true: the API responded correctly; 503 here is intentional behavior.
                success = true;
                logger.LogInformation("WeatherForecast feature is disabled on the API side (503). Returning empty.");
                return new WeatherApiResult([], IsUnavailable: true);
            }

            response.EnsureSuccessStatusCode();

            var forecasts = await response.Content.ReadFromJsonAsync<WeatherForecast[]>(cancellationToken: cancellationToken);
            success = true;

            if (forecasts == null || forecasts.Length == 0)
                return new WeatherApiResult([], IsUnavailable: false);

            var trimmed = forecasts.Length <= maxItems ? forecasts : forecasts[..maxItems];
            return new WeatherApiResult(trimmed, IsUnavailable: false);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error fetching weather forecast. Returning empty.");
            return new WeatherApiResult([], IsUnavailable: true);
        }
        finally
        {
            stopwatch.Stop();

            // Track API call duration with endpoint and success status
            Microsoft.Extensions.Hosting.ApplicationMetrics.ApiCallDuration.Record(
                stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("endpoint", "weatherforecast"),
                new KeyValuePair<string, object?>("success", success ? SuccessTrue : SuccessFalse));
        }
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary, int Humidity)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
