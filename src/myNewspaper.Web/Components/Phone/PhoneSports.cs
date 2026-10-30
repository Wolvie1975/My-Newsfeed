using System.Globalization;
using System.Text.Json.Nodes;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;
using myNewspaper.Web.Profiles;

namespace myNewspaper.Web.Components.Phone;

/// <summary>Data rules shared by the folded sports pages. Published order and unknown values stay intact.</summary>
public static class PhoneSports
{
    public static int? Movement(SportsRankingTeam team) => team.Rank is { } rank && team.PreviousRank is { } previous ? previous - rank : null;
    public static string MoveLabel(SportsRankingTeam team) => Movement(team) switch
    {
        > 0 and var up => $"▲ {up}",
        < 0 and var down => $"▼ {-down}",
        0 => "—",
        _ => team.PreviouslyUnranked ? "NEW" : "—"
    };
    public static HashSet<string> Teams(ProfileContext context) =>
        (ProfileSettings.Parse(context.Profile?.Settings)[ProfileSettings.Teams] as JsonArray ?? [])
        .OfType<JsonValue>().Where(v => v.TryGetValue<string>(out _)).Select(v => v.GetValue<string>()).ToHashSet(StringComparer.Ordinal);
    public static bool Yours(IReadOnlySet<string> teams, params string?[] names) => names.Any(n => teams.Contains(TeamNames.Key(n)));
    public static string Matchup(EventItem game) => game.IsNeutral == true || game.IsAway is null
        ? $"{game.SchoolName} vs {game.Opponent}" : game.IsAway == true
            ? $"{game.SchoolName} at {game.Opponent}" : $"{game.Opponent} at {game.SchoolName}";
    public static int? Rank(string? name, IEnumerable<SportsRankingTeam> poll) =>
        poll.Where(t => TeamNames.Matches(t.TeamName, name)).Select(t => t.Rank).FirstOrDefault();
    /// <summary>Two ranked teams with the best combined rank, then highest-ranked participant, then first scheduled.</summary>
    public static EventItem? Featured(IEnumerable<EventItem> games, IReadOnlyList<SportsRankingTeam> poll) => games
        .OrderBy(g => Rank(g.SchoolName, poll) is > 0 && Rank(g.Opponent, poll) is > 0 ? 0 : 1)
        .ThenBy(g => Rank(g.SchoolName, poll) is { } a && Rank(g.Opponent, poll) is { } b ? a + b : Math.Min(Rank(g.SchoolName, poll) ?? int.MaxValue, Rank(g.Opponent, poll) ?? int.MaxValue))
        .ThenBy(g => g.EventDate).ThenBy(g => g.TimeTbd || g.StartsAtUtc is null).ThenBy(g => g.StartsAtUtc).ThenBy(g => g.Id).FirstOrDefault();
    /// <summary>Apply the same matchup priority only within the site's current calendar date.</summary>
    public static EventItem? FeaturedToday(IEnumerable<EventItem> games, IReadOnlyList<SportsRankingTeam> poll, DateOnly today) =>
        Featured(games.Where(g => g.EventDate == today), poll);
    public static string Statistic(StandingTeam team, params string[] labels) =>
        team.Statistics.FirstOrDefault(s => labels.Contains(s.DisplayName, StringComparer.OrdinalIgnoreCase) || labels.Contains(s.Name, StringComparer.OrdinalIgnoreCase))?.DisplayValue ?? "—";
    public static bool IsSoccer(string? provider, string? sport) => provider is "espn-nwsl" or "espn-mls" or "espn-championship" or "espn-epl"
        || provider == "big12" && sport == "Soccer";
    public static bool IsSingleTable(string? provider) => provider is "espn-nwsl" or "espn-championship" or "espn-epl" or "big12";
    public static StandingColumn? PrimaryColumn(StandingGroup group, bool soccer = false) => soccer
        ? group.Columns.FirstOrDefault(c => c.Name is "points" or "collegeSoccerPoints" || c.Label.Equals("Points", StringComparison.OrdinalIgnoreCase)) ?? new("points", "PTS")
        : group.Columns.Count > 0 ? group.Columns[0] : null;
    public static string PrimaryLabel(StandingGroup group, bool soccer = false) => soccer ? "PTS" : PrimaryColumn(group)?.Label ?? "";
    public static string Primary(StandingGroup group, StandingTeam team, bool soccer = false) => PrimaryColumn(group, soccer) is { } column
        ? StandingsQuery.StatisticValue(team, column.Name) : "—";
    public static string Secondary(StandingGroup group, StandingTeam team, bool soccer = false)
    {
        if (!soccer) return string.Join(" · ", group.Columns.Skip(1).Take(2).Select(c => $"{c.Label} {StandingsQuery.StatisticValue(team, c.Name)}"));
        var college = group.Columns.Any(c => c.Label == "Big 12 Record" || c.Name.StartsWith("collegeSoccer", StringComparison.Ordinal));
        var wins = StandingsQuery.StatisticValue(team, college ? "collegeSoccerWins" : "wins");
        var draws = StandingsQuery.StatisticValue(team, college ? "collegeSoccerDraws" : "ties");
        var losses = StandingsQuery.StatisticValue(team, college ? "collegeSoccerLosses" : "losses");
        return $"W {wins} · D {draws} · L {losses}";
    }
    public static double? WinFraction(string? record)
    {
        var parts = record?.Replace('–', '-').Split('-');
        if (parts is not { Length: 2 or 3 } || parts.Any(p => !int.TryParse(p, out var n) || n < 0)) return null;
        var wins = int.Parse(parts[0], CultureInfo.InvariantCulture);
        var total = parts.Sum(p => (long)int.Parse(p, CultureInfo.InvariantCulture));
        return total > 0 ? (double)wins / total : null;
    }
    public static string ShortDate(DateOnly date) => date.ToString("ddd, MMM d", CultureInfo.InvariantCulture);
    public static string Percent(decimal points, decimal leader) => leader <= 0 ? "0%" : (Math.Clamp(points / leader, 0, 1) * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";
}
