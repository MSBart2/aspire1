using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace aspire1.Web.Tests;

public class WeatherApiClientTests
{
    [Fact]
    public async Task GetWeatherAsync_SuccessfulResponse_ReturnsForecasts()
    {
        // Arrange
        var forecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Sunny", 65),
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(1)), 22, "Cloudy", 75)
        };

        var httpClient = CreateHttpClientWithResponse(forecasts);
        var logger = Substitute.For<ILogger<WeatherApiClient>>();
        var client = new WeatherApiClient(httpClient, logger);

        // Act
        var result = await client.GetWeatherAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].TemperatureC.Should().Be(20);
        result[0].Summary.Should().Be("Sunny");
        result[1].TemperatureC.Should().Be(22);
        result[1].Summary.Should().Be("Cloudy");
    }

    [Fact]
    public async Task GetWeatherAsync_WithMaxItems_ReturnsLimitedForecasts()
    {
        // Arrange
        var forecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Sunny", 65),
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(1)), 22, "Cloudy", 75),
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(2)), 18, "Rainy", 85)
        };

        var httpClient = CreateHttpClientWithResponse(forecasts);
        var logger = Substitute.For<ILogger<WeatherApiClient>>();
        var client = new WeatherApiClient(httpClient, logger);

        // Act
        var result = await client.GetWeatherAsync(maxItems: 2);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetWeatherAsync_EmptyResponse_ReturnsEmptyArray()
    {
        // Arrange
        var httpClient = CreateHttpClientWithResponse(Array.Empty<WeatherForecast>());
        var logger = Substitute.For<ILogger<WeatherApiClient>>();
        var client = new WeatherApiClient(httpClient, logger);

        // Act
        var result = await client.GetWeatherAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWeatherAsync_ServiceUnavailableResponse_ReturnsEmptyArray()
    {
        // Arrange — 503 means the feature flag is disabled on the API side
        var handler = new MockHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var logger = Substitute.For<ILogger<WeatherApiClient>>();
        var client = new WeatherApiClient(httpClient, logger);

        // Act
        var result = await client.GetWeatherAsync();

        // Assert
        result.Should().BeEmpty("503 means feature is disabled — client should return [] not throw");
    }

    [Fact]
    public async Task GetWeatherAsync_InternalServerError_ReturnsEmptyArray()
    {
        // Arrange — other HTTP errors are caught gracefully
        var handler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError, "");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var logger = Substitute.For<ILogger<WeatherApiClient>>();
        var client = new WeatherApiClient(httpClient, logger);

        // Act
        var result = await client.GetWeatherAsync();

        // Assert
        result.Should().BeEmpty("HTTP errors should return empty array instead of throwing");
    }

    [Fact]
    public async Task GetWeatherAsync_HttpRequestException_ReturnsEmptyArray()
    {
        // Arrange — handler that throws to simulate network failure
        var handler = new ThrowingHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var logger = Substitute.For<ILogger<WeatherApiClient>>();
        var client = new WeatherApiClient(httpClient, logger);

        // Act
        var result = await client.GetWeatherAsync();

        // Assert
        result.Should().BeEmpty("network errors should return empty array instead of propagating");
    }

    [Fact]
    public async Task GetWeatherAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var forecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Sunny", 65)
        };

        var httpClient = CreateHttpClientWithResponse(forecasts);
        var logger = Substitute.For<ILogger<WeatherApiClient>>();
        var client = new WeatherApiClient(httpClient, logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await client.GetWeatherAsync(cancellationToken: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task GetWeatherAsync_VariousMaxItems_ReturnsCorrectCount(int maxItems)
    {
        // Arrange
        var forecasts = Enumerable.Range(0, 15)
            .Select(i => new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
                20 + i,
                $"Summary{i}",
                50 + i))
            .ToArray();

        var httpClient = CreateHttpClientWithResponse(forecasts);
        var logger = Substitute.For<ILogger<WeatherApiClient>>();
        var client = new WeatherApiClient(httpClient, logger);

        // Act
        var result = await client.GetWeatherAsync(maxItems: maxItems);

        // Assert
        result.Should().HaveCount(Math.Min(maxItems, forecasts.Length));
    }

    [Fact]
    public void WeatherForecast_TemperatureF_CalculatesCorrectly()
    {
        // Arrange
        var forecast = new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 0, "Test", 50);

        // Act
        var temperatureF = forecast.TemperatureF;

        // Assert
        temperatureF.Should().Be(32); // 0°C = 32°F
    }

    [Fact]
    public void WeatherForecast_Humidity_StoresCorrectly()
    {
        // Arrange
        var expectedHumidity = 75;
        var forecast = new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Sunny", expectedHumidity);

        // Act & Assert
        forecast.Humidity.Should().Be(expectedHumidity);
    }

    [Theory]
    [InlineData(20, 95)]
    [InlineData(50, 50)]
    [InlineData(85, 30)]
    public void WeatherForecast_VariousHumidityValues_StoresCorrectly(int temperature, int humidity)
    {
        // Arrange & Act
        var forecast = new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), temperature, "Test", humidity);

        // Assert
        forecast.Humidity.Should().Be(humidity);
        forecast.TemperatureC.Should().Be(temperature);
    }

    private static HttpClient CreateHttpClientWithResponse(WeatherForecast[] forecasts)
    {
        var json = JsonSerializer.Serialize(forecasts);
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, json);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };
        return httpClient;
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public MockHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, System.Text.Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }

    private class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("Simulated network failure");
    }
}
