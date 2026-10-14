using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Web.Data;

public sealed record TvShowChoice(int Id, string Title);
public sealed record TvEpisodeItem(int ProviderId, string SeriesTitle, string? Poster, string? Platform,
    string? Title, int? Season, int? Number, bool Special, DateOnly Date, DateTime? StartsUtc, string Url, int ShowId = 0);
public sealed record TvSchedule(IReadOnlyList<TvEpisodeItem> Items, int Total, IReadOnlyList<TvShowChoice> Shows,
    IReadOnlyList<string> Unannounced, DateTime? Updated);

public static class TvQuery
{
    public static async Task<TvSchedule> GetAsync(WebScraperContext db, FeedDates dates, DateOnly start, int? show, int days = 7)
    {
        var end = start.AddDays(days);
        var window = new ResultsWindow(ResultsWindow.ForDay(start, dates.Zone).StartUtc, ResultsWindow.ForDay(end, dates.Zone).StartUtc);
        var enabled = db.TvTrackedSeries.AsNoTracking().Where(t => t.Enabled);
        var shows = await enabled.OrderBy(t => t.Series != null ? t.Series.Title : t.ExpectedTitle)
            .Select(t => new TvShowChoice(t.TvmazeShowId, t.Series != null ? t.Series.Title : t.ExpectedTitle)).ToListAsync();
        var episodes = db.TvEpisodes.AsNoTracking().Where(e => enabled.Any(t => t.SeriesId == e.SeriesId));
        if (show is { } id) episodes = episodes.Where(e => e.Series.TvmazeShowId == id);
        var matching = episodes.Where(e => e.IsDateOnly
            ? e.Airdate >= start && e.Airdate < end
            : e.StartsAtUtc >= window.StartUtc && e.StartsAtUtc < window.EndUtc);
        var count = await matching.CountAsync();
        // SQL filters before fetching; date-only releases retain their source date, timed releases use the display zone.
        var rows = await matching.OrderBy(e => e.Airdate).ThenBy(e => e.Series.Title).ThenBy(e => e.SeasonNumber)
            .ThenBy(e => e.EpisodeNumber).ThenBy(e => e.TvmazeEpisodeId).Take(300)
            .Select(e => new { e.TvmazeEpisodeId, Show = e.Series.Title, e.Series.PosterUrl,
                ShowId = e.Series.TvmazeShowId, Platform = e.Series.WebChannelName ?? e.Series.NetworkName, e.Title, e.SeasonNumber, e.EpisodeNumber,
                e.IsSpecial, e.Airdate, e.IsDateOnly, e.StartsAtUtc, e.SourceUrl }).ToListAsync();
        var items = rows.Select(e => new TvEpisodeItem(e.TvmazeEpisodeId, e.Show, e.PosterUrl, e.Platform, e.Title,
            e.SeasonNumber, e.EpisodeNumber, e.IsSpecial, e.IsDateOnly ? e.Airdate!.Value : DateOnly.Parse(dates.DayKey(e.StartsAtUtc!.Value)),
            e.IsDateOnly ? null : e.StartsAtUtc, e.SourceUrl, e.ShowId))
            .OrderBy(e => e.Date).ThenBy(e => e.StartsUtc == null).ThenBy(e => e.StartsUtc)
            .ThenBy(e => e.SeriesTitle).ThenBy(e => e.Season).ThenBy(e => e.Number).ThenBy(e => e.ProviderId).ToList();
        var today = dates.Today;
        var unannounced = await enabled.Where(t => (show == null || t.TvmazeShowId == show)
                && !db.TvEpisodes.Any(e => e.SeriesId == t.SeriesId && e.Airdate >= today))
            .OrderBy(t => t.ExpectedTitle).Select(t => t.Series != null ? t.Series.Title : t.ExpectedTitle).ToListAsync();
        return new(items, count, shows, unannounced, await enabled.MaxAsync(t => t.LastSuccessAt));
    }

    public static async Task<IReadOnlyList<TvEpisodeItem>> GetOnDateAsync(WebScraperContext db, FeedDates dates, DateOnly day) =>
        (await GetAsync(db, dates, day, null, days: 1)).Items;

    public static async Task FollowAsync(WebScraperContext db, int providerId, string title)
    {
        if (providerId <= 0 || string.IsNullOrWhiteSpace(title) || title.Length > 500) throw new ArgumentException("Invalid series.");
        var existing = await db.TvTrackedSeries.SingleOrDefaultAsync(t => t.TvmazeShowId == providerId);
        if (existing is not null) { existing.Enabled = true; await db.SaveChangesAsync(); return; }
        var series = await db.TvSeries.AsNoTracking().SingleOrDefaultAsync(s => s.TvmazeShowId == providerId);
        var added = new TvTrackedSeries { TvmazeShowId = providerId, ExpectedTitle = title, Enabled = true, SeriesId = series?.Id };
        db.Add(added);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (AdminData.IsUniqueViolation(ex))
        {
            db.Entry(added).State = EntityState.Detached;
            // Another request may have followed it first; preserve the scraper's cache, lease, and status.
            await db.TvTrackedSeries.Where(t => t.TvmazeShowId == providerId).ExecuteUpdateAsync(s => s.SetProperty(t => t.Enabled, true));
        }
    }

    public static Task<int> SetEnabledAsync(WebScraperContext db, int trackingId, bool enabled) =>
        db.TvTrackedSeries.Where(t => t.Id == trackingId).ExecuteUpdateAsync(s => s.SetProperty(t => t.Enabled, enabled));
}
