using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Home.Api.Dtos;

namespace Home.Api.Services;

public class WeatherService(IHttpClientFactory httpClientFactory, ILogger<WeatherService> logger)
{
    private static readonly ConcurrentDictionary<string, (DateTime Expires, WeatherResponse Value)> Cache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public async Task<WeatherResponse> GetAsync(
        string? city,
        double? latitude,
        double? longitude,
        CancellationToken ct = default)
    {
        var cacheKey = BuildCacheKey(city, latitude, longitude);
        if (Cache.TryGetValue(cacheKey, out var cached) && cached.Expires > DateTime.UtcNow)
            return cached.Value;

        var resolved = await ResolveLocationAsync(city, latitude, longitude, ct);

        WeatherResponse? result = null;
        Exception? lastError = null;

        try
        {
            result = await FetchOpenMeteoAsync(resolved, ct);
        }
        catch (Exception ex)
        {
            lastError = ex;
            logger.LogWarning(ex, "Open-Meteo forecast failed for {City}", resolved.City);
        }

        if (result is null)
        {
            try
            {
                result = await FetchWttrAsync(resolved, ct);
            }
            catch (Exception ex)
            {
                lastError = ex;
                logger.LogWarning(ex, "wttr.in forecast failed for {City}", resolved.City);
            }
        }

        if (result is null)
            throw new InvalidOperationException(
                lastError?.Message ?? "Unable to load weather from any provider");

        Cache[cacheKey] = (DateTime.UtcNow.Add(CacheTtl), result);
        return result;
    }

    private async Task<ResolvedLocation> ResolveLocationAsync(
        string? city,
        double? latitude,
        double? longitude,
        CancellationToken ct)
    {
        if (latitude is not null && longitude is not null)
        {
            return new ResolvedLocation(
                latitude.Value,
                longitude.Value,
                string.IsNullOrWhiteSpace(city) ? "Custom" : city.Trim());
        }

        var queryCity = string.IsNullOrWhiteSpace(city) ? "Moscow" : city.Trim();

        try
        {
            var client = httpClientFactory.CreateClient("weather");
            var url =
                $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(queryCity)}&count=1&language=en&format=json";
            using var res = await client.GetAsync(url, ct);
            res.EnsureSuccessStatusCode();
            await using var stream = await res.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var hit = doc.RootElement.GetProperty("results").EnumerateArray().FirstOrDefault();
            if (hit.ValueKind == JsonValueKind.Object)
            {
                return new ResolvedLocation(
                    hit.GetProperty("latitude").GetDouble(),
                    hit.GetProperty("longitude").GetDouble(),
                    hit.GetProperty("name").GetString() ?? queryCity);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Geocoding failed for {City}, falling back to city name only", queryCity);
        }

        // wttr.in can resolve city names without coordinates.
        return new ResolvedLocation(null, null, queryCity);
    }

    private async Task<WeatherResponse> FetchOpenMeteoAsync(ResolvedLocation location, CancellationToken ct)
    {
        if (location.Latitude is null || location.Longitude is null)
            throw new InvalidOperationException("Coordinates required for Open-Meteo");

        var client = httpClientFactory.CreateClient("weather");
        var url =
            $"https://api.open-meteo.com/v1/forecast?latitude={location.Latitude.Value.ToString(CultureInfo.InvariantCulture)}&longitude={location.Longitude.Value.ToString(CultureInfo.InvariantCulture)}&current=temperature_2m,weather_code,wind_speed_10m";

        using var res = await client.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();
        await using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var current = doc.RootElement.GetProperty("current");
        var code = current.GetProperty("weather_code").GetInt32();

        return new WeatherResponse(
            location.City,
            current.GetProperty("temperature_2m").GetDouble(),
            current.GetProperty("wind_speed_10m").GetDouble(),
            code,
            DescribeWmo(code),
            "open-meteo");
    }

    private async Task<WeatherResponse> FetchWttrAsync(ResolvedLocation location, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("weather");
        var target = location.Latitude is not null && location.Longitude is not null
            ? $"{location.Latitude.Value.ToString(CultureInfo.InvariantCulture)},{location.Longitude.Value.ToString(CultureInfo.InvariantCulture)}"
            : location.City;

        var url = $"https://wttr.in/{Uri.EscapeDataString(target)}?format=j1";
        using var res = await client.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();
        await using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var current = doc.RootElement.GetProperty("current_condition")[0];
        var temp = ParseDouble(current, "temp_C");
        var wind = ParseDouble(current, "windspeedKmph");
        var condition = current.TryGetProperty("weatherDesc", out var descArr)
            && descArr.GetArrayLength() > 0
                ? descArr[0].GetProperty("value").GetString() ?? "Weather"
                : "Weather";

        int? code = null;
        if (current.TryGetProperty("weatherCode", out var codeEl)
            && int.TryParse(codeEl.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            code = parsed;
        }

        var city = location.City;
        if (doc.RootElement.TryGetProperty("nearest_area", out var areas) && areas.GetArrayLength() > 0)
        {
            var area = areas[0];
            if (area.TryGetProperty("areaName", out var names) && names.GetArrayLength() > 0)
                city = names[0].GetProperty("value").GetString() ?? city;
        }

        return new WeatherResponse(city, temp, wind, code, condition, "wttr.in");
    }

    private static double ParseDouble(JsonElement el, string name)
    {
        var raw = el.GetProperty(name).GetString();
        return double.Parse(raw ?? "0", CultureInfo.InvariantCulture);
    }

    private static string BuildCacheKey(string? city, double? lat, double? lon)
    {
        if (lat is not null && lon is not null)
            return $"coord:{lat.Value:F3},{lon.Value:F3}";
        return $"city:{(city ?? "Moscow").Trim().ToLowerInvariant()}";
    }

    private static string DescribeWmo(int code) => code switch
    {
        0 => "Clear",
        1 => "Mainly clear",
        2 => "Partly cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        61 or 63 or 65 => "Rain",
        71 or 73 or 75 => "Snow",
        80 or 81 or 82 => "Showers",
        95 or 96 or 99 => "Thunderstorm",
        _ => "Weather"
    };

    private sealed record ResolvedLocation(double? Latitude, double? Longitude, string City);
}
