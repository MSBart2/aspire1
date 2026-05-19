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
/// Tests verifying the WeatherDiagnosticsPanel renders correctly based on the
/// WeatherDiagnostics feature flag state.
/// </summary>
public class WeatherDiagnosticsPanelTests : BunitContext
{
    [Fact]
    public void DiagnosticsPanel_WhenFlagDisabled_IsNotRendered()
    {
        // Arrange — WeatherDiagnostics flag is OFF
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("WeatherForecast").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(Task.FromResult(false));
        featureManager.IsEnabledAsync("WeatherDiagnostics").Returns(Task.FromResult(false));

        var fakeForecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Today), 22, "Sunny", 65)
        };
        Services.AddSingleton(featureManager);
        Services.AddSingleton(BuildFakeWeatherApiClient(fakeForecasts));

        // Act
        var cut = Render<Weather>();

        // Assert — diagnostics panel must NOT be present
        cut.FindAll("[data-testid='weather-diagnostics-panel']").Should().BeEmpty(
            "diagnostics panel must not render when WeatherDiagnostics flag is disabled");
    }

    [Fact]
    public void DiagnosticsPanel_WhenFlagEnabled_IsRendered()
    {
        // Arrange — WeatherDiagnostics flag is ON
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("WeatherForecast").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherDiagnostics").Returns(Task.FromResult(true));

        var fakeForecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Today), 22, "Sunny", 65)
        };
        Services.AddSingleton(featureManager);
        Services.AddSingleton(BuildFakeWeatherApiClient(fakeForecasts));

        // Act
        var cut = Render<Weather>();

        // Assert — diagnostics panel must be present
        cut.FindAll("[data-testid='weather-diagnostics-panel']").Should().NotBeEmpty(
            "diagnostics panel must render when WeatherDiagnostics flag is enabled");
    }

    [Fact]
    public void DiagnosticsPanel_DisplaysCorrectTemperatureValues()
    {
        // Arrange
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("WeatherForecast").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(Task.FromResult(false));
        featureManager.IsEnabledAsync("WeatherDiagnostics").Returns(Task.FromResult(true));

        var fakeForecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Today), 25, "Warm", 70)
        };
        Services.AddSingleton(featureManager);
        Services.AddSingleton(BuildFakeWeatherApiClient(fakeForecasts));

        // Act
        var cut = Render<Weather>();

        // Assert — panel contains correct temperature data
        var panel = cut.Find("[data-testid='weather-diagnostics-panel']");
        panel.InnerHtml.Should().Contain("25°C", "should display TemperatureC value");
        panel.InnerHtml.Should().Contain("77°F", "should display calculated TemperatureF value");
    }

    [Fact]
    public void DiagnosticsPanel_DisplaysNA_WhenHumidityIsZero()
    {
        // Arrange
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("WeatherForecast").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherDiagnostics").Returns(Task.FromResult(true));

        var fakeForecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Today), 18, "Cloudy", 0)
        };
        Services.AddSingleton(featureManager);
        Services.AddSingleton(BuildFakeWeatherApiClient(fakeForecasts));

        // Act
        var cut = Render<Weather>();

        // Assert — panel shows N/A for humidity when value is 0
        var panel = cut.Find("[data-testid='weather-diagnostics-panel']");
        panel.InnerHtml.Should().Contain("N/A",
            "should display 'N/A' for humidity when the value is 0");
    }

    [Fact]
    public void DiagnosticsPanel_DisplaysHumidityPercentage_WhenValueIsPositive()
    {
        // Arrange
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("WeatherForecast").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherDiagnostics").Returns(Task.FromResult(true));

        var fakeForecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Today), 20, "Rainy", 85)
        };
        Services.AddSingleton(featureManager);
        Services.AddSingleton(BuildFakeWeatherApiClient(fakeForecasts));

        // Act
        var cut = Render<Weather>();

        // Assert
        var panel = cut.Find("[data-testid='weather-diagnostics-panel']");
        panel.InnerHtml.Should().Contain("85%",
            "should display actual humidity percentage when value is positive");
    }

    [Fact]
    public void DiagnosticsPanel_DisplaysMetricNames()
    {
        // Arrange
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("WeatherForecast").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(Task.FromResult(false));
        featureManager.IsEnabledAsync("WeatherDiagnostics").Returns(Task.FromResult(true));

        var fakeForecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Today), 15, "Cool", 40)
        };
        Services.AddSingleton(featureManager);
        Services.AddSingleton(BuildFakeWeatherApiClient(fakeForecasts));

        // Act
        var cut = Render<Weather>();

        // Assert — panel references known metric names
        var panel = cut.Find("[data-testid='weather-diagnostics-panel']");
        panel.InnerHtml.Should().Contain("aspire1.metrics/weather.api.calls",
            "should display the weather API calls metric name");
        panel.InnerHtml.Should().Contain("aspire1.metrics/api.call.duration",
            "should display the API call duration metric name");
    }

    private static WeatherApiClient BuildFakeWeatherApiClient(WeatherForecast[] forecasts)
    {
        var json = JsonSerializer.Serialize(forecasts);
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
}
