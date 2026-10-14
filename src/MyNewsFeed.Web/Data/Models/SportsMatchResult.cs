using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MyNewsFeed.Web.Data.Models;

[Table("SportsMatchResults")]
public sealed class SportsMatchResult
{
    [Key, Column("ID")] public long Id { get; set; }
    public int SourceId { get; set; }
    public string ProviderKey { get; set; } = "";
    public int GameId { get; set; }
    public int Season { get; set; }
    public DateOnly MatchDate { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public string HomeTeamName { get; set; } = "";
    public string AwayTeamName { get; set; } = "";
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public int? HomeShootoutScore { get; set; }
    public int? AwayShootoutScore { get; set; }
    public string? HomeLogoUrl { get; set; }
    public string? AwayLogoUrl { get; set; }
    public string ScheduleStatus { get; set; } = "";
    public string? SourceUrl { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}
