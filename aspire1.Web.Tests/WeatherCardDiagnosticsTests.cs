using aspire1.Contracts;
using aspire1.Web.Components;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using System.Net;
using System.Text.Json;

namespace aspire1.Web.Tests;

/// <summary>
/// Tests for the feature-flagged diagnostics panel on WeatherCard.razor.
///
/// The diagnostics panel is a compact developer-facing overlay that surfaces raw
/// forecast values and active feature flag states. It is hidden unless
/// <c>ShowDiagnostics=true</c> is passed to the component. In production,
/// the <c>WeatherCardDiagnostics</c> feature flag controls that parameter from
/// <c>Weather.razor</c>.
/// </summary>
public class WeatherCardDiagnosticsTests : BunitContext
{
    private static readonly WeatherForecast SampleForecast =
        new(new DateOnly(2025, 7, 15), 22, "Partly cloudy", 65);

    // ── Panel visibility ──────────────────────────────────────────────────────

    [Fact]
    public void DiagnosticsPanel_WhenShowDiagnosticsIsFalse_IsNotRendered()
    {
        var cut = Render<WeatherCard>(parameters => parameters
            .Add(p => p.Forecast, SampleForecast)
            .Add(p => p.ShowDiagnostics, false));

        cut.FindAll("[data-testid='weather-card-diagnostics']").Should().BeEmpty(
            "the diagnostics panel must be hidden when ShowDiagnostics is false");
    }

    [Fact]
    public void DiagnosticsPanel_WhenShowDiagnosticsIsTrue_IsRendered()
    {
        var cut = Render<WeatherCard>(parameters => parameters
            .Add(p => p.Forecast, SampleForecast)
            .Add(p => p.ShowDiagnostics, true));

        cut.FindAll("[data-testid='weather-card-diagnostics']").Should().HaveCount(1,
            "the diagnostics panel must be visible when ShowDiagnostics is true");
    }

    // ── Panel content correctness ─────────────────────────────────────────────

    [Fact]
    public void DiagnosticsPanel_ShowsCorrectDateKey()
    {
        var cut = Render<WeatherCard>(parameters => parameters
            .Add(p => p.Forecast, SampleForecast)
            .Add(p => p.ShowDiagnostics, true));

        cut.Find("[data-testid='diag-date']").TextContent
            .Should().Be("2025-07-15", "date key should be formatted as yyyy-MM-dd");
    }

    [Fact]
    public void DiagnosticsPanel_ShowsCorrectTemperatureCAndF()
    {
        var cut = Render<WeatherCard>(parameters => parameters
            .Add(p => p.Forecast, SampleForecast)
            .Add(p => p.ShowDiagnostics, true));

        var tempText = cut.Find("[data-testid='diag-temp']").TextContent;
        tempText.Should().Contain("22°C", "Celsius value must appear");
        tempText.Should().Contain("°F", "Fahrenheit value must appear");
    }

    [Fact]
    public void DiagnosticsPanel_WhenHumidityIsPresent_ShowsHumidityValue()
    {
        var cut = Render<WeatherCard>(parameters => parameters
            .Add(p => p.Forecast, SampleForecast)   // Humidity = 65
            .Add(p => p.ShowDiagnostics, true));

        cut.Find("[data-testid='diag-humidity']").TextContent
            .Should().Be("65%", "humidity percentage should appear when Humidity > 0");
    }

    [Fact]
    public void DiagnosticsPanel_WhenHumidityIsZero_ShowsNa()
    {
        var zeroHumidity = new WeatherForecast(SampleForecast.Date, SampleForecast.TemperatureC, SampleForecast.Summary, 0);
        var cut = Render<WeatherCard>(parameters => parameters
            .Add(p => p.Forecast, zeroHumidity)
            .Add(p => p.ShowDiagnostics, true));

        cut.Find("[data-testid='diag-humidity']").TextContent
            .Should().Be("n/a", "humidity should show 'n/a' when value is 0");
    }

