using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Web.Scores;

/// <param name="Synced">Follows whose games were fetched and saved.</param>
/// <param name="Failed">Follows with a failed request; each has its LastError set.</param>
/// <param name="Games">Game rows added or changed.</param>
/// <param name="Requests">Requests made to ESPN.</param>
public sealed record ScoresSyncReport(int Synced, int Failed, int Games, int Requests);

/// <summary>Fetches followed teams' and leagues' games from ESPN and upserts them into dbo.ResultGames.</summary>
public sealed class ScoresSync(
    IDbContextFactory<WebScraperContext> factory,
    EspnClient espn,
    IOptions<ScoresOptions> options,
    ILogger<ScoresSync> log)
{
    /// <summary>ESPN's scoreboard days are US Eastern calendar days.</summary>
    private static readonly TimeZoneInfo Eastern = FindEastern();

    /// <summary>A pause between requests, so a backfill does not hit ESPN in a burst.</summary>
    private static readonly TimeSpan Politeness = TimeSpan.FromMilliseconds(300);

    private readonly ScoresOptions settings = options.Value;

    /// <summary>The background run: every enabled follow not synced within SyncIntervalHours, oldest first.</summary>
    public async Task<ScoresSyncReport> SyncDueAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var staleBefore = utcNow.AddHours(-settings.SyncIntervalHours);
        List<int> due;
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            due = await db.ResultFollows.AsNoTracking()
                .Where(f => f.Enabled && (f.LastSyncedAt == null || f.LastSyncedAt < staleBefore))
                .OrderBy(f => f.LastSyncedAt ?? DateTime.MinValue)
                .Select(f => f.Id)
                .ToListAsync(ct);
        }

        return await SyncAsync(due, utcNow, ct);
    }

    /// <summary>An admin's "Sync now": one follow, or every enabled one, regardless of when it was last synced.</summary>
    public async Task<ScoresSyncReport> SyncNowAsync(int? followId, CancellationToken ct = default)
    {
        List<int> ids;
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            ids = await db.ResultFollows.AsNoTracking()
                .Where(f => followId == null ? f.Enabled : f.Id == followId)
                .Select(f => f.Id)
                .ToListAsync(ct);
        }

        return await SyncAsync(ids, DateTime.UtcNow, ct);
    }

    private async Task<ScoresSyncReport> SyncAsync(List<int> followIds, DateTime utcNow, CancellationToken ct)
    {
        int synced = 0, failed = 0, games = 0, requests = 0;

        foreach (var id in followIds)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var follow = await db.ResultFollows.FindAsync([id], ct);
            if (follow is null)
            {
                continue;
            }

            var fetched = new List<EspnGame>();
            string? error = null;
            if (follow.Kind == FollowKinds.Team)
            {
                // Regular season and postseason, two requests; a postseason not yet scheduled is simply empty.
                foreach (var seasonType in EspnClient.ScheduleSeasonTypes)
                {
                    if (requests > 0)
                    {
                        await Task.Delay(Politeness, ct);
                    }

                    var result = await espn.GetTeamScheduleAsync(follow.Sport, follow.League, follow.TeamId!, seasonType, ct);
                    requests++;
                    if (!result.Ok)
                    {
                        error = result.Error;
                        break;
                    }

                    fetched.AddRange(result.Value!);
                }
            }
            else
            {
                foreach (var day in DaysToFetch(follow.LastSyncedAt, TodayEastern(utcNow), settings.BackfillDays))
                {
                    if (requests > 0)
                    {
                        await Task.Delay(Politeness, ct);
                    }

                    var result = await espn.GetScoreboardAsync(follow.Sport, follow.League, day, ct);
                    requests++;
                    if (!result.Ok)
                    {
                        error = result.Error;
                        break;
                    }

                    fetched.AddRange(result.Value!);
                }
            }

            // Games already fetched are kept even when a later day failed; LastSyncedAt only moves on full success, so
            // the failed days are fetched again next time.
            games += await UpsertGamesAsync(db, follow.Sport, follow.League, fetched, utcNow, ct);
            if (error is null)
            {
                follow.LastSyncedAt = utcNow;
                follow.LastError = null;
                synced++;
            }
            else
            {
                follow.LastError = error.Length > 1000 ? error[..1000] : error;
                failed++;
                log.LogWarning("Results sync of {Name} ({Sport}/{League}) failed: {Error}", follow.Name, follow.Sport, follow.League, error);
            }

            await db.SaveChangesAsync(ct);
        }

        if (followIds.Count > 0)
        {
            log.LogInformation("Results sync: {Synced} synced, {Failed} failed, {Games} games changed, {Requests} ESPN requests.",
                synced, failed, games, requests);
        }

        return new(synced, failed, games, requests);
    }

    /// <summary>
    /// The scoreboard days a league follow needs, oldest first: from the day before its last sync (a late game finishes
    /// after midnight) to today, or the last <paramref name="backfillDays"/> days when it has never synced or was
    /// switched off for longer than that.
    /// </summary>
    public static IReadOnlyList<DateOnly> DaysToFetch(DateTime? lastSyncedUtc, DateOnly todayEastern, int backfillDays)
    {
        var earliest = todayEastern.AddDays(-Math.Max(0, backfillDays));
        var from = lastSyncedUtc is { } last ? TodayEastern(last).AddDays(-1) : earliest;
        if (from < earliest)
        {
            from = earliest;
        }

        var days = new List<DateOnly>();
        for (var day = from; day <= todayEastern; day = day.AddDays(1))
        {
            days.Add(day);
        }

        return days;
    }

    public static DateOnly TodayEastern(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Eastern));

    /// <summary>
    /// Adds new games and refreshes known ones (by sport, league and ESPN event id); saving is left to the caller.
    /// Returns how many rows were added or changed. Games are never deleted: a postponed game keeps its row.
    /// </summary>
    public static async Task<int> UpsertGamesAsync(
        WebScraperContext db, string sport, string league, IReadOnlyList<EspnGame> games, DateTime utcNow, CancellationToken ct = default)
    {
        var byId = games.DistinctBy(g => g.EventId).ToDictionary(g => g.EventId);
        var existing = new Dictionary<string, ResultGame>();
        foreach (var chunk in byId.Keys.Chunk(1000))
        {
            var ids = chunk;
            foreach (var row in await db.ResultGames.Where(g => g.Sport == sport && g.League == league && ids.Contains(g.EventId)).ToListAsync(ct))
            {
                existing[row.EventId] = row;
            }
        }

        foreach (var game in byId.Values)
        {
            if (!existing.TryGetValue(game.EventId, out var row))
            {
                row = new ResultGame { Sport = sport, League = league, EventId = game.EventId, FirstSeenAt = utcNow };
                db.ResultGames.Add(row);
            }

            Apply(game, row);
            row.LastSeenAt = utcNow;
        }

        // LastSeenAt changes on every row, so count real changes from the data columns only.
        db.ChangeTracker.DetectChanges();
        return db.ChangeTracker.Entries<ResultGame>().Count(e =>
            e.State == EntityState.Added
            || (e.State == EntityState.Modified && e.Properties.Any(p => p.IsModified && p.Metadata.Name != nameof(ResultGame.LastSeenAt))));
    }

    private static void Apply(EspnGame g, ResultGame row)
    {
        row.StartsAtUtc = g.StartsAtUtc;
        row.State = Cut(g.State, 10)!;
        row.Completed = g.Completed;
        row.StatusText = Cut(g.StatusText, 60);
        row.HomeTeamId = Cut(g.Home.TeamId, 20)!;
        row.HomeName = Cut(g.Home.Name, 200)!;
        row.HomeLogoUrl = Cut(g.Home.LogoUrl, 2048);
        row.HomeScore = g.Home.Score;
        row.HomeShootout = g.Home.Shootout;
        row.AwayTeamId = Cut(g.Away.TeamId, 20)!;
        row.AwayName = Cut(g.Away.Name, 200)!;
        row.AwayLogoUrl = Cut(g.Away.LogoUrl, 2048);
        row.AwayScore = g.Away.Score;
        row.AwayShootout = g.Away.Shootout;
        row.WinnerSide = g.WinnerSide;
        row.Venue = Cut(g.Venue, 300);
    }

    private static string? Cut(string? value, int max) => value is null || value.Length <= max ? value : value[..max];

    private static TimeZoneInfo FindEastern()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/New_York"); }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }
}

/// <summary>
/// Runs <see cref="ScoresSync.SyncDueAsync"/> shortly after startup and then every 30 minutes. A check that finds
/// nothing due costs one database query and no ESPN request; ESPN is only asked about follows older than
/// Results:SyncIntervalHours.
/// </summary>
public sealed class ScoresSyncService(IServiceScopeFactory scopes, ILogger<ScoresSyncService> log) : BackgroundService
{
    private static readonly TimeSpan StartDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan CheckEvery = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartDelay, stoppingToken);
            using var timer = new PeriodicTimer(CheckEvery);
            do
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<ScoresSync>().SyncDueAsync(DateTime.UtcNow, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // A database outage must not stop the service for good; the next tick tries again.
                    log.LogError(ex, "Results sync run failed.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
