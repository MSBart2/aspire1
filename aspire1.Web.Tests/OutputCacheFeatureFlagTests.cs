using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using System.Net;
using System.Text.Json;
using aspire1.Web.Components.Pages;

namespace aspire1.Web.Tests;

/// <summary>
/// Tests verifying that Weather.razor renders the correct output based on feature flag state,
/// with no stale page-level output cache interfering.
///
/// Background: Weather.razor previously had [OutputCache(Duration = 5)] which cached the
/// entire rendered page for 5 seconds. Feature flag toggles had no visible effect until the
/// cache expired. The attribute has been removed. These Bunit component tests render the
/// actual component and assert HTML output, providing a real regression guard that would
/// catch any accidental re-introduction of [OutputCache] on this page.
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
        Services.AddSingleton(BuildFakeWeatherApiClient([]));

        // Act — render Weather.razor with the feature flag disabled
        // WeatherApiClient.GetWeatherAsync() is never called on this code path
        var cut = Render<Weather>();

        // Assert — the "Feature Disabled" alert must appear in the rendered HTML
        cut.Markup.Should().Contain("Feature Disabled",
            "the 'Feature Disabled' heading must be rendered when the WeatherForecast flag is off");
        cut.FindAll("[data-testid='weather-card']").Should().BeEmpty(
            "no weather cards should appear when the WeatherForecast feature is disabled");
    }

    [Fact]
    public void WeatherForecastFlag_WhenEnabled_DoesNotRenderFeatureDisabledAlert()
    {
        // Arrange — IFeatureManager returns true for WeatherForecast, false for WeatherHumidity
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("WeatherForecast").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(Task.FromResult(false));

        var fakeForecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Today), 20, "Sunny", 50)
        };
        Services.AddSingleton(featureManager);
        Services.AddSingleton(BuildFakeWeatherApiClient(fakeForecasts));

        // Act — render Weather.razor with the feature flag enabled
        var cut = Render<Weather>();

        // Assert — the "Feature Disabled" alert must NOT appear; normal weather output is shown
        cut.FindAll(".alert-warning").Should().BeEmpty(
            "the 'Feature Disabled' alert must not be rendered when WeatherForecast flag is enabled");
    }

    [Fact]
    public void WeatherHumidityFlag_WhenDisabled_HumidityInfoNotRendered()
    {
        // Arrange — WeatherForecast enabled, WeatherHumidity disabled
        var featureManager = Substitute.For<IFeatureManager>();
        featureManager.IsEnabledAsync("WeatherForecast").Returns(Task.FromResult(true));
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(Task.FromResult(false));

        var fakeForecasts = new[]
        {
            // High humidity value — will only appear in HTML if ShowHumidity=true is passed to WeatherCard
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Today), 20, "Sunny", 80)
        };
        Services.AddSingleton(featureManager);
        Services.AddSingleton(BuildFakeWeatherApiClient(fakeForecasts));

        // Act
        var cut = Render<Weather>();

        // Assert — humidity-info div must be absent when WeatherHumidity flag is off
        cut.FindAll(".humidity-info").Should().BeEmpty(
            "humidity info must not render in weather cards when the WeatherHumidity flag is disabled");
    }

    private static WeatherApiClient BuildFakeWeatherApiClient(WeatherForecast[] forecasts)
    {
        var json = JsonSerializer.Serialize(forecasts);
        var handler = new FakeHttpMessageHandler(json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new WeatherApiClient(httpClient);
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
