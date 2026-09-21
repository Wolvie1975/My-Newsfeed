using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Tests;

// Integration tests against the live WebScraper database. Each test works in a transaction that is never committed.
public class AdminYoutubeTests
{
    private static WebScraperContext Create() => new(
        new DbContextOptionsBuilder<WebScraperContext>()
            .UseSqlServer("Server=sql2025,1433;Database=WebScraper;User Id=sa;Password=Fedora_Dev_2025!;Encrypt=False;TrustServerCertificate=True;")
            .Options);

    private static string Tag() => Guid.NewGuid().ToString("N")[..10];

    private static YouTubeVideo Video(string tag, string channelId, string? channelName = null, int n = 1) => new()
    {
        VideoId = $"zt{tag}{n}",
        ChannelId = channelId,
        ChannelName = channelName,
        Title = $"Test video {tag} {n}",
        Url = $"https://www.youtube.com/watch?v=zt{tag}{n}",
        PublishedAt = new DateTime(2026, 1, 1).AddMinutes(n),
    };

    [Theory]
    [InlineData("UC7Cj7VhPm234Zclm0_rTGPg", "https://www.youtube.com/feeds/videos.xml?channel_id=UC7Cj7VhPm234Zclm0_rTGPg")]
    [InlineData("  UCabc-_123  ", "https://www.youtube.com/feeds/videos.xml?channel_id=UCabc-_123")]
    [InlineData("UC a&b", "https://www.youtube.com/feeds/videos.xml?channel_id=UC%20a%26b")]
    public void Feed_url_is_the_standard_atom_address_for_the_channel(string channelId, string expected) =>
        Assert.Equal(expected, AdminData.YoutubeFeedUrl(channelId));

    [Fact]
    public async Task Sync_creates_a_feed_for_a_channel_that_has_none_and_links_its_videos()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var tag = Tag();
        var channel = $"UCzt{tag}";

        db.YouTubeVideos.AddRange(Video(tag, channel, "Older name", 1), Video(tag, channel, "Newer name", 2));
        await db.SaveChangesAsync();

        var result = await AdminData.SyncYoutubeFeedsAsync(db);

