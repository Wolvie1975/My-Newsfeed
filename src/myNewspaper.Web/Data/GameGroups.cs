namespace myNewspaper.Web.Data;

public sealed record GameLeague<T>(string Name, IReadOnlyList<T> Games);

public static class GameGroups
{
    public static string ScheduleLeague(EventItem game) => game.ProviderKey switch
    {
        "espn-championship" => "EFL Championship",
        "espn-epl" => "Premier League",
        "espn-mlb" => "MLB",
        "espn-mls" => "MLS",
        "espn-nwsl" => "NWSL",
        "espn-wnba" => "WNBA",
        "big12" => game.Sport switch
        {
            "Football" => "College Football",
            "Men's Basketball" => "Men's College Basketball",
            "Women's Basketball" => "Women's College Basketball",
            "Soccer" => "Women's College Soccer",
            "Volleyball" => "Women's College Volleyball",
            _ => game.Sport ?? "Other events"
        },
        _ => game.Sport ?? "Other events"
    };

    public static IReadOnlyList<GameLeague<EventItem>> Schedule(IEnumerable<EventItem> games) =>
        games.GroupBy(ScheduleLeague).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new GameLeague<EventItem>(g.Key, g
                .OrderBy(e => e.TimeTbd || e.StartsAtUtc == null || EventsText.StatusLabel(e.ScheduleStatus) != null)
                .ThenBy(e => e.TimeTbd || EventsText.StatusLabel(e.ScheduleStatus) != null ? null : e.StartsAtUtc)
                .ThenBy(e => e.Id).ToList())).ToList();

    public static IReadOnlyList<GameLeague<ResultItem>> Results(IEnumerable<ResultItem> games) =>
        games.GroupBy(g => g.LeagueName).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new GameLeague<ResultItem>(g.Key, g.OrderBy(e => e.StartsAtUtc)
                .ThenBy(e => e.EventId, StringComparer.Ordinal).ToList())).ToList();
}
