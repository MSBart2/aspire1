using aspire1.WeatherService.Services;

namespace aspire1.WeatherService.Tests.Services;

public class WeatherSummaryFormatterTests
{
    // ── GetTemperatureLabel ──────────────────────────────────────────────────

    [Theory]
    [InlineData(-20, "Freezing")]
    [InlineData(-1,  "Freezing")]
    public void GetTemperatureLabel_BelowZero_ReturnsFreezingLabel(int tempC, string expected)
    {
        WeatherSummaryFormatter.GetTemperatureLabel(tempC).Should().Be(expected);
    }

    [Theory]
    [InlineData(0,  "Cold")]
    [InlineData(8,  "Cold")]
    [InlineData(15, "Cold")]
    public void GetTemperatureLabel_ZeroToFifteen_ReturnsColdLabel(int tempC, string expected)
    {
        WeatherSummaryFormatter.GetTemperatureLabel(tempC).Should().Be(expected);
    }

    [Theory]
    [InlineData(16, "Mild")]
    [InlineData(20, "Mild")]
    [InlineData(25, "Mild")]
    public void GetTemperatureLabel_SixteenToTwentyFive_ReturnsMildLabel(int tempC, string expected)
    {
        WeatherSummaryFormatter.GetTemperatureLabel(tempC).Should().Be(expected);
    }

    [Theory]
    [InlineData(26, "Hot")]
    [InlineData(33, "Hot")]
    [InlineData(40, "Hot")]
    public void GetTemperatureLabel_TwentySixToForty_ReturnsHotLabel(int tempC, string expected)
    {
        WeatherSummaryFormatter.GetTemperatureLabel(tempC).Should().Be(expected);
    }

    [Theory]
    [InlineData(41,  "Scorching")]
    [InlineData(55,  "Scorching")]
    [InlineData(100, "Scorching")]
    public void GetTemperatureLabel_AboveForty_ReturnsScorchingLabel(int tempC, string expected)
    {
        WeatherSummaryFormatter.GetTemperatureLabel(tempC).Should().Be(expected);
    }

    // ── Band boundary values ─────────────────────────────────────────────────

    [Theory]
    [InlineData(-1, "Freezing")]
    [InlineData(0,  "Cold")]
    [InlineData(15, "Cold")]
    [InlineData(16, "Mild")]
    [InlineData(25, "Mild")]
    [InlineData(26, "Hot")]
    [InlineData(40, "Hot")]
    [InlineData(41, "Scorching")]
    public void GetTemperatureLabel_BoundaryValues_ReturnCorrectLabel(int tempC, string expected)
    {
        WeatherSummaryFormatter.GetTemperatureLabel(tempC).Should().Be(expected);
    }

    // ── FormatSummary — without humidity ─────────────────────────────────────

    [Theory]
    [InlineData(-5,  "Freezing")]
    [InlineData(10,  "Cold")]
    [InlineData(20,  "Mild")]
    [InlineData(30,  "Hot")]
    [InlineData(50,  "Scorching")]
    public void FormatSummary_NoHumidity_ReturnsLabelOnly(int tempC, string expected)
    {
        WeatherSummaryFormatter.FormatSummary(tempC).Should().Be(expected);
    }

    [Fact]
    public void FormatSummary_ZeroHumidity_ReturnsLabelOnly()
    {
        WeatherSummaryFormatter.FormatSummary(20, 0).Should().Be("Mild");
    }

    // ── FormatSummary — with humidity ────────────────────────────────────────

    [Theory]
    [InlineData(-5,  65, "Freezing. Humidity: 65%")]
    [InlineData(10,  40, "Cold. Humidity: 40%")]
    [InlineData(20,  55, "Mild. Humidity: 55%")]
    [InlineData(30,  80, "Hot. Humidity: 80%")]
    [InlineData(50,  30, "Scorching. Humidity: 30%")]
    public void FormatSummary_WithHumidity_ReturnsLabelAndHumidity(int tempC, int humidity, string expected)
    {
        WeatherSummaryFormatter.FormatSummary(tempC, humidity).Should().Be(expected);
    }
}
