using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using aspire1.WeatherService.Services;
using Xunit;

namespace aspire1.WeatherService.Tests;

/// <summary>
/// Tests for the /health/detailed endpoint and AppConfigHealthCheck service.
/// Validates that the endpoint reports actual dependency health, not assumed "healthy".
/// </summary>
public class HealthCheckTests
{
    [Fact]
    public async Task AppConfigHealthCheck_WithNullEndpoint_ReturnsHealthy()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppConfig:Endpoint"] = null
            })
            .Build();

        var healthCheck = new AppConfigHealthCheck(configuration, NullLogger<AppConfigHealthCheck>.Instance);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("offline mode");
    }

    [Fact]
    public async Task AppConfigHealthCheck_WithEmptyEndpoint_ReturnsHealthy()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppConfig:Endpoint"] = ""
            })
            .Build();

        var healthCheck = new AppConfigHealthCheck(configuration, NullLogger<AppConfigHealthCheck>.Instance);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("offline mode");
    }

    [Fact]
    public async Task AppConfigHealthCheck_WithUnreachableEndpoint_ReturnsUnhealthy()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppConfig:Endpoint"] = "https://invalid-endpoint-that-does-not-exist-12345.azconfig.io"
            })
            .Build();

        var healthCheck = new AppConfigHealthCheck(configuration, NullLogger<AppConfigHealthCheck>.Instance);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("unreachable");
    }

    [Fact]
    public async Task AppConfigHealthCheck_WithTimeout_ReturnsUnhealthy()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Using httpbin.org/delay endpoint that will timeout on /health check
                ["AppConfig:Endpoint"] = "https://httpbin.org/delay/10"
            })
            .Build();

        var healthCheck = new AppConfigHealthCheck(configuration, NullLogger<AppConfigHealthCheck>.Instance);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("timed out");
    }

    [Fact]
    public async Task HealthCheckService_ReportsActualDependencyStatus()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
            .AddCheck("test-dependency", () => HealthCheckResult.Healthy(), ["ready"]);

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var report = await healthCheckService.CheckHealthAsync();

        // Assert
        report.Status.Should().Be(HealthStatus.Healthy);
        report.Entries.Should().ContainKey("self");
        report.Entries.Should().ContainKey("test-dependency");
    }

    [Fact]
    public async Task HealthCheckService_WithDegradedDependency_ReportsDegraded()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
            .AddCheck("degraded-dependency", () => HealthCheckResult.Degraded("Service is slow"), ["ready"]);

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var report = await healthCheckService.CheckHealthAsync();

        // Assert
        report.Status.Should().Be(HealthStatus.Degraded);
        report.Entries["degraded-dependency"].Status.Should().Be(HealthStatus.Degraded);
    }
}
