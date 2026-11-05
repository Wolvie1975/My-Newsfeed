using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Shared.Data;
using myNewspaper.Web.Components;

namespace myNewspaper.Web.Endpoints;

/// <summary>Small public endpoints: refreshable homepage fragments, redirects and the weather API.</summary>
public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this WebApplication app)
    {
        // Hourly homepage spotlight; only this card refreshes.
        app.MapGet("/home/opinion", async (HttpContext http, IDbContextFactory<WebScraperContext> factory, TimeProvider clock) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            await using var db = await factory.CreateDbContextAsync(http.RequestAborted);
            var snapshot = await HomeOpinionQuery.GetAsync(db, clock.GetUtcNow().UtcDateTime, http.RequestAborted);
            return new RazorComponentResult<HomeOpinionCard>(new { Snapshot = snapshot });
        });

        // Hourly homepage lead; only the lead story card refreshes.
        app.MapGet("/home/lead", async (HttpContext http, IDbContextFactory<WebScraperContext> factory, TimeProvider clock) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            await using var db = await factory.CreateDbContextAsync(http.RequestAborted);
            var snapshot = await HomeLeadQuery.GetAsync(db, clock.GetUtcNow().UtcDateTime, http.RequestAborted);
            return new RazorComponentResult<HomeLeadStory>(new { Snapshot = snapshot });
        });

        // Homepage Top 25 poll picker; only the rankings card is swapped, so the page keeps its scroll position.
        app.MapGet("/home/rankings", async (HttpContext http, IDbContextFactory<WebScraperContext> factory, TimeProvider clock, ILoggerFactory loggers,
            FeedDates dates, string? rankingSourceId) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            var log = loggers.CreateLogger("myNewspaper.Web.HomeRankings");
            RankingsPage data;
            try
            {
                await using var db = await factory.CreateDbContextAsync(http.RequestAborted);
                data = await RankingsQuery.GetAsync(db, StandingsQuery.ParseSourceId(rankingSourceId), log, dates.TodayAt(clock.GetUtcNow().UtcDateTime), ct: http.RequestAborted);
            }
            catch (Exception ex) when (DataFailures.IsReadFailure(ex))
            {
                log.LogError(ex, "Could not read home rankings.");
                data = new([], null, null, [], "Rankings are temporarily unavailable.");
            }
            return new RazorComponentResult<HomeRankings>(new { Data = data });
        });

        // Native navigation selects work with JavaScript disabled; never redirect off-site.
        app.MapGet("/navigate", (string? to) =>
        {
            var local = !string.IsNullOrEmpty(to) && to[0] == '/'
                && (to.Length == 1 || to[1] is not ('/' or '\\')) && !to.Any(char.IsControl);
            return Results.LocalRedirect(local ? to! : "/");
        });

        // College news moved to Sports headlines (Big 12 category).
        app.MapGet("/college/news", (HttpContext http) =>
        {
            var keep = http.Request.Query.Where(q => q.Key is "q" or "source" or "after");
            var query = QueryString.Create(keep.Select(q => KeyValuePair.Create(q.Key, (string?)q.Value.ToString())));
            return Results.Redirect(http.Request.PathBase + "/sports/news/big12" + query, permanent: true);
        });

        // Top 25 joined College Sports rankings.
        app.MapGet("/rankings", async (HttpContext http, IDbContextFactory<WebScraperContext> factory, int? sourceId, CancellationToken ct) =>
        {
            if (sourceId is { } id)
            {
                await using var db = await factory.CreateDbContextAsync(ct);
                var sport = await RankingsQuery.SportForSourceAsync(db, id, ct);
                if (sport is not null)
                {
                    var key = CollegeHub.Resolve(sport).Key;
                    return Results.Redirect($"{http.Request.PathBase}/college/rankings?sport={key}&sourceId={id}", permanent: true);
                }
                return Results.Redirect(http.Request.PathBase + "/college/rankings", permanent: true);
            }
            return Results.Redirect(http.Request.PathBase + "/college/rankings?sport=football", permanent: true);
        });

        // Preserve old result bookmarks as final-score views of Scoreboard.
        app.MapGet("/results", (HttpContext http) =>
        {
            var query = new Dictionary<string, string?> { ["status"] = "final" };
            foreach (var key in new[] { "day", "follow" })
                if (http.Request.Query.TryGetValue(key, out var value)) query[key] = value.ToString();
            return Results.LocalRedirect(QueryHelpers.AddQueryString("/scoreboard", query));
        });

        // F1 news moved to Sports headlines; old F1 › News links keep their source filter.
        app.MapGet("/f1/news", (HttpContext http) =>
        {
            var query = new Dictionary<string, string?>();
            if (int.TryParse(http.Request.Query["source"], out var source) && source > 0) query["source"] = source.ToString(CultureInfo.InvariantCulture);
            return Results.LocalRedirect(QueryHelpers.AddQueryString("/sports/news/f1", query));
        });

        app.MapGet("/scoreboard/fragment", (HttpContext http, string? day, string? competition, bool? compact, int? follow, string? status, string? show, string? scoreboardShow, bool? sportFilter, bool? phone) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            return new RazorComponentResult<ScoreboardPanel>(new Dictionary<string, object?>
            {
                ["Day"] = day,
                ["Competition"] = competition,
                ["Compact"] = compact == true,
                ["Follow"] = follow,
                ["Status"] = status,
                ["Show"] = show,
                ["CompactShow"] = scoreboardShow,
                ["SportFilter"] = sportFilter == true,
                ["PhoneFront"] = phone == true && compact != true,
            });
        });

        app.MapGet("/local/weather", async (LocalWeather weather, HttpContext context, double? lat, double? lon) =>
        {
            context.Response.Headers.CacheControl = lat is not null || lon is not null ? "private,no-store" : "public,max-age=120";
            try { return Results.Json(await weather.GetAsync(context.RequestAborted, lat, lon)); }
            catch (ArgumentException) { return Results.BadRequest(new { error = "Valid latitude and longitude are required." }); }
        });
    }
}
