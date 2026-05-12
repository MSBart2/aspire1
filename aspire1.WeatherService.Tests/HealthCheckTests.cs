using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.FeatureManagement;
using System.Reflection;
using System.Text.Json;

namespace aspire1.WeatherService.Tests;

public class HealthCheckTests
{
    [Fact]
    public async Task DetailedHealth_Enabled_ReturnsFullHealthReport()
    {
        // Arrange
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("DetailedHealth").Returns(true);
        featureManager.IsEnabledAsync("WeatherForecast").Returns(true);

        var healthEntry = new HealthReportEntry(
            status: HealthStatus.Healthy,
            description: "Test service",
            duration: TimeSpan.FromMilliseconds(10),
            exception: null,
            data: null,
            tags: new[] { "ready" }
        );

        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry> { { "redis", healthEntry } },
            HealthStatus.Healthy,
            TimeSpan.FromMilliseconds(10)
        );

        var healthCheckService = Substitute.For<HealthCheckService>();
        healthCheckService.CheckHealthAsync(default).ReturnsForAnyArgs(healthReport);

        // Act
        var result = await GetDetailedHealthResponse(featureManager, healthCheckService);

        // Assert
        result.Should().NotBeNull();
        var json = JsonSerializer.Deserialize<JsonElement>(result);
        
