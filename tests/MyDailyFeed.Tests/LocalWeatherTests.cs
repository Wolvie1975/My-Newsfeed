using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using MyDailyFeed.Web.Data;

namespace MyDailyFeed.Tests;

public class LocalWeatherTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) {
            Calls++; return Task.FromResult(respond(request));
        }
    }
    private sealed class Clients(HttpMessageHandler handler) : IHttpClientFactory
    { public HttpClient CreateClient(string name) => new(handler, disposeHandler:false); }
    private static HttpResponseMessage Json(string text) => new(HttpStatusCode.OK) { Content = new StringContent(text) };
    [Fact]
    public async Task Forecast_is_cached_and_only_nws_forecast_addresses_are_requested()
    {
        var now = DateTimeOffset.UtcNow;
        var handler = new Handler(request => request.RequestUri!.AbsolutePath.StartsWith("/points/")
            ? Json("""{"properties":{"forecast":"https://api.weather.gov/gridpoints/EAX/1,2/forecast"}}""")
            : Json(System.Text.Json.JsonSerializer.Serialize(new { properties = new { updated = now,
                periods = new[] { new { name = "Tonight", isDaytime = false, temperature = 70, temperatureUnit = "F", shortForecast = "Sunny", windSpeed = "5 mph", windDirection = "S", startTime = now } } } })));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new LocalWeather(new Clients(handler),cache,NullLogger<LocalWeather>.Instance);
        var first = await service.GetAsync(default);
        var second = await service.GetAsync(default);
        Assert.Same(first,second); Assert.Equal(2,handler.Calls);
        Assert.Equal(70,Assert.Single(first.Periods).Temperature); Assert.False(first.Stale);
        Assert.False(first.Periods[0].IsDaytime);
    }
    [Fact]
    public async Task Unexpected_remote_forecast_host_is_rejected_without_requesting_it()
    {
        var handler = new Handler(_ => Json("""{"properties":{"forecast":"https://other.invalid/forecast"}}"""));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new LocalWeather(new Clients(handler),cache,NullLogger<LocalWeather>.Instance);
        var result = await service.GetAsync(default);
        Assert.Empty(result.Periods); Assert.True(result.Stale); Assert.Equal(1,handler.Calls);
    }
    [Fact]
    public async Task Provider_failure_labels_last_success_stale_and_backs_off()
    {
        var handler = new Handler(_ => new(HttpStatusCode.ServiceUnavailable));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var last = new LocalWeatherPage([new("Tonight",50,"F","Clear","Calm",DateTimeOffset.UtcNow)],DateTimeOffset.UtcNow,false);
        cache.Set("kc-weather-last",last,TimeSpan.FromHours(1));
        var service = new LocalWeather(new Clients(handler),cache,NullLogger<LocalWeather>.Instance);
        var result = await service.GetAsync(default);
        Assert.True(result.Stale); Assert.Equal(last.Periods,result.Periods);
        Assert.Same(result,await service.GetAsync(default)); Assert.Equal(1,handler.Calls);
    }
    [Fact]
    public void Coordinates_validate_pairs_ranges_and_round_approximate_locations()
    {
        Assert.Equal((39.0997, -94.5786), LocalWeather.Coordinates(null, null));
        Assert.Equal((40.71, -74.01), LocalWeather.Coordinates(40.7128, -74.006));
        Assert.Throws<ArgumentException>(() => LocalWeather.Coordinates(91, 0));
        Assert.Throws<ArgumentException>(() => LocalWeather.Coordinates(0, -181));
        Assert.Throws<ArgumentException>(() => LocalWeather.Coordinates(double.NaN, 0));
        Assert.Throws<ArgumentException>(() => LocalWeather.Coordinates(0, double.PositiveInfinity));
        Assert.Throws<ArgumentException>(() => LocalWeather.Coordinates(0, null));
    }

    [Fact]
    public async Task Different_locations_have_separate_caches_and_city_labels()
    {
        var paths = new List<string>();
        var now = DateTimeOffset.UtcNow;
        var handler = new Handler(request => {
            var path = request.RequestUri!.AbsolutePath; paths.Add(path);
            if (path.StartsWith("/points/")) return Json("""{"properties":{"relativeLocation":{"properties":{"city":"New York","state":"NY"}},"forecast":"https://api.weather.gov/gridpoints/OKX/1,2/forecast"}}""");
            return Json(System.Text.Json.JsonSerializer.Serialize(new { properties = new { updated = now, periods = new[] {
                new { name = "Today", temperature = 65, temperatureUnit = "F", shortForecast = "Clear", windSpeed = "Calm", windDirection = "", startTime = now }
            } } }));
        });
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new LocalWeather(new Clients(handler), cache, NullLogger<LocalWeather>.Instance);
        var kc = await service.GetAsync(default);
        var ny = await service.GetAsync(default, 40.7128, -74.006);
        Assert.Equal("Kansas City", kc.Location); Assert.Equal("New York, NY", ny.Location);
        Assert.Equal(40.71, ny.Latitude); Assert.Equal(-74.01, ny.Longitude);
        Assert.Same(ny, await service.GetAsync(default, 40.711, -74.009));
        Assert.Same(kc, await service.GetAsync(default));
        Assert.Equal(4, handler.Calls); Assert.Contains("/points/40.71,-74.01", paths);
    }

    [Fact]
    public async Task Location_failure_does_not_reuse_another_locations_forecast()
    {
        var handler = new Handler(_ => new(HttpStatusCode.ServiceUnavailable));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set("kc-weather-last", new LocalWeatherPage([new("Today", 70, "F", "Clear", "Calm", DateTimeOffset.UtcNow)], DateTimeOffset.UtcNow, false));
        var service = new LocalWeather(new Clients(handler), cache, NullLogger<LocalWeather>.Instance);
        var result = await service.GetAsync(default, 51.5, -0.12);
        Assert.Empty(result.Periods); Assert.Equal("Your location", result.Location);
        Assert.Same(result, await service.GetAsync(default, 51.5, -0.12)); Assert.Equal(1, handler.Calls);
    }

}
