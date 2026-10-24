using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace myNewspaper.Web.Data.Models;

public partial class SportsEvent
{
    [StringLength(100)] public string? ScheduleStatus { get; set; }
    [Column(TypeName = "varchar(100)")] public string? CanonicalKey { get; set; }
    [Column(TypeName = "varchar(50)")] public string? ProviderKey { get; set; }
    public int? SchoolId { get; set; }
    [StringLength(100)] public string? SchoolName { get; set; }
    public int? OpponentSchoolId { get; set; }
    public int? ProviderSportId { get; set; }
    public bool? IsNeutral { get; set; }
    [StringLength(30)] public string? CalendarTimeZone { get; set; }
    [StringLength(200)] public string? Radio { get; set; }
    [StringLength(2048)] public string? AudioUrl { get; set; }
    public ICollection<SportsEventSource> EventSources { get; set; } = [];
}

public partial class SportsEventsType
{
    [Column(TypeName = "varchar(50)")] public string? ProviderKey { get; set; }
    public int? ProviderSchoolId { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    [StringLength(1000)] public string? LastError { get; set; }
    public int? LastEventCount { get; set; }
    public int? LastIssueCount { get; set; }
    public ICollection<SportsEventSource> EventSources { get; set; } = [];
}

[Table("SportsEventSources")]
public class SportsEventSource
{
    [Key, Column("ID")] public int Id { get; set; }
    public int? SportsEventId { get; set; }
    public int SportsEventsTypeId { get; set; }
    public int ProviderGameId { get; set; }
    [StringLength(2048)] public string SourceUrl { get; set; } = null!;
    public int? SchoolId { get; set; }
    [StringLength(100)] public string? SchoolName { get; set; }
    public int? OpponentSchoolId { get; set; }
    public string? Payload { get; set; }
    [StringLength(1000)] public string? Issue { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public SportsEvent? Event { get; set; }
    public SportsEventsType Feed { get; set; } = null!;
}
