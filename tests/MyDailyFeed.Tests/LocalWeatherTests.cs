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
}
