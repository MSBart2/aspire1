namespace aspire1.Contracts;

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary, int Humidity)
{
    public int TemperatureF => (int)Math.Round(TemperatureC * 1.8 + 32);
}
