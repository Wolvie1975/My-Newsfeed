using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Web.Data;
public sealed record RankingsPage(IReadOnlyList<SportsRankingSource> Sources, SportsRankingSource? Selected,
    SportsRankingPoll? Poll, IReadOnlyList<SportsRankingTeam> Teams, string? AvailabilityError = null);

public static class RankingsQuery
{
    public static SportsRankingSource? SelectSource(IEnumerable<SportsRankingSource> sources, int? id)
    {
        var enabled = sources.Where(s => s.Enabled).OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase).ThenBy(s => s.Id).ToList();
        return enabled.FirstOrDefault(s => s.Id == id) ?? enabled.FirstOrDefault();
    }
    public static string Previous(SportsRankingTeam team) => team.PreviousRank?.ToString()
        ?? (team.PreviouslyUnranked ? "NR" : "—");
    public static string Movement(SportsRankingTeam team)
    {
        if (team.PreviousRank is not { } previous) return team.PreviouslyUnranked ? "New" : "—";
        if (team.Rank is not { } rank) return "—";
        var change = previous - rank;
        return change > 0 ? $"Up {change}" : change < 0 ? $"Down {-change}" : "Unchanged";
    }
    public static bool OldPoll(SportsRankingPoll poll, DateTime now) =>
        DateTime.SpecifyKind(now, DateTimeKind.Utc) - DateTime.SpecifyKind(poll.PublishedAtUtc, DateTimeKind.Utc) > TimeSpan.FromDays(14);
    public static string? Freshness(SportsRankingSource source, DateTime now) => !string.IsNullOrWhiteSpace(source.LastError)
        ? "Latest refresh failed; showing last available rankings."
        : source.LastSuccessAt is not { } last || DateTime.SpecifyKind(now, DateTimeKind.Utc) - DateTime.SpecifyKind(last, DateTimeKind.Utc) > TimeSpan.FromHours(2)
            ? "Rankings may be out of date." : null;
    public static async Task<RankingsPage> GetAsync(WebScraperContext db, int? sourceId, ILogger log, DateOnly? today = null, string? collegeSport = null)
    {
        var sources = await db.SportsRankingSources.AsNoTracking().Where(s => s.Enabled && (collegeSport == null || s.Sport == collegeSport)).OrderBy(s => s.DisplayName).ThenBy(s => s.Id).ToListAsync();
        var day = today ?? FeedDates.Create("America/Chicago").Today;
        if (sources.Any(s => s.Sport.Contains("Basketball", StringComparison.OrdinalIgnoreCase)))
        {
            var start = CollegeSeasons.Start("Basketball", day);
            var started = (await db.SportsEvents.AsNoTracking().Where(e => e.ProviderKey == "big12"
                && e.EventDate >= start && e.EventDate <= day
                && (e.ScheduleStatus == null || (e.ScheduleStatus != "STATUS_CANCELED" && e.ScheduleStatus != "STATUS_CANCELLED")))
                .Select(e => e.Sport).Distinct().ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var polls = await db.SportsRankingPolls.AsNoTracking().Where(p => p.IsCurrent)
                .Select(p => new { p.SourceId, p.PollId, p.SeasonLabel }).ToListAsync();
            sources = sources.Where(s => !s.Sport.Contains("Basketball", StringComparison.OrdinalIgnoreCase)
                || (started.Contains(s.Sport) && polls.Any(p => p.SourceId == s.Id && p.PollId == s.PollId
                    && CollegeSeasons.Matches(p.SeasonLabel, s.Sport, day)))).ToList();
        }
        var selected = SelectSource(sources, sourceId);
        if (selected is null) return new(sources, null, null, []);
        try
        {
            var poll = await db.SportsRankingPolls.AsNoTracking().Where(p => p.SourceId == selected.Id && p.IsCurrent && p.PollId == selected.PollId
                && db.SportsRankingSources.Any(s => s.Id == p.SourceId && s.Enabled))
                .OrderByDescending(p => p.PublishedAtUtc).ThenByDescending(p => p.Id).FirstOrDefaultAsync();
            if (poll is null) return new(sources, selected, null, []);
            var teams = await db.SportsRankingTeams.AsNoTracking().Where(t => t.PollRowId == poll.Id && t.IsCurrent
                && db.SportsRankingPolls.Any(p => p.Id == t.PollRowId && p.IsCurrent
                    && db.SportsRankingSources.Any(s => s.Id == p.SourceId && s.Enabled)))
                .OrderBy(t => t.Rank == null).ThenBy(t => t.Rank).ThenBy(t => t.TeamName).ThenBy(t => t.Id).ToListAsync();
            return new(sources, selected, poll, teams);
        }
        catch (System.Data.Common.DbException ex)
        { log.LogError(ex, "Rankings read failed for source {SourceId}.", selected.Id); return new(sources, selected, null, [], "Rankings are temporarily unavailable."); }
    }
}
