using aspire1.WeatherService.Services;

namespace aspire1.WeatherService.Tests.Services;

public class WeatherSummaryFormatterTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Temperature band — representative values
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(-15, 50, "Freezing")]
    [InlineData(-5,  50, "Bracing")]
    [InlineData(5,   50, "Chilly")]
    [InlineData(14,  50, "Cool")]
    [InlineData(20,  50, "Mild")]
    [InlineData(26,  50, "Warm")]
    [InlineData(34,  50, "Hot")]
    [InlineData(42,  50, "Scorching")]
    public void GetSummary_TypicalTemperature_ReturnsExpectedBaseLabel(int temperatureC, int humidity, string expected)
    {
        var result = WeatherSummaryFormatter.GetSummary(temperatureC, humidity);
        result.Should().Be(expected);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Boundary values — exact band edges
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(-10, 50, "Bracing")]   // first value in Bracing band
    [InlineData(-11, 50, "Freezing")]  // last value in Freezing band
    [InlineData(0,   50, "Chilly")]    // first value in Chilly band
    [InlineData(-1,  50, "Bracing")]   // last value in Bracing band
    [InlineData(10,  50, "Cool")]      // first value in Cool band
    [InlineData(9,   50, "Chilly")]    // last value in Chilly band
    [InlineData(18,  50, "Mild")]      // first value in Mild band
    [InlineData(17,  50, "Cool")]      // last value in Cool band
    [InlineData(24,  50, "Warm")]      // first value in Warm band
    [InlineData(23,  50, "Mild")]      // last value in Mild band
    [InlineData(30,  50, "Hot")]       // first value in Hot band
    [InlineData(29,  50, "Warm")]      // last value in Warm band
    [InlineData(38,  50, "Scorching")] // first value in Scorching band
    [InlineData(37,  50, "Hot")]       // last value in Hot band
    public void GetSummary_BoundaryTemperature_ReturnsCorrectBandLabel(int temperatureC, int humidity, string expected)
    {
        var result = WeatherSummaryFormatter.GetSummary(temperatureC, humidity);
        result.Should().Be(expected);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Humidity suffix
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(20, 80)]   // exactly at threshold
    [InlineData(20, 94)]   // high end of generated range
    [InlineData(20, 100)]  // absolute max
    public void GetSummary_HighHumidity_AppendsAndHumidSuffix(int temperatureC, int humidity)
    {
        var result = WeatherSummaryFormatter.GetSummary(temperatureC, humidity);
        result.Should().EndWith(" and Humid");
    }

    [Theory]
    [InlineData(20, 30)]   // exactly at threshold
    [InlineData(20, 20)]   // low end of generated range
    [InlineData(20, 0)]    // absolute min
    public void GetSummary_LowHumidity_AppendsAndDrySuffix(int temperatureC, int humidity)
    {
        var result = WeatherSummaryFormatter.GetSummary(temperatureC, humidity);
        result.Should().EndWith(" and Dry");
    }

    [Theory]
    [InlineData(20, 31)]   // just above Dry threshold
    [InlineData(20, 50)]   // midpoint
    [InlineData(20, 79)]   // just below Humid threshold
    public void GetSummary_MidHumidity_ReturnsBaseLabelOnly(int temperatureC, int humidity)
    {
        var result = WeatherSummaryFormatter.GetSummary(temperatureC, humidity);
        result.Should().NotContain(" and ");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Combined temperature + humidity
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(35, 85, "Hot and Humid")]
    [InlineData(-5, 25, "Bracing and Dry")]
    [InlineData(20, 50, "Mild")]
    [InlineData(42, 20, "Scorching and Dry")]
    [InlineData(5,  90, "Chilly and Humid")]
    public void GetSummary_CombinedInputs_ReturnsExpectedFullSummary(int temperatureC, int humidity, string expected)
    {
        var result = WeatherSummaryFormatter.GetSummary(temperatureC, humidity);
        result.Should().Be(expected);
    }
}
