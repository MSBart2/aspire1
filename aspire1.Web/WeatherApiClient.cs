namespace aspire1.Web;

public class WeatherApiClient(HttpClient httpClient, ILogger<WeatherApiClient> logger)
{
    // Constants for telemetry to avoid string allocations
    private const string SuccessTrue = "true";
    private const string SuccessFalse = "false";

    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var success = false;

        try
        {
            var response = await httpClient.GetAsync("/weatherforecast", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                // Feature flag is disabled on the API side — not an error, just temporarily unavailable
                logger.LogInformation("WeatherForecast feature is disabled on the API side (503). Returning empty.");
                return [];
            }

            response.EnsureSuccessStatusCode();

            var forecasts = await response.Content.ReadFromJsonAsync<WeatherForecast[]>(cancellationToken: cancellationToken);
            success = true;

            if (forecasts == null || forecasts.Length == 0)
                return [];

            return forecasts.Length <= maxItems ? forecasts : forecasts[..maxItems];
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error fetching weather forecast. Returning empty.");
            return [];
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
