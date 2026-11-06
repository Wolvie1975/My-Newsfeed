using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using myNewspaper.Shared.Data;

namespace myNewspaper.Tests;

public class LocalWeatherTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++; return Task.FromResult(respond(request));
        }
    }
    private sealed class Clients(HttpMessageHandler handler) : IHttpClientFactory
    { public HttpClient CreateClient(string name) => new(handler, disposeHandler: false); }
    private static HttpResponseMessage Json(string text) => new(HttpStatusCode.OK) { Content = new StringContent(text) };
    [Fact]
    public async Task Forecast_is_cached_and_only_nws_forecast_addresses_are_requested()
    {
        var now = DateTimeOffset.UtcNow;
        var handler = new Handler(request => request.RequestUri!.AbsolutePath.StartsWith("/points/")
            ? Json("""{"properties":{"forecast":"https://api.weather.gov/gridpoints/EAX/1,2/forecast"}}""")
            : Json(System.Text.Json.JsonSerializer.Serialize(new
            {
                properties = new
                {
                    updated = now,
                    periods = new[] { new { name = "Tonight", isDaytime = false, temperature = 70, temperatureUnit = "F", shortForecast = "Sunny", windSpeed = "5 mph", windDirection = "S", startTime = now } }
                }
            })));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new LocalWeather(new Clients(handler), cache, TimeProvider.System, NullLogger<LocalWeather>.Instance);
        var first = await service.GetAsync(default);
        var second = await service.GetAsync(default);
        Assert.Same(first, second); Assert.Equal(2, handler.Calls);
        Assert.Equal(70, Assert.Single(first.Periods).Temperature); Assert.False(first.Stale);
        Assert.False(first.Periods[0].IsDaytime);
    }
    [Fact]
    public async Task Unexpected_remote_forecast_host_is_rejected_without_requesting_it()
    {
        var handler = new Handler(_ => Json("""{"properties":{"forecast":"https://other.invalid/forecast"}}"""));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new LocalWeather(new Clients(handler), cache, TimeProvider.System, NullLogger<LocalWeather>.Instance);
        var result = await service.GetAsync(default);
        Assert.Empty(result.Periods); Assert.True(result.Stale); Assert.Equal(1, handler.Calls);
    }
    [Fact]
    public async Task Provider_failure_labels_last_success_stale_and_backs_off()
    {
        var handler = new Handler(_ => new(HttpStatusCode.ServiceUnavailable));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var last = new LocalWeatherPage([new("Tonight", 50, "F", "Clear", "Calm", DateTimeOffset.UtcNow)], DateTimeOffset.UtcNow, false);
        cache.Set("kc-weather-last", last, TimeSpan.FromHours(1));
        var service = new LocalWeather(new Clients(handler), cache, TimeProvider.System, NullLogger<LocalWeather>.Instance);
        var result = await service.GetAsync(default);
        Assert.True(result.Stale); Assert.Equal(last.Periods, result.Periods);
        Assert.Same(result, await service.GetAsync(default)); Assert.Equal(1, handler.Calls);
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
        var handler = new Handler(request =>
        {
            var path = request.RequestUri!.AbsolutePath; paths.Add(path);
            if (path.StartsWith("/points/")) return Json("""{"properties":{"relativeLocation":{"properties":{"city":"New York","state":"NY"}},"forecast":"https://api.weather.gov/gridpoints/OKX/1,2/forecast"}}""");
            return Json(System.Text.Json.JsonSerializer.Serialize(new
            {
                properties = new
                {
                    updated = now,
                    periods = new[] {
                new { name = "Today", temperature = 65, temperatureUnit = "F", shortForecast = "Clear", windSpeed = "Calm", windDirection = "", startTime = now }
            }
                }
            }));
        });
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new LocalWeather(new Clients(handler), cache, TimeProvider.System, NullLogger<LocalWeather>.Instance);
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
        var service = new LocalWeather(new Clients(handler), cache, TimeProvider.System, NullLogger<LocalWeather>.Instance);
        var result = await service.GetAsync(default, 51.5, -0.12);
        Assert.Empty(result.Periods); Assert.Equal("Your location", result.Location);
        Assert.Same(result, await service.GetAsync(default, 51.5, -0.12)); Assert.Equal(1, handler.Calls);
    }

    // A regression must fail these tests, not hang them.
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);

    // Holds each response until released, like a slow weather service.
    private sealed class SlowHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Interlocked.Increment(ref Calls);
            await Release.Task;
            return respond(request);
        }
    }

    private static HttpResponseMessage Forecast(HttpRequestMessage request) => request.RequestUri!.AbsolutePath.StartsWith("/points/")
        ? Json("""{"properties":{"forecast":"https://api.weather.gov/gridpoints/EAX/1,2/forecast"}}""")
        : Json(System.Text.Json.JsonSerializer.Serialize(new
        {
            properties = new
            {
                updated = DateTimeOffset.UtcNow,
                periods = new[] { new { name = "Today", isDaytime = true, temperature = 61, temperatureUnit = "F", shortForecast = "Sunny", windSpeed = "5 mph", windDirection = "S", startTime = DateTimeOffset.UtcNow } }
            }
        }));

    [Fact]
    public async Task A_caller_that_stops_waiting_does_not_cancel_the_fetch_and_the_next_request_is_served_from_cache()
    {
        var handler = new SlowHandler(Forecast);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new LocalWeather(new Clients(handler), cache, TimeProvider.System, NullLogger<LocalWeather>.Instance);

        using (var impatient = new CancellationTokenSource(TimeSpan.FromMilliseconds(50)))
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(impatient.Token).WaitAsync(Limit));
        var waiting = service.GetAsync(default);   // joins the same fetch rather than starting another
        handler.Release.SetResult();
        var page = await waiting.WaitAsync(Limit);

        Assert.Equal(61, Assert.Single(page.Periods).Temperature);
        Assert.Same(page, await service.GetAsync(default));
        Assert.Equal(2, handler.Calls);           // one points request and one forecast request, for three callers
    }

    [Fact]
    public async Task A_failure_after_the_caller_gave_up_is_still_cached_so_requests_back_off()
    {
        var handler = new SlowHandler(_ => new(HttpStatusCode.ServiceUnavailable));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new LocalWeather(new Clients(handler), cache, TimeProvider.System, NullLogger<LocalWeather>.Instance);

        using (var impatient = new CancellationTokenSource(TimeSpan.FromMilliseconds(50)))
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(impatient.Token).WaitAsync(Limit));
        handler.Release.SetResult();
        await Task.Delay(100);
        var page = await service.GetAsync(default).WaitAsync(Limit);

        Assert.Empty(page.Periods);
        Assert.True(page.Stale);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Alert_failures_are_cached_so_requests_back_off()
    {
        var handler = new Handler(_ => new(HttpStatusCode.ServiceUnavailable));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new LocalWeather(new Clients(handler), cache, TimeProvider.System, NullLogger<LocalWeather>.Instance);

        Assert.Empty(await service.GetAlertsAsync(default));
        Assert.Empty(await service.GetAlertsAsync(default));
        Assert.Equal(1, handler.Calls);
    }

    private static LocalForecast Period(string name, int temperature, string summary, bool day) =>
        new(name, temperature, "F", summary, "5 mph S", DateTimeOffset.UnixEpoch, day);

    [Fact]
    public void Glance_pairs_a_daytime_lead_with_its_night_and_lists_the_rest()
    {
        var glance = ForecastGlance.From([
            Period("Today", 53, "Patchy Drizzle", true), Period("Tonight", 35, "Mostly Cloudy then Patchy Frost", false),
            Period("Saturday", 56, "Patchy Frost then Mostly Sunny", true), Period("Saturday Night", 40, "Clear", false)])!;
        Assert.Equal("Today", glance.Lead.Name);
        Assert.Equal(35, glance.Low);
        Assert.Equal("Patchy drizzle. Tonight, mostly cloudy then patchy frost.", glance.Summary);
        Assert.Equal(new[] { "Saturday", "Saturday Night" }, glance.Next.Select(p => p.Name));
        Assert.Equal("Patchy frost then mostly sunny", ForecastGlance.Sentence(glance.Next[0].Summary));
    }

    [Fact]
    public void Glance_led_by_a_night_has_no_separate_low()
    {
        var glance = ForecastGlance.From([
            Period("Tonight", 35, "Mostly Cloudy", false), Period("Saturday", 56, "Sunny", true),
            Period("Saturday Night", 40, "Clear", false), Period("Sunday", 60, "Sunny", true)])!;
        Assert.Null(glance.Low);
        Assert.Equal("Mostly cloudy.", glance.Summary);
        Assert.Equal(new[] { "Saturday", "Saturday Night" }, glance.Next.Select(p => p.Name));
        Assert.Null(ForecastGlance.From([]));
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public async Task Hourly_starts_from_next_hour_returns_exactly_8_and_preserves_missing_rain_chance()
    {
        var baseTime = new DateTimeOffset(2026, 11, 6, 15, 30, 0, TimeSpan.FromHours(-6)); // 3:30 PM CST
        var handler = new Handler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.StartsWith("/points/"))
            {
                return Json("""
                {
                    "properties": {
                        "forecast": "https://api.weather.gov/gridpoints/EAX/1,2/forecast",
                        "forecastHourly": "https://api.weather.gov/gridpoints/EAX/1,2/forecast/hourly",
                        "timeZone": "America/Chicago"
                    }
                }
                """);
            }

            var periods = new List<object>
            {
                // Current/past hour (3 PM): should be skipped
                new {
                    startTime = baseTime.AddMinutes(-30),
                    isDaytime = true,
                    temperature = 50,
                    temperatureUnit = "F",
                    shortForecast = "Mostly Cloudy",
                    windSpeed = "5 mph",
                    windDirection = "N",
                    probabilityOfPrecipitation = new { value = (int?)10 }
                }
            };

            // 10 future hours (4 PM to 1 AM)
            for (var i = 1; i <= 10; i++)
            {
                var dt = baseTime.AddMinutes(-30).AddHours(i);
                periods.Add(new
                {
                    startTime = dt,
                    isDaytime = dt.Hour is >= 6 and < 18,
                    temperature = 48 - i,
                    temperatureUnit = "F",
                    shortForecast = "Partly Cloudy",
                    windSpeed = "5 mph",
                    windDirection = "NW",
                    probabilityOfPrecipitation = new { value = i == 3 ? (int?)null : i * 5 }
                });
            }

            return Json(System.Text.Json.JsonSerializer.Serialize(new
            {
                properties = new
                {
                    updated = baseTime,
                    periods
                }
            }));
        });

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var timeProvider = new FixedClock(baseTime);
        var service = new LocalWeather(new Clients(handler), cache, timeProvider, NullLogger<LocalWeather>.Instance);

        var page = await service.GetHourlyAsync(default);
        Assert.Equal("Kansas City", page.Location);
        Assert.Equal("America/Chicago", page.TimeZone);
        Assert.False(page.Stale);
        Assert.Equal(8, page.Hours.Count);
        Assert.Equal("4 PM", page.Hours[0].Label);
        Assert.Equal("11 PM", page.Hours[^1].Label);
        Assert.Null(page.Hours[2].RainChance); // 3rd hour has null pop
        Assert.Equal(5, page.Hours[0].RainChance);

        // Cached
        var cached = await service.GetHourlyAsync(default);
        Assert.Same(page, cached);
        Assert.Equal(2, handler.Calls); // 1 point + 1 forecastHourly
    }

    [Fact]
    public async Task Hourly_with_fewer_than_8_future_periods_counts_as_failure()
    {
        var baseTime = new DateTimeOffset(2026, 11, 6, 15, 30, 0, TimeSpan.FromHours(-6));
        var handler = new Handler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.StartsWith("/points/"))
            {
                return Json("""
                {
                    "properties": {
                        "forecast": "https://api.weather.gov/gridpoints/EAX/1,2/forecast",
                        "forecastHourly": "https://api.weather.gov/gridpoints/EAX/1,2/forecast/hourly",
                        "timeZone": "America/Chicago"
                    }
                }
                """);
            }

            // Only 5 future periods
            var periods = Enumerable.Range(1, 5).Select(i => new
            {
                startTime = baseTime.AddHours(i),
                isDaytime = true,
                temperature = 45,
                temperatureUnit = "F",
                shortForecast = "Clear",
                windSpeed = "5 mph",
                windDirection = "N",
                probabilityOfPrecipitation = new { value = 0 }
            });

            return Json(System.Text.Json.JsonSerializer.Serialize(new
            {
                properties = new
                {
                    updated = baseTime,
                    periods
                }
            }));
        });

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var timeProvider = new FixedClock(baseTime);
        var service = new LocalWeather(new Clients(handler), cache, timeProvider, NullLogger<LocalWeather>.Instance);

        var result = await service.GetHourlyAsync(default);
        Assert.Empty(result.Hours);
        Assert.True(result.Stale);
    }

    private static LocalHourlyPeriod HourlyPeriod(string label, DateTimeOffset starts, int? rainChance, int temp = 45) =>
        new(label, starts, true, temp, "F", "Cloudy", rainChance, "5 mph N");

    [Fact]
    public void RainSummary_covers_all_four_cases_and_crossing_midnight_and_noon()
    {
        var start = new DateTimeOffset(2026, 11, 6, 16, 0, 0, TimeSpan.FromHours(-6)); // 4 PM

        // Case 1 standard: 30% or more, e.g. 6 PM (30%) and 7 PM (40%). End is 8 PM.
        var case1 = new List<LocalHourlyPeriod>
        {
            HourlyPeriod("4 PM", start, 10),
            HourlyPeriod("5 PM", start.AddHours(1), 15),
            HourlyPeriod("6 PM", start.AddHours(2), 30),
            HourlyPeriod("7 PM", start.AddHours(3), 40),
            HourlyPeriod("8 PM", start.AddHours(4), 20),
            HourlyPeriod("9 PM", start.AddHours(5), 10),
            HourlyPeriod("10 PM", start.AddHours(6), 5),
            HourlyPeriod("11 PM", start.AddHours(7), 0),
        };
        Assert.Equal("Rain most likely 6–8 PM, up to 40% at 7 PM", LocalWeather.CalculateRainSummary(case1));

        // Case 1 crossing midnight: 11 PM (35%), 12 AM (45%). End is 1 AM.
        var midnightStart = new DateTimeOffset(2026, 11, 6, 21, 0, 0, TimeSpan.FromHours(-6)); // 9 PM
        var case1Midnight = new List<LocalHourlyPeriod>
        {
            HourlyPeriod("9 PM", midnightStart, 10),
            HourlyPeriod("10 PM", midnightStart.AddHours(1), 15),
            HourlyPeriod("11 PM", midnightStart.AddHours(2), 35),
            HourlyPeriod("12 AM", midnightStart.AddHours(3), 45),
            HourlyPeriod("1 AM", midnightStart.AddHours(4), 20),
            HourlyPeriod("2 AM", midnightStart.AddHours(5), 10),
            HourlyPeriod("3 AM", midnightStart.AddHours(6), 5),
            HourlyPeriod("4 AM", midnightStart.AddHours(7), 0),
        };
        Assert.Equal("Rain most likely 11 PM–1 AM, up to 45% at 12 AM", LocalWeather.CalculateRainSummary(case1Midnight));

        // Case 1 crossing noon: 11 AM (50%), 12 PM (40%). End is 1 PM.
        var noonStart = new DateTimeOffset(2026, 11, 6, 9, 0, 0, TimeSpan.FromHours(-6)); // 9 AM
        var case1Noon = new List<LocalHourlyPeriod>
        {
            HourlyPeriod("9 AM", noonStart, 10),
            HourlyPeriod("10 AM", noonStart.AddHours(1), 20),
            HourlyPeriod("11 AM", noonStart.AddHours(2), 50),
            HourlyPeriod("12 PM", noonStart.AddHours(3), 40),
            HourlyPeriod("1 PM", noonStart.AddHours(4), 15),
            HourlyPeriod("2 PM", noonStart.AddHours(5), 10),
            HourlyPeriod("3 PM", noonStart.AddHours(6), 5),
            HourlyPeriod("4 PM", noonStart.AddHours(7), 0),
        };
        Assert.Equal("Rain most likely 11 AM–1 PM, up to 50% at 11 AM", LocalWeather.CalculateRainSummary(case1Noon));

        // Case 2: Highest is 1-29%
        var case2 = new List<LocalHourlyPeriod>
        {
            HourlyPeriod("4 PM", start, 0),
            HourlyPeriod("5 PM", start.AddHours(1), 10),
            HourlyPeriod("6 PM", start.AddHours(2), 25),
            HourlyPeriod("7 PM", start.AddHours(3), 15),
            HourlyPeriod("8 PM", start.AddHours(4), 5),
            HourlyPeriod("9 PM", start.AddHours(5), 0),
            HourlyPeriod("10 PM", start.AddHours(6), 0),
            HourlyPeriod("11 PM", start.AddHours(7), 0),
        };
        Assert.Equal("Low chance of rain through 11 PM, up to 25%", LocalWeather.CalculateRainSummary(case2));

        // Case 3: All 0%
        var case3 = new List<LocalHourlyPeriod>
        {
            HourlyPeriod("4 PM", start, 0),
            HourlyPeriod("5 PM", start.AddHours(1), 0),
            HourlyPeriod("6 PM", start.AddHours(2), 0),
            HourlyPeriod("7 PM", start.AddHours(3), 0),
            HourlyPeriod("8 PM", start.AddHours(4), 0),
            HourlyPeriod("9 PM", start.AddHours(5), 0),
            HourlyPeriod("10 PM", start.AddHours(6), 0),
            HourlyPeriod("11 PM", start.AddHours(7), 0),
        };
        Assert.Equal("No rain expected through 11 PM", LocalWeather.CalculateRainSummary(case3));

        // Case 4: Any hour missing (null)
        var case4 = new List<LocalHourlyPeriod>
        {
            HourlyPeriod("4 PM", start, 0),
            HourlyPeriod("5 PM", start.AddHours(1), null),
            HourlyPeriod("6 PM", start.AddHours(2), 20),
            HourlyPeriod("7 PM", start.AddHours(3), 10),
            HourlyPeriod("8 PM", start.AddHours(4), 0),
            HourlyPeriod("9 PM", start.AddHours(5), 0),
            HourlyPeriod("10 PM", start.AddHours(6), 0),
            HourlyPeriod("11 PM", start.AddHours(7), 0),
        };
        Assert.Null(LocalWeather.CalculateRainSummary(case4));
        Assert.Null(LocalWeather.CalculateRainSummary([]));
    }
}
