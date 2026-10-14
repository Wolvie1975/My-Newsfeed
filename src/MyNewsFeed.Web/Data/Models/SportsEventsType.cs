using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data.Models;

[Table("SportsEventsType")]
[Index("RssUrl", Name = "UQ_SportsEventsType_RssUrl", IsUnique = true)]
public partial class SportsEventsType
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    public string RssUrl { get; set; } = null!;

    [StringLength(200)]
    public string EventsTypeName { get; set; } = null!;

    public DateTime DateAdded { get; set; }

    [StringLength(100)]
    public string? SchoolName { get; set; }

    [InverseProperty("SportsEventsType")]
    public virtual ICollection<SportsEvent> SportsEvents { get; set; } = new List<SportsEvent>();
}
