namespace Home.Api.Dtos;

public record WeatherResponse(
    string City,
    double TemperatureC,
    double WindKmh,
    int? WeatherCode,
    string Condition,
    string Source);
