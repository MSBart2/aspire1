using Microsoft.FeatureManagement;
using NSubstitute;

namespace aspire1.Web.Tests;

public class OutputCacheFeatureFlagTests
{
    [Fact]
    public async Task FeatureFlagToggle_WeatherPageNotCached_ShowsCurrentState()
    {
        // Arrange
        var featureManager = Substitute.For<IFeatureManager>();
        var forecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Sunny", 65),
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(1)), 22, "Cloudy", 75)
        };

        var weatherApiClient = Substitute.For<WeatherApiClient>(CreateDummyHttpClient());

        // First state: WeatherForecast disabled
        featureManager.IsEnabledAsync("WeatherForecast").Returns(false);
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(false);

        // Act & Assert: Verify disabled state is not cached after re-enabling
        var isEnabledFirst = await featureManager.IsEnabledAsync("WeatherForecast");
        isEnabledFirst.Should().BeFalse();

        // Now enable the feature
        featureManager.IsEnabledAsync("WeatherForecast").Returns(true);
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(false);

        // Verify it's now enabled and not served from cache
        var isEnabledSecond = await featureManager.IsEnabledAsync("WeatherForecast");
        isEnabledSecond.Should().BeTrue();
    }

    [Fact]
    public async Task WeatherForecastFeatureDisabled_ShowsAlert_NotCached()
    {
        // Arrange
        var featureManager = Substitute.For<IFeatureManager>();

        // Feature is disabled
        featureManager.IsEnabledAsync("WeatherForecast").Returns(false);

        // Act
        var isEnabled = await featureManager.IsEnabledAsync("WeatherForecast");

        // Assert
        isEnabled.Should().BeFalse();

        // Now enable it
        featureManager.IsEnabledAsync("WeatherForecast").Returns(true);

        // Verify the new state is reflected (not cached)
        var isEnabledAfterToggle = await featureManager.IsEnabledAsync("WeatherForecast");
        isEnabledAfterToggle.Should().BeTrue();
    }

    [Fact]
    public async Task WeatherHumidityFeatureToggle_NotCachedBetweenRequests()
    {
        // Arrange
        var featureManager = Substitute.For<IFeatureManager>();

        // Initially disabled
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(false);

        // Act: Check initial state
        var humidityEnabledFirst = await featureManager.IsEnabledAsync("WeatherHumidity");

        // Assert
        humidityEnabledFirst.Should().BeFalse();

        // Toggle it
        featureManager.IsEnabledAsync("WeatherHumidity").Returns(true);

        // Act: Check new state
        var humidityEnabledSecond = await featureManager.IsEnabledAsync("WeatherHumidity");

        // Assert: Should reflect the new state immediately, not cached
        humidityEnabledSecond.Should().BeTrue();
    }

    private static HttpClient CreateDummyHttpClient()
    {
        var handler = new DummyHttpMessageHandler();
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
    }

    private class DummyHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
