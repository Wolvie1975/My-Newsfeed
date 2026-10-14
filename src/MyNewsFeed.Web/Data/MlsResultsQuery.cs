using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data.Models;
namespace MyNewsFeed.Web.Data;

public sealed record MlsResultsPage(SportsEventsType? Source, int? Season,
    IReadOnlyList<SportsMatchResult> Items, int Total, int PageIndex);

public static class MlsResultsQuery
{
    public const int PageSize = 25;
    public static string Score(SportsMatchResult game) => game.ScheduleStatus == "STATUS_FINAL_PEN"
        ? $"{game.HomeScore}–{game.AwayScore} ({game.HomeShootoutScore}–{game.AwayShootoutScore} on penalties)"
        : $"{game.HomeScore}–{game.AwayScore}";
    public static string Status(SportsMatchResult game) => game.ScheduleStatus switch
    {
        "STATUS_FINAL_PEN" => "Final · Penalties",
        "STATUS_FINAL_AET" or "STATUS_FINAL_OT" => "Final · Extra time",
        "STATUS_FULL_TIME" or "STATUS_FINAL" => "Final",
        _ => game.ScheduleStatus.Replace("STATUS_", "").Replace('_', ' ')
    };
    public static string? Winner(SportsMatchResult game)
    {
        var home = game.ScheduleStatus == "STATUS_FINAL_PEN" ? game.HomeShootoutScore : game.HomeScore;
        var away = game.ScheduleStatus == "STATUS_FINAL_PEN" ? game.AwayShootoutScore : game.AwayScore;
        if (home is null || away is null || home < 0 || away < 0) return null;
        if (home == away) return game.ScheduleStatus == "STATUS_FINAL_PEN" ? null : "Draw";
        return (home > away ? game.HomeTeamName : game.AwayTeamName) + " win";
    }
    public static async Task<MlsResultsPage> GetAsync(WebScraperContext db, int pageIndex = 0)
    {
        var source = await db.SportsEventsTypes.AsNoTracking().Where(s => s.Enabled && s.ProviderKey == "espn-mls")
            .OrderBy(s => s.Id).FirstOrDefaultAsync();
        if (source is null) return new(null, null, [], 0, 0);
        var query = db.SportsMatchResults.AsNoTracking().Where(r => r.SourceId == source.Id && r.ProviderKey == "espn-mls"
            && r.HomeTeamId != r.AwayTeamId && r.HomeScore != null && r.AwayScore != null && r.HomeScore >= 0 && r.AwayScore >= 0
            && (r.ScheduleStatus == "STATUS_FULL_TIME" || r.ScheduleStatus.StartsWith("STATUS_FINAL"))
            && (r.ScheduleStatus != "STATUS_FINAL_PEN" ||
                (r.HomeShootoutScore != null && r.AwayShootoutScore != null && r.HomeShootoutScore >= 0 && r.AwayShootoutScore >= 0
                 && r.HomeShootoutScore != r.AwayShootoutScore)));
        // Keep the latest validated stored season during off-season or failed replacement imports.
        var season = await query.MaxAsync(r => (int?)r.Season);
        if (season is null) return new(source, null, [], 0, 0);
        query = query.Where(r => r.Season == season);
        var total = await query.CountAsync();
        pageIndex = Math.Clamp(pageIndex, 0, Math.Max(0, (total - 1) / PageSize));
        var rows = await query.OrderByDescending(r => r.MatchDate).ThenByDescending(r => r.StartsAtUtc).ThenByDescending(r => r.Id)
            .Skip(pageIndex * PageSize).Take(PageSize).ToListAsync();
        return new(source, season, rows, total, pageIndex);
    }
}
