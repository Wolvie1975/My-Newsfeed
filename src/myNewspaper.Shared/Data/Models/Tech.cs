using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace myNewspaper.Web.Data.Models;

[Table("TechReleaseProjects")]
public sealed class TechReleaseProject
{
    [Key, Column("ID")] public int Id { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public string SourceKind { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string NotesLabel { get; set; } = "Release notes";
    public bool Enabled { get; set; }
    public int SortOrder { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public string? LastError { get; set; }
}

[Table("TechReleases")]
public sealed class TechRelease
{
    [Key, Column("ID")] public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Version { get; set; } = "";
    public DateTime ReleasedAt { get; set; }
    public string? NotesUrl { get; set; }
    public bool IsPrerelease { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public TechReleaseProject Project { get; set; } = null!;
}

[Table("TechDevReadingItems")]
public sealed class TechDevReadingItem
{
    public string Site { get; set; } = "";
    public int Rank { get; set; }
    public string Title { get; set; } = "";
    public string? Url { get; set; }
    public string? Domain { get; set; }
    public int Points { get; set; }
    public int Comments { get; set; }
    public string DiscussionUrl { get; set; } = "";
    public DateTime SnapshotAt { get; set; }
}

[Table("TechStatusServices")]
public sealed class TechStatusService
{
    [Key, Column("ID")] public int Id { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string PageUrl { get; set; } = "";
    public bool Enabled { get; set; }
    public int SortOrder { get; set; }
}

[Table("TechServiceStatus")]
public sealed class TechServiceStatus
{
    [Key] public int ServiceId { get; set; }
    public string Indicator { get; set; } = "unknown";
    public string? Description { get; set; }
    public string? IncidentName { get; set; }
    public string? IncidentComponent { get; set; }
    public DateTime? IncidentStartedAt { get; set; }
    public string? IncidentUrl { get; set; }
    public DateTime CheckedAt { get; set; }
}

