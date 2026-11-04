using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Shared.Data;
using myNewspaper.Web.Components.Feed;

namespace myNewspaper.Web.Endpoints;

public static class FeedEndpoints
{
    /// <summary>"Load more" on the public feed: the next batch of article cards, rendered by the same component as the page.</summary>
    public static void MapFeed(this WebApplication app) => app.MapGet("/feed/more", MoreAsync);

    private static async Task<IResult> MoreAsync(HttpContext http, IDbContextFactory<WebScraperContext> factory, ILoggerFactory loggers,
        FeedDates dates, string? after, string? category, string? q, string? day, string? source, string? view, string? sport,
        string? cards, string? groupBy, string? group)
    {
        if (FeedCursor.Parse(after) is not { } cursor)
        {
            return Results.BadRequest();
        }

        int? categoryId = int.TryParse(category, out var parsedCategory) ? parsedCategory : null;
        await using var db = await factory.CreateDbContextAsync();
        var boxOffice = view == "box-office";
        var big12News = view == "big12-news";
        if (big12News)
        {
            categoryId = await Big12NewsQuery.CategoryIdAsync(db);
            if (categoryId is null) return Results.NotFound();
        }
        if (boxOffice)
        {
            categoryId = await BoxOfficeQuery.CategoryIdAsync(db);
            if (categoryId is null) return Results.NotFound();
            q = null;
        }

        var sectionCategoryName = view switch
        {
            "entertainment-news" => NewsSections.Entertainment,
            "tv-film-news" => NewsSections.TvFilm,
            "sports-news" => NewsSections.Sports,
            "mens-college-basketball-news" => NewsSections.MensBasketball,
            "sports-big12-news" => Big12NewsQuery.CategoryName,
            "sports-f1-news" => F1NewsQuery.CategoryName,
            _ => null,
        };
        var hub = NewsSections.HubForView(view);
        FeedCategory? sectionCategory = null;
        if (hub is not null && (sectionCategoryName is not null || categoryId is not null || hub != "tech"))
        {
            sectionCategory = await FeedQuery.HubCategoryAsync(db, hub, sectionCategoryName, categoryId, http.RequestAborted);
            if (sectionCategory is null) return Results.NotFound();
            categoryId = sectionCategory.Id;
        }

        int? sourceId = int.TryParse(source, out var parsedSource) && parsedSource > 0 ? parsedSource : null;
        if (sourceId is { } id && !await FeedQuery.IsEnabledSourceAsync(db, id, categoryId, http.RequestAborted)) sourceId = null;
        var page = await FeedQuery.GetFeedAsync(db, categoryId, q, cursor, FeedQuery.MoreBatch, sourceId,
            headlinesOnly: !boxOffice && !big12News && hub is null && sectionCategory is null,
            hub: boxOffice || big12News ? null : (hub ?? "headlines"));

        // Headline cards: the page decides hour or day grouping; the last group stops a repeated heading at the seam.
        var cardMode = cards == "1";
        var grouping = groupBy == "hour" ? "hour" : "day";
        var nowUtc = DateTime.UtcNow;
        await using var renderer = new HtmlRenderer(http.RequestServices, loggers);
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<FeedArticles>(ParameterView.FromDictionary(
                new Dictionary<string, object?>
                {
                    [nameof(FeedArticles.Items)] = page.Items,
                    [nameof(FeedArticles.PreviousDay)] = day,
                    [nameof(FeedArticles.Cards)] = cardMode,
                    [nameof(FeedArticles.GroupBy)] = grouping,
                    [nameof(FeedArticles.PreviousGroup)] = group,
                }));
            return output.ToHtmlString();
        });

        string? nextHref = null;
        if (page.Next is { } next)
        {
            var token = next.ToToken();
            if (big12News)
            {
                nextHref = CollegeHub.Href("news", CollegeHub.Resolve(sport)) + "&" + FeedQuery.QueryString(null, q, token, sourceId).TrimStart('?');
            }
            else
            {
                var route = boxOffice ? "box-office" : hub == "tech" ? "tech/news" : sectionCategory is not null ? NewsSections.RouteFor(sectionCategory)! : "news/all";
                nextHref = route + FeedQuery.QueryString(categoryId, q, token, sourceId,
                    allCategories: !boxOffice && categoryId is null);
            }
        }

        http.Response.Headers.CacheControl = "no-store";
        return Results.Json(new
        {
            html,
            nextHref,
            shown = page.Shown,
            total = page.Total,
            added = page.Items.Count,
            lastDay = page.Items.Count > 0 ? dates.DayKey(page.Items[^1]) : day,
            lastGroup = cardMode && page.Items.Count > 0 ? dates.CardGroupKey(page.Items[^1], grouping, nowUtc) : group,
        });
    }
}
