using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Tests;

public class VideosTests
{
    [Theory]
    [InlineData(null, "abc_DEF-12", "https://i.ytimg.com/vi/abc_DEF-12/mqdefault.jpg")]
    [InlineData("", "abc", "https://i.ytimg.com/vi/abc/mqdefault.jpg")]
    [InlineData("http://i.ytimg.com/vi/x/hq.jpg", "x", "https://i.ytimg.com/vi/x/hq.jpg")]
    [InlineData("javascript:alert(1)", "x", "https://i.ytimg.com/vi/x/mqdefault.jpg")]
    [InlineData(null, "a/../b", null)]
    [InlineData(null, "", null)]
    [InlineData(null, null, null)]
    public void Thumbnails_use_the_stored_picture_else_youtubes_standard_one(string? stored, string? id, string? expected) =>
        Assert.Equal(expected, VideosText.Thumbnail(stored, id));

    [Theory]
    [InlineData(0, "0 views")]
    [InlineData(1, "1 view")]
    [InlineData(999, "999 views")]
    [InlineData(1234, "1,234 views")]
    [InlineData(9999, "9,999 views")]
    [InlineData(12345, "12.3K views")]
    [InlineData(120000, "120K views")]
    [InlineData(1200000, "1.2M views")]
    [InlineData(3000000000, "3B views")]
    public void View_counts_are_short(long count, string expected) => Assert.Equal(expected, VideosText.Views(count));

    [Fact]
    public void An_unknown_view_count_shows_nothing()
    {
        Assert.Null(VideosText.Views(null));
        Assert.Null(VideosText.Views(-5));
    }

    [Theory]
    [InlineData("UCabc-_123", "https://www.youtube.com/channel/UCabc-_123")]
    [InlineData("UC x", null)]
    [InlineData("", null)]
    public void Channel_links_are_built_from_the_id(string id, string? expected) =>
        Assert.Equal(expected, VideosText.ChannelUrl(id));

    [Theory]
    [InlineData(null, null, "")]
    [InlineData("UC1", null, "?channel=UC1")]
    [InlineData(null, "big 12", "?q=big%2012")]
    [InlineData("UC1", "a&b", "?channel=UC1&q=a%26b")]
    public void Query_strings_keep_only_the_filters_that_are_set(string? channel, string? search, string expected) =>
        Assert.Equal(expected, VideosQuery.QueryString(channel, search));

    private static WebScraperContext Create() => new(
        new DbContextOptionsBuilder<WebScraperContext>()
            .UseSqlServer("Server=sql2025,1433;Database=WebScraper;User Id=sa;Password=Fedora_Dev_2025!;Encrypt=False;TrustServerCertificate=True;")
            .Options);

    private static string Tag() => Guid.NewGuid().ToString("N")[..10];

    private static YouTubeVideo Video(string token, string channel, string title, DateTime published, long? views = null,
        string? channelName = null, YoutubeVideoFeed? feed = null) => new()
    {
        VideoId = Guid.NewGuid().ToString("N")[..11],
        ChannelId = $"UC{token}{channel}",
        ChannelName = channelName,
        Title = $"zz {token} {title}",
        Url = $"https://www.youtube.com/watch?v={token}",
        PublishedAt = published,
        ViewCount = views,
        FirstSeenAt = DateTime.UtcNow,
        LastSeenAt = DateTime.UtcNow,
        YoutubeVideoFeed = feed,
    };

    [Fact]
    public async Task Videos_come_newest_first_and_can_be_filtered_by_channel_and_search()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = Tag();
        db.YouTubeVideos.AddRange(
            Video(token, "A", "old", new DateTime(2026, 1, 1)),
            Video(token, "A", "new", new DateTime(2026, 3, 1)),
            Video(token, "B", "middle news", new DateTime(2026, 2, 1)));
        await db.SaveChangesAsync();

        var all = await VideosQuery.GetVideosAsync(db, null, token);
        Assert.Equal(new[] { "new", "middle news", "old" }, all.Items.Select(i => i.Title.Split(' ', 3)[2]));
        Assert.Equal(3, all.Total);

        var one = await VideosQuery.GetVideosAsync(db, $"UC{token}A", token);
        Assert.Equal(2, one.Total);
        Assert.All(one.Items, i => Assert.Equal($"UC{token}A", i.ChannelId));

        var found = await VideosQuery.GetVideosAsync(db, null, $"{token} middle");
        Assert.Single(found.Items);
    }

    [Fact]
    public async Task The_channel_name_prefers_the_lookup_table_then_the_video_then_the_id()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = Tag();
        var feed = new YoutubeVideoFeed
        {
            ChannelId = $"UC{token}A", ChannelName = $"Feed {token}", Url = $"https://{token}.test.invalid/feed", DateAdded = DateTime.UtcNow,
        };
        db.YouTubeVideos.AddRange(
            Video(token, "A", "a", new DateTime(2026, 1, 1), channelName: "Video copy", feed: feed),
            Video(token, "B", "b", new DateTime(2026, 1, 1), channelName: $"Own {token}"),
            Video(token, "C", "c", new DateTime(2026, 1, 1)));
        await db.SaveChangesAsync();

        var page = await VideosQuery.GetVideosAsync(db, null, token);
        var names = page.Items.ToDictionary(i => i.ChannelId, i => i.ChannelName);
        Assert.Equal($"Feed {token}", names[$"UC{token}A"]);
        Assert.Equal($"Own {token}", names[$"UC{token}B"]);
        Assert.Equal($"UC{token}C", names[$"UC{token}C"]);

        var channels = await VideosQuery.GetChannelsAsync(db);
        Assert.Contains(channels, c => c.ChannelId == $"UC{token}A" && c.Name == $"Feed {token}");
    }

    [Fact]
    public async Task The_page_is_capped_but_the_total_is_not()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = Tag();
        for (var i = 0; i < 5; i++)
        {
            db.YouTubeVideos.Add(Video(token, "A", $"v{i}", new DateTime(2026, 1, 1).AddDays(i)));
        }
        await db.SaveChangesAsync();

        var page = await VideosQuery.GetVideosAsync(db, null, token, limit: 3);
        Assert.Equal(3, page.Items.Count);
        Assert.Equal(5, page.Total);
    }
}
