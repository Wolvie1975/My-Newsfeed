using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace myNewspaper.Shared.Data;

public sealed record LocalForecast(string Name, int Temperature, string Unit, string Summary, string Wind, DateTimeOffset Starts, bool IsDaytime = true);
public sealed record LocalWeatherPage(IReadOnlyList<LocalForecast> Periods, DateTimeOffset? Updated, bool Stale, string Location = "Kansas City", double Latitude = 39.0997, double Longitude = -94.5786);
public sealed record WeatherAlert(string Event, string Headline, string Severity, DateTimeOffset? Ends);

public sealed class LocalWeather(IHttpClientFactory clients, IMemoryCache cache, TimeProvider clock, ILogger<LocalWeather> logger)
    : IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public void Dispose() => gate.Dispose();
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
        try
        {
            if (cache.TryGetValue<LocalWeatherPage>(key, out fresh)) return fresh!;
            try
            {
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
                var nowUtc = clock.GetUtcNow();
                var periods = properties.GetProperty("periods").EnumerateArray().Select(p => new LocalForecast(
                    p.GetProperty("name").GetString() ?? "Forecast", p.GetProperty("temperature").GetInt32(),
                    p.GetProperty("temperatureUnit").GetString() ?? "F", p.GetProperty("shortForecast").GetString() ?? "",
                    (p.GetProperty("windSpeed").GetString() ?? "") + " " + (p.GetProperty("windDirection").GetString() ?? ""),
                    p.GetProperty("startTime").GetDateTimeOffset(),
                    !p.TryGetProperty("isDaytime", out var daytime) || daytime.ValueKind != JsonValueKind.False)).Where(p => p.Starts >= nowUtc.AddHours(-12)).Take(4).ToList();
                var updated = properties.TryGetProperty("updated", out var stamp) && stamp.TryGetDateTimeOffset(out var date) ? date : nowUtc;
                var result = new LocalWeatherPage(periods, updated, updated < nowUtc.AddHours(-6), location, lat, lon);
                cache.Set(key + "-last", result, TimeSpan.FromHours(6));
                cache.Set(key, result, TimeSpan.FromMinutes(15));
                return result;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or KeyNotFoundException || ex is OperationCanceledException && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Weather forecast unavailable");
                var result = cache.TryGetValue<LocalWeatherPage>(key + "-last", out var last)
                    ? last! with { Stale = true } : new LocalWeatherPage([], null, true, location, lat, lon);
                cache.Set(key, result, TimeSpan.FromMinutes(2));
                return result;
            }
        }
        finally { gate.Release(); }
    }

    /// <summary>Active NWS alerts for the forecast point; empty on any failure. Cached 5 minutes.</summary>
    public async Task<IReadOnlyList<WeatherAlert>> GetAlertsAsync(CancellationToken cancellationToken, double? latitude = null, double? longitude = null)
    {
        var (lat, lon) = Coordinates(latitude, longitude);
        var custom = latitude is not null;
        var key = custom ? FormattableString.Invariant($"alerts:{lat:F2}:{lon:F2}") : "kc-alerts";
        if (cache.TryGetValue<IReadOnlyList<WeatherAlert>>(key, out var cached) && cached is not null)
            return cached;

        try
        {
            var http = clients.CreateClient("local-weather");
            var pointUrl = "https://api.weather.gov/alerts/active?point=" + lat.ToString(CultureInfo.InvariantCulture) + "," + lon.ToString(CultureInfo.InvariantCulture);
            if (!Uri.TryCreate(pointUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "api.weather.gov")
                return [];

            using var response = await http.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return [];

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = doc.RootElement;
            if (!root.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array)
                return [];

            var list = new List<WeatherAlert>();
            foreach (var feature in features.EnumerateArray())
            {
                if (!feature.TryGetProperty("properties", out var props)) continue;
                var evt = props.TryGetProperty("event", out var e) ? e.GetString() ?? "" : "";
                var headline = props.TryGetProperty("headline", out var h) ? h.GetString() ?? "" : "";
                var severity = props.TryGetProperty("severity", out var s) ? s.GetString() ?? "" : "";
                DateTimeOffset? ends = null;
                if (props.TryGetProperty("ends", out var endEl) && endEl.TryGetDateTimeOffset(out var endDate))
                    ends = endDate;
                else if (props.TryGetProperty("expires", out var expEl) && expEl.TryGetDateTimeOffset(out var expDate))
                    ends = expDate;

                list.Add(new WeatherAlert(evt, headline, severity, ends));
            }

            IReadOnlyList<WeatherAlert> result = list;
            cache.Set(key, result, TimeSpan.FromMinutes(5));
            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or KeyNotFoundException || ex is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Weather alerts unavailable");
            return [];
        }
    }
}
