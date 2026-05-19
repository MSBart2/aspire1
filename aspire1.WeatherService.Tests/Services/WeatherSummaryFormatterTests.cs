using aspire1.WeatherService.Services;

namespace aspire1.WeatherService.Tests.Services;

public class WeatherSummaryFormatterTests
{
    // ── Temperature band mid-values (humidity=50, neutral) ──────────────────

    [Theory]
    [InlineData(-15, "Freezing")]
    [InlineData(-5,  "Bracing")]
    [InlineData(5,   "Chilly")]
    [InlineData(13,  "Cool")]
    [InlineData(19,  "Mild")]
    [InlineData(25,  "Warm")]
    [InlineData(30,  "Balmy")]
    [InlineData(35,  "Hot")]
    [InlineData(42,  "Sweltering")]
    [InlineData(50,  "Scorching")]
    public void GetSummary_MidBandValues_ReturnsExpectedBaseLabel(int temperatureC, string expectedLabel)
    {
        var result = WeatherSummaryFormatter.GetSummary(temperatureC, humidity: 50);
        result.Should().Be(expectedLabel);
    }

    // ── Band boundary thresholds ─────────────────────────────────────────────

    [Theory]
    [InlineData(-10, "Bracing")]
    [InlineData(0,   "Chilly")]
    [InlineData(10,  "Cool")]
    [InlineData(16,  "Mild")]
    [InlineData(22,  "Warm")]
    [InlineData(28,  "Balmy")]
    [InlineData(32,  "Hot")]
    [InlineData(38,  "Sweltering")]
    [InlineData(45,  "Scorching")]
    public void GetSummary_BoundaryThresholds_ReturnsExpectedBaseLabel(int temperatureC, string expectedLabel)
    {
        var result = WeatherSummaryFormatter.GetSummary(temperatureC, humidity: 50);
        result.Should().Be(expectedLabel);
    }

    // ── Humidity >= 70 → "and Humid" suffix ─────────────────────────────────

    [Fact]
    public void GetSummary_HumidityAtThreshold_AppendsAndHumid()
    {
        WeatherSummaryFormatter.GetSummary(20, humidity: 70)
            .Should().EndWith(" and Humid");
    }

    [Fact]
    public void GetSummary_HumidityAboveThreshold_AppendsAndHumid()
    {
        WeatherSummaryFormatter.GetSummary(20, humidity: 85)
            .Should().EndWith(" and Humid");
    }

    // ── Humidity < 30 → "and Dry" suffix ────────────────────────────────────

    [Fact]
    public void GetSummary_HumidityBelowDryThreshold_AppendsAndDry()
    {
        WeatherSummaryFormatter.GetSummary(20, humidity: 20)
            .Should().EndWith(" and Dry");
    }

    [Fact]
    public void GetSummary_HumidityJustBelowDryThreshold_AppendsAndDry()
    {
        WeatherSummaryFormatter.GetSummary(20, humidity: 29)
            .Should().EndWith(" and Dry");
    }

    // ── Neutral humidity (30–69) → no suffix ────────────────────────────────

    [Fact]
    public void GetSummary_NeutralHumidity_ReturnsBaseLabelOnly()
    {
        WeatherSummaryFormatter.GetSummary(20, humidity: 50)
            .Should().NotContain(" and ");
    }

    // ── Extreme combos ───────────────────────────────────────────────────────

    [Fact]
    public void GetSummary_FreezingAndHighHumidity_ReturnsFreezingAndHumid()
    {
        WeatherSummaryFormatter.GetSummary(-20, humidity: 85)
            .Should().Be("Freezing and Humid");
    }

    [Fact]
    public void GetSummary_ScorchingAndLowHumidity_ReturnsScorchingAndDry()
    {
        WeatherSummaryFormatter.GetSummary(50, humidity: 20)
            .Should().Be("Scorching and Dry");
    }
}
