using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Scores;

namespace MyNewsFeed.Web.Data;

public sealed record ResultItem(
    string EventId,
    string LeagueName,
    DateTime StartsAtUtc,
    string? StatusText,
    string HomeName,
    string? HomeLogoUrl,
    int? HomeScore,
    int? HomeShootout,
    string AwayName,
    string? AwayLogoUrl,
    int? AwayScore,
    int? AwayShootout,
    string? WinnerSide,
    string? Venue);

public sealed record FollowChip(int Id, string Label);

/// <param name="Total">Every finished game matching the filter (the list itself is capped).</param>
public sealed record ResultsPage(IReadOnlyList<ResultItem> Items, int Total);

/// <summary>Read side of the public results page: finished games of the teams and leagues followed.</summary>
public static class ResultsQuery
{
    /// <summary>A safety cap on one page; older results are cut off rather than paged.</summary>
    public const int Limit = 200;

    /// <summary>Enabled follows, for the filter chips: leagues first, then teams, each by name.</summary>
    public static async Task<IReadOnlyList<FollowChip>> GetFollowsAsync(WebScraperContext db)
    {
        var follows = await db.ResultFollows.AsNoTracking()
            .Where(f => f.Enabled)
            .Select(f => new { f.Id, f.Kind, f.Name, f.Sport, f.League })
            .ToListAsync();

        return follows
            .OrderBy(f => f.Kind == FollowKinds.League ? 0 : 1)
            .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(f => EspnLeagues.NameOf(f.Sport, f.League), StringComparer.OrdinalIgnoreCase)
            .Select(f => new FollowChip(f.Id, EspnLeagues.FollowLabel(f.Kind, f.Name, f.Sport, f.League)))
            .ToList();
    }

    /// <summary>When results were last refreshed, or null when never.</summary>
    public static async Task<DateTime?> GetLastSyncedAsync(WebScraperContext db) =>
        await db.ResultFollows.AsNoTracking().Where(f => f.Enabled).MaxAsync(f => f.LastSyncedAt);

    /// <summary>
    /// Finished games, newest first, of every enabled follow or only <paramref name="followId"/>. A followed league
    /// matches every game in it; a followed team matches its games in that league.
    /// </summary>
    public static async Task<ResultsPage> GetResultsAsync(WebScraperContext db, int? followId, int limit = Limit)
    {
        var follows = await db.ResultFollows.AsNoTracking()
            .Where(f => f.Enabled && (followId == null || f.Id == followId))
            .Select(f => new { f.Kind, f.Sport, f.League, f.TeamId })
            .ToListAsync();

        // Keys rather than a chain of ORs, so the query has a fixed shape however many follows there are.
        var leagueKeys = follows.Where(f => f.Kind == FollowKinds.League).Select(f => f.Sport + "|" + f.League).Distinct().ToList();
        var teamKeys = follows.Where(f => f.Kind == FollowKinds.Team).Select(f => f.Sport + "|" + f.League + "|" + f.TeamId).Distinct().ToList();
        if (leagueKeys.Count == 0 && teamKeys.Count == 0)
        {
            return new ResultsPage([], 0);
        }

        var query = db.ResultGames.AsNoTracking()
            .Where(g => g.Completed)
            .Where(g => leagueKeys.Contains(g.Sport + "|" + g.League)
                || teamKeys.Contains(g.Sport + "|" + g.League + "|" + g.HomeTeamId)
                || teamKeys.Contains(g.Sport + "|" + g.League + "|" + g.AwayTeamId));

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(g => g.StartsAtUtc)
            .ThenBy(g => g.League)
            .Take(limit)
            .ToListAsync();

        var items = rows.Select(g => new ResultItem(g.EventId, EspnLeagues.NameOf(g.Sport, g.League), g.StartsAtUtc, g.StatusText,
                g.HomeName, g.HomeLogoUrl, g.HomeScore, g.HomeShootout,
                g.AwayName, g.AwayLogoUrl, g.AwayScore, g.AwayShootout, g.WinnerSide, g.Venue))
            .ToList();

        return new ResultsPage(items, total);
    }
}
