using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using aspire1.WeatherService.Services;
using Xunit;
using System.Net;
using NSubstitute;

namespace aspire1.WeatherService.Tests;

/// <summary>
/// Tests for the /health/detailed endpoint behavior and AppConfigHealthCheck service.
/// Validates that the endpoint reports actual dependency health — no more assumed "healthy".
/// </summary>
public class HealthCheckTests
{
    // Creates AppConfigHealthCheck with a controlled HTTP response for unit testing.
    private static AppConfigHealthCheck CreateHealthCheck(
        string? endpoint,
        HttpResponseMessage? response = null,
        Exception? throwException = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppConfig:Endpoint"] = endpoint
            })
            .Build();

        var handler = new TestMessageHandler(response, throwException);
        var httpClient = new HttpClient(handler);

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("appconfig-health").Returns(httpClient);

        return new AppConfigHealthCheck(config, factory, NullLogger<AppConfigHealthCheck>.Instance);
    }

    [Fact]
    public async Task AppConfigHealthCheck_WithNullEndpoint_ReturnsHealthy()
    {
        var healthCheck = CreateHealthCheck(null);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("offline mode");
    }

    [Fact]
    public async Task AppConfigHealthCheck_WithEmptyEndpoint_ReturnsHealthy()
    {
        var healthCheck = CreateHealthCheck("");

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("offline mode");
    }

    [Fact]
    public async Task AppConfigHealthCheck_With401Response_ReturnsHealthy()
    {
        // 401 from Azure App Config = service is up and correctly rejecting unauthenticated requests
        var healthCheck = CreateHealthCheck(
            "https://mystore.azconfig.io",
            new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("reachable");
    }

    [Fact]
    public async Task AppConfigHealthCheck_With200Response_ReturnsHealthy()
    {
        var healthCheck = CreateHealthCheck(
            "https://mystore.azconfig.io",
            new HttpResponseMessage(HttpStatusCode.OK));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task AppConfigHealthCheck_With500Response_ReturnsUnhealthy()
    {
        var healthCheck = CreateHealthCheck(
            "https://mystore.azconfig.io",
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task AppConfigHealthCheck_WithNetworkException_ReturnsUnhealthy()
    {
        var healthCheck = CreateHealthCheck(
            "https://mystore.azconfig.io",
            throwException: new HttpRequestException("Network unreachable"));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("unreachable");
    }

    [Fact]
    public async Task AppConfigHealthCheck_WithTimeout_ReturnsUnhealthy()
    {
        // Simulates a timeout using a mocked handler — no external network calls
        var healthCheck = CreateHealthCheck(
            "https://mystore.azconfig.io",
            throwException: new TaskCanceledException("Request timed out due to 5s timeout"));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("timed out");
    }

    [Fact]
    public async Task HealthCheckService_ReportsActualDependencyStatus()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
            .AddCheck("test-dependency", () => HealthCheckResult.Healthy(), ["ready"]);

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        var report = await healthCheckService.CheckHealthAsync();

        report.Status.Should().Be(HealthStatus.Healthy);
        report.Entries.Should().ContainKey("self");
        report.Entries.Should().ContainKey("test-dependency");
    }

    [Fact]
    public async Task HealthCheckService_WithDegradedDependency_ReportsDegraded()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
            .AddCheck("degraded-dependency", () => HealthCheckResult.Degraded("Service is slow"), ["ready"]);

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        var report = await healthCheckService.CheckHealthAsync();

        report.Status.Should().Be(HealthStatus.Degraded);
        report.Entries["degraded-dependency"].Status.Should().Be(HealthStatus.Degraded);
    }

    /// <summary>
    /// Controlled HTTP message handler for unit testing — no real network calls.
    /// </summary>
    private sealed class TestMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage? _response;
        private readonly Exception? _exception;

        public TestMessageHandler(HttpResponseMessage? response = null, Exception? exception = null)
        {
            _response = response;
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_exception is not null) throw _exception;
            return Task.FromResult(_response ?? new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