    [Fact]
    public void DiagnosticsPanel_ShowsSummary()
    {
        var cut = Render<WeatherCard>(parameters => parameters
            .Add(p => p.Forecast, SampleForecast)
            .Add(p => p.ShowDiagnostics, true));

        cut.Find("[data-testid='diag-summary']").TextContent
            .Should().Be("Partly cloudy", "summary field should reflect the forecast summary");
    }

    [Fact]
    public void DiagnosticsPanel_WhenSummaryIsNull_ShowsNa()
    {
        var nullSummary = new WeatherForecast(SampleForecast.Date, SampleForecast.TemperatureC, null, 0);
        var cut = Render<WeatherCard>(parameters => parameters
            .Add(p => p.Forecast, nullSummary)
            .Add(p => p.ShowDiagnostics, true));

        cut.Find("[data-testid='diag-summary']").TextContent
            .Should().Be("n/a", "null summary should display as 'n/a'");
    }

    [Fact]
    public void DiagnosticsPanel_ShowsActiveFeatureFlagStates()
    {
        var cut = Render<WeatherCard>(parameters => parameters
            .Add(p => p.Forecast, SampleForecast)
            .Add(p => p.ShowHumidity, true)
            .Add(p => p.ShowDiagnostics, true));

        var flagText = cut.Find("[data-testid='diag-flags']").TextContent;
        flagText.Should().Contain("WeatherHumidity: True",
            "active flag state for WeatherHumidity must be reflected");
        flagText.Should().Contain("WeatherCardDiagnostics: true",
            "diagnostics panel always reports itself as active when visible");
    }

    [Fact]
    public void DiagnosticsPanel_DoesNotRenderWhenForecastIsNull()
    {
        var cut = Render<WeatherCard>(parameters => parameters
            .Add(p => p.Forecast, (WeatherForecast?)null)
            .Add(p => p.ShowDiagnostics, true));

        cut.FindAll("[data-testid='weather-card-diagnostics']").Should().BeEmpty(
            "no diagnostics panel should appear when Forecast is null (outer @if guards all output)");
    }

    // ── Integration: Weather.razor flag wiring ────────────────────────────────

    [Fact]
    public void WeatherPage_WhenDiagnosticsFlagDisabled_NoDiagnosticsPanelRendered()
    {
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("WeatherForecast").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(Task.FromResult(false));
        featureManager.IsEnabledAsync("WeatherCardDiagnostics").Returns(Task.FromResult(false));

        Services.AddSingleton(featureManager);
        Services.AddSingleton(BuildFakeWeatherApiClient([SampleForecast]));

        var cut = Render<aspire1.Web.Components.Pages.Weather>();

        cut.FindAll("[data-testid='weather-card-diagnostics']").Should().BeEmpty(
            "diagnostics panel must not appear when WeatherCardDiagnostics flag is off");
    }

    [Fact]
    public void WeatherPage_WhenDiagnosticsFlagEnabled_DiagnosticsPanelRendered()
    {
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("WeatherForecast").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(Task.FromResult(false));
        featureManager.IsEnabledAsync("WeatherCardDiagnostics").Returns(Task.FromResult(true));

        Services.AddSingleton(featureManager);
        Services.AddSingleton(BuildFakeWeatherApiClient([SampleForecast]));

        var cut = Render<aspire1.Web.Components.Pages.Weather>();

        cut.FindAll("[data-testid='weather-card-diagnostics']").Should().HaveCount(1,
            "one diagnostics panel should appear per forecast card when the flag is enabled");
    }

    private static aspire1.Web.WeatherApiClient BuildFakeWeatherApiClient(WeatherForecast[] forecasts)
    {
        var json = JsonSerializer.Serialize(forecasts);
        var handler = new FakeHttpMessageHandler(json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var logger = LoggerFactory
            .Create(b => b.AddConsole())
            .CreateLogger<aspire1.Web.WeatherApiClient>();
        return new aspire1.Web.WeatherApiClient(httpClient, logger);
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
