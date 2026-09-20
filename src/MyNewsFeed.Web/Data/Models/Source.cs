using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data.Models;

[Index("SourceCategoryId", Name = "IX_Sources_SourceCategoryId")]
[Index("UrlHash", Name = "UQ_Sources_UrlHash", IsUnique = true)]
public partial class Source
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [StringLength(2048)]
    public string Url { get; set; } = null!;

    [MaxLength(32)]
    public byte[]? UrlHash { get; set; }

    [StringLength(200)]
    public string? Label { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastScrapedAt { get; set; }

    [StringLength(1000)]
    public string? LastError { get; set; }

    public int? SourceCategoryId { get; set; }

    [InverseProperty("Source")]
    public virtual ICollection<Page> Pages { get; set; } = new List<Page>();

    [ForeignKey("SourceCategoryId")]
    [InverseProperty("Sources")]
    public virtual SourceCategory? SourceCategory { get; set; }
}
