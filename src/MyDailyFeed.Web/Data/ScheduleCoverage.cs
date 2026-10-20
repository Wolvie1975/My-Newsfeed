using Microsoft.EntityFrameworkCore;
using MyDailyFeed.Web.Data.Models;

namespace MyDailyFeed.Web.Data;

public sealed record CoverageRow(int FeedId, string Feed, string Sport, bool Enabled, string Status,
    DateTime? LastSynced, string? Error);

public static class ScheduleCoverage
{
    public static (string Sport, string League, string? Group)? Map(string? provider, string? sport) => provider switch
    {
        "espn-nwsl" => ("soccer", "usa.nwsl", null),
        "espn-wnba" => ("basketball", "wnba", null),
        "espn-mlb" => ("baseball", "mlb", null),
        "espn-mls" => ("soccer", "usa.1", null),
        "big12" => sport switch
        {
            "Football" => ("football", "college-football", "4"),
            "Men's Basketball" => ("basketball", "mens-college-basketball", "8"),
            "Women's Basketball" => ("basketball", "womens-college-basketball", "8"),
            "Volleyball" => ("volleyball", "womens-college-volleyball", "8"),
            "Soccer" => ("soccer", "usa.ncaa.w.1", "8"),
            _ => null
        },
        _ => null
    };
    public static bool Covers(SportsEventsType feed, string? sport, ResultFollow follow)
    {
        var mapping = Map(feed.ProviderKey, sport);
        if (mapping is not { } m || !follow.Enabled || follow.Sport != m.Sport || follow.League != m.League) return false;
        return follow.Kind == "league" || (follow.Kind == "conference" && m.Group != null && follow.GroupId == m.Group)
            || (follow.Kind == "team" && (feed.ProviderKey == "espn-nwsl" ? follow.TeamId == "20907"
                : feed.ProviderKey == "big12" && TeamNames.Matches(feed.SchoolName, follow.Name)));
    }
    public static async Task<IReadOnlyList<CoverageRow>> GetAsync(WebScraperContext db)
    {
        var feeds = await db.SportsEventsTypes.AsNoTracking().OrderBy(f => f.EventsTypeName).ToListAsync();
        var observations = await db.SportsEvents.AsNoTracking().Select(e => new { FeedId = e.SportsEventsTypeId, e.Sport })
            .Union(db.SportsEventSources.Where(s => s.SportsEventId != null).Select(s => new { FeedId = (int?)s.SportsEventsTypeId, Sport = s.Event!.Sport }))
            .Distinct().ToListAsync();
        var follows = await db.ResultFollows.AsNoTracking().ToListAsync();
        var rows = new List<CoverageRow>();
        foreach (var feed in feeds)
        {
            var sports = observations.Where(o => o.FeedId == feed.Id).Select(o => o.Sport).Distinct().DefaultIfEmpty(null);
            foreach (var sport in sports)
            {
                var covered = follows.Where(f => Covers(feed, sport, f)).ToList();
                var error = string.Join("; ", covered.Where(f => !string.IsNullOrWhiteSpace(f.LastError)).Select(f => f.LastError));
                var status = !feed.Enabled ? "Feed disabled" : Map(feed.ProviderKey, sport) == null ? "Unmapped sport"
                    : covered.Count == 0 ? "Missing enabled results follow" : error.Length > 0 ? "Results sync error"
                    : covered.Any(f => f.LastSyncedAt == null) ? "Awaiting first sync" : "Covered";
                rows.Add(new(feed.Id, feed.EventsTypeName, sport ?? feed.ProviderKey ?? "No games yet", feed.Enabled, status,
                    covered.Count == 0 ? null : covered.Min(f => f.LastSyncedAt), error.Length == 0 ? null : error));
            }
        }
        return rows;
    }
}
