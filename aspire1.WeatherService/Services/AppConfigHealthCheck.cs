using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;

namespace aspire1.WeatherService.Services;

/// <summary>
/// Health check for Azure App Configuration service.
/// Verifies connectivity to the App Configuration endpoint.
/// Designed for offline-first operation with graceful fallback.
/// </summary>
public class AppConfigHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppConfigHealthCheck> _logger;
    private const int TimeoutSeconds = 5;

    public AppConfigHealthCheck(IConfiguration configuration, ILogger<AppConfigHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var appConfigEndpoint = _configuration["AppConfig:Endpoint"];
            
            // If App Config is not configured, consider it healthy (offline-first design)
            if (string.IsNullOrEmpty(appConfigEndpoint))
            {
                _logger.LogDebug("App Configuration endpoint not configured. Running in offline mode.");
                return HealthCheckResult.Healthy("App Configuration not configured (offline mode)");
            }

            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
            };

            // Attempt to reach the App Config endpoint
            try
            {
                var response = await httpClient.GetAsync($"{appConfigEndpoint}/health", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _logger.LogDebug("App Configuration health check passed");
                    return HealthCheckResult.Healthy("App Configuration service is responding");
                }
                
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    _logger.LogWarning("App Configuration service returned 503 - Service Unavailable");
                    return HealthCheckResult.Degraded("App Configuration service is temporarily unavailable");
                }

                _logger.LogWarning("App Configuration health check returned unexpected status: {StatusCode}", response.StatusCode);
                return HealthCheckResult.Degraded($"App Configuration health check returned {response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to connect to App Configuration endpoint");
                return HealthCheckResult.Unhealthy("App Configuration service is unreachable", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "App Configuration health check timed out after {Timeout}s", TimeoutSeconds);
                return HealthCheckResult.Unhealthy($"App Configuration health check timed out after {TimeoutSeconds}s", ex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during App Configuration health check");
            return HealthCheckResult.Unhealthy("Unexpected error checking App Configuration health", ex);
        }
    }
}
