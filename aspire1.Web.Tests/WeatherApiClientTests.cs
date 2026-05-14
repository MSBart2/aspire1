using aspire1.Contracts;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace aspire1.Web.Tests;

public class WeatherApiClientTests
{
    private readonly ILogger<WeatherApiClient> _mockLogger = LoggerFactory
        .Create(builder => builder.AddConsole())
        .CreateLogger<WeatherApiClient>();

    [Fact]
    public async Task GetWeatherAsync_SuccessfulResponse_ReturnsForecasts()
    {
        // Arrange
        var forecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Sunny", 65),
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(1)), 22, "Cloudy", 75)
        };

        var httpClient = CreateStreamingHttpClient(forecasts);
        var client = new WeatherApiClient(httpClient, _mockLogger);

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
    public async Task GetWeatherAsync_WithMaxItems_StopsStreamingAtLimit()
    {
        // Arrange
        var forecasts = Enumerable.Range(0, 100)
            .Select(i => new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
                20 + i % 30,
                $"Summary{i}",
                50 + i % 50))
            .ToArray();

        var httpClient = CreateStreamingHttpClient(forecasts);
        var client = new WeatherApiClient(httpClient, _mockLogger);

        // Act
        var result = await client.GetWeatherAsync(maxItems: 5);

        // Assert — streaming should stop at 5, not load entire 100-item array into memory
        result.Should().HaveCount(5);
        result.Should().Equal(forecasts.Take(5));
    }

    [Fact]
    public async Task GetWeatherAsync_EmptyResponse_ReturnsEmptyArray()
    {
        // Arrange
        var httpClient = CreateStreamingHttpClient(Array.Empty<WeatherForecast>());
        var client = new WeatherApiClient(httpClient, _mockLogger);

        // Act
        var result = await client.GetWeatherAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWeatherAsync_HttpError500_ReturnsEmpty()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError, "");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new WeatherApiClient(httpClient, _mockLogger);

        // Act
        var result = await client.GetWeatherAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWeatherAsync_ServiceUnavailable503_ReturnsEmptyGracefully()
    {
        // Arrange — this is the exact scenario from issue #11: feature flag disabled on API returns 503
        var handler = new MockHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new WeatherApiClient(httpClient, _mockLogger);

        // Act
        var result = await client.GetWeatherAsync();

        // Assert — should not throw, should return empty for UI to handle gracefully
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWeatherAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var forecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Sunny", 65)
        };

        var httpClient = CreateStreamingHttpClient(forecasts);
        var client = new WeatherApiClient(httpClient, _mockLogger);
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

        var httpClient = CreateStreamingHttpClient(forecasts);
        var client = new WeatherApiClient(httpClient, _mockLogger);

        // Act
        var result = await client.GetWeatherAsync(maxItems: maxItems);

        // Assert
        result.Should().HaveCount(Math.Min(maxItems, forecasts.Length));
    }

    [Fact]
    public async Task GetWeatherAsync_InvalidMaxItems_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, "[]"))
        {
            BaseAddress = new Uri("http://localhost")
        };
        var client = new WeatherApiClient(httpClient, _mockLogger);

        // Act & Assert — maxItems must be between 1 and 1000
        var actZero = async () => await client.GetWeatherAsync(maxItems: 0);
        var actNegative = async () => await client.GetWeatherAsync(maxItems: -5);
        var actTooLarge = async () => await client.GetWeatherAsync(maxItems: 1001);

        await actZero.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await actNegative.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await actTooLarge.Should().ThrowAsync<ArgumentOutOfRangeException>();
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

    [Theory]
    [InlineData(0, 32)]       // Freezing point
    [InlineData(20, 68)]      // Room temperature
    [InlineData(-20, -4)]     // Cold snap (the canary in the coal mine)
    [InlineData(100, 212)]    // Boiling point
    [InlineData(-40, -40)]    // Same in both scales
    public void WeatherForecast_TemperatureF_VariousTemperatures_CalculatesCorrectly(int temperatureC, int expectedF)
    {
        // Arrange
        var forecast = new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), temperatureC, "Test", 50);

        // Act
        var temperatureF = forecast.TemperatureF;

        // Assert
        temperatureF.Should().Be(expectedF);
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

    private static HttpClient CreateStreamingHttpClient(WeatherForecast[] forecasts)
    {
        var json = JsonSerializer.Serialize(forecasts);
        var handler = new MockStreamingHttpMessageHandler(json);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };
        return httpClient;
    }

    /// <summary>
    /// Mock HTTP handler that streams JSON array items for testing streaming pagination.
    /// </summary>
    private class MockStreamingHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _json;

        public MockStreamingHttpMessageHandler(string json)
        {
            _json = json;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Return streaming JSON as-is; the client reads it via response.Content.ReadFromJsonAsAsyncEnumerable
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, System.Text.Encoding.UTF8, "application/json")
            };

            return await Task.FromResult(response);
        }
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
}
