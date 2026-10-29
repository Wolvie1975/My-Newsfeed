using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace myNewspaper.Web.Data.Models;

[Table("SportsStandingsSources")]
public sealed class SportsStandingsSource
{
    [Key, Column("ID")] public int Id { get; set; }
    public string ProviderKey { get; set; } = "";
    public string Sport { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool Enabled { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public string? LastError { get; set; }
    public string? LatestSeason { get; set; }
    public int? LastTeamCount { get; set; }
}

[Table("SportsStandings")]
public sealed class SportsStanding
{
    [Key, Column("ID")] public long Id { get; set; }
    public int SourceId { get; set; }
    public string Season { get; set; } = "";
    public string? SeasonType { get; set; }
    public string? Title { get; set; }
    public string? SourceUrl { get; set; }
    public string GroupId { get; set; } = "";
    public string? GroupName { get; set; }
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string? LogoUrl { get; set; }
    public string? TeamUrl { get; set; }
    public int Position { get; set; }
    public int? Rank { get; set; }
    public string? StatsJson { get; set; }
    public string? Note { get; set; }
    public DateTime? FetchedAt { get; set; }
    public bool IsCurrent { get; set; }
}
