using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;
using myNewspaper.Web.Profiles;

namespace myNewspaper.Tests;

public class VideosTests
{
    [Theory]
    [InlineData(null, "abc_DEF-12", "https://i.ytimg.com/vi/abc_DEF-12/mqdefault.jpg")]
    [InlineData("", "abc", "https://i.ytimg.com/vi/abc/mqdefault.jpg")]
    [InlineData("http://i.ytimg.com/vi/x/hq720_custom_1.jpg", "x", "https://i.ytimg.com/vi/x/mqdefault.jpg")]
    [InlineData("http://i.ytimg.com/vi/x/hq.jpg", "a/../b", "https://i.ytimg.com/vi/x/hq.jpg")]
    [InlineData("javascript:alert(1)", "x", "https://i.ytimg.com/vi/x/mqdefault.jpg")]
    [InlineData(null, "a/../b", null)]
    [InlineData(null, "", null)]
    [InlineData(null, null, null)]
    public void Thumbnails_use_youtubes_standard_picture_else_the_stored_one(string? stored, string? id, string? expected) =>
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

    [Fact]
    public void Home_rotation_limits_channels_is_stable_within_hour_and_changes_next_hour()
    {
        var collected = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        var candidates = Enumerable.Range(1, 40).Select(i => new VideoItem(
            i, $"video{i}", $"channel{i}", $"Channel {i}", $"Video {i}",
            $"https://example.com/{i}", collected.AddMinutes(-i), null, null)).ToList();
        candidates.Add(candidates[0] with { Id = 100, PublishedAt = collected.AddDays(-1) });

        var selected = VideosQuery.SelectHomeVideos(candidates, collected);
        Assert.Equal(10, selected.Count);
        Assert.Equal(10, selected.Select(v => v.ChannelId).Distinct().Count());
        Assert.DoesNotContain(selected, v => v.Id == 100);
        Assert.Equal(selected, VideosQuery.SelectHomeVideos(candidates.AsEnumerable().Reverse(), collected.AddMinutes(59)));
        Assert.False(selected.Select(v => v.Id).SequenceEqual(
            VideosQuery.SelectHomeVideos(candidates, collected.AddHours(1)).Select(v => v.Id)));
        Assert.Equal(selected.OrderByDescending(v => v.PublishedAt), selected);
        Assert.Equal(40, VideosQuery.SelectHomeVideos(candidates, collected, 100).Count);
        Assert.Empty(VideosQuery.SelectHomeVideos(candidates, collected, 0));
        Assert.Empty(VideosQuery.SelectHomeVideos([], collected));
    }

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

    [Theory]
    [InlineData("News & Politics", "news-politics")]
    [InlineData("Video Games", "video-games")]
    [InlineData("  Tech ", "tech")]
    [InlineData("Sports", "sports")]
    public void Category_slugs_are_lower_case_words_joined_by_hyphens(string name, string slug) =>
        Assert.Equal(slug, VideosQuery.Slug(name));

    [Fact]
    public async Task Category_tabs_filter_videos_and_channels_keep_uncategorized_under_other_and_hide_empty_ones()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = Tag();
        var tech = new YoutubeCategory { CategoryName = $"Zz {token} Tech", DateAdded = DateTime.UtcNow };
        var empty = new YoutubeCategory { CategoryName = $"Zz {token} Empty", DateAdded = DateTime.UtcNow };
        db.YoutubeCategories.AddRange(tech, empty);
        YoutubeVideoFeed Feed(string channel, YoutubeCategory? category) => new()
        {
            ChannelId = $"UC{token}{channel}",
            ChannelName = $"Feed {channel}",
            Url = $"https://{token}{channel}.test.invalid/feed",
            DateAdded = DateTime.UtcNow,
            YoutubeCategory = category,
        };
        var techFeed = Feed("A", tech);
        var loose = Feed("B", null);
        db.YouTubeVideos.AddRange(
            Video(token, "A", "tech one", new DateTime(2026, 3, 1), feed: techFeed),
            Video(token, "A", "tech two", new DateTime(2026, 2, 1), feed: techFeed),
            Video(token, "B", "loose", new DateTime(2026, 1, 1), feed: loose),
            Video(token, "C", "no feed", new DateTime(2026, 1, 2)));
        await db.SaveChangesAsync();

        var tabs = await VideosQuery.GetCategoriesAsync(db, Viewer.Household);
        var techTab = Assert.Single(tabs, c => c.Name == tech.CategoryName);
        Assert.Equal(VideosQuery.Slug(tech.CategoryName), techTab.Slug);
        Assert.DoesNotContain(tabs, c => c.Name == empty.CategoryName);
        Assert.Equal(VideosQuery.Other, tabs[^1]);

