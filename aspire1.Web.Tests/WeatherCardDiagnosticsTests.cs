using aspire1.Contracts;
using aspire1.Web.Components;
using aspire1.Web.Components.Pages;
using aspire1.Web.Services;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using NSubstitute;
using System.Net;
using System.Text.Json;

namespace aspire1.Web.Tests;

/// <summary>
/// bUnit component tests for the WeatherCard diagnostics panel introduced in issue #32.
/// The panel renders inside each weather card when the WeatherCardDiagnostics feature flag is enabled,
/// and is absent from the DOM when the flag is disabled.
/// </summary>
public class WeatherCardDiagnosticsTests : BunitContext
{
    private static WeatherForecast MakeForecast(int tempC = 20, int humidity = 55) =>
        new(DateOnly.FromDateTime(DateTime.Today), tempC, "Sunny", humidity);

    private void RegisterReactionServices()
    {
        var notifier = Substitute.For<IReactionNotifier>();
        Services.AddSingleton(notifier);
        Services.AddScoped(sp => new ReactionService(sp.GetRequiredService<IReactionNotifier>()));
    }

    [Fact]
    public void DiagnosticsPanel_WhenShowDiagnosticsFalse_PanelNotRendered()
    {
        // Arrange
        RegisterReactionServices();
        var forecast = MakeForecast();

        // Act
        var cut = Render<WeatherCard>(parameters => parameters
            .Add(p => p.Forecast, forecast)
            .Add(p => p.ShowHumidity, false)
            .Add(p => p.ShowDiagnostics, false)
            .Add(p => p.HumidityEnabled, false));

        // Assert
        cut.FindAll(".weather-diag-panel").Should().BeEmpty(
            "diagnostics panel must not render when ShowDiagnostics is false");
    }

    [Fact]
    public void DiagnosticsPanel_WhenShowDiagnosticsTrue_PanelRendered()
    {
        // Arrange
        RegisterReactionServices();
        var forecast = MakeForecast();

        // Act
        var cut = Render<WeatherCard>(parameters => parameters
            .Add(p => p.Forecast, forecast)
            .Add(p => p.ShowHumidity, false)
            .Add(p => p.ShowDiagnostics, true)
            .Add(p => p.HumidityEnabled, false));

        // Assert
        cut.FindAll(".weather-diag-panel").Should().HaveCount(1,
            "diagnostics panel must render when ShowDiagnostics is true");
    }

    [Fact]
    public void DiagnosticsPanel_WhenEnabled_ShowsForecastDateAndTemperatures()
    {
        // Arrange
        RegisterReactionServices();
        var date = new DateOnly(2025, 6, 15);
        var forecast = new WeatherForecast(date, 22, "Cloudy", 60);
        var expectedDateKey = "2025-06-15";
        var expectedTempC = "22";
        var expectedTempF = forecast.TemperatureF.ToString();

        // Act
        var cut = Render<WeatherCard>(parameters => parameters
            .Add(p => p.Forecast, forecast)
            .Add(p => p.ShowHumidity, false)
            .Add(p => p.ShowDiagnostics, true)
            .Add(p => p.HumidityEnabled, false));

        // Assert
        var panel = cut.Find(".weather-diag-panel");
        panel.InnerHtml.Should().Contain(expectedDateKey,
            "diagnostics panel must show the ISO date key");
        panel.InnerHtml.Should().Contain(expectedTempC,
            "diagnostics panel must show TemperatureC");
        panel.InnerHtml.Should().Contain(expectedTempF,
            "diagnostics panel must show TemperatureF");
    }

    [Fact]
    public void DiagnosticsPanel_WhenEnabled_HumidityFlagStateReflected()
    {
        // Arrange
        RegisterReactionServices();
        var forecast = MakeForecast();

        // Act — render with HumidityEnabled=true
        var cutEnabled = Render<WeatherCard>(parameters => parameters
            .Add(p => p.Forecast, forecast)
            .Add(p => p.ShowHumidity, true)
            .Add(p => p.ShowDiagnostics, true)
            .Add(p => p.HumidityEnabled, true));

        // Act — render with HumidityEnabled=false
        var cutDisabled = Render<WeatherCard>(parameters => parameters
            .Add(p => p.Forecast, forecast)
            .Add(p => p.ShowHumidity, false)
            .Add(p => p.ShowDiagnostics, true)
            .Add(p => p.HumidityEnabled, false));

        // Assert
        cutEnabled.Find(".weather-diag-panel").InnerHtml.Should().Contain("✅ Enabled",
            "humidity flag row must show Enabled when HumidityEnabled=true");
        // The disabled card should contain the disabled marker for humidity
        var disabledPanel = cutDisabled.Find(".weather-diag-panel");
        disabledPanel.InnerHtml.Should().Contain("❌ Disabled",
            "humidity flag row must show Disabled when HumidityEnabled=false");
    }

    [Fact]
    public void WeatherCardDiagnosticsFlag_WhenDisabled_PanelAbsentInWeatherPage()
    {
        // Arrange — IFeatureManager returns true for WeatherForecast and WeatherHumidity,
        // but FALSE for WeatherCardDiagnostics. The panel must not appear anywhere.
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("WeatherForecast").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(Task.FromResult(false));
        featureManager.IsEnabledAsync("WeatherCardDiagnostics").Returns(Task.FromResult(false));

        var fakeForecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Today), 18, "Partly Cloudy", 45)
        };

        Services.AddSingleton(featureManager);
        Services.AddSingleton(BuildFakeWeatherApiClient(fakeForecasts));

        var notifier = Substitute.For<IReactionNotifier>();
        Services.AddSingleton(notifier);
        Services.AddScoped(sp => new ReactionService(sp.GetRequiredService<IReactionNotifier>()));

        // Act
        var cut = Render<Weather>();

        // Assert — no diagnostics panel anywhere in rendered output
        cut.FindAll(".weather-diag-panel").Should().BeEmpty(
            "diagnostics panel must not appear in weather page when WeatherCardDiagnostics flag is disabled");
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
