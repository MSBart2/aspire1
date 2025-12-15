using aspire1.WeatherService.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace aspire1.WeatherService.Tests.Services;

public class CachedWeatherServiceTests
{
    private readonly IDistributedCache _mockCache;
    private readonly ILogger<CachedWeatherService> _mockLogger;
    private readonly CachedWeatherService _sut;

    public CachedWeatherServiceTests()
    {
        _mockCache = Substitute.For<IDistributedCache>();
        _mockLogger = Substitute.For<ILogger<CachedWeatherService>>();
        _sut = new CachedWeatherService(_mockCache, _mockLogger);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_CacheHit_ReturnsCachedData()
    {
        // Arrange
        var expectedForecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Sunny", 65),
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(1)), 25, "Hot", 45)
        };
        var cachedJson = JsonSerializer.Serialize(expectedForecasts);
        var cachedBytes = Encoding.UTF8.GetBytes(cachedJson);

        _mockCache.GetAsync("api:weather:forecast:5", Arg.Any<CancellationToken>())
            .Returns(cachedBytes);

        // Act
        var result = await _sut.GetWeatherForecastAsync(5);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Summary.Should().Be("Sunny");
        result[1].Summary.Should().Be("Hot");

        await _mockCache.Received(1).GetAsync("api:weather:forecast:5", Arg.Any<CancellationToken>());
        await _mockCache.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetWeatherForecastAsync_CacheMiss_GeneratesAndCachesData()
    {
        // Arrange
        _mockCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        // Act
        var result = await _sut.GetWeatherForecastAsync(10);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(10);
        result.Should().OnlyContain(f => f.Date > DateOnly.FromDateTime(DateTime.Now));

        await _mockCache.Received(1).GetAsync("api:weather:forecast:10", Arg.Any<CancellationToken>());
        await _mockCache.Received(1).SetAsync(
            "api:weather:forecast:10",
            Arg.Any<byte[]>(),
            Arg.Is<DistributedCacheEntryOptions>(opts => opts.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(5)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetWeatherForecastAsync_CacheReadFails_GeneratesData()
    {
        // Arrange
        _mockCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<byte[]?>(new InvalidOperationException("Redis unavailable")));

        // Act
        var result = await _sut.GetWeatherForecastAsync(5);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_CacheWriteFails_ReturnsData()
    {
        // Arrange
        _mockCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);
        _mockCache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Redis unavailable")));

        // Act
        var result = await _sut.GetWeatherForecastAsync(3);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task GetWeatherForecastAsync_GeneratesCorrectCount(int count)
    {
        // Arrange
        _mockCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        // Act
        var result = await _sut.GetWeatherForecastAsync(count);

        // Assert
        result.Should().HaveCount(count);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_GeneratesValidTemperatures()
    {
        // Arrange
        _mockCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        // Act
        var result = await _sut.GetWeatherForecastAsync(10);

        // Assert
        result.Should().OnlyContain(f => f.TemperatureC >= -20 && f.TemperatureC < 55);
        result.Should().OnlyContain(f => f.TemperatureF == 32 + (int)(f.TemperatureC / 0.5556));
    }

    [Fact]
    public async Task GetWeatherForecastAsync_GeneratesValidSummaries()
    {
        // Arrange
        var validSummaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
        _mockCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        // Act
        var result = await _sut.GetWeatherForecastAsync(20);

        // Assert
        result.Should().OnlyContain(f => validSummaries.Contains(f.Summary));
    }

    [Fact]
    public async Task GetWeatherForecastAsync_GeneratesValidHumidity()
    {
        // Arrange
        _mockCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        // Act
        var result = await _sut.GetWeatherForecastAsync(10);

        // Assert
        result.Should().OnlyContain(f => f.Humidity >= 20 && f.Humidity < 95);
    }
}
