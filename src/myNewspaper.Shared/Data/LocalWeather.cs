using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace myNewspaper.Shared.Data;

public sealed record LocalForecast(string Name, int Temperature, string Unit, string Summary, string Wind, DateTimeOffset Starts, bool IsDaytime = true);
public sealed record LocalWeatherPage(IReadOnlyList<LocalForecast> Periods, DateTimeOffset? Updated, bool Stale, string Location = "Kansas City", double Latitude = 39.0997, double Longitude = -94.5786);
public sealed record WeatherAlert(string Event, string Headline, string Severity, DateTimeOffset? Ends);

public sealed record LocalHourlyPeriod(
    string Label,
    DateTimeOffset Starts,
    bool IsDaytime,
    int Temperature,
    string Unit,
    string Summary,
    int? RainChance,
    string Wind);

public sealed record LocalHourlyWeatherPage(
    string Location,
    string TimeZone,
    DateTimeOffset? Updated,
    bool Stale,
    string? RainSummary,
    IReadOnlyList<LocalHourlyPeriod> Hours);

/// <summary>
/// Home's forecast at a glance: the first period leads. A daytime lead is paired with the night after it, whose low and
/// conditions join the summary; the periods after those follow as a short list.
/// </summary>
public sealed record ForecastGlance(LocalForecast Lead, int? Low, string Summary, IReadOnlyList<LocalForecast> Next)
{
    public static ForecastGlance? From(IReadOnlyList<LocalForecast> periods, int next = 2)
    {
        if (periods.Count == 0) return null;
        var lead = periods[0];
        var night = lead.IsDaytime && periods.Count > 1 && !periods[1].IsDaytime ? periods[1] : null;
        var parts = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(lead.Summary)) parts.Add(Sentence(lead.Summary) + ".");
        if (night is not null && !string.IsNullOrWhiteSpace(night.Summary))
            parts.Add($"{night.Name}, {night.Summary.Trim().ToLowerInvariant()}.");
        return new ForecastGlance(lead, night?.Temperature, string.Join(' ', parts),
            periods.Skip(night is null ? 1 : 2).Take(next).ToList());
    }

    /// <summary>NWS short forecasts are title case ("Mostly Cloudy then Patchy Frost"); this reads as a sentence.</summary>
    public static string Sentence(string text)
    {
        var lower = text.Trim().ToLowerInvariant();
        return lower.Length == 0 ? lower : char.ToUpperInvariant(lower[0]) + lower[1..];
    }
}

/// <summary>
/// NWS forecasts and alerts, cached. Each location has at most one fetch in flight, bounded by the client's own timeout
/// rather than the caller's: a caller that stops waiting (Home allows 3 s) doesn't cancel it, so its result, forecast or
/// failure, is cached for the next request instead of every request waiting out the slow service again.
/// </summary>
public sealed class LocalWeather(IHttpClientFactory clients, IMemoryCache cache, TimeProvider clock, ILogger<LocalWeather> logger)
{
    private static readonly TimeSpan FailureBackoff = TimeSpan.FromMinutes(2);
    private readonly ConcurrentDictionary<string, Lazy<Task<LocalWeatherPage>>> forecastFetches = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<LocalHourlyWeatherPage>>> hourlyFetches = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<IReadOnlyList<WeatherAlert>>>> alertFetches = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<NwsPointInfo>>> pointFetches = new();

    private sealed record NwsPointInfo(string ForecastUrl, string ForecastHourlyUrl, string TimeZone, string Location);

