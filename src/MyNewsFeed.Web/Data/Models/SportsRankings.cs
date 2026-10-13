using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MyNewsFeed.Web.Data.Models;

[Table("SportsRankingSources")]
public sealed class SportsRankingSource
{
    [Key, Column("ID")] public int Id { get; set; }
    public string ProviderKey { get; set; } = "";
    public string Sport { get; set; } = "";
    public string LeagueId { get; set; } = "";
    public string PollId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Url { get; set; } = "";
    public bool Enabled { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public string? LastError { get; set; }
}
[Table("SportsRankingPolls")]
public sealed class SportsRankingPoll
{
    [Key, Column("ID")] public long Id { get; set; }
    public int SourceId { get; set; }
    public string PollId { get; set; } = "";
    public string PollName { get; set; } = "";
    public int Season { get; set; }
    public string SeasonLabel { get; set; } = "";
    public DateTime PublishedAtUtc { get; set; }
    public DateTime? ProviderUpdatedAtUtc { get; set; }
    public string? Period { get; set; }
    public string? SeasonPhase { get; set; }
    public string SourceUrl { get; set; } = "";
    public bool IsCurrent { get; set; }
    public DateTime FetchedAt { get; set; }
}
[Table("SportsRankingTeams")]
public sealed class SportsRankingTeam
{
    [Key, Column("ID")] public long Id { get; set; }
    public long PollRowId { get; set; }
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public int? Rank { get; set; }
    public int? PreviousRank { get; set; }
    public bool PreviouslyUnranked { get; set; }
    public double? Points { get; set; }
    public int? FirstPlaceVotes { get; set; }
    public string? Record { get; set; }
    public string? LogoUrl { get; set; }
    public string? TeamUrl { get; set; }
    public int? Big12SchoolId { get; set; }
    public bool? IsBig12 { get; set; }
    public bool IsCurrent { get; set; }
}
