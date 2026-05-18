using aspire1.WeatherService.Services;

namespace aspire1.WeatherService.Tests.Services;

public class WeatherSummaryFormatterTests
{
    [Theory]
    [InlineData(-30, "Freezing")]
    [InlineData(-1,  "Freezing")]
    [InlineData(0,   "Cold")]
    [InlineData(15,  "Cold")]
    [InlineData(16,  "Mild")]
    [InlineData(25,  "Mild")]
    [InlineData(26,  "Warm")]
    [InlineData(40,  "Warm")]
    [InlineData(41,  "Hot")]
    [InlineData(60,  "Hot")]
    public void GetSummary_VariousTemperatures_ReturnsCorrectLabel(int temperatureC, string expectedSummary)
    {
        WeatherSummaryFormatter.GetSummary(temperatureC).Should().Be(expectedSummary);
    }

    [Fact]
    public void GetSummary_FullGeneratedRange_NeverReturnsNullOrEmpty()
    {
        for (var temp = -20; temp < 55; temp++)
        {
            WeatherSummaryFormatter.GetSummary(temp).Should().NotBeNullOrWhiteSpace(
                because: $"temperature {temp}°C should always produce a non-empty label");
        }
    }
}
