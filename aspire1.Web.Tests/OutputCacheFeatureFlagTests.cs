using aspire1.Contracts;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using System.Net;
using System.Text.Json;
using aspire1.Web.Components.Pages;

namespace aspire1.Web.Tests;

/// <summary>
/// Tests verifying that Weather.razor renders the correct output based on feature flag state,
/// with no stale page-level output cache interfering.
/// </summary>
public class OutputCacheFeatureFlagTests : BunitContext
{
    [Fact]
    public void WeatherForecastFlag_WhenDisabled_RendersFeatureDisabledAlert()
    {
        // Arrange — IFeatureManager returns false for WeatherForecast
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("WeatherForecast").Returns(Task.FromResult(false));

        Services.AddSingleton(featureManager);
        Services.AddSingleton(BuildFakeWeatherApiClient([], null));

        // Act — WeatherApiClient.GetWeatherAsync() is never called on this code path
        var cut = Render<Weather>();

        // Assert
        cut.Markup.Should().Contain("Feature Disabled",
            "the 'Feature Disabled' heading must be rendered when the WeatherForecast flag is off");
        cut.FindAll("[data-testid='weather-card']").Should().BeEmpty(
            "no weather cards should appear when the WeatherForecast feature is disabled");
    }

    [Fact]
    public void WeatherForecastFlag_WhenEnabled_DoesNotRenderFeatureDisabledAlert()
    {
        // Arrange
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("WeatherForecast").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(Task.FromResult(false));
        featureManager.IsEnabledAsync("WeatherDiagnostics").Returns(Task.FromResult(false));

        var fakeForecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Today), 20, "Sunny", 50)
        };

        Services.AddSingleton(featureManager);
        Services.AddSingleton(BuildFakeWeatherApiClient(fakeForecasts, null));

        // Act
        var cut = Render<Weather>();

        // Assert
        cut.FindAll(".alert-warning").Should().BeEmpty(
            "the 'Feature Disabled' alert must not be rendered when WeatherForecast flag is enabled");
    }

    [Fact]
    public void WeatherHumidityFlag_WhenDisabled_HumidityInfoNotRendered()
    {
        // Arrange
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("WeatherForecast").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(Task.FromResult(false));
        featureManager.IsEnabledAsync("WeatherDiagnostics").Returns(Task.FromResult(false));

        var fakeForecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Today), 20, "Sunny", 80)
        };

        Services.AddSingleton(featureManager);
        Services.AddSingleton(BuildFakeWeatherApiClient(fakeForecasts, null));

        // Act
        var cut = Render<Weather>();

        // Assert
        cut.FindAll(".humidity-info").Should().BeEmpty(
            "humidity info must not render in weather cards when the WeatherHumidity flag is disabled");
    }

    [Fact]
    public void WeatherDiagnosticsFlag_WhenDisabled_DiagnosticsDisclosureNotRendered()
    {
        // Arrange
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("WeatherForecast").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherDiagnostics").Returns(Task.FromResult(false));

        var fakeForecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Today), 20, "Sunny", 80)
        };
        var diagnostics = new WeatherDiagnostics("hit", "Redis cache", DateTimeOffset.UtcNow, ["weather.api.calls", "cache.hits"]);

        Services.AddSingleton(featureManager);
        Services.AddSingleton(BuildFakeWeatherApiClient(fakeForecasts, diagnostics));

        // Act
        var cut = Render<Weather>();

        // Assert
        cut.FindAll("[data-testid='weather-diagnostics']").Should().BeEmpty(
            "diagnostics must stay invisible when the WeatherDiagnostics feature flag is off");
    }

    [Fact]
    public void WeatherDiagnosticsFlag_WhenEnabled_RendersDiagnosticsDisclosure()
    {
        // Arrange
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("WeatherForecast").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherDiagnostics").Returns(Task.FromResult(true));

        var fakeForecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Today), 20, "Sunny", 80)
        };
        var diagnostics = new WeatherDiagnostics("miss", "fresh generation", DateTimeOffset.UtcNow, ["weather.api.calls", "cache.misses"]);

        Services.AddSingleton(featureManager);
        Services.AddSingleton(BuildFakeWeatherApiClient(fakeForecasts, diagnostics));

        // Act
        var cut = Render<Weather>();

        // Assert
        cut.Find("[data-testid='weather-diagnostics']").OuterHtml.Should().Contain("Dev diagnostics");
        cut.Find("[data-testid='weather-diagnostics-source']").TextContent.Should().Be("fresh generation (miss)");
    }

    [Fact]
    public void WeatherForecastFlag_RaceCondition_FeatureFlagEnabledButApiReturns503_ShowsEmptyList()
    {
        // Arrange — frontend flag says enabled, backend already returns 503.
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("WeatherForecast").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(Task.FromResult(false));
        featureManager.IsEnabledAsync("WeatherDiagnostics").Returns(Task.FromResult(false));

        var handler = new FakeHttpMessageHandler503();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var logger = LoggerFactory
            .Create(builder => builder.AddConsole())
            .CreateLogger<WeatherApiClient>();
        var client = new WeatherApiClient(httpClient, logger);

        Services.AddSingleton(featureManager);
        Services.AddSingleton(client);

        // Act
        var cut = Render<Weather>();

        // Assert
        cut.Markup.Should().Contain("Weather Forecast", "heading should render");
        cut.FindAll("[data-testid='weather-card']").Should().BeEmpty(
            "no weather cards should appear when API returns no data (race condition handled gracefully)");
        cut.Markup.Should().NotContain("Feature Disabled",
            "should not show 'Feature Disabled' when the frontend flag still says true");
    }

    private static WeatherApiClient BuildFakeWeatherApiClient(WeatherForecast[] forecasts, WeatherDiagnostics? diagnostics)
    {
        var json = JsonSerializer.Serialize(new WeatherForecastResponse(forecasts, diagnostics));
        var handler = new FakeHttpMessageHandler(json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var logger = LoggerFactory
            .Create(builder => builder.AddConsole())
            .CreateLogger<WeatherApiClient>();
        return new WeatherApiClient(httpClient, logger);
    }

    private sealed class FakeHttpMessageHandler(string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });
    }

    private sealed class FakeHttpMessageHandler503 : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            });
    }
}
