using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Components;
using myNewspaper.Web.Data;

namespace myNewspaper.Web.Endpoints;

/// <summary>Small public endpoints: refreshable homepage fragments, redirects and the weather API.</summary>
public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this WebApplication app)
    {
        // Hourly homepage spotlight; only this card refreshes.
        app.MapGet("/home/opinion", async (HttpContext http, IDbContextFactory<WebScraperContext> factory) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            await using var db = await factory.CreateDbContextAsync();
            var snapshot = await HomeOpinionQuery.GetAsync(db, DateTime.UtcNow);
            return new RazorComponentResult<HomeOpinionCard>(new { Snapshot = snapshot });
        });

        // Hourly homepage lead; only the lead story card refreshes.
        app.MapGet("/home/lead", async (HttpContext http, IDbContextFactory<WebScraperContext> factory) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            await using var db = await factory.CreateDbContextAsync();
            var snapshot = await HomeLeadQuery.GetAsync(db, DateTime.UtcNow);
            return new RazorComponentResult<HomeLeadStory>(new { Snapshot = snapshot });
        });

        // Native navigation selects work with JavaScript disabled; never redirect off-site.
        app.MapGet("/navigate", (string? to) =>
        {
            var local = !string.IsNullOrEmpty(to) && to[0] == '/'
                && (to.Length == 1 || to[1] is not ('/' or '\\')) && !to.Any(char.IsControl);
            return Results.LocalRedirect(local ? to! : "/");
        });

        // Preserve old result bookmarks as final-score views of Scoreboard.
        app.MapGet("/results", (HttpContext http) =>
        {
            var query = new Dictionary<string, string?> { ["status"] = "final" };
            foreach (var key in new[] { "day", "follow" })
                if (http.Request.Query.TryGetValue(key, out var value)) query[key] = value.ToString();
            return Results.LocalRedirect(QueryHelpers.AddQueryString("/scoreboard", query));
        });

        app.MapGet("/scoreboard/fragment", (HttpContext http, string? day, string? competition, bool? compact, int? follow, string? status, string? show) =>
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
