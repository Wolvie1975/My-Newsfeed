using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Scores;

namespace MyNewsFeed.Web.Data;

public sealed record ResultItem(
    string EventId,
    string LeagueName,
    string LeagueTag,
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

    /// <summary>
    /// Enabled follows that have at least one finished game, for the filter: leagues first, then conferences, then
    /// teams, each by name. One out of season (college basketball in October) stays hidden until its first final, so
    /// the filter never offers an empty page; <paramref name="selected"/> is kept regardless, so a shared link to it
    /// still shows which follow it is.
    /// </summary>
    public static async Task<IReadOnlyList<FollowChip>> GetFollowsAsync(WebScraperContext db, int? selected = null)
    {
        var follows = await db.ResultFollows.AsNoTracking()
            .Where(f => f.Enabled)
            .Select(f => new { f.Id, f.Kind, f.Name, f.Sport, f.League, f.TeamId })
            .ToListAsync();

        // Which leagues and which teams in them have a finished game: a few hundred keys, read once.
        var finished = db.ResultGames.AsNoTracking().Where(g => g.Completed);
        var leaguesPlayed = (await finished.Select(g => g.Sport + "|" + g.League).Distinct().ToListAsync()).ToHashSet();
        var teamsPlayed = (await finished.Select(g => g.Sport + "|" + g.League + "|" + g.HomeTeamId)
            .Union(finished.Select(g => g.Sport + "|" + g.League + "|" + g.AwayTeamId))
            .ToListAsync()).ToHashSet();

        var conferenceIds = follows.Where(f => f.Kind == FollowKinds.Conference).Select(f => f.Id).ToList();
        var members = (await db.ResultFollowTeams.AsNoTracking()
            .Where(m => conferenceIds.Contains(m.FollowId))
            .Select(m => new { m.FollowId, m.TeamId })
            .ToListAsync()).ToLookup(m => m.FollowId, m => m.TeamId);

        return follows
            .Where(f => f.Id == selected || f.Kind switch
            {
                FollowKinds.League => leaguesPlayed.Contains(f.Sport + "|" + f.League),
                FollowKinds.Team => teamsPlayed.Contains(f.Sport + "|" + f.League + "|" + f.TeamId),
                FollowKinds.Conference => members[f.Id].Any(t => teamsPlayed.Contains(f.Sport + "|" + f.League + "|" + t)),
                _ => false,
            })
            .OrderBy(f => FollowKinds.Order(f.Kind))
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
    /// matches every game in it; a followed team matches its games in that league; a followed conference matches the
    /// games of its member schools in that league, non-conference games included.
    /// </summary>
    public static async Task<ResultsPage> GetResultsAsync(WebScraperContext db, int? followId, int limit = Limit)
    {
        var follows = await db.ResultFollows.AsNoTracking()
            .Where(f => f.Enabled && (followId == null || f.Id == followId))
            .Select(f => new { f.Id, f.Kind, f.Sport, f.League, f.TeamId })
            .ToListAsync();

        // A conference is its member schools, each matched like a followed team.
        var conferences = follows.Where(f => f.Kind == FollowKinds.Conference).ToDictionary(f => f.Id);
        var members = conferences.Count == 0 ? [] : await db.ResultFollowTeams.AsNoTracking()
            .Where(m => conferences.Keys.Contains(m.FollowId))
            .Select(m => new { m.FollowId, m.TeamId })
            .ToListAsync();

        // Keys rather than a chain of ORs, so the query has a fixed shape however many follows there are.
        var leagueKeys = follows.Where(f => f.Kind == FollowKinds.League).Select(f => f.Sport + "|" + f.League).Distinct().ToList();
        var teamKeys = follows.Where(f => f.Kind == FollowKinds.Team).Select(f => f.Sport + "|" + f.League + "|" + f.TeamId)
            .Concat(members.Select(m => conferences[m.FollowId].Sport + "|" + conferences[m.FollowId].League + "|" + m.TeamId))
            .Distinct().ToList();
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

        var items = rows.Select(g => new ResultItem(g.EventId, EspnLeagues.NameOf(g.Sport, g.League), EspnLeagues.TagOf(g.Sport, g.League), g.StartsAtUtc, g.StatusText,
                g.HomeName, g.HomeLogoUrl, g.HomeScore, g.HomeShootout,
                g.AwayName, g.AwayLogoUrl, g.AwayScore, g.AwayShootout, g.WinnerSide, g.Venue))
            .ToList();

        return new ResultsPage(items, total);
    }
}
