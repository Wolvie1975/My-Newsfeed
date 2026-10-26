using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace myNewspaper.Web.Data.Models;

/// <summary>A YouTube channel category (Sports, Tech…), separate from the news SourceCategories. Owned by the scraper.</summary>
[Table("YoutubeCategories")]
[Index("CategoryName", IsUnique = true)]
public partial class YoutubeCategory
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("Category_Name")]
    [StringLength(100)]
    public string CategoryName { get; set; } = null!;

    public DateTime DateAdded { get; set; }

    [InverseProperty("YoutubeCategory")]
    public virtual ICollection<YoutubeVideoFeed> YoutubeVideoFeeds { get; set; } = new List<YoutubeVideoFeed>();
}
