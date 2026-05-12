using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace aspire1.WeatherService.Services;

/// <summary>
/// Health check that probes Azure App Configuration reachability via the key-value list endpoint.
/// Treats HTTP 200 and HTTP 401 (unauthorized but reachable) as healthy.
/// Returns <see cref="HealthCheckResult.Healthy"/> when not configured (offline-first design).
/// </summary>
public class AppConfigHealthCheck(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IHealthCheck
{
    /// <summary>
    /// Checks Azure App Configuration reachability by probing the <c>/kv</c> endpoint.
    /// HTTP 200 or 401 indicates the service is UP; any other status code indicates degraded;
    /// a network exception indicates unhealthy.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var endpoint = configuration["AppConfig:Endpoint"];
        if (string.IsNullOrEmpty(endpoint))
        {
            return HealthCheckResult.Healthy("Azure App Configuration not configured");
        }

        try
        {
            using var httpClient = httpClientFactory.CreateClient("appconfig-health");
            var probeUrl = new Uri(endpoint.TrimEnd('/') + "/kv?api-version=2023-11-01");
            var response = await httpClient.GetAsync(probeUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            // 401 = unauthorized but service is reachable = service is UP
            // 200 = accessible and authenticated = UP
            return (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded($"Azure App Configuration returned {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Azure App Configuration unreachable: {ex.Message}", ex);
        }
    }
}
