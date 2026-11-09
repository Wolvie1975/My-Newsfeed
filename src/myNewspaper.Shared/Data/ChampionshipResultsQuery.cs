using Microsoft.EntityFrameworkCore;
using myNewspaper.Shared.Data.Models;
namespace myNewspaper.Shared.Data;

public sealed record ChampionshipResultsPage(SportsEventsType? Source, int? Season,
    IReadOnlyList<SportsMatchResult> Items, int Total, int PageIndex);

public static class ChampionshipResultsQuery
{
    public const int PageSize = 25;
    public static string? WrexhamOutcome(SportsMatchResult game)
    {
        if (game.HomeScore is not { } home || game.AwayScore is not { } away || home < 0 || away < 0
            || (game.HomeTeamId != 352 && game.AwayTeamId != 352)) return null;
        if (game.ScheduleStatus.Contains("PEN", StringComparison.OrdinalIgnoreCase)) return "Penalties";
        if (game.ScheduleStatus != "STATUS_FULL_TIME" && !game.ScheduleStatus.StartsWith("STATUS_FINAL", StringComparison.Ordinal)) return null;
        var difference = game.HomeTeamId == 352 ? home - away : away - home;
        return difference > 0 ? "Wrexham win" : difference < 0 ? "Wrexham loss" : "Draw";
    }
    public static IQueryable<SportsMatchResult> Visible(WebScraperContext db, int sourceId) => db.SportsMatchResults.AsNoTracking().Where(r => r.SourceId == sourceId && r.ProviderKey == "espn-championship"
            && r.HomeScore != null && r.AwayScore != null && r.HomeScore >= 0 && r.AwayScore >= 0
            && (r.ScheduleStatus == "STATUS_FULL_TIME" || r.ScheduleStatus.StartsWith("STATUS_FINAL")));

    public static async Task<ChampionshipResultsPage> GetAsync(WebScraperContext db, bool wrexhamOnly, int pageIndex = 0, CancellationToken ct = default)
    {
        var source = await db.SportsEventsTypes.AsNoTracking().Where(s => s.Enabled && s.ProviderKey == "espn-championship")
            .OrderBy(s => s.Id).FirstOrDefaultAsync(ct);
        if (source is null) return new(null, null, [], 0, 0);
        var query = Visible(db, source.Id);
        // The scraper retains its last successful season until a validated new-season import replaces it.
        var season = await query.MaxAsync(r => (int?)r.Season, ct);
        if (season is null) return new(source, null, [], 0, 0);
        query = query.Where(r => r.Season == season);
        if (wrexhamOnly) query = query.Where(r => r.HomeTeamId == 352 || r.AwayTeamId == 352);
        var total = await query.CountAsync(ct);
        pageIndex = Math.Clamp(pageIndex, 0, Math.Max(0, (total - 1) / PageSize));
        var rows = await query.OrderByDescending(r => r.StartsAtUtc).ThenByDescending(r => r.GameId).ThenByDescending(r => r.Id)
            .Skip(pageIndex * PageSize).Take(PageSize).ToListAsync(ct);
        return new(source, season, rows, total, pageIndex);
    }
}
