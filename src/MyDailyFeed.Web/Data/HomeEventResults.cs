using Microsoft.EntityFrameworkCore;
using MyDailyFeed.Web.Data.Models;

namespace MyDailyFeed.Web.Data;

/// <summary>Home-page filtering only; stored schedules and results remain intact.</summary>
public static class HomeEventResults
{
    public static async Task<IReadOnlyList<EventItem>> WithoutFinalResultsAsync(
        WebScraperContext db, IReadOnlyList<EventItem> games)
    {
        if (games.Count == 0) return games;
        var keys = games.Select(Identity).Where(k => k != null).Cast<string>().Distinct().ToArray();
        var starts = games.Where(g => !g.TimeTbd && g.StartsAtUtc != null)
            .Select(g => g.StartsAtUtc!.Value).Distinct().ToArray();
        var results = await db.ResultGames.AsNoTracking()
            .Where(r => r.Completed && (keys.Contains("espn:" + r.League + ":" + r.EventId) || starts.Contains(r.StartsAtUtc)))
            .ToListAsync();
        var ids = games.Where(g => g.GameId != null).Select(g => g.GameId!.Value).Distinct().ToArray();
        var seasonResults = await db.SportsMatchResults.AsNoTracking()
            .Where(r => ids.Contains(r.GameId) && r.HomeScore >= 0 && r.AwayScore >= 0
                && (r.ScheduleStatus == "STATUS_FULL_TIME" || r.ScheduleStatus.StartsWith("STATUS_FINAL")))
            .ToListAsync();
        return games.Where(g => !results.Any(r => Matches(g, r)) && !seasonResults.Any(r => Matches(g, r))).ToList();
    }

    public static bool Matches(EventItem game, ResultGame result)
    {
        if (!result.Completed) return false;
        if (Identity(game) is { } key)
            return key == $"espn:{result.League}:{result.EventId}";

        // Big 12 IDs belong to a different provider. Require the sport, exact confirmed
        // start and both participants, including ESPN's college-name aliases.
        if (game.ProviderKey is not (null or "big12") || game.TimeTbd || game.StartsAtUtc != result.StartsAtUtc)
            return false;
        var sport = game.Sport switch
        {
            "Basketball" => "Men's Basketball", "Women's Soccer" => "Soccer",
            "Women's Volleyball" => "Volleyball", _ => game.Sport
        };
        var competition = ScheduleCoverage.Map("big12", sport);
        return competition is { } c && (c.Sport, c.League) == (result.Sport, result.League)
            && ((TeamNames.Matches(game.SchoolName, result.HomeName) && TeamNames.Matches(game.Opponent, result.AwayName))
                || (TeamNames.Matches(game.SchoolName, result.AwayName) && TeamNames.Matches(game.Opponent, result.HomeName)));
    }

    public static bool Matches(EventItem game, SportsMatchResult result) =>
        game.GameId == result.GameId && game.ProviderKey == result.ProviderKey
        && game.ProviderKey is not null && game.ProviderKey.StartsWith("espn-")
        && result.HomeScore >= 0 && result.AwayScore >= 0
        && (result.ScheduleStatus == "STATUS_FULL_TIME" || result.ScheduleStatus.StartsWith("STATUS_FINAL"));

    private static string? Identity(EventItem game)
    {
        if (game.CanonicalKey?.StartsWith("espn:", StringComparison.Ordinal) == true) return game.CanonicalKey;
        var league = game.ProviderKey switch
        {
            "espn-nwsl" => "usa.nwsl", "espn-mls" => "usa.1", "espn-championship" => "eng.2",
            "espn-epl" => "eng.1", "espn-wnba" => "wnba", "espn-mlb" => "mlb", "espn-nhl" => "nhl",
            _ => null
        };
        return league != null && game.GameId is { } id ? $"espn:{league}:{id}" : null;
    }
}
