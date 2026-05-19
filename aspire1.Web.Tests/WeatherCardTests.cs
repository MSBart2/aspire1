using aspire1.Contracts;
using aspire1.Web.Components;
using Bunit;

namespace aspire1.Web.Tests;

public class WeatherCardTests : BunitContext
{
    private static readonly WeatherForecast Forecast = new(DateOnly.FromDateTime(new DateTime(2026, 5, 19)), 21, "Sunny", 64);

    [Fact]
    public void WeatherCard_WhenDiagnosticsDisabled_DoesNotRenderDisclosure()
    {
        var cut = Render<WeatherCard>(parameters => parameters
            .Add(card => card.Forecast, Forecast)
            .Add(card => card.ShowHumidity, true)
            .Add(card => card.ShowDiagnostics, false)
            .Add(card => card.WeatherForecastEnabled, true));

        cut.FindAll("[data-testid='weather-diagnostics']").Should().BeEmpty();
    }

    [Fact]
    public void WeatherCard_WhenDiagnosticsEnabled_RendersSafeDiagnosticsDetails()
    {
        var diagnostics = new WeatherDiagnostics(
            "hit",
            "Redis cache",
            new DateTimeOffset(2026, 5, 19, 18, 5, 0, TimeSpan.Zero),
            ["weather.api.calls", "cache.hits", "api.call.duration", "weather.sunny.count"]);

        var cut = Render<WeatherCard>(parameters => parameters
            .Add(card => card.Forecast, Forecast)
            .Add(card => card.Diagnostics, diagnostics)
            .Add(card => card.ShowHumidity, true)
            .Add(card => card.ShowDiagnostics, true)
            .Add(card => card.WeatherForecastEnabled, true));

        cut.Find("[data-testid='weather-diagnostics']").OuterHtml.Should().Contain("Dev diagnostics");
        cut.Find("[data-testid='weather-diagnostics-source']").TextContent.Should().Be("Redis cache (hit)");
        cut.Find("[data-testid='weather-diagnostics-humidity']").TextContent.Should().Be("64%");
        cut.Find("[data-testid='weather-diagnostics-flags']").TextContent.Should().Contain("WeatherDiagnostics: true");
        cut.Find("[data-testid='weather-diagnostics-metrics']").TextContent.Should().Contain("cache.hits");
    }

    [Fact]
    public void WeatherCard_WhenHumidityHidden_DiagnosticsExplainsWhy()
    {
        var cut = Render<WeatherCard>(parameters => parameters
            .Add(card => card.Forecast, Forecast with { Humidity = 0 })
            .Add(card => card.ShowHumidity, false)
            .Add(card => card.ShowDiagnostics, true)
            .Add(card => card.WeatherForecastEnabled, true));

        cut.Find("[data-testid='weather-diagnostics-humidity']").TextContent.Should().Be("hidden by WeatherHumidity flag");
        cut.Find("[data-testid='weather-diagnostics-source']").TextContent.Should().Be("not available from API");
    }
}
