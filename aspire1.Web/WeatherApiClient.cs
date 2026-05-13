namespace aspire1.Web;

public class WeatherApiClient(HttpClient httpClient)
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
            using var response = await httpClient.GetAsync("/weatherforecast", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                // API side feature disabled — return empty so UI can show friendly message
                return Array.Empty<WeatherForecast>();
            }

            response.EnsureSuccessStatusCode();

            var forecasts = await response.Content.ReadFromJsonAsync<WeatherForecast[]?>(cancellationToken: cancellationToken)
                            ?? Array.Empty<WeatherForecast>();

            success = true;

            if (forecasts.Length <= maxItems) return forecasts;
            return forecasts.Take(maxItems).ToArray();
        }
        catch (HttpRequestException)
        {
            // Network or non-success response that couldn't be parsed — return empty and let UI handle it
            return Array.Empty<WeatherForecast>();
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
    public int TemperatureF => (int)Math.Round(TemperatureC * 1.8 + 32);
}
