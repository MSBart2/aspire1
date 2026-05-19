using aspire1.Contracts;
using Microsoft.Extensions.Logging;

namespace aspire1.Web;

/// <summary>
/// HTTP client for weather forecast API with graceful error handling.
/// </summary>
/// <remarks>
/// Deserializes the shared weather response envelope and trims the forecast list on the client.
/// Returns an empty envelope on service unavailable (feature flag disabled) or network errors,
/// allowing Blazor components to display user-friendly degraded state.
/// </remarks>
public class WeatherApiClient(HttpClient httpClient, ILogger<WeatherApiClient> logger)
{
    // Constants for telemetry and validation
    private const string SuccessTrue = "true";
    private const string SuccessFalse = "false";
    private const int MaxItemsLimit = 1000;

    /// <summary>
    /// Retrieves weather forecasts with diagnostics metadata and graceful degradation on service unavailability.
    /// </summary>
    /// <param name="maxItems">Maximum number of forecasts to return (must be 1-1000). Default: 10.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>Weather response envelope with forecasts and optional diagnostics.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxItems is outside valid range [1, 1000].</exception>
    public async Task<WeatherForecastResponse> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        static WeatherForecastResponse EmptyResponse() => new([], null);

        // Validate input before making HTTP request
        if (maxItems <= 0 || maxItems > MaxItemsLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(maxItems), $"maxItems must be between 1 and {MaxItemsLimit}");
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var success = false;

        try
        {
            using var response = await httpClient.GetAsync("/weatherforecast", HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                // API side feature disabled — return empty so UI can show friendly message
                logger.LogInformation("Weather API returned 503 (feature flag disabled). Returning empty forecasts for graceful degradation.");
                return EmptyResponse();
            }

            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<WeatherForecastResponse>(cancellationToken: cancellationToken);

            success = true;
            if (payload is null)
            {
                return EmptyResponse();
            }

            return payload with
            {
                Forecasts = payload.Forecasts.Take(maxItems).ToArray()
            };
        }
        catch (HttpRequestException ex)
        {
            // Log the specific error type for observability
            logger.LogWarning(ex, "Weather API request failed (network error or non-success status). Returning empty forecasts for graceful degradation.");
            return EmptyResponse();
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogWarning(ex, "Weather API returned invalid JSON. Returning empty forecasts for graceful degradation.");
            return EmptyResponse();
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
