using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDailyFeed.Web.Data.Models;

[Table("MovieCatalogs")]
public class MovieCatalog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string ReleasesJson { get; set; } = null!;
}

[Table("MovieReleases")]
public class MovieRelease
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID { get; set; }

    public int MovieSourceLinkId { get; set; }

    [MaxLength(200)]
    public string ReleaseKey { get; set; } = null!;

    [MaxLength(2)]
    [Column(TypeName = "char(2)")]
    public string CountryCode { get; set; } = null!;

    [MaxLength(30)]
    [Column(TypeName = "varchar(30)")]
    public string ReleaseType { get; set; } = null!;

    [Column(TypeName = "date")]
    public DateOnly? ReleaseDate { get; set; }

    [MaxLength(12)]
    [Column(TypeName = "varchar(12)")]
    public string DateStatus { get; set; } = null!;

    [MaxLength(12)]
    [Column(TypeName = "varchar(12)")]
    public string ReleaseStatus { get; set; } = null!;

    [MaxLength(100)]
    public string? Platform { get; set; }

    [MaxLength(100)]
    public string? Format { get; set; }

    [MaxLength(100)]
    public string? ReleaseScope { get; set; }

    [MaxLength(200)]
    public string? Distributor { get; set; }

    [MaxLength(200)]
    public string? Edition { get; set; }

    public bool IsReRelease { get; set; }

    [MaxLength(2048)]
    public string SourceUrl { get; set; } = null!;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime FirstSeenAt { get; set; }

    public DateTime LastSeenAt { get; set; }
}

[Table("MovieReleaseSources")]
public class MovieReleaseSource
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID { get; set; }

    [MaxLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string SourceKey { get; set; } = null!;

    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(2048)]
    public string Url { get; set; } = null!;

    public bool Enabled { get; set; }

    public DateTime? LastScrapedAt { get; set; }

    [MaxLength(1000)]
    public string? LastError { get; set; }

    public DateTime CreatedAt { get; set; }
}

[Table("Movies")]
public class Movie
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID { get; set; }

    [MaxLength(500)]
    public string Title { get; set; } = null!;

    [Column(TypeName = "smallint")]
    public short? OriginalYear { get; set; }

    [MaxLength(20)]
    [Column(TypeName = "varchar(20)")]
    public string? ImdbId { get; set; }

    public int? TmdbId { get; set; }

    public string? Description { get; set; }

    [MaxLength(2048)]
    public string? PosterUrl { get; set; }

    [MaxLength(20)]
    public string? Certification { get; set; }

    [Column(TypeName = "smallint")]
    public short? RuntimeMinutes { get; set; }

    public DateTime FirstSeenAt { get; set; }

    public DateTime LastSeenAt { get; set; }
}

[Table("MovieSourceLinks")]
public class MovieSourceLink
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID { get; set; }

    public int MovieId { get; set; }

    public int MovieReleaseSourceId { get; set; }

    [MaxLength(200)]
    public string ExternalMovieId { get; set; } = null!;

    [MaxLength(2048)]
    public string Url { get; set; } = null!;

    public DateTime FirstSeenAt { get; set; }

    public DateTime LastSeenAt { get; set; }
}

