using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;

namespace aspire1.WeatherService.Services;

/// <summary>
/// Health check for Azure App Configuration service.
/// Probes the <c>/kv</c> endpoint with a known API version — an HTTP 401 (Unauthorized) response
/// confirms the service is reachable and correctly rejecting unauthenticated requests. HTTP 2xx
/// responses also indicate a healthy service. Network failures or 5xx errors are reported as Unhealthy.
/// Designed for offline-first operation: if the endpoint is not configured, reports Healthy.
/// </summary>
public class AppConfigHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AppConfigHealthCheck> _logger;

    public AppConfigHealthCheck(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<AppConfigHealthCheck> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Checks Azure App Configuration reachability by probing <c>/kv?api-version=2023-11-01</c>.
    /// Returns <see cref="HealthCheckResult.Healthy"/> for 2xx and 401 responses (service up),
    /// <see cref="HealthCheckResult.Unhealthy"/> on 5xx or network/timeout failures,
    /// and <see cref="HealthCheckResult.Degraded"/> for any other unexpected status code.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var appConfigEndpoint = _configuration["AppConfig:Endpoint"];

            if (string.IsNullOrEmpty(appConfigEndpoint))
            {
                _logger.LogDebug("App Configuration endpoint not configured. Running in offline mode.");
                return HealthCheckResult.Healthy("App Configuration not configured (offline mode)");
            }

            var httpClient = _httpClientFactory.CreateClient("appconfig-health");
            // /kv is a valid Azure App Configuration REST endpoint; unauthenticated requests
            // receive HTTP 401 which proves the service is up and healthy.
            var probeUrl = $"{appConfigEndpoint.TrimEnd('/')}/kv?api-version=2023-11-01";

            try
            {
                var response = await httpClient.GetAsync(probeUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogDebug("App Configuration health check passed (HTTP {StatusCode})", (int)response.StatusCode);
                    return HealthCheckResult.Healthy("App Configuration is reachable");
                }

                if ((int)response.StatusCode >= 500)
                {
                    _logger.LogWarning("App Configuration returned server error: {StatusCode}", response.StatusCode);
                    return HealthCheckResult.Unhealthy($"App Configuration returned server error: {response.StatusCode}");
                }

                _logger.LogWarning("App Configuration returned unexpected status: {StatusCode}", response.StatusCode);
                return HealthCheckResult.Degraded($"App Configuration returned unexpected status: {response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to reach App Configuration endpoint");
                return HealthCheckResult.Unhealthy("App Configuration is unreachable", ex);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "App Configuration health check timed out");
                return HealthCheckResult.Unhealthy("App Configuration health check timed out", ex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during App Configuration health check");
            return HealthCheckResult.Unhealthy("Unexpected error checking App Configuration health", ex);
        }
    }
}
