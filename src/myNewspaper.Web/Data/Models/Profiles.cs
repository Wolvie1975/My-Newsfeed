using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace myNewspaper.Web.Data.Models;

// App-owned profile tables (db/020). MyDailyFeedProfileSaved stays raw SQL inside SharedArticles, like the shared list.

[Table("MyDailyFeedProfiles")]
public class Profile
{
    public int Id { get; set; }
    [MaxLength(40)] public string Name { get; set; } = "";
    [Column(TypeName = "char(7)")] public string Color { get; set; } = "#0051BA";
    public bool Hidden { get; set; }
    public int SortOrder { get; set; }
    [MaxLength(200)] public string? PinHash { get; set; }   // null = must choose a PIN on next open
    public int FailedPinCount { get; set; }
    public DateTime? LockedAt { get; set; }                 // cleared only by the admin
    public Guid? LockedByDeviceId { get; set; }
    public string Settings { get; set; } = "{}";
    public DateTime? BrowserImportAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastReadAt { get; set; }
}

[Table("MyDailyFeedProfileDevices")]
public class ProfileDevice
{
    public Guid Id { get; set; }
    public int? ProfileId { get; set; }                     // null = device known, no profile chosen yet
    [MaxLength(32)] public byte[] TokenHash { get; set; } = [];
    [MaxLength(100)] public string Label { get; set; } = "";
    [MaxLength(300)] public string? UserAgent { get; set; }
    [MaxLength(16)] public string PickerMode { get; set; } = "every-visit";   // every-visit | daily | until-signout
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Profile? Profile { get; set; }
}

[Table("MyDailyFeedProfileTvFollows"), PrimaryKey(nameof(ProfileId), nameof(TvTrackedSeriesId))]
public class ProfileTvFollow
{
    public int ProfileId { get; set; }
    public int TvTrackedSeriesId { get; set; }
    public DateTime FollowedAt { get; set; }
}

[Table("MyDailyFeedProfileChannelFollows"), PrimaryKey(nameof(ProfileId), nameof(YoutubeVideoFeedId))]
public class ProfileChannelFollow
{
    public int ProfileId { get; set; }
    public int YoutubeVideoFeedId { get; set; }
    public DateTime FollowedAt { get; set; }
}

/// <summary>A household channel already turned on for every profile; the rollout never re-adds a removed follow.</summary>
[Table("MyDailyFeedChannelRollouts")]
public class ChannelRollout
{
    [Key] public int YoutubeVideoFeedId { get; set; }
    public DateTime RolledOutAt { get; set; }
}

[Table("MyDailyFeedContentRequests")]
public class ContentRequest
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    [MaxLength(16)] public string Kind { get; set; } = "youtube";   // youtube | news
    [MaxLength(200)] public string Name { get; set; } = "";
    [MaxLength(2048)] public string? Link { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
    [MaxLength(16)] public string Status { get; set; } = "waiting";  // waiting | added | declined
    public bool? CheckOk { get; set; }                                // null = not checked yet
    [MaxLength(300)] public string? CheckSummary { get; set; }
    [MaxLength(40)] public string? ResolvedChannelId { get; set; }
    public string? ResolvedFeedUrls { get; set; }                     // JSON array of discovered feed URLs
    [MaxLength(500)] public string? AdminReply { get; set; }          // shown to the requester when declined
    public int? YoutubeVideoFeedId { get; set; }
    public int? SourceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public Profile? Profile { get; set; }
}

/// <summary>The single household row (Id = 1): picker defaults, device retention and the base edition.</summary>
[Table("MyDailyFeedHousehold")]
public class Household
{
    public int Id { get; set; } = 1;
    [MaxLength(16)] public string DefaultPickerMode { get; set; } = "every-visit";
    public int? ForgetDevicesDays { get; set; }                       // null = never
    public DateTime? RequestsSeenAt { get; set; }                     // admin's "Mark all seen"
    public string BaseSettings { get; set; } = "{}";                  // same shape as Profile.Settings
}
