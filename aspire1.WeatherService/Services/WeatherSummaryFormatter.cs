namespace aspire1.WeatherService.Services;

public static class WeatherSummaryFormatter
{
    public static string GetSummary(int temperatureC) => temperatureC switch
    {
        < 0 => "Freezing",
        <= 15 => "Cold",
        <= 25 => "Mild",
        <= 35 => "Warm",
        _ => "Hot"
    };

    public static string GetHumidityDescription(int humidity) => humidity switch
    {
        < 30 => "dry",
        <= 60 => "comfortable",
        _ => "humid"
    };
}
