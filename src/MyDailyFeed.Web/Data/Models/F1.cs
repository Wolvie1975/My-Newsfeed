using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyDailyFeed.Web.Data.Models;

[Keyless]
public sealed class F1Source
{
    [Column("ID")] public int Id { get; set; }
    public bool Enabled { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public string? LastError { get; set; }
}
[Keyless]
public sealed class F1Race
{
    public int Season { get; set; }
    public int Round { get; set; }
    public string RaceName { get; set; } = "";
    public string CircuitName { get; set; } = "";
    public string? Locality { get; set; }
    public string? Country { get; set; }
    public DateOnly RaceDate { get; set; }
    public string? RaceTimeUtc { get; set; }
    public string? RaceUrl { get; set; }
    public string? FirstPractice { get; set; }
    public string? SecondPractice { get; set; }
    public string? ThirdPractice { get; set; }
    public string? Qualifying { get; set; }
    public string? Sprint { get; set; }
    public string? SprintQualifying { get; set; }
    public string? SprintShootout { get; set; }
    public DateTime FetchedAt { get; set; }
}
[Keyless]
public sealed class F1Classification
{
    public int Season { get; set; }
    public int Round { get; set; }
    public string SessionType { get; set; } = "";
    public int Position { get; set; }
    public string? PositionText { get; set; }
    [Column(TypeName = "decimal(12,3)")] public decimal Points { get; set; }
    public string DriverId { get; set; } = "";
    public string? DriverCode { get; set; }
    public string GivenName { get; set; } = "";
    public string FamilyName { get; set; } = "";
    public string ConstructorName { get; set; } = "";
    public int Grid { get; set; }
    public int Laps { get; set; }
    public string? Status { get; set; }
    public string? TimeDisplay { get; set; }
    public long? TimeMillis { get; set; }
    public string? FastestLapTime { get; set; }
    public int? FastestLapRank { get; set; }
    public DateTime FetchedAt { get; set; }
}
[Keyless]
public sealed class F1DriverStanding
{
    public int Season { get; set; }
    public int StandingsRound { get; set; }
    public int Position { get; set; }
    public string? PositionText { get; set; }
    [Column(TypeName = "decimal(12,3)")] public decimal Points { get; set; }
    public int Wins { get; set; }
    public string DriverId { get; set; } = "";
    public string? DriverCode { get; set; }
    public string GivenName { get; set; } = "";
    public string FamilyName { get; set; } = "";
    public string? ConstructorsJson { get; set; }
    public DateTime FetchedAt { get; set; }
}
[Keyless]
public sealed class F1ConstructorStanding
{
    public int Season { get; set; }
    public int StandingsRound { get; set; }
    public int Position { get; set; }
    public string? PositionText { get; set; }
    [Column(TypeName = "decimal(12,3)")] public decimal Points { get; set; }
    public int Wins { get; set; }
    public string ConstructorId { get; set; } = "";
    public string ConstructorName { get; set; } = "";
    public string? Nationality { get; set; }
    public DateTime FetchedAt { get; set; }
}
