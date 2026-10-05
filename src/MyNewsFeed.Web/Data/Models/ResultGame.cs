using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data.Models;

[Index("Sport", "League", "AwayTeamId", Name = "IX_ResultGames_AwayTeam")]
[Index("Sport", "League", "HomeTeamId", Name = "IX_ResultGames_HomeTeam")]
[Index("StartsAtUtc", Name = "IX_ResultGames_StartsAtUtc")]
[Index("Sport", "League", "EventId", Name = "UQ_ResultGames_Event", IsUnique = true)]
public partial class ResultGame
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [StringLength(40)]
    public string Sport { get; set; } = null!;

    [StringLength(60)]
    public string League { get; set; } = null!;

    [StringLength(20)]
    public string EventId { get; set; } = null!;

    public DateTime StartsAtUtc { get; set; }

    [StringLength(10)]
    public string State { get; set; } = null!;

    public bool Completed { get; set; }

    [StringLength(60)]
    public string? StatusText { get; set; }

    [StringLength(20)]
    public string HomeTeamId { get; set; } = null!;

    [StringLength(200)]
    public string HomeName { get; set; } = null!;

    [StringLength(2048)]
    public string? HomeLogoUrl { get; set; }

    public int? HomeScore { get; set; }

    public int? HomeShootout { get; set; }

    [StringLength(20)]
    public string AwayTeamId { get; set; } = null!;

    [StringLength(200)]
    public string AwayName { get; set; } = null!;

    [StringLength(2048)]
    public string? AwayLogoUrl { get; set; }

    public int? AwayScore { get; set; }

    public int? AwayShootout { get; set; }

    [StringLength(4)]
    public string? WinnerSide { get; set; }

    [StringLength(300)]
    public string? Venue { get; set; }

    public DateTime FirstSeenAt { get; set; }

    public DateTime LastSeenAt { get; set; }
}
