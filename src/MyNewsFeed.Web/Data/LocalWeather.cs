using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace MyNewsFeed.Web.Data;

public sealed record LocalForecast(string Name, int Temperature, string Unit, string Summary, string Wind, DateTimeOffset Starts, bool IsDaytime = true);
public sealed record LocalWeatherPage(IReadOnlyList<LocalForecast> Periods, DateTimeOffset? Updated, bool Stale);

public sealed class LocalWeather(IHttpClientFactory clients, IMemoryCache cache, ILogger<LocalWeather> logger)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public async Task<LocalWeatherPage> GetAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue<LocalWeatherPage>("kc-weather", out var fresh)) return fresh!;
        await gate.WaitAsync(cancellationToken);
        try {
            if (cache.TryGetValue<LocalWeatherPage>("kc-weather", out fresh)) return fresh!;
            try {
                var http = clients.CreateClient("local-weather");
                using var point = await http.GetAsync("https://api.weather.gov/points/39.0997,-94.5786", cancellationToken);
                point.EnsureSuccessStatusCode();
                using var pointData = JsonDocument.Parse(await point.Content.ReadAsStringAsync(cancellationToken));
                var url = pointData.RootElement.GetProperty("properties").GetProperty("forecast").GetString();
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
                var result = new LocalWeatherPage(periods, updated, updated < DateTimeOffset.UtcNow.AddHours(-6));
                cache.Set("kc-weather-last", result, TimeSpan.FromHours(6));
                cache.Set("kc-weather", result, TimeSpan.FromMinutes(15));
                return result;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or KeyNotFoundException || ex is OperationCanceledException && !cancellationToken.IsCancellationRequested) {
                logger.LogWarning(ex, "Kansas City weather unavailable");
                var result = cache.TryGetValue<LocalWeatherPage>("kc-weather-last", out var last)
                    ? last! with { Stale = true } : new LocalWeatherPage([], null, true);
                cache.Set("kc-weather", result, TimeSpan.FromMinutes(2));
                return result;
            }
        }
        finally { gate.Release(); }
    }
}
