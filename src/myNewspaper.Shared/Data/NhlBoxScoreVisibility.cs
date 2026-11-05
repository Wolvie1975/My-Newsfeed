using Microsoft.EntityFrameworkCore;
using myNewspaper.Shared.Data.Models;
namespace myNewspaper.Shared.Data;

/// <summary>Season-cache visibility for NHL detail when no ordinary followed result exists.</summary>
public static class NhlBoxScoreVisibility
{
    public static IQueryable<SportsMatchResult> Visible(WebScraperContext db, int sourceId, int? teamId) =>
        db.SportsMatchResults.AsNoTracking().Where(r => r.SourceId == sourceId && r.ProviderKey == "espn-nhl"
            && (teamId == null || teamId == 0 || r.HomeTeamId == teamId || r.AwayTeamId == teamId)
            && r.HomeTeamId != r.AwayTeamId && r.HomeScore != null && r.AwayScore != null
            && r.HomeScore >= 0 && r.AwayScore >= 0 && r.ScheduleStatus.StartsWith("STATUS_FINAL"));

    public static async Task<bool> ContainsAsync(WebScraperContext db, int gameId, CancellationToken ct = default)
    {
        var source = await db.SportsEventsTypes.AsNoTracking().Where(s => s.Enabled && s.ProviderKey == "espn-nhl")
            .OrderBy(s => s.Id).FirstOrDefaultAsync(ct);
        if (source is null) return false;
        var visible = Visible(db, source.Id, source.ProviderSchoolId);
        // Like MLS/Championship, retain the latest validated stored season during import failures.
        var season = await visible.MaxAsync(r => (int?)r.Season, ct);
        return season is not null && await visible.AnyAsync(r => r.Season == season && r.GameId == gameId, ct);
    }
}