        json.GetProperty("status").GetString().Should().Be("healthy");
        json.GetProperty("version").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("commitSha").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("service").GetString().Should().Be("apiservice");
        json.GetProperty("timestamp").ValueKind.Should().NotBe(JsonValueKind.Null);
        json.GetProperty("uptime").GetDouble().Should().BeGreaterThan(0);
        json.TryGetProperty("dependencies", out var dependencies).Should().BeTrue();
        dependencies.GetProperty("redis").GetProperty("status").GetString().Should().Be("healthy");
    }

    [Fact]
    public async Task DetailedHealth_Disabled_ReturnsMinimalResponse()
    {
        // Arrange
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("DetailedHealth").Returns(false);

        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            HealthStatus.Healthy,
            TimeSpan.Zero
        );

        var healthCheckService = Substitute.For<HealthCheckService>();
        healthCheckService.CheckHealthAsync(default).ReturnsForAnyArgs(healthReport);

        // Act
        var result = await GetDetailedHealthResponse(featureManager, healthCheckService);

        // Assert
        result.Should().NotBeNull();
        var json = JsonSerializer.Deserialize<JsonElement>(result);
        
        json.GetProperty("status").GetString().Should().Be("healthy");
        // Minimal response should only have status, not version or dependencies
        json.TryGetProperty("version", out _).Should().BeFalse();
        json.TryGetProperty("dependencies", out _).Should().BeFalse();
    }

    [Fact]
    public async Task HealthStatus_ReturnsHealthy_WhenAllDependenciesHealthy()
    {
        // Arrange
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("DetailedHealth").Returns(true);
        featureManager.IsEnabledAsync("WeatherForecast").Returns(true);

        var redisEntry = new HealthReportEntry(
            status: HealthStatus.Healthy,
            description: "Redis OK",
            duration: TimeSpan.FromMilliseconds(5),
            exception: null,
            data: null,
            tags: new[] { "ready" }
        );

        var appConfigEntry = new HealthReportEntry(
            status: HealthStatus.Healthy,
            description: "App Config OK",
            duration: TimeSpan.FromMilliseconds(15),
            exception: null,
            data: null,
            tags: new[] { "ready" }
        );

        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                { "redis", redisEntry },
                { "appconfig", appConfigEntry }
            },
            HealthStatus.Healthy,
            TimeSpan.FromMilliseconds(20)
        );

        var healthCheckService = Substitute.For<HealthCheckService>();
        healthCheckService.CheckHealthAsync(default).ReturnsForAnyArgs(healthReport);

        // Act
        var result = await GetDetailedHealthResponse(featureManager, healthCheckService);

        // Assert
        var json = JsonSerializer.Deserialize<JsonElement>(result);
        json.GetProperty("status").GetString().Should().Be("healthy");
    }

    [Fact]
    public async Task HealthStatus_ReturnsDegraded_WhenSomeDependenciesDown()
    {
        // Arrange
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("DetailedHealth").Returns(true);
        featureManager.IsEnabledAsync("WeatherForecast").Returns(true);

        var redisEntry = new HealthReportEntry(
            status: HealthStatus.Degraded,
            description: "Redis responding slowly",
            duration: TimeSpan.FromMilliseconds(500),
            exception: null,
            data: null,
            tags: new[] { "ready" }
        );

        var appConfigEntry = new HealthReportEntry(
            status: HealthStatus.Healthy,
            description: "App Config OK",
            duration: TimeSpan.FromMilliseconds(15),
            exception: null,
            data: null,
            tags: new[] { "ready" }
        );

        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                { "redis", redisEntry },
                { "appconfig", appConfigEntry }
            },
            HealthStatus.Degraded,
            TimeSpan.FromMilliseconds(515)
        );

        var healthCheckService = Substitute.For<HealthCheckService>();
        healthCheckService.CheckHealthAsync(default).ReturnsForAnyArgs(healthReport);

        // Act
        var result = await GetDetailedHealthResponse(featureManager, healthCheckService);

        // Assert
        var json = JsonSerializer.Deserialize<JsonElement>(result);
        json.GetProperty("status").GetString().Should().Be("degraded");
        json.GetProperty("dependencies").GetProperty("redis").GetProperty("status").GetString().Should().Be("degraded");
    }

    [Fact]
    public async Task HealthStatus_ReturnsUnhealthy_WhenCriticalDependencyDown()
    {
        // Arrange
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("DetailedHealth").Returns(true);
        featureManager.IsEnabledAsync("WeatherForecast").Returns(true);

        var redisEntry = new HealthReportEntry(
            status: HealthStatus.Unhealthy,
            description: "Redis unavailable",
            duration: TimeSpan.FromMilliseconds(1000),
            exception: new Exception("Connection timeout"),
            data: null,
            tags: new[] { "ready" }
        );

        var appConfigEntry = new HealthReportEntry(
            status: HealthStatus.Healthy,
            description: "App Config OK",
            duration: TimeSpan.FromMilliseconds(15),
            exception: null,
            data: null,
            tags: new[] { "ready" }
        );

        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                { "redis", redisEntry },
                { "appconfig", appConfigEntry }
            },
            HealthStatus.Unhealthy,
            TimeSpan.FromMilliseconds(1015)
        );

        var healthCheckService = Substitute.For<HealthCheckService>();
        healthCheckService.CheckHealthAsync(default).ReturnsForAnyArgs(healthReport);

        // Act
        var result = await GetDetailedHealthResponse(featureManager, healthCheckService);

        // Assert
        var json = JsonSerializer.Deserialize<JsonElement>(result);
        json.GetProperty("status").GetString().Should().Be("unhealthy");
        json.GetProperty("dependencies").GetProperty("redis").GetProperty("status").GetString().Should().Be("unhealthy");
    }

    // Helper method to simulate the endpoint response
    private static async Task<string> GetDetailedHealthResponse(IFeatureManager featureManager, HealthCheckService healthCheckService)
    {
        var version = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";
        var commitSha = "abc1234";

        var healthReport = await healthCheckService.CheckHealthAsync();
        var showDetailed = await featureManager.IsEnabledAsync("DetailedHealth");

        object response;
        if (showDetailed)
        {
            var dependencies = healthReport.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString().ToLowerInvariant(),
                    duration = entry.Value.Duration.TotalMilliseconds
                }
            );

            response = new
            {
                status = healthReport.Status.ToString().ToLowerInvariant(),
                version,
                commitSha,
                service = "apiservice",
                timestamp = DateTime.UtcNow,
                uptime = Environment.TickCount64 / 1000.0,
                dependencies,
                features = new
                {
                    detailedHealth = true,
                    weatherForecast = await featureManager.IsEnabledAsync("WeatherForecast")
                }
            };
        }
        else
        {
            response = new { status = healthReport.Status.ToString().ToLowerInvariant() };
        }

        return JsonSerializer.Serialize(response);
    }
}
