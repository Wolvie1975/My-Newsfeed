using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data.Models;

[Index("Sport", "League", "TeamId", "GroupId", Name = "UQ_ResultFollows_Team", IsUnique = true)]
public partial class ResultFollow
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [StringLength(10)]
    public string Kind { get; set; } = null!;

    [StringLength(40)]
    public string Sport { get; set; } = null!;

    [StringLength(60)]
    public string League { get; set; } = null!;

    [StringLength(20)]
    public string? TeamId { get; set; }

    /// <summary>ESPN's conference id, on a conference follow only; see db/013 for which league numbers it.</summary>
    [StringLength(20)]
    public string? GroupId { get; set; }

    [StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(2048)]
    public string? LogoUrl { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastSyncedAt { get; set; }

    [StringLength(1000)]
    public string? LastError { get; set; }
}
