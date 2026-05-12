namespace aspire1.Web;

/// <summary>Encapsulates the result of a weather forecast API call, distinguishing between
/// a service-unavailable response (feature flag disabled / transient error) and a legitimately
/// empty 200 response (no forecast data available).</summary>
public sealed record WeatherApiResult(WeatherForecast[] Forecasts, bool IsUnavailable = false);

public class WeatherApiClient(HttpClient httpClient, ILogger<WeatherApiClient> logger)
{
    // Constants for telemetry to avoid string allocations
    private const string SuccessTrue = "true";
    private const string SuccessFalse = "false";

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
