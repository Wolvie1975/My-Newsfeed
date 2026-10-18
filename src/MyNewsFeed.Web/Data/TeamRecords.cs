using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Web.Data;

public sealed record TeamRecordValue(string Text, DateTime? UpdatedAt, bool Stale, bool IsCollege = false);

/// <summary>One standings snapshot per page. Provider-scoped IDs never cross into Big 12 identities.</summary>
public sealed class TeamRecords
{
    private readonly List<(string Provider, string Sport, string Id, string Name, TeamRecordValue Record)> entries = [];
    public static TeamRecords Empty => new();

    public static async Task<TeamRecords> LoadAsync(WebScraperContext db, DateOnly today, MyNewsFeed.Web.Scores.LeaguePhases phases)
    {
        var sources = await db.SportsStandingsSources.AsNoTracking().Where(s => s.Enabled).ToListAsync();
        sources = sources.Where(s => phases.IsRegular(s, DateTime.UtcNow)).ToList();
        if (sources.Count == 0) return Empty;
        var ids = sources.Select(s => s.Id).ToArray();
        var rows = await db.SportsStandings.AsNoTracking().Where(r => r.IsCurrent && ids.Contains(r.SourceId)).ToListAsync();
        return Build(sources, rows, today, DateTime.UtcNow);
    }

    public static TeamRecords Build(IEnumerable<SportsStandingsSource> sources, IEnumerable<SportsStanding> rows, DateOnly today, DateTime now)
    {
        var result = new TeamRecords();
        foreach (var source in sources.Where(s => s.Enabled))
        foreach (var row in rows.Where(r => r.SourceId == source.Id && r.IsCurrent
            && (source.ProviderKey == "big12" ? CollegeSeasons.Matches(r.Season, source.Sport, today)
                : !string.IsNullOrWhiteSpace(source.LatestSeason) && r.Season == source.LatestSeason)))
        {
            var text = Format(source.ProviderKey, source.Sport, StandingsQuery.ParseStatistics(row.StatsJson, row.Id, NullLogger.Instance));
            if (text is null) continue;
            var updated = row.FetchedAt ?? source.LastSuccessAt;
            var stale = !string.IsNullOrWhiteSpace(source.LastError) || updated is null
                || now - DateTime.SpecifyKind(updated.Value, DateTimeKind.Utc) > TimeSpan.FromHours(2);
            result.entries.Add((source.ProviderKey, NormalizeSport(source.Sport), row.TeamId, TeamNames.Key(row.TeamName), new(text, updated, stale, source.ProviderKey == "big12")));
        }
        return result;
    }

    public TeamRecordValue? Find(string? provider, string? sport, string? name, string? id = null)
    {
        provider ??= "big12";
        if (provider == "espn-wnba") sport = "Women's Basketball";
        var scoped = entries.Where(e => e.Provider == provider && e.Sport == NormalizeSport(sport));
        // ESPN result IDs are not Big 12 school IDs. College matches use known aliases within the sport.
        var matches = provider != "big12" && !string.IsNullOrWhiteSpace(id)
            ? scoped.Where(e => e.Id == id) : scoped.Where(e => e.Name != "" && e.Name == TeamNames.Key(name));
        var records = matches.Select(e => e.Record).ToList();
        return records.Select(r => r.Text).Distinct().Count() == 1
            ? records.OrderByDescending(r => r.UpdatedAt).First() : null;
    }

    private static string NormalizeSport(string? sport) => sport switch
    {
        "Basketball" => "Men's Basketball", "Women's Soccer" => "Soccer", "Women's Volleyball" => "Volleyball", _ => sport ?? ""
    };

    public static string? Format(string provider, string sport, IReadOnlyList<StandingStatistic> stats)
    {
        static string? Record(string? text)
        {
            var parts = text?.Replace('–', '-').Split('-');
            return parts is { Length: 2 or 3 } && parts.All(p => int.TryParse(p, out var n) && n >= 0)
                ? string.Join('-', parts.Select(p => int.Parse(p).ToString(CultureInfo.InvariantCulture))) : null;
        }
        if (provider == "big12")
        {
            var overall = Record(stats.FirstOrDefault(s => s.DisplayName is "Overall" or "Overall Record")?.DisplayValue);
            var conference = Record(stats.FirstOrDefault(s => s.DisplayName is "Conf" or "Big 12 Record" or "Conference Record")?.DisplayValue);
            var parts = new[] { overall, conference is null ? null : $"({conference})" }.Where(p => p != null);
            var label = string.Join(" ", parts);
            return label.Length == 0 ? null : label;
        }
        string? Count(params string[] names)
        {
            var stat = stats.FirstOrDefault(s => names.Contains(s.Name, StringComparer.OrdinalIgnoreCase));
            if (stat?.Value is { } number && number >= 0 && number <= int.MaxValue && Math.Truncate(number) == number)
                return ((int)number).ToString(CultureInfo.InvariantCulture);
            return int.TryParse(stat?.DisplayValue, out var value) && value >= 0 ? value.ToString(CultureInfo.InvariantCulture) : null;
        }
        var wins = Count("wins"); var losses = Count("losses");
        if (wins is null || losses is null) return null;
        var third = provider == "espn-nhl" ? Count("otLosses", "overtimeLosses")
            : sport == "Soccer" || provider == "espn-nfl" ? Count("ties", "draws") : null;
        if (provider == "espn-nhl" || sport == "Soccer" || provider == "espn-nfl")
            return third is null ? null : $"{wins}–{losses}–{third} " + (provider == "espn-nhl" ? "W–L–OT" : provider == "espn-nfl" ? "W–L–T" : "W–L–D");
        return $"{wins}–{losses} W–L";
    }

    public static (string? Provider, string? Sport) Competition(string? sport, string? league) => league switch
    {
        "college-football" => ("big12", "Football"), "mens-college-basketball" => ("big12", "Men's Basketball"),
        "womens-college-basketball" => ("big12", "Women's Basketball"), "womens-college-volleyball" => ("big12", "Volleyball"),
        "usa.ncaa.w.1" => ("big12", "Soccer"), "usa.1" => ("espn-mls", "Soccer"), "usa.nwsl" => ("espn-nwsl", "Soccer"),
        "eng.1" => ("espn-epl", "Soccer"), "eng.2" => ("espn-championship", "Soccer"),
        "mlb" => ("espn-mlb", "Baseball"), "wnba" => ("espn-wnba", "Women's Basketball"),
        "nhl" => ("espn-nhl", "Hockey"), "nfl" => ("espn-nfl", "Football"), _ => ("unsupported", sport)
    };
}
