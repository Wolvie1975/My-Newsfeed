namespace MyNewsFeed.Web.Data;

public sealed record TonightItem(string Kind, string Title, DateTime? StartsUtc, string TimeLabel, string? Url,
    string? HomeTeam = null, string? AwayTeam = null);

public static class TodayTonight
{
    public static IReadOnlyList<TonightItem> Build(IReadOnlyList<EventItem> games, IReadOnlyList<TvEpisodeItem> episodes,
        IReadOnlyList<MovieReleaseItem> movies, FeedDates dates) =>
        games.Select(g => new TonightItem(g.Sport ?? "Sports", EventsText.Matchup(g.SchoolName, g.Opponent, g.IsNeutral == true ? null : g.IsAway),
            g.TimeTbd || EventsText.StatusLabel(g.ScheduleStatus) != null ? null : g.StartsAtUtc,
            EventsText.TimeLabel(g, dates), FeedQuery.SafeHref(g.LiveStatsUrl) ?? FeedQuery.SafeHref(g.Url),
            TeamNames.Key(g.SchoolName), TeamNames.Key(g.Opponent)))
        .Concat(episodes.Select(e => new TonightItem("TV & Streaming", e.SeriesTitle + (string.IsNullOrWhiteSpace(e.Title) ? "" : " · " + e.Title),
            e.StartsUtc, e.StartsUtc is { } time ? dates.EventTime(time) : "Date only", FeedQuery.SafeHref(e.Url))))
        .Concat(movies.Where(m => m.Type is not ("digital" or "digital_rental" or "digital_purchase"))
            .Select(m => new TonightItem("Movies", m.Title, null, "Released today", FeedQuery.SafeHref(m.SourceUrl))))
        .OrderBy(i => i.StartsUtc == null).ThenBy(i => i.StartsUtc).ThenBy(i => i.Title).ToList();
}
