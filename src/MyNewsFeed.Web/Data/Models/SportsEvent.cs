using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data.Models;

[Index("EventDate", Name = "IX_SportsEvents_EventDate")]
[Index("SportsEventsTypeId", Name = "IX_SportsEvents_SportsEventsTypeId")]
[Index("UrlHash", Name = "UQ_SportsEvents_UrlHash", IsUnique = true)]
public partial class SportsEvent
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [StringLength(2048)]
    public string Url { get; set; } = null!;

    [MaxLength(32)]
    public byte[]? UrlHash { get; set; }

    public int? GameId { get; set; }

    [StringLength(500)]
    public string Title { get; set; } = null!;

    [StringLength(100)]
    public string? Sport { get; set; }

    [StringLength(200)]
    public string? Opponent { get; set; }

    public bool? IsAway { get; set; }

    [StringLength(300)]
    public string? Location { get; set; }

    public DateOnly EventDate { get; set; }

    public DateTime? StartsAtUtc { get; set; }

    public DateTime? EndsAtUtc { get; set; }

    public bool TimeTbd { get; set; }

    [StringLength(200)]
    public string? Tv { get; set; }

    [StringLength(2048)]
    public string? StreamUrl { get; set; }

    [StringLength(2048)]
    public string? LiveStatsUrl { get; set; }

    [StringLength(2048)]
    public string? TeamLogoUrl { get; set; }

    [StringLength(2048)]
    public string? OpponentLogoUrl { get; set; }

    public DateTime FirstSeenAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public int? SportsEventsTypeId { get; set; }

    [ForeignKey("SportsEventsTypeId")]
    [InverseProperty("SportsEvents")]
    public virtual SportsEventsType? SportsEventsType { get; set; }
}
