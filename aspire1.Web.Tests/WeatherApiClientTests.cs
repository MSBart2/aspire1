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
    public async Task GetWeatherAsync_SuccessfulResponse_ReturnsForecastsAndDiagnostics()
    {
        // Arrange
        var forecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Sunny", 65),
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(1)), 22, "Cloudy", 75)
        };
        var diagnostics = new WeatherDiagnostics("hit", "Redis cache", DateTimeOffset.UtcNow, ["weather.api.calls", "cache.hits"]);

        var httpClient = CreateEnvelopeHttpClient(new WeatherForecastResponse(forecasts, diagnostics));
        var client = new WeatherApiClient(httpClient, _mockLogger);

        // Act
        var result = await client.GetWeatherAsync();

        // Assert
        result.Forecasts.Should().HaveCount(2);
        result.Forecasts[0].TemperatureC.Should().Be(20);
        result.Forecasts[0].Summary.Should().Be("Sunny");
        result.Forecasts[1].TemperatureC.Should().Be(22);
        result.Forecasts[1].Summary.Should().Be("Cloudy");
        result.Diagnostics.Should().NotBeNull();
        result.Diagnostics!.Source.Should().Be("Redis cache");
        result.Diagnostics.CacheStatus.Should().Be("hit");
    }

    [Fact]
    public async Task GetWeatherAsync_WithMaxItems_TrimsForecastsAtLimit()
    {
        // Arrange
        var forecasts = Enumerable.Range(0, 100)
            .Select(i => new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
                20 + i % 30,
                $"Summary{i}",
                50 + i % 50))
            .ToArray();

        var httpClient = CreateEnvelopeHttpClient(new WeatherForecastResponse(forecasts, null));
        var client = new WeatherApiClient(httpClient, _mockLogger);

        // Act
        var result = await client.GetWeatherAsync(maxItems: 5);

        // Assert
        result.Forecasts.Should().HaveCount(5);
        result.Forecasts.Should().Equal(forecasts.Take(5));
    }

    [Fact]
    public async Task GetWeatherAsync_EmptyResponse_ReturnsEmptyEnvelope()
    {
        // Arrange
        var httpClient = CreateEnvelopeHttpClient(new WeatherForecastResponse([], null));
        var client = new WeatherApiClient(httpClient, _mockLogger);

        // Act
        var result = await client.GetWeatherAsync();

        // Assert
        result.Forecasts.Should().BeEmpty();
        result.Diagnostics.Should().BeNull();
    }

    [Fact]
    public async Task GetWeatherAsync_HttpError500_ReturnsEmptyEnvelope()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError, "");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new WeatherApiClient(httpClient, _mockLogger);

        // Act
        var result = await client.GetWeatherAsync();

        // Assert
        result.Forecasts.Should().BeEmpty();
        result.Diagnostics.Should().BeNull();
    }

    [Fact]
    public async Task GetWeatherAsync_ServiceUnavailable503_ReturnsEmptyGracefully()
    {
        // Arrange — API-side feature flag disabled should degrade gracefully for the UI
        var handler = new MockHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new WeatherApiClient(httpClient, _mockLogger);

        // Act
        var result = await client.GetWeatherAsync();

        // Assert
        result.Forecasts.Should().BeEmpty();
        result.Diagnostics.Should().BeNull();
    }

    [Fact]
    public async Task GetWeatherAsync_InvalidJson_ReturnsEmptyEnvelope()
    {
        // Arrange
        var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, "{ definitely-not-json"))
        {
            BaseAddress = new Uri("http://localhost")
        };
        var client = new WeatherApiClient(httpClient, _mockLogger);

        // Act
        var result = await client.GetWeatherAsync();

        // Assert
        result.Forecasts.Should().BeEmpty();
        result.Diagnostics.Should().BeNull();
    }

    [Fact]
    public async Task GetWeatherAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var forecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Sunny", 65)
        };

        var httpClient = CreateEnvelopeHttpClient(new WeatherForecastResponse(forecasts, null));
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

        var httpClient = CreateEnvelopeHttpClient(new WeatherForecastResponse(forecasts, null));
        var client = new WeatherApiClient(httpClient, _mockLogger);

        // Act
        var result = await client.GetWeatherAsync(maxItems: maxItems);

        // Assert
        result.Forecasts.Should().HaveCount(Math.Min(maxItems, forecasts.Length));
    }

    [Fact]
    public async Task GetWeatherAsync_InvalidMaxItems_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, "{}"))
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

    private static HttpClient CreateEnvelopeHttpClient(WeatherForecastResponse response)
    {
        var json = JsonSerializer.Serialize(response);
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
}