        Assert.True(result.FeedsCreated >= 1);
        Assert.True(result.VideosLinked >= 2);
        var feed = await db.YoutubeVideoFeeds.AsNoTracking().SingleAsync(f => f.ChannelId == channel);
        Assert.Equal(AdminData.YoutubeFeedUrl(channel), feed.Url);
        Assert.False(string.IsNullOrEmpty(feed.ChannelName));   // taken from the videos
        Assert.True(feed.DateAdded > DateTime.UtcNow.AddMinutes(-5));
        Assert.Equal(2, await db.YouTubeVideos.CountAsync(v => v.ChannelId == channel && v.YoutubeVideoFeedId == feed.Id));
    }

    [Fact]
    public async Task Sync_links_to_an_existing_feed_without_creating_a_duplicate_and_can_be_repeated()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var tag = Tag();
        var channel = $"UCzt{tag}";

        var feed = new YoutubeVideoFeed { ChannelId = channel, ChannelName = "My chosen name", Url = "https://example.test/my-feed" };
        db.YoutubeVideoFeeds.Add(feed);
        db.YouTubeVideos.Add(Video(tag, channel, "Name on the video"));
        await db.SaveChangesAsync();

        await AdminData.SyncYoutubeFeedsAsync(db);

        Assert.Equal(1, await db.YoutubeVideoFeeds.CountAsync(f => f.ChannelId == channel));
        var saved = await db.YoutubeVideoFeeds.AsNoTracking().SingleAsync(f => f.ChannelId == channel);
        Assert.Equal("My chosen name", saved.ChannelName);                 // an existing feed is never overwritten
        Assert.Equal("https://example.test/my-feed", saved.Url);
        Assert.Equal(feed.Id, (await db.YouTubeVideos.AsNoTracking().SingleAsync(v => v.ChannelId == channel)).YoutubeVideoFeedId);

        var again = await AdminData.SyncYoutubeFeedsAsync(db);
        Assert.Equal(new AdminData.YoutubeSyncResult(0, 0), again);         // nothing left to do
    }

    [Fact]
    public async Task Sync_leaves_videos_that_already_have_a_feed_alone()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var tag = Tag();
        var channel = $"UCzt{tag}";

        var chosen = new YoutubeVideoFeed { ChannelId = $"UCzt{tag}x", ChannelName = "Chosen by hand", Url = "https://example.test/x" };
        var matching = new YoutubeVideoFeed { ChannelId = channel, ChannelName = "Matches the channel", Url = "https://example.test/y" };
        var video = Video(tag, channel);
        video.YoutubeVideoFeed = chosen;   // deliberately linked to a different feed
        db.AddRange(chosen, matching, video);
        await db.SaveChangesAsync();

        await AdminData.SyncYoutubeFeedsAsync(db);

        Assert.Equal(chosen.Id, (await db.YouTubeVideos.AsNoTracking().SingleAsync(v => v.Id == video.Id)).YoutubeVideoFeedId);
    }

    [Fact]
    public async Task A_feed_without_videos_can_be_deleted()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var feed = new YoutubeVideoFeed { ChannelId = $"UCzt{Tag()}", Url = "https://example.test/f" };
        db.YoutubeVideoFeeds.Add(feed);
        await db.SaveChangesAsync();

        Assert.True(await AdminData.DeleteYoutubeFeedAsync(db, feed.Id, includeVideos: false));
        Assert.False(await AdminData.DeleteYoutubeFeedAsync(db, feed.Id, includeVideos: false));   // already gone
    }

    [Fact]
    public async Task A_feed_with_videos_is_protected_unless_its_videos_are_deleted_too()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var tag = Tag();
        var feed = new YoutubeVideoFeed { ChannelId = $"UCzt{tag}", ChannelName = "Doomed", Url = "https://example.test/d" };
        var mine = new[] { Video(tag, feed.ChannelId, n: 1), Video(tag, feed.ChannelId, n: 2) };
        foreach (var v in mine) { v.YoutubeVideoFeed = feed; }
        db.YoutubeVideoFeeds.Add(feed);
        db.YouTubeVideos.AddRange(mine);
        await db.SaveChangesAsync();
        // An unrelated video must survive.
        var bystander = Video(tag, $"UCzt{tag}other", n: 3);
        db.YouTubeVideos.Add(bystander);
        await db.SaveChangesAsync();

        var blocked = await Assert.ThrowsAnyAsync<Exception>(() => AdminData.DeleteYoutubeFeedAsync(db, feed.Id, includeVideos: false));
        Assert.True(AdminData.IsForeignKeyViolation(blocked));
        Assert.Equal(2, await db.YouTubeVideos.CountAsync(v => v.YoutubeVideoFeedId == feed.Id));

        Assert.True(await AdminData.DeleteYoutubeFeedAsync(db, feed.Id, includeVideos: true));
        Assert.False(await db.YoutubeVideoFeeds.AnyAsync(f => f.Id == feed.Id));
        Assert.Equal(0, await db.YouTubeVideos.CountAsync(v => v.YoutubeVideoFeedId == feed.Id));
        Assert.True(await db.YouTubeVideos.AnyAsync(v => v.Id == bystander.Id));
    }

    [Fact]
    public async Task Two_feeds_cannot_share_a_channel()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var channel = $"UCzt{Tag()}";
        db.YoutubeVideoFeeds.Add(new YoutubeVideoFeed { ChannelId = channel, Url = "https://example.test/1" });
        await db.SaveChangesAsync();

        db.YoutubeVideoFeeds.Add(new YoutubeVideoFeed { ChannelId = channel.ToLowerInvariant(), Url = "https://example.test/2" });
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.True(AdminData.IsUniqueViolation(ex));   // the database ignores letter case, so this counts as the same channel
    }

    [Fact]
    public async Task A_video_can_be_moved_between_feeds_and_unlinked_but_not_pointed_at_a_missing_feed()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var tag = Tag();
        var one = new YoutubeVideoFeed { ChannelId = $"UCzt{tag}a", Url = "https://example.test/a" };
        var two = new YoutubeVideoFeed { ChannelId = $"UCzt{tag}b", Url = "https://example.test/b" };
        var video = Video(tag, one.ChannelId);
        db.AddRange(one, two, video);
        await db.SaveChangesAsync();

        video.YoutubeVideoFeedId = one.Id;
        await db.SaveChangesAsync();
        video.YoutubeVideoFeedId = two.Id;
        await db.SaveChangesAsync();
        video.YoutubeVideoFeedId = null;
        await db.SaveChangesAsync();
        Assert.Null((await db.YouTubeVideos.AsNoTracking().SingleAsync(v => v.Id == video.Id)).YoutubeVideoFeedId);

        video.YoutubeVideoFeedId = 2_000_000_000;
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.True(AdminData.IsForeignKeyViolation(ex));
    }

    [Fact]
    public async Task No_test_rows_are_left_in_the_youtube_tables()
    {
        await using var db = Create();
        Assert.False(await db.YouTubeVideos.AnyAsync(v => v.VideoId.StartsWith("zt")));
        Assert.False(await db.YoutubeVideoFeeds.AnyAsync(f => f.ChannelId.StartsWith("UCzt")));
    }
}
