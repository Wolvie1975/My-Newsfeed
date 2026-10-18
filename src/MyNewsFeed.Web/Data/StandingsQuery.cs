using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Web.Data;

public sealed record StandingStatistic(string Name, string DisplayName, string DisplayValue, double? Value = null);
public sealed record StandingColumn(string Name, string Label);
public sealed record StandingTeam(SportsStanding Row, IReadOnlyList<StandingStatistic> Statistics);
public sealed record StandingGroup(string Id, string Name, IReadOnlyList<StandingTeam> Teams, IReadOnlyList<StandingColumn> Columns);
public sealed record StandingsPage(IReadOnlyList<SportsStandingsSource> Sources, SportsStandingsSource? Selected,
    IReadOnlyList<StandingGroup> Groups, string? AvailabilityError = null);

public static class StandingsQuery
{
    public static int? ParseSourceId(string? value) => int.TryParse(value, out var id) && id > 0 ? id : null;

    public static SportsStandingsSource? SelectSource(IEnumerable<SportsStandingsSource> sources, int? requested)
    {
        var enabled = sources.Where(s => s.Enabled).OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase).ThenBy(s => s.Id).ToList();
        return enabled.FirstOrDefault(s => s.Id == requested) ?? enabled.FirstOrDefault();
    }

    public static string? Freshness(SportsStandingsSource source, DateTime utcNow)
    {
        if (!string.IsNullOrWhiteSpace(source.LastError)) return "Latest refresh failed; showing last available standings.";
        return source.LastSuccessAt is not { } last || utcNow - DateTime.SpecifyKind(last, DateTimeKind.Utc) > TimeSpan.FromHours(2)
            ? "Standings may be out of date." : null;
    }

    public static IReadOnlyList<StandingStatistic> ParseStatistics(string? json, long rowId, ILogger logger)
    {
        try
        {
            using var document = JsonDocument.Parse(json ?? "");
            if (document.RootElement.ValueKind != JsonValueKind.Array) throw new JsonException("Expected a statistic array.");
            var result = new List<StandingStatistic>();
            foreach (var stat in document.RootElement.EnumerateArray())
            {
                if (stat.ValueKind != JsonValueKind.Object) throw new JsonException("Expected a statistic object.");
                static string Text(JsonElement element, string name) => element.TryGetProperty(name, out var value)
                    && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
                var name = Text(stat, "name");
                if (name.Length == 0) continue;
                double? number = stat.TryGetProperty("value", out var numeric) && numeric.ValueKind == JsonValueKind.Number
                    && numeric.TryGetDouble(out var value) && double.IsFinite(value) ? value : null;
                result.Add(new(name, Text(stat, "displayName"), Text(stat, "displayValue"), number));
            }
            return result;
        }
        catch (JsonException)
        {
            logger.LogWarning("Invalid standings statistics for row {RowId}.", rowId);
            return [];
        }
    }

    public static string StatisticValue(StandingTeam team, string name)
    {
        if (name.StartsWith("collegeBasketball:", StringComparison.Ordinal))
        {
            var key = name[18..];
            var statName = key switch { "Conf" => "Big 12 Record", "Overall" => "Overall Record", "CPct" or "Pct" => "Pct.", _ => key };
            return team.Statistics.Where(s => s.DisplayName == statName).Skip(key == "Pct" ? 1 : 0)
                .FirstOrDefault()?.DisplayValue is { Length: > 0 } basketballValue ? basketballValue : "—";
        }
        if (name.StartsWith("collegeFootball:", StringComparison.Ordinal))
            return team.Statistics.FirstOrDefault(s => s.DisplayName == name[16..])?.DisplayValue is { Length: > 0 } footballValue ? footballValue : "—";
        if (name.StartsWith("collegeSoccer", StringComparison.Ordinal))
        {
            if (name == "collegeSoccerPoints")
                return team.Statistics.FirstOrDefault(s => s.DisplayName == "Points")?.DisplayValue ?? "—";
            var record = team.Statistics.FirstOrDefault(s => s.DisplayName == "Big 12 Record")?.DisplayValue?.Split('-');
            var draws = 0;
            if (record is null || record.Length is not (2 or 3) || !int.TryParse(record[0], out var wins)
                || !int.TryParse(record[1], out var losses) || (record.Length == 3 && !int.TryParse(record[2], out draws))
                || wins < 0 || losses < 0 || draws < 0) return "—";
            return name switch
            {
                "collegeSoccerGamesPlayed" => ((long)wins + losses + draws).ToString(System.Globalization.CultureInfo.InvariantCulture),
                "collegeSoccerWins" => record[0], "collegeSoccerDraws" => draws.ToString(System.Globalization.CultureInfo.InvariantCulture), "collegeSoccerLosses" => record[1],
                _ => "—"
            };
        }
        var stat = team.Statistics.FirstOrDefault(s => s.Name == name);
        if (stat is null && name == "otLosses") stat = team.Statistics.FirstOrDefault(s => s.Name == "overtimeLosses");
        var value = stat?.DisplayValue;
        if (string.IsNullOrWhiteSpace(value)) return "—";
        return name == "Last Ten Games" ? value.Split(',')[0].Trim() : value;
    }

    public static string NflConference(StandingGroup group) =>
        group.Name.StartsWith("AFC", StringComparison.OrdinalIgnoreCase) || group.Name == "American Football Conference"
            ? "American Football Conference"
            : group.Name.StartsWith("NFC", StringComparison.OrdinalIgnoreCase) || group.Name == "National Football Conference"
                ? "National Football Conference" : "Other divisions";

    public static string NhlConference(StandingGroup group) => group.Name switch
    {
        "Atlantic Division" or "Metropolitan Division" => "Eastern Conference",
        "Central Division" or "Pacific Division" => "Western Conference",
        _ => "Other divisions"
    };

    public static IReadOnlyList<StandingColumn> Columns(string provider, IReadOnlyList<StandingTeam> teams, string? sport = null)
    {
        if (provider == "big12" && sport?.Contains("Basketball", StringComparison.OrdinalIgnoreCase) == true)
            return [new("column1", "BIG 12 RECORD"), new("column2", "PCT."), new("column3", "HOME"),
                new("column4", "AWAY"), new("column5", "OVERALL RECORD"), new("column6", "PCT."),
                new("column7", "HOME"), new("column8", "AWAY"), new("column9", "NEUTRAL"), new("column10", "STREAK")];
        if (provider == "big12" && string.Equals(sport, "Football", StringComparison.OrdinalIgnoreCase))
            return [new("collegeFootball:Conf", "W-L"), new("collegeFootball:CPct.", "PCT"),
                new("collegeFootball:Overall", "W-L"), new("collegeFootball:Pct.", "PCT"),
                new("collegeFootball:Streak", "STRK")];
        if (provider == "big12" && string.Equals(sport, "Soccer", StringComparison.OrdinalIgnoreCase))
            return [new("column1", "Big 12 Record"), new("column2", "Conf. Pct."), new("column3", "Points"),
                new("column4", "Home"), new("column5", "Away"), new("column6", "Streak"),
                new("column7", "Overall Record"), new("column8", "Pct."), new("column9", "Home"),
                new("column10", "Away"), new("column11", "Neutral"), new("column12", "Streak")];
        if (provider == "big12" && string.Equals(sport, "Volleyball", StringComparison.OrdinalIgnoreCase))
            return teams.SelectMany(t => t.Statistics).DistinctBy(s => s.Name)
                .Where(s => s.DisplayName is not ("Conf. Home" or "Conf. Away" or "Conf. Neutral" or "Conf. Streak"))
                .Select(s => new StandingColumn(s.Name, string.IsNullOrWhiteSpace(s.DisplayName) ? s.Name : s.DisplayName)).ToList();
        if (provider == "espn-nhl")
            return [new("gamesPlayed", "GP"), new("wins", "W"), new("losses", "L"),
                new("otLosses", "OTL"), new("points", "PTS"), new("regWins", "RW"),
                new("rotWins", "ROW"), new("shootoutWins", "SOW"), new("shootoutLosses", "SOL"),
                new("Home", "HOME"), new("Road", "AWAY"), new("pointsFor", "GF"),
                new("pointsAgainst", "GA"), new("pointDifferential", "DIFF"),
                new("Last Ten Games", "L10"), new("streak", "STRK")];
        if (provider == "espn-nfl")
            return [new("wins", "W"), new("losses", "L"), new("ties", "T"),
                new("winPercent", "PCT"), new("Home", "HOME"), new("Road", "AWAY"),
                new("vs. Div.", "DIV"), new("vs. Conf.", "CONF"), new("pointsFor", "PF"),
                new("pointsAgainst", "PA"), new("pointDifferential", "DIFF"), new("streak", "STRK")];
        if (provider is not ("espn-mlb" or "espn-wnba" or "espn-nwsl" or "espn-mls" or "espn-championship" or "espn-epl"))
            return teams.SelectMany(t => t.Statistics).DistinctBy(s => s.Name)
                .Select(s => new StandingColumn(s.Name, string.IsNullOrWhiteSpace(s.DisplayName) ? s.Name : s.DisplayName)).ToList();
        return provider is "espn-nwsl" or "espn-mls" or "espn-championship" or "espn-epl"
            ? [new("gamesPlayed", "GP"), new("wins", "W"), new("ties", "D"), new("losses", "L"),
                new("pointsFor", "F"), new("pointsAgainst", "A"), new("pointDifferential", "GD"), new("points", "P")]
            : [new("wins", "W"), new("losses", "L"), new("winPercent", "Pct"), new("gamesBehind", "GB"), new("streak", "Streak")];
    }

    public static IReadOnlyList<StandingGroup> BuildGroups(IEnumerable<SportsStanding> rows, string provider, ILogger logger, string? sport = null) =>
        rows.Where(r => r.IsCurrent).OrderBy(r => r.GroupId, StringComparer.Ordinal).ThenBy(r => r.Position)
            .ThenBy(r => r.TeamName, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.Id)
            .GroupBy(r => r.GroupId)
            .Select(group =>
            {
                var teams = group.Select(row => new StandingTeam(row, ParseStatistics(row.StatsJson, row.Id, logger))).ToList();
                if (provider == "espn-mls")
                    teams = teams.OrderByDescending(Points).ThenBy(t => t.Row.Position)
                        .ThenBy(t => t.Row.TeamName, StringComparer.OrdinalIgnoreCase).ThenBy(t => t.Row.Id).ToList();
                return new StandingGroup(group.Key, group.First().GroupName ?? "Standings", teams, Columns(provider, teams, sport));
            }).ToList();

    private static double? Points(StandingTeam team)
    {
        var points = team.Statistics.FirstOrDefault(s => s.Name == "points");
        if (points?.Value is { } numeric) return numeric;
        return double.TryParse(points?.DisplayValue, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var display) && double.IsFinite(display) ? display : null;
    }

    public static async Task<StandingsPage> GetAsync(WebScraperContext db, int? sourceId, ILogger logger, DateOnly? today = null, IEnumerable<string>? hiddenProviders = null, string? collegeSport = null)
    {
        var hidden = hiddenProviders?.ToArray() ?? [];
        var sources = await db.SportsStandingsSources.AsNoTracking().Where(s => s.Enabled && !hidden.Contains(s.ProviderKey)
                && !(s.ProviderKey == "big12" && s.Sport == "Women's Basketball")
                && (collegeSport == null || (s.ProviderKey == "big12" && s.Sport == collegeSport)))
            .OrderBy(s => s.DisplayName).ThenBy(s => s.Id).ToListAsync();
        var day = today ?? FeedDates.Create("America/Chicago").Today;
        var selected = SelectSource(sources, sourceId);
        if (selected is null) return new(sources, null, []);
        try
        {
            var rows = await db.SportsStandings.AsNoTracking()
                .Where(r => r.SourceId == selected.Id && r.IsCurrent
                    && db.SportsStandingsSources.Any(s => s.Id == r.SourceId && s.Enabled))
                .OrderBy(r => r.GroupId).ThenBy(r => r.Position).ThenBy(r => r.TeamName).ThenBy(r => r.Id).ToListAsync();
            return new(sources, selected, BuildGroups(rows.Where(r => selected.ProviderKey != "big12" || CollegeSeasons.Matches(r.Season, selected.Sport, day)), selected.ProviderKey, logger, selected.Sport));
        }
        catch (System.Data.Common.DbException ex)
        {
            logger.LogError(ex, "Could not read standings for source {SourceId}.", selected.Id);
            return new(sources, selected, [], "Standings are temporarily unavailable. Please try again.");
        }
    }
}
