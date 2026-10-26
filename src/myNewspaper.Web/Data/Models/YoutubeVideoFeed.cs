using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace myNewspaper.Web.Data.Models;

[Table("YoutubeVideoFeed")]
[Index("ChannelId", Name = "UQ_YoutubeVideoFeed_ChannelId", IsUnique = true)]
public partial class YoutubeVideoFeed
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [StringLength(40)]
    public string ChannelId { get; set; } = null!;

    [StringLength(200)]
    public string? ChannelName { get; set; }

    [StringLength(2048)]
    public string Url { get; set; } = null!;

    public DateTime DateAdded { get; set; }

    /// <summary>The channel's category; null until one is assigned (shown under "Other").</summary>
    public int? YoutubeCategoryId { get; set; }

    [ForeignKey("YoutubeCategoryId")]
    [InverseProperty("YoutubeVideoFeeds")]
    public virtual YoutubeCategory? YoutubeCategory { get; set; }

    [InverseProperty("YoutubeVideoFeed")]
    public virtual ICollection<YouTubeVideo> YouTubeVideos { get; set; } = new List<YouTubeVideo>();
}
