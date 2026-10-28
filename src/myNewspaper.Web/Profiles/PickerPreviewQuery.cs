using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;
using myNewspaper.Web.Saved;

namespace myNewspaper.Web.Profiles;

/// <summary>One column on the edition picker. Numbers only: never a show or channel name (handoff 4.3 and 6.1).</summary>
public sealed record PickerEdition(int Id, string Name, string Color, bool Locked, bool NeedsPin, DateTime? LastReadAt,
    int EpisodesToday, int NewVideos, int Saved);

public sealed record PickerPage(IReadOnlyList<PickerEdition> Editions, string? Lead);

/// <summary>
/// The edition picker's previews. Other people's columns are visible to anyone at the picker, so this returns counts and
/// never selects a show, channel or saved-article title; a template change can't leak one because the names aren't here.
/// </summary>
public static class PickerPreviewQuery
{
    /// <summary>Videos are "new" since the last read, looking back at most this far.</summary>
    public static readonly TimeSpan NewVideoWindow = TimeSpan.FromDays(7);

    public static async Task<PickerPage> GetAsync(WebScraperContext db, SharedArticles saved, FeedDates dates, DateTime nowUtc,
        CancellationToken ct = default)
    {
        var profiles = await db.Profiles.AsNoTracking().Where(p => !p.Hidden).OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .Select(p => new { p.Id, p.Name, p.Color, Locked = p.LockedAt != null, NeedsPin = p.PinHash == null, p.LastReadAt })
            .ToListAsync(ct);
        var today = dates.TodayAt(nowUtc);
        var day = ResultsWindow.ForDay(today, dates.Zone);
        var editions = new List<PickerEdition>(profiles.Count);
        foreach (var p in profiles)
        {
            var viewer = Viewer.ForProfile(p.Id);
            var shows = FollowQuery.TrackedSeries(db, viewer);
            var episodes = await db.TvEpisodes.AsNoTracking()
                .Where(e => shows.Any(t => t.SeriesId == e.SeriesId))
                .CountAsync(e => e.IsDateOnly ? e.Airdate == today : e.StartsAtUtc >= day.StartUtc && e.StartsAtUtc < day.EndUtc, ct);
            var since = p.LastReadAt is { } read && nowUtc - read < NewVideoWindow ? read : nowUtc - NewVideoWindow;
            var videos = await FollowQuery.Videos(db, viewer).CountAsync(v => v.PublishedAt > since, ct);
            var savedCount = (await saved.Read(p.Id, ct)).Count;
            editions.Add(new(p.Id, p.Name, p.Color, p.Locked, p.NeedsPin, p.LastReadAt, episodes, videos, savedCount));
        }
        string? lead = null;
        if (editions.Count > 0)
        {
            try { lead = (await HomeLeadQuery.GetAsync(db, nowUtc)).Article?.Title; }
            catch (Exception ex) when (DataFailures.IsReadFailure(ex)) { }
        }
        return new(editions, lead);
    }
}