        var inTech = await VideosQuery.GetVideosAsync(db, Viewer.Household, null, token, category: techTab);
        Assert.Equal(new[] { "tech one", "tech two" }, inTech.Items.Select(i => i.Title.Split(' ', 3)[2]));
        Assert.All(inTech.Items, i => Assert.Equal(tech.CategoryName, i.Category));
        // Uncategorized channels and videos without a channel row stay visible: under Other and under All.
        var other = await VideosQuery.GetVideosAsync(db, Viewer.Household, null, token, category: VideosQuery.Other);
        Assert.Equal(new[] { "no feed", "loose" }, other.Items.Select(i => i.Title.Split(' ', 3)[2]));
        Assert.All(other.Items, i => Assert.Null(i.Category));
        Assert.Equal(4, (await VideosQuery.GetVideosAsync(db, Viewer.Household, null, token)).Total);

        var techChannels = await VideosQuery.GetChannelsAsync(db, Viewer.Household, techTab);
        Assert.Contains(techChannels, c => c.ChannelId == $"UC{token}A");
        Assert.DoesNotContain(techChannels, c => c.ChannelId == $"UC{token}B");
    }

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

        var all = await VideosQuery.GetVideosAsync(db, Viewer.Household, null, token);
        Assert.Equal(new[] { "new", "middle news", "old" }, all.Items.Select(i => i.Title.Split(' ', 3)[2]));
        Assert.Equal(3, all.Total);

        var one = await VideosQuery.GetVideosAsync(db, Viewer.Household, $"UC{token}A", token);
        Assert.Equal(2, one.Total);
        Assert.All(one.Items, i => Assert.Equal($"UC{token}A", i.ChannelId));

        var found = await VideosQuery.GetVideosAsync(db, Viewer.Household, null, $"{token} middle");
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
            ChannelId = $"UC{token}A",
            ChannelName = $"Feed {token}",
            Url = $"https://{token}.test.invalid/feed",
            DateAdded = DateTime.UtcNow,
        };
        db.YouTubeVideos.AddRange(
            Video(token, "A", "a", new DateTime(2026, 1, 1), channelName: "Video copy", feed: feed),
            Video(token, "B", "b", new DateTime(2026, 1, 1), channelName: $"Own {token}"),
            Video(token, "C", "c", new DateTime(2026, 1, 1)));
        await db.SaveChangesAsync();

        var page = await VideosQuery.GetVideosAsync(db, Viewer.Household, null, token);
        var names = page.Items.ToDictionary(i => i.ChannelId, i => i.ChannelName);
        Assert.Equal($"Feed {token}", names[$"UC{token}A"]);
        Assert.Equal($"Own {token}", names[$"UC{token}B"]);
        Assert.Equal($"UC{token}C", names[$"UC{token}C"]);

        var channels = await VideosQuery.GetChannelsAsync(db, Viewer.Household);
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

        var page = await VideosQuery.GetVideosAsync(db, Viewer.Household, null, token, limit: 3);
        Assert.Equal(3, page.Items.Count);
        Assert.Equal(5, page.Total);
        var next = await VideosQuery.GetVideosAsync(db, Viewer.Household, null, token, limit: 3, offset: 3);
        Assert.Equal(2, next.Items.Count); Assert.Equal(5, next.Total);
        Assert.Equal(5, page.Items.Concat(next.Items).Select(i => i.Id).Distinct().Count());
    }

    [Fact]
    public async Task The_latest_per_channel_is_one_video_each_newest_channel_first_with_the_feed_name()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = Tag();
        var feed = new YoutubeVideoFeed
        {
            ChannelId = $"UC{token}A",
            ChannelName = $"Feed {token}",
            Url = $"https://{token}.test.invalid/feed",
            DateAdded = DateTime.UtcNow,
        };
        db.YouTubeVideos.AddRange(
            Video(token, "A", "a old", new DateTime(2031, 1, 1), channelName: "Video copy", feed: feed),
            Video(token, "A", "a new", new DateTime(2031, 3, 1), channelName: "Video copy", feed: feed),
            Video(token, "B", "b only", new DateTime(2031, 4, 1)),
            Video(token, "C", "c tie one", new DateTime(2031, 2, 1)),
            Video(token, "C", "c tie two", new DateTime(2031, 2, 1)));
        await db.SaveChangesAsync();

        var latest = (await VideosQuery.GetLatestPerChannelAsync(db, Viewer.Household)).Where(v => v.ChannelId.StartsWith($"UC{token}")).ToList();

        Assert.Equal(new[] { $"UC{token}B", $"UC{token}A", $"UC{token}C" }, latest.Select(v => v.ChannelId));
        Assert.Equal($"zz {token} a new", latest[1].Title);
        Assert.Equal($"Feed {token}", latest[1].ChannelName);
        Assert.Equal($"zz {token} c tie two", latest[2].Title);   // a tie keeps the higher id
    }
}
