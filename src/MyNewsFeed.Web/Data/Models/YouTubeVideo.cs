using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data.Models;

[Index("ChannelId", "PublishedAt", Name = "IX_YouTubeVideos_Channel_PublishedAt", IsDescending = new[] { false, true })]
[Index("YoutubeVideoFeedId", Name = "IX_YouTubeVideos_YoutubeVideoFeedId")]
[Index("VideoId", Name = "UQ_YouTubeVideos_VideoId", IsUnique = true)]
public partial class YouTubeVideo
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [StringLength(20)]
    public string VideoId { get; set; } = null!;

    [StringLength(40)]
    public string ChannelId { get; set; } = null!;

    [StringLength(200)]
    public string? ChannelName { get; set; }

    [StringLength(500)]
    public string Title { get; set; } = null!;

    [StringLength(2048)]
    public string Url { get; set; } = null!;

    public DateTime PublishedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [StringLength(2048)]
    public string? ThumbnailUrl { get; set; }

    public string? Description { get; set; }

    public long? ViewCount { get; set; }

    public int? RatingCount { get; set; }

    [Column(TypeName = "decimal(3, 2)")]
    public decimal? RatingAverage { get; set; }

    public DateTime FirstSeenAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public int? YoutubeVideoFeedId { get; set; }

    [ForeignKey("YoutubeVideoFeedId")]
    [InverseProperty("YouTubeVideos")]
    public virtual YoutubeVideoFeed? YoutubeVideoFeed { get; set; }
}
