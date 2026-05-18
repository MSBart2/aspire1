using aspire1.WeatherService.Services;

namespace aspire1.WeatherService.Tests.Services;

public class WeatherSummaryFormatterTests
{
    [Theory]
    [InlineData(-10, "Freezing")]
    [InlineData(5, "Cold")]
    [InlineData(15, "Mild")]
    [InlineData(25, "Warm")]
    [InlineData(35, "Hot")]
    public void FormatSummary_TemperatureBands_ReturnsCorrectBand(int temperatureC, string expected)
    {
        var result = WeatherSummaryFormatter.FormatSummary(temperatureC);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(-1, "Freezing")]
    [InlineData(0, "Cold")]
    [InlineData(9, "Cold")]
    [InlineData(10, "Mild")]
    [InlineData(19, "Mild")]
    [InlineData(20, "Warm")]
    [InlineData(29, "Warm")]
    [InlineData(30, "Hot")]
    public void FormatSummary_BoundaryValues_ReturnsCorrectBand(int temperatureC, string expected)
    {
        var result = WeatherSummaryFormatter.FormatSummary(temperatureC);

        result.Should().Be(expected);
    }

    [Fact]
    public void FormatSummary_NoHumidity_ReturnsNoBandOnlySuffix()
    {
        var result = WeatherSummaryFormatter.FormatSummary(20, 0);

        result.Should().Be("Warm");
    }

    [Fact]
    public void FormatSummary_WithHumidity_ReturnsHumidSuffix()
    {
        var result = WeatherSummaryFormatter.FormatSummary(20, 75);

        result.Should().Be("Warm and humid");
    }

    [Fact]
    public void FormatSummary_FreezingWithHumidity_ReturnsFreezingAndHumid()
    {
        var result = WeatherSummaryFormatter.FormatSummary(-5, 60);

        result.Should().Be("Freezing and humid");
    }
}
