using Microsoft.EntityFrameworkCore;
using MyDailyFeed.Web.Data.Models;
namespace MyDailyFeed.Web.Data;

/// <summary>Season-cache visibility for NHL detail when no ordinary followed result exists.</summary>
public static class NhlBoxScoreVisibility
{
    public static IQueryable<SportsMatchResult> Visible(WebScraperContext db, int sourceId, int? teamId) =>
        db.SportsMatchResults.AsNoTracking().Where(r=>r.SourceId==sourceId && r.ProviderKey=="espn-nhl"
            && (teamId==null || teamId==0 || r.HomeTeamId==teamId || r.AwayTeamId==teamId)
            && r.HomeTeamId!=r.AwayTeamId && r.HomeScore!=null && r.AwayScore!=null
            && r.HomeScore>=0 && r.AwayScore>=0 && r.ScheduleStatus.StartsWith("STATUS_FINAL"));

    public static async Task<bool> ContainsAsync(WebScraperContext db,int gameId)
    {
        var source=await db.SportsEventsTypes.AsNoTracking().Where(s=>s.Enabled && s.ProviderKey=="espn-nhl")
            .OrderBy(s=>s.Id).FirstOrDefaultAsync();
        if(source is null)return false;
        var visible=Visible(db,source.Id,source.ProviderSchoolId);
        // Like MLS/Championship, retain the latest validated stored season during import failures.
        var season=await visible.MaxAsync(r=>(int?)r.Season);
        return season!=null && await visible.AnyAsync(r=>r.Season==season && r.GameId==gameId);
    }
}
