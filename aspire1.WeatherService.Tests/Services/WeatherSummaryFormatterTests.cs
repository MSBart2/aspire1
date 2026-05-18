using aspire1.WeatherService.Services;

namespace aspire1.WeatherService.Tests.Services;

public class WeatherSummaryFormatterTests
{
    [Theory]
    [InlineData(-20, "Freezing")]
    [InlineData(-1,  "Freezing")]
    [InlineData(0,   "Cold")]
    [InlineData(8,   "Cold")]
    [InlineData(15,  "Cold")]
    [InlineData(16,  "Mild")]
    [InlineData(20,  "Mild")]
    [InlineData(25,  "Mild")]
    [InlineData(26,  "Warm")]
    [InlineData(35,  "Warm")]
    [InlineData(40,  "Warm")]
    [InlineData(41,  "Hot")]
    [InlineData(54,  "Hot")]
    public void Format_TemperatureOnly_ReturnsCorrectLabel(int temperatureC, string expectedLabel)
    {
        var result = WeatherSummaryFormatter.Format(temperatureC);

        result.Should().Be(expectedLabel);
    }

    [Theory]
    [InlineData(-5,  80, "Freezing – humidity 80%")]
    [InlineData(10,  60, "Cold – humidity 60%")]
    [InlineData(20,  45, "Mild – humidity 45%")]
    [InlineData(30,  70, "Warm – humidity 70%")]
    [InlineData(50,  30, "Hot – humidity 30%")]
    public void Format_WithHumidity_IncludesHumidityText(int temperatureC, int humidity, string expectedSummary)
    {
        var result = WeatherSummaryFormatter.Format(temperatureC, humidity);

        result.Should().Be(expectedSummary);
    }

    [Fact]
    public void Format_WithNullHumidity_OmitsHumidityText()
    {
        var result = WeatherSummaryFormatter.Format(20, null);

        result.Should().Be("Mild");
        result.Should().NotContain("humidity");
    }

    [Fact]
    public void Format_WithZeroHumidity_OmitsHumidityText()
    {
        var result = WeatherSummaryFormatter.Format(20, 0);

        result.Should().Be("Mild");
        result.Should().NotContain("humidity");
    }

    [Theory]
    [InlineData(-1,  "Freezing")]
    [InlineData(15,  "Cold")]
    [InlineData(25,  "Mild")]
    [InlineData(40,  "Warm")]
    [InlineData(41,  "Hot")]
    public void Format_BoundaryTemperatures_ProduceCorrectLabel(int temperatureC, string expectedLabel)
    {
        var result = WeatherSummaryFormatter.Format(temperatureC);

        result.Should().StartWith(expectedLabel);
    }
}