    // Joins the fetch in flight for this key, or starts one; the caller's token only limits how long it waits.
    private static Task<T> SharedFetchAsync<T>(ConcurrentDictionary<string, Lazy<Task<T>>> fetches, string key,
        Func<Task<T>> fetch, CancellationToken cancellationToken)
    {
        async Task<T> RunAsync()
        {
            try { return await fetch(); }
            finally { fetches.TryRemove(key, out _); }
        }
        // Lazy: GetOrAdd may build two entries under a race, but only the stored one is ever started.
        return fetches.GetOrAdd(key, _ => new Lazy<Task<T>>(RunAsync)).Value.WaitAsync(cancellationToken);
    }

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
        return await SharedFetchAsync(forecastFetches, key, () => FetchForecastAsync(key, lat, lon, custom, location), cancellationToken);
    }

    private Task<NwsPointInfo> GetPointInfoAsync(double lat, double lon, bool custom, string defaultLocation)
    {
        var pointKey = FormattableString.Invariant($"nws-point:{lat:F2}:{lon:F2}");
        if (cache.TryGetValue<NwsPointInfo>(pointKey, out var cached) && cached is not null)
            return Task.FromResult(cached);

        return SharedFetchAsync(pointFetches, pointKey, async () =>
        {
            if (cache.TryGetValue<NwsPointInfo>(pointKey, out var found) && found is not null)
                return found;

            var http = clients.CreateClient("local-weather");
            using var point = await http.GetAsync("https://api.weather.gov/points/" + lat.ToString(CultureInfo.InvariantCulture) + "," + lon.ToString(CultureInfo.InvariantCulture), CancellationToken.None);
            point.EnsureSuccessStatusCode();
            using var pointData = JsonDocument.Parse(await point.Content.ReadAsStringAsync(CancellationToken.None));
            var pointProperties = pointData.RootElement.GetProperty("properties");
            var location = defaultLocation;
            if (custom && pointProperties.TryGetProperty("relativeLocation", out var relative)
                && relative.TryGetProperty("properties", out var place))
            {
                var city = place.TryGetProperty("city", out var c) ? c.GetString() : null;
                var state = place.TryGetProperty("state", out var st) ? st.GetString() : null;
                if (!string.IsNullOrWhiteSpace(city)) location = string.Join(", ", new[] { city, state }.Where(v => !string.IsNullOrWhiteSpace(v)));
            }
            var forecastUrl = pointProperties.GetProperty("forecast").GetString() ?? "";
            var forecastHourlyUrl = pointProperties.TryGetProperty("forecastHourly", out var fhu) ? fhu.GetString() ?? "" : "";
            var timeZone = pointProperties.TryGetProperty("timeZone", out var tz) ? tz.GetString() ?? "America/Chicago" : "America/Chicago";

            var info = new NwsPointInfo(forecastUrl, forecastHourlyUrl, timeZone, location);
            cache.Set(pointKey, info, TimeSpan.FromDays(1));
            return info;
        }, CancellationToken.None);
    }

    private async Task<LocalWeatherPage> FetchForecastAsync(string key, double lat, double lon, bool custom, string location)
    {
        if (cache.TryGetValue<LocalWeatherPage>(key, out var fresh)) return fresh!;
        // No caller token below: the client's timeout bounds the fetch, and its outcome is cached either way.
        var cancellationToken = CancellationToken.None;
        try
        {
            var point = await GetPointInfoAsync(lat, lon, custom, location);
            location = point.Location;
            var url = point.ForecastUrl;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "api.weather.gov")
                throw new JsonException("Unexpected forecast address.");
            var http = clients.CreateClient("local-weather");
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
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or KeyNotFoundException or OperationCanceledException)
        {
            logger.LogWarning(ex, "Weather forecast unavailable");
            var result = cache.TryGetValue<LocalWeatherPage>(key + "-last", out var last)
                ? last! with { Stale = true } : new LocalWeatherPage([], null, true, location, lat, lon);
            cache.Set(key, result, FailureBackoff);
            return result;
        }
    }

    public async Task<LocalHourlyWeatherPage> GetHourlyAsync(CancellationToken cancellationToken, double? latitude = null, double? longitude = null)
    {
        var (lat, lon) = Coordinates(latitude, longitude);
        var custom = latitude is not null;
        var key = custom ? FormattableString.Invariant($"weather-hourly:{lat:F2}:{lon:F2}") : "kc-weather-hourly";
        var location = custom ? "Your location" : "Kansas City";
        if (cache.TryGetValue<LocalHourlyWeatherPage>(key, out var fresh)) return fresh!;
        return await SharedFetchAsync(hourlyFetches, key, () => FetchHourlyAsync(key, lat, lon, custom, location), cancellationToken);
    }

    private async Task<LocalHourlyWeatherPage> FetchHourlyAsync(string key, double lat, double lon, bool custom, string defaultLocation)
    {
        if (cache.TryGetValue<LocalHourlyWeatherPage>(key, out var fresh)) return fresh!;
        var cancellationToken = CancellationToken.None;
        var location = defaultLocation;
        var timeZone = "America/Chicago";
        try
        {
            var point = await GetPointInfoAsync(lat, lon, custom, defaultLocation);
            location = point.Location;
            timeZone = point.TimeZone;
            var url = point.ForecastHourlyUrl;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "api.weather.gov")
                throw new JsonException("Unexpected hourly forecast address.");

            var http = clients.CreateClient("local-weather");
            using var forecast = await http.GetAsync(uri, cancellationToken);
            forecast.EnsureSuccessStatusCode();
            using var data = JsonDocument.Parse(await forecast.Content.ReadAsStringAsync(cancellationToken));
            var properties = data.RootElement.GetProperty("properties");
            var nowUtc = clock.GetUtcNow();

            var periods = properties.GetProperty("periods").EnumerateArray().Select(p =>
            {
                var starts = p.GetProperty("startTime").GetDateTimeOffset();
                int? rainChance = null;
                if (p.TryGetProperty("probabilityOfPrecipitation", out var pop)
                    && pop.TryGetProperty("value", out var val)
                    && val.ValueKind == JsonValueKind.Number)
                {
                    rainChance = val.GetInt32();
                }

                return new LocalHourlyPeriod(
                    starts.ToString("h tt", CultureInfo.InvariantCulture),
                    starts,
                    !p.TryGetProperty("isDaytime", out var daytime) || daytime.ValueKind != JsonValueKind.False,
                    p.GetProperty("temperature").GetInt32(),
                    p.GetProperty("temperatureUnit").GetString() ?? "F",
                    p.GetProperty("shortForecast").GetString() ?? "",
                    rainChance,
                    (p.GetProperty("windSpeed").GetString() ?? "") + " " + (p.GetProperty("windDirection").GetString() ?? ""));
            }).Where(p => p.Starts > nowUtc).Take(8).ToList();

            if (periods.Count < 8)
                throw new InvalidOperationException("Fewer than 8 hours available.");

            var updated = properties.TryGetProperty("updated", out var stamp) && stamp.TryGetDateTimeOffset(out var date) ? date : nowUtc;
            var rainSummary = CalculateRainSummary(periods);
            var result = new LocalHourlyWeatherPage(location, timeZone, updated, updated < nowUtc.AddHours(-6), rainSummary, periods);
            cache.Set(key + "-last", result, TimeSpan.FromHours(6));
            cache.Set(key, result, TimeSpan.FromMinutes(15));
            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or KeyNotFoundException or OperationCanceledException)
        {
            logger.LogWarning(ex, "Hourly weather forecast unavailable");
            var result = cache.TryGetValue<LocalHourlyWeatherPage>(key + "-last", out var last)
                ? last! with { Stale = true } : new LocalHourlyWeatherPage(location, timeZone, null, true, null, []);
            cache.Set(key, result, FailureBackoff);
            return result;
        }
    }

    public static string? CalculateRainSummary(IReadOnlyList<LocalHourlyPeriod> hours)
    {
        if (hours is null || hours.Count == 0 || hours.Any(h => h.RainChance is null))
            return null;

        var likely = hours.Where(h => h.RainChance >= 30).ToList();
        if (likely.Count > 0)
        {
            var first = likely[0];
            var last = likely[^1];
            var endDt = last.Starts.AddHours(1);
            var startHour = first.Starts.ToString("%h", CultureInfo.InvariantCulture);
            var startDesignator = first.Starts.ToString("tt", CultureInfo.InvariantCulture);
            var endHour = endDt.ToString("%h", CultureInfo.InvariantCulture);
            var endDesignator = endDt.ToString("tt", CultureInfo.InvariantCulture);

            var range = string.Equals(startDesignator, endDesignator, StringComparison.OrdinalIgnoreCase)
                ? $"{startHour}–{endHour} {endDesignator}"
                : $"{startHour} {startDesignator}–{endHour} {endDesignator}";

            var max = hours.Max(h => h.RainChance!.Value);
            var maxHour = hours.First(h => h.RainChance == max);
            return $"Rain most likely {range}, up to {max}% at {maxHour.Label}";
        }

        var highest = hours.Max(h => h.RainChance!.Value);
        if (highest > 0)
        {
            var lastHour = hours[^1];
            return $"Low chance of rain through {lastHour.Label}, up to {highest}%";
        }

        var finalHour = hours[^1];
        return $"No rain expected through {finalHour.Label}";
    }

    /// <summary>Active NWS alerts for the forecast point; empty on any failure. Cached 5 minutes, failures 2.</summary>
    public async Task<IReadOnlyList<WeatherAlert>> GetAlertsAsync(CancellationToken cancellationToken, double? latitude = null, double? longitude = null)
    {
        var (lat, lon) = Coordinates(latitude, longitude);
        var custom = latitude is not null;
        var key = custom ? FormattableString.Invariant($"alerts:{lat:F2}:{lon:F2}") : "kc-alerts";
        if (cache.TryGetValue<IReadOnlyList<WeatherAlert>>(key, out var cached) && cached is not null)
            return cached;
        return await SharedFetchAsync(alertFetches, key, () => FetchAlertsAsync(key, lat, lon), cancellationToken);
    }

    private async Task<IReadOnlyList<WeatherAlert>> FetchAlertsAsync(string key, double lat, double lon)
    {
        if (cache.TryGetValue<IReadOnlyList<WeatherAlert>>(key, out var cached) && cached is not null)
            return cached;
        var cancellationToken = CancellationToken.None;
        IReadOnlyList<WeatherAlert> Failed()
        {
            IReadOnlyList<WeatherAlert> none = [];
            cache.Set(key, none, FailureBackoff);
            return none;
        }
        try
        {
            var http = clients.CreateClient("local-weather");
            var pointUrl = "https://api.weather.gov/alerts/active?point=" + lat.ToString(CultureInfo.InvariantCulture) + "," + lon.ToString(CultureInfo.InvariantCulture);
            if (!Uri.TryCreate(pointUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "api.weather.gov")
                return Failed();

            using var response = await http.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Failed();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = doc.RootElement;
            if (!root.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array)
                return Failed();

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
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or KeyNotFoundException or OperationCanceledException)
        {
            logger.LogWarning(ex, "Weather alerts unavailable");
            return Failed();
        }
    }
}
