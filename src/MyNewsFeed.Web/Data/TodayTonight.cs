namespace MyNewsFeed.Web.Data;

public sealed record TonightItem(string Kind, string Title, DateTime? StartsUtc, string TimeLabel, string? Url,
    string? HomeTeam = null, string? AwayTeam = null,
    string? TeamLogoUrl = null, string? OpponentLogoUrl = null,
    string SchoolName = "Our team", string OpponentName = "Opponent to be announced", string Separator = "vs",
    string? Location = null, string? Tv = null, string? StreamUrl = null, bool? IsAway = null, bool IsNeutral = false, string? ProviderKey = null, int? SchoolId = null, int? OpponentSchoolId = null);

public static class TodayTonight
{
    public static IReadOnlyList<TonightItem> Build(IReadOnlyList<EventItem> games, FeedDates dates) =>
        games.Where(IncludeGame).Select(g => new TonightItem(g.Sport ?? "Sports", EventsText.Matchup(g.SchoolName, g.Opponent, g.IsNeutral == true ? null : g.IsAway),
            g.TimeTbd || EventsText.StatusLabel(g.ScheduleStatus) != null ? null : g.StartsAtUtc,
            EventsText.TimeLabel(g, dates), FeedQuery.SafeHref(g.LiveStatsUrl) ?? FeedQuery.SafeHref(g.Url),
            TeamNames.Key(g.SchoolName), TeamNames.Key(g.Opponent),
            EventsText.LogoUrl(g.TeamLogoUrl), EventsText.LogoUrl(g.OpponentLogoUrl),
            string.IsNullOrWhiteSpace(g.SchoolName) ? "Our team" : g.SchoolName.Trim(),
            string.IsNullOrWhiteSpace(g.Opponent) ? "Opponent to be announced" : g.Opponent.Trim(),
            g.IsNeutral != true && g.IsAway == true ? "@" : "vs",
            string.IsNullOrWhiteSpace(g.Location) ? null : g.Location.Trim(),
            string.IsNullOrWhiteSpace(g.Tv) ? null : g.Tv.Trim(), FeedQuery.SafeHref(g.StreamUrl), g.IsAway, g.IsNeutral == true, g.ProviderKey, g.SchoolId, g.OpponentSchoolId))
        .OrderBy(i => i.StartsUtc == null).ThenBy(i => i.StartsUtc).ThenBy(i => i.Title).ToList();

    private static bool IncludeGame(EventItem game) => game.ProviderKey switch
    {
        "espn-epl" => false,
        "espn-nhl" => HasTeam(game, "St. Louis Blues", "St Louis Blues", "Montreal Canadiens", "Montréal Canadiens"),
        "espn-championship" => HasTeam(game, "Wrexham", "Wrexham AFC"),
        "espn-mls" => HasTeam(game, "Sporting Kansas City", "Sporting KC",
            "St. Louis CITY SC", "St. Louis City", "St Louis City SC", "St Louis City"),
        _ => true
    };

    private static bool HasTeam(EventItem game, params string[] teams) =>
        teams.Any(team => TeamNames.Matches(game.SchoolName, team) || TeamNames.Matches(game.Opponent, team));
}
