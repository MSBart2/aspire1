using aspire1.WeatherService.Services;

namespace aspire1.WeatherService.Tests.Services;

public class WeatherSummaryFormatterTests
{
    [Theory]
    [InlineData(-20, "Freezing")]
    [InlineData(-1, "Freezing")]
    public void GetSummary_FreezingTemperature_ReturnsFreezing(int temperatureC, string expected)
    {
        var result = WeatherSummaryFormatter.GetSummary(temperatureC);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, "Cold")]
    [InlineData(15, "Cold")]
    public void GetSummary_ColdTemperature_ReturnsCold(int temperatureC, string expected)
    {
        var result = WeatherSummaryFormatter.GetSummary(temperatureC);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(16, "Mild")]
    [InlineData(25, "Mild")]
    public void GetSummary_MildTemperature_ReturnsMild(int temperatureC, string expected)
    {
        var result = WeatherSummaryFormatter.GetSummary(temperatureC);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(26, "Warm")]
    [InlineData(35, "Warm")]
    public void GetSummary_WarmTemperature_ReturnsWarm(int temperatureC, string expected)
    {
        var result = WeatherSummaryFormatter.GetSummary(temperatureC);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(36, "Hot")]
    [InlineData(54, "Hot")]
    public void GetSummary_HotTemperature_ReturnsHot(int temperatureC, string expected)
    {
        var result = WeatherSummaryFormatter.GetSummary(temperatureC);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(20, "dry")]
    [InlineData(29, "dry")]
    public void GetHumidityDescription_DryHumidity_ReturnsDry(int humidity, string expected)
    {
        var result = WeatherSummaryFormatter.GetHumidityDescription(humidity);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(30, "comfortable")]
    [InlineData(60, "comfortable")]
    public void GetHumidityDescription_ComfortableHumidity_ReturnsComfortable(int humidity, string expected)
    {
        var result = WeatherSummaryFormatter.GetHumidityDescription(humidity);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(61, "humid")]
    [InlineData(94, "humid")]
    public void GetHumidityDescription_HumidHumidity_ReturnsHumid(int humidity, string expected)
    {
        var result = WeatherSummaryFormatter.GetHumidityDescription(humidity);
        result.Should().Be(expected);
    }
}
