using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Web.Profiles;

public sealed record ProfileFollows(IReadOnlySet<int> TvTrackedSeriesIds, IReadOnlySet<int> YoutubeVideoFeedIds);

/// <summary>
/// Every "what does someone follow" read (profiles handoff 4.3). Public pages pass the current profile; there is
/// deliberately no admin method, because admin pages never read follows.
/// </summary>
public static class FollowQuery
{
    public static async Task<ProfileFollows> ForProfile(WebScraperContext db, int profileId, CancellationToken ct = default)
    {
        var shows = await db.ProfileTvFollows.AsNoTracking().Where(f => f.ProfileId == profileId)
            .Select(f => f.TvTrackedSeriesId).ToListAsync(ct);
        var channels = await db.ProfileChannelFollows.AsNoTracking().Where(f => f.ProfileId == profileId)
            .Select(f => f.YoutubeVideoFeedId).ToListAsync(ct);
        return new(shows.ToHashSet(), channels.ToHashSet());
    }

    /// <summary>The enabled tracked series this viewer sees. Apply before any show or search filter.</summary>
    public static IQueryable<TvTrackedSeries> TrackedSeries(WebScraperContext db, Viewer viewer)
    {
        var enabled = db.TvTrackedSeries.AsNoTracking().Where(t => t.Enabled);
        if (viewer.IsHousehold) return enabled;
        var id = viewer.FilterId;
        return enabled.Where(t => db.ProfileTvFollows.Any(f => f.ProfileId == id && f.TvTrackedSeriesId == t.Id));
    }

    /// <summary>
    /// The videos this viewer sees. Apply before any channel or search filter. A profile only sees videos linked to a
    /// feed it follows, so a video the scraper hasn't linked to a feed yet is shown to nobody.
    /// </summary>
    public static IQueryable<YouTubeVideo> Videos(WebScraperContext db, Viewer viewer)
    {
        var videos = db.YouTubeVideos.AsNoTracking();
        if (viewer.IsHousehold) return videos;
        var id = viewer.FilterId;
        return videos.Where(v => db.ProfileChannelFollows.Any(f => f.ProfileId == id && f.YoutubeVideoFeedId == v.YoutubeVideoFeedId));
    }
}
