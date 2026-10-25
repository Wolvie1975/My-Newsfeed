using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace myNewspaper.Web.Data.Models;

[Table("TvSeries")]
public class TvSeries
{
    [Key, Column("ID")] public int Id { get; set; }
    public int TvmazeShowId { get; set; }
    [MaxLength(500)] public string Title { get; set; } = null!;
    public int? NetworkId { get; set; }
    [MaxLength(200)] public string? NetworkName { get; set; }
    [MaxLength(2)][Column(TypeName = "char(2)")] public string? NetworkCountryCode { get; set; }
    [MaxLength(100)] public string? NetworkTimeZone { get; set; }
    public int? WebChannelId { get; set; }
    [MaxLength(200)] public string? WebChannelName { get; set; }
    [MaxLength(2)][Column(TypeName = "char(2)")] public string? WebChannelCountryCode { get; set; }
    [MaxLength(100)] public string? WebChannelTimeZone { get; set; }
    [MaxLength(50)] public string? Status { get; set; }
    public DateOnly? PremiereDate { get; set; }
    [MaxLength(2048)] public string? PosterUrl { get; set; }
    [MaxLength(2048)] public string SourceUrl { get; set; } = null!;
    [MaxLength(20)][Column(TypeName = "varchar(20)")] public string? ImdbId { get; set; }
    public int? TheTvdbId { get; set; }
    public int? TvRageId { get; set; }
    public string RawMetadata { get; set; } = null!;
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public ICollection<TvEpisode> Episodes { get; set; } = [];
}

[Table("TvEpisodes")]
public class TvEpisode
{
    [Key, Column("ID")] public int Id { get; set; }
    public int TvmazeEpisodeId { get; set; }
    public int SeriesId { get; set; }
    public int? SeasonNumber { get; set; }
    public int? EpisodeNumber { get; set; }
    [MaxLength(500)] public string? Title { get; set; }
    [MaxLength(50)][Column(TypeName = "varchar(50)")] public string? EpisodeType { get; set; }
    public bool IsSpecial { get; set; }
    public DateOnly? Airdate { get; set; }
    [Column(TypeName = "time(0)")] public TimeOnly? Airtime { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public bool IsDateOnly { get; set; }
    public int? RuntimeMinutes { get; set; }
    [MaxLength(2048)] public string? ImageUrl { get; set; }
    [MaxLength(2048)] public string SourceUrl { get; set; } = null!;
    [MaxLength(50)] public string? RawAirdate { get; set; }
    [MaxLength(50)] public string? RawAirtime { get; set; }
    [MaxLength(100)] public string? RawAirstamp { get; set; }
    public string RawMetadata { get; set; } = null!;
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public TvSeries Series { get; set; } = null!;
}

[Table("TvTrackedSeries")]
public class TvTrackedSeries
{
    [Key, Column("ID")] public int Id { get; set; }
    public int TvmazeShowId { get; set; }
    public int? SeriesId { get; set; }
    [MaxLength(500)] public string ExpectedTitle { get; set; } = null!;
    public bool Enabled { get; set; } = true;
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    [MaxLength(1000)] public string? LastError { get; set; }
    public int? EpisodeCount { get; set; }
    public int? LastIssueCount { get; set; }
    public DateTime? CacheExpiresAt { get; set; }
    public DateTime? RetryAfter { get; set; }
    public DateTime? LeaseUntil { get; set; }
    public Guid? LeaseToken { get; set; }
    public string? CachedShow { get; set; }
    public string? CachedEpisodes { get; set; }
    public DateTime CreatedAt { get; set; }
    public TvSeries? Series { get; set; }
}

