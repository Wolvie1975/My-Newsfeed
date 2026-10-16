using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data;

public sealed record SportsTeamChoice(string Key, string Name, string Group);

public static class SportsTeamPreferences
{
    public static string Keys(params string?[] names) => System.Text.Json.JsonSerializer.Serialize(
        names.Select(TeamNames.Key).Where(k => k.Length > 0).Distinct());

    public static async Task<IReadOnlyList<SportsTeamChoice>> GetChoicesAsync(WebScraperContext db)
    {
        var providers = new[] { "espn-nfl", "espn-nhl", "espn-mlb", "espn-mls", "espn-nwsl", "espn-wnba", "espn-epl", "espn-championship" };
        var standings = await (from row in db.SportsStandings.AsNoTracking()
            join source in db.SportsStandingsSources.AsNoTracking() on row.SourceId equals source.Id
            where source.Enabled && row.IsCurrent && providers.Contains(source.ProviderKey)
            select new { Name = row.TeamName, Provider = source.ProviderKey }).Distinct().ToListAsync();
        var fixtures = await db.SportsEvents.AsNoTracking()
            .Where(e => e.SportsEventsType != null && e.SportsEventsType.Enabled && providers.Contains(e.ProviderKey!))
            .Select(e => new { e.SchoolName, e.Opponent, Provider = e.ProviderKey! }).Distinct().ToListAsync();
        static SportsTeamChoice Pro(string? name, string provider) => new(TeamNames.Key(name), name?.Trim() ?? "",
            provider switch { "espn-epl" => "Premier League", "espn-championship" => "EFL Championship", _ => provider.Replace("espn-", "").ToUpperInvariant() });
        return GetChoices().Concat(standings.Select(s => Pro(s.Name, s.Provider)))
            .Concat(fixtures.SelectMany(f => new[] { Pro(f.SchoolName, f.Provider), Pro(f.Opponent, f.Provider) }))
            .Where(c => c.Key.Length > 0).DistinctBy(c => (c.Group, c.Key))
            .OrderBy(c => c.Group).ThenBy(c => c.Name).ToList();
    }

    public static IReadOnlyList<SportsTeamChoice> GetChoices() => TeamNames.Big12Schools
        .Select(name => new SportsTeamChoice(TeamNames.Key(name), name, "Big 12 schools"))
        .OrderBy(c => c.Name).ToList();
}
