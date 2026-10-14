using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data.Models;

[Index("SourceId", Name = "IX_Pages_SourceId")]
[Index("UrlHash", Name = "UQ_Pages_UrlHash", IsUnique = true)]
public partial class Page
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [StringLength(2048)]
    public string Url { get; set; } = null!;

    [MaxLength(32)]
    public byte[]? UrlHash { get; set; }

    [StringLength(500)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? Published { get; set; }

    public DateTime ScrapedAt { get; set; }

    public int SourceId { get; set; }

    [StringLength(2048)]
    public string? ImageUrl { get; set; }

    [ForeignKey("SourceId")]
    [InverseProperty("Pages")]
    public virtual Source Source { get; set; } = null!;
}
