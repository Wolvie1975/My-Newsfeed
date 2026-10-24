using System.Text.Json;
using System.Globalization;
using Microsoft.Extensions.Caching.Memory;

namespace myNewspaper.Web.Data;

public sealed record LocalForecast(string Name, int Temperature, string Unit, string Summary, string Wind, DateTimeOffset Starts, bool IsDaytime = true);
public sealed record LocalWeatherPage(IReadOnlyList<LocalForecast> Periods, DateTimeOffset? Updated, bool Stale, string Location = "Kansas City", double Latitude = 39.0997, double Longitude = -94.5786);

public sealed class LocalWeather(IHttpClientFactory clients, IMemoryCache cache, ILogger<LocalWeather> logger)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public static (double Latitude, double Longitude) Coordinates(double? latitude, double? longitude)
    {
        if (latitude is null && longitude is null) return (39.0997, -94.5786);
        if (latitude is null || longitude is null || !double.IsFinite(latitude.Value) || !double.IsFinite(longitude.Value)
            || latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            throw new ArgumentException("Valid latitude and longitude are required.");
        return (Math.Round(latitude.Value, 2, MidpointRounding.AwayFromZero), Math.Round(longitude.Value, 2, MidpointRounding.AwayFromZero));
    }
    public async Task<LocalWeatherPage> GetAsync(CancellationToken cancellationToken, double? latitude = null, double? longitude = null)
    {
        var (lat, lon) = Coordinates(latitude, longitude);
        var custom = latitude is not null;
        var key = custom ? FormattableString.Invariant($"weather:{lat:F2}:{lon:F2}") : "kc-weather";
        var location = custom ? "Your location" : "Kansas City";
        if (cache.TryGetValue<LocalWeatherPage>(key, out var fresh)) return fresh!;
        await gate.WaitAsync(cancellationToken);
        try {
            if (cache.TryGetValue<LocalWeatherPage>(key, out fresh)) return fresh!;
            try {
                var http = clients.CreateClient("local-weather");
                using var point = await http.GetAsync("https://api.weather.gov/points/" + lat.ToString(CultureInfo.InvariantCulture) + "," + lon.ToString(CultureInfo.InvariantCulture), cancellationToken);
                point.EnsureSuccessStatusCode();
                using var pointData = JsonDocument.Parse(await point.Content.ReadAsStringAsync(cancellationToken));
                var pointProperties = pointData.RootElement.GetProperty("properties");
                if (custom && pointProperties.TryGetProperty("relativeLocation", out var relative)
                    && relative.TryGetProperty("properties", out var place))
                {
                    var city = place.TryGetProperty("city", out var c) ? c.GetString() : null;
                    var state = place.TryGetProperty("state", out var st) ? st.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(city)) location = string.Join(", ", new[] { city, state }.Where(v => !string.IsNullOrWhiteSpace(v)));
                }
                var url = pointProperties.GetProperty("forecast").GetString();
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "api.weather.gov")
                    throw new JsonException("Unexpected forecast address.");
                using var forecast = await http.GetAsync(uri, cancellationToken);
                forecast.EnsureSuccessStatusCode();
                using var data = JsonDocument.Parse(await forecast.Content.ReadAsStringAsync(cancellationToken));
                var properties = data.RootElement.GetProperty("properties");
                var periods = properties.GetProperty("periods").EnumerateArray().Select(p => new LocalForecast(
                    p.GetProperty("name").GetString() ?? "Forecast", p.GetProperty("temperature").GetInt32(),
                    p.GetProperty("temperatureUnit").GetString() ?? "F", p.GetProperty("shortForecast").GetString() ?? "",
                    (p.GetProperty("windSpeed").GetString() ?? "") + " " + (p.GetProperty("windDirection").GetString() ?? ""),
                    p.GetProperty("startTime").GetDateTimeOffset(),
                    !p.TryGetProperty("isDaytime", out var daytime) || daytime.ValueKind != JsonValueKind.False)).Where(p => p.Starts >= DateTimeOffset.UtcNow.AddHours(-12)).Take(4).ToList();
                var updated = properties.TryGetProperty("updated", out var stamp) && stamp.TryGetDateTimeOffset(out var date) ? date : DateTimeOffset.UtcNow;
                var result = new LocalWeatherPage(periods, updated, updated < DateTimeOffset.UtcNow.AddHours(-6), location, lat, lon);
                cache.Set(key + "-last", result, TimeSpan.FromHours(6));
                cache.Set(key, result, TimeSpan.FromMinutes(15));
                return result;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or KeyNotFoundException || ex is OperationCanceledException && !cancellationToken.IsCancellationRequested) {
                logger.LogWarning(ex, "Weather forecast unavailable");
                var result = cache.TryGetValue<LocalWeatherPage>(key + "-last", out var last)
                    ? last! with { Stale = true } : new LocalWeatherPage([], null, true, location, lat, lon);
                cache.Set(key, result, TimeSpan.FromMinutes(2));
                return result;
            }
        }
        finally { gate.Release(); }
    }
}
