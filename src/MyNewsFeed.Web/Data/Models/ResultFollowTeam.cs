using System.ComponentModel.DataAnnotations;

namespace MyNewsFeed.Web.Data.Models;

/// <summary>A member school of a conference follow, by ESPN team id; the sync replaces a follow's rows on every run.</summary>
public class ResultFollowTeam
{
    public int FollowId { get; set; }

    [StringLength(20)]
    public string TeamId { get; set; } = null!;
}
