using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;

namespace myNewspaper.Web.Profiles;

/// <summary>
/// Turns household channels on for every profile (decision 11, handoff 4.2). A household channel is any YouTube feed
/// no request points at: the scraper's, the admin's, or one from before profiles. Each is rolled out once; a profile
/// that turns it off afterwards just deletes its follow, and the rollout row keeps it from coming back.
/// </summary>
public static class HouseholdChannels
{
    /// <summary>
    /// One pass. Returns how many feeds were rolled out. A request approval inserts its feed, the follow and the
    /// request's YoutubeVideoFeedId in one transaction, so this never sees a requested feed without its request.
    /// </summary>
    public static async Task<int> RunOnceAsync(WebScraperContext db, CancellationToken ct = default)
    {
        var rolledOut = await db.Database.SqlQuery<int>($"""
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;
            -- One pass at a time, so two runs can't both insert the same rollout row.
            EXEC sp_getapplock @Resource = N'myNewspaper.ChannelRollout', @LockMode = N'Exclusive', @LockOwner = N'Transaction';
            DECLARE @feeds TABLE (Id int PRIMARY KEY);
            INSERT @feeds (Id)
            SELECT y.ID FROM dbo.YoutubeVideoFeed y
            WHERE NOT EXISTS (SELECT 1 FROM dbo.MyDailyFeedChannelRollouts r WHERE r.YoutubeVideoFeedId = y.ID)
              AND NOT EXISTS (SELECT 1 FROM dbo.MyDailyFeedContentRequests q WHERE q.YoutubeVideoFeedId = y.ID);
            INSERT dbo.MyDailyFeedProfileChannelFollows (ProfileId, YoutubeVideoFeedId)
            SELECT p.Id, f.Id FROM dbo.MyDailyFeedProfiles p CROSS JOIN @feeds f
            WHERE NOT EXISTS (SELECT 1 FROM dbo.MyDailyFeedProfileChannelFollows c WHERE c.ProfileId = p.Id AND c.YoutubeVideoFeedId = f.Id);
            INSERT dbo.MyDailyFeedChannelRollouts (YoutubeVideoFeedId) SELECT Id FROM @feeds;
            COMMIT TRANSACTION;
            SELECT COUNT(*) AS Value FROM @feeds;
            """).ToListAsync(ct);
        return rolledOut.Single();
    }
}

/// <summary>Runs the rollout every 15 minutes, and soon after <see cref="Trigger"/> (the admin saving a feed).</summary>
public sealed class ChannelRolloutService(IDbContextFactory<WebScraperContext> factory, ILogger<ChannelRolloutService> log) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private readonly SemaphoreSlim wake = new(0, 1);

    public void Trigger()
    {
        if (wake.CurrentCount == 0) try { wake.Release(); } catch (SemaphoreFullException) { }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var db = await factory.CreateDbContextAsync(stoppingToken);
                var count = await HouseholdChannels.RunOnceAsync(db, stoppingToken);
                if (count > 0) log.LogInformation("Turned on {Count} household channels for every profile.", count);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                log.LogError(ex, "Household channel rollout failed; it will try again.");
            }
            try { await wake.WaitAsync(Interval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }
}
