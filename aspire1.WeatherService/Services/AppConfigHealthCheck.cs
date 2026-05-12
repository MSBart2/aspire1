using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;

namespace aspire1.WeatherService.Services;

public class AppConfigHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var endpoint = configuration["AppConfig:Endpoint"];
        if (string.IsNullOrEmpty(endpoint))
        {
            return HealthCheckResult.Healthy("Azure App Configuration not configured");
        }

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var healthUrl = new Uri(endpoint.TrimEnd('/') + "/health");
            var response = await httpClient.GetAsync(healthUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded("Azure App Configuration not responding successfully");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Azure App Configuration unreachable: {ex.Message}", ex);
        }
    }
}
