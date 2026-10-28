using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Web.Profiles;

public sealed record MyShow(int TrackingId, int TvmazeShowId, string Title);
public sealed record MyChannel(int FeedId, string Name, bool Following);

/// <summary>What Settings → Shows &amp; channels lists for the open profile (handoff 4.1, 4.2 and 6.3).</summary>
public sealed record MyFollowsPage(IReadOnlyList<MyShow> Shows, IReadOnlyList<MyChannel> Household, IReadOnlyList<MyChannel> Requested,
    IReadOnlyList<MyChannel> Picks);

/// <summary>
/// The open profile's own follows, and the changes it can make to them. Only household channels (rolled out to
/// everyone), household picks and channels this profile requested are ever listed or followable: another profile's
/// requested channel never appears, so it can't be discovered from here (4.3).
/// </summary>
public static class MyFollows
{
    private static string ChannelName(YoutubeVideoFeed f) => string.IsNullOrWhiteSpace(f.ChannelName) ? f.ChannelId : f.ChannelName.Trim();

    public static async Task<MyFollowsPage> GetAsync(WebScraperContext db, int profileId, CancellationToken ct = default)
    {
        var shows = await FollowQuery.TrackedSeries(db, Viewer.ForProfile(profileId))
            .Select(t => new MyShow(t.Id, t.TvmazeShowId, t.Series != null ? t.Series.Title : t.ExpectedTitle))
            .ToListAsync(ct);
        var followed = (await FollowQuery.ForProfile(db, profileId, ct)).YoutubeVideoFeedIds;
        var rolledOut = await db.ChannelRollouts.AsNoTracking().Select(r => r.YoutubeVideoFeedId).ToListAsync(ct);
        var requested = await db.ContentRequests.AsNoTracking()
            .Where(r => r.ProfileId == profileId && r.YoutubeVideoFeedId != null).Select(r => r.YoutubeVideoFeedId!.Value).ToListAsync(ct);
        var feeds = await db.YoutubeVideoFeeds.AsNoTracking()
            .Where(f => rolledOut.Contains(f.Id) || f.IsHouseholdPick || requested.Contains(f.Id) || followed.Contains(f.Id))
            .ToListAsync(ct);
        MyChannel Row(YoutubeVideoFeed f) => new(f.Id, ChannelName(f), followed.Contains(f.Id));
        var byName = StringComparer.OrdinalIgnoreCase;
        return new(
            [.. shows.OrderBy(s => s.Title, byName)],
            [.. feeds.Where(f => rolledOut.Contains(f.Id)).Select(Row).OrderBy(c => c.Name, byName)],
            // Followed channels that aren't household channels: this profile's own requests (or picks it followed).
            [.. feeds.Where(f => !rolledOut.Contains(f.Id) && followed.Contains(f.Id)).Select(Row).OrderBy(c => c.Name, byName)],
            [.. feeds.Where(f => f.IsHouseholdPick && !rolledOut.Contains(f.Id) && !followed.Contains(f.Id)).Select(Row).OrderBy(c => c.Name, byName)]);
    }

    /// <summary>
    /// Follow or unfollow a channel. Following is allowed only for a household channel, a household pick, or one this
    /// profile requested; anything else answers false, exactly as an unknown ID does.
    /// </summary>
    public static async Task<bool> SetChannelAsync(WebScraperContext db, int profileId, int feedId, bool follow, CancellationToken ct = default)
    {
        if (!follow)
        {
            await db.ProfileChannelFollows.Where(f => f.ProfileId == profileId && f.YoutubeVideoFeedId == feedId).ExecuteDeleteAsync(ct);
            return true;
        }
        var allowed = await db.YoutubeVideoFeeds.AnyAsync(f => f.Id == feedId && (f.IsHouseholdPick
            || db.ChannelRollouts.Any(r => r.YoutubeVideoFeedId == f.Id)
            || db.ContentRequests.Any(r => r.ProfileId == profileId && r.YoutubeVideoFeedId == f.Id)), ct);
        if (!allowed) return false;
        if (!await db.ProfileChannelFollows.AnyAsync(f => f.ProfileId == profileId && f.YoutubeVideoFeedId == feedId, ct))
        {
            var row = new ProfileChannelFollow { ProfileId = profileId, YoutubeVideoFeedId = feedId };
            db.ProfileChannelFollows.Add(row);
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException ex) when (AdminData.IsUniqueViolation(ex)) { }   // a double tap followed it already
            // Unfollowing deletes in SQL, so don't keep a tracked copy that a later follow would collide with.
            db.Entry(row).State = EntityState.Detached;
        }
        return true;
    }

    /// <summary>Follow a TVmaze show: track it for the scraper if nobody does yet, then follow it for this profile.</summary>
    public static async Task FollowShowAsync(WebScraperContext db, int profileId, int tvmazeShowId, string title, CancellationToken ct = default)
    {
        await TvQuery.FollowAsync(db, tvmazeShowId, title);
        var trackingId = await db.TvTrackedSeries.Where(t => t.TvmazeShowId == tvmazeShowId).Select(t => t.Id).SingleAsync(ct);
        if (await db.ProfileTvFollows.AnyAsync(f => f.ProfileId == profileId && f.TvTrackedSeriesId == trackingId, ct)) return;
        var follow = new ProfileTvFollow { ProfileId = profileId, TvTrackedSeriesId = trackingId };
        db.ProfileTvFollows.Add(follow);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (AdminData.IsUniqueViolation(ex)) { }
        db.Entry(follow).State = EntityState.Detached;
    }

    /// <summary>
    /// Unfollow a show. When nobody follows it any more the scraper stops collecting it (the same as the admin's Stop
    /// following); its history is kept, so following it again picks up where it was.
    /// </summary>
    public static async Task<bool> UnfollowShowAsync(WebScraperContext db, int profileId, int trackingId, CancellationToken ct = default)
    {
        var removed = await db.ProfileTvFollows.Where(f => f.ProfileId == profileId && f.TvTrackedSeriesId == trackingId).ExecuteDeleteAsync(ct);
        if (removed > 0 && !await db.ProfileTvFollows.AnyAsync(f => f.TvTrackedSeriesId == trackingId, ct))
            await TvQuery.SetEnabledAsync(db, trackingId, false);
        return removed > 0;
    }
}
