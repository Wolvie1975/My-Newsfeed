using myNewspaper.Shared.Data;
using myNewspaper.Web.Components.Phone;
using Xunit;

namespace myNewspaper.Tests;

public sealed class TechCoveredTests
{
    private static readonly DateTime BaseTime = new(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);

    private static FeedItem Story(
        int id,
        string title,
        string sourceLabel,
        string sourceUrl = "https://example.com/feed",
        string? description = null,
        string? category = "Linux",
        int minutesAfterBase = 0) =>
        new(
            Id: id,
            Title: title,
            Url: $"https://example.com/articles/{id}",
            Description: description,
            ImageUrl: null,
            SourceLabel: sourceLabel,
            SourceUrl: sourceUrl,
            CategoryId: 1,
            CategoryName: category,
            Date: BaseTime.AddMinutes(minutesAfterBase));

    [Fact]
    public void Two_Near_Identical_Titles_From_Different_Sources_Group()
    {
        var items = new[]
        {
            Story(1, "Mesa 26.3 graphics driver brings Vulkan ray-tracing speedups", "Phoronix", "https://phoronix.com/rss", "Summary from Phoronix", minutesAfterBase: 0),
            Story(2, "Mesa 26.3 graphics driver brings major Vulkan ray-tracing speedups", "9to5Linux", "https://9to5linux.com/feed", null, minutesAfterBase: 15),
        };

        var covered = TechQuery.GroupMostCovered(items, minSources: 2);

        var group = Assert.Single(covered);
        Assert.Equal(2, group.Sources);
        Assert.Equal(1, group.Lead.Id);
        Assert.Equal(["Phoronix", "9to5Linux"], group.SourceNames);
        Assert.Equal(["PH", "9L"], group.SourceMarks.Select(s => s.Mark));
    }

    [Fact]
    public void Same_Source_Twice_Does_Not_Group()
    {
        var items = new[]
        {
            Story(1, "Steam Deck OLED firmware update fixes audio regression", "PC Gamer", "https://pcgamer.com/rss"),
            Story(2, "Steam Deck OLED firmware update fixes audio regression today", "PC Gamer", "https://pcgamer.com/rss"),
        };

        var covered = TechQuery.GroupMostCovered(items, minSources: 2);

        Assert.Empty(covered);
    }

    [Fact]
    public void Linux_6_18_Released_And_Linux_Kernel_6_18_Is_Out_With_Group()
    {
        var items = new[]
        {
            Story(1, "Linux 6.18 released", "LWN", "https://lwn.net/headlines/rss", description: null, minutesAfterBase: 0),
            Story(2, "Linux kernel 6.18 is out with faster Btrfs, Rust abstractions and scheduler updates", "Phoronix", "https://phoronix.com/rss", description: "Linus Torvalds announced Linux 6.18 on Sunday afternoon.", minutesAfterBase: 10),
        };

        var covered = TechQuery.GroupMostCovered(items, minSources: 2);

        var group = Assert.Single(covered);
        Assert.Equal(2, group.Sources);
        // Story 2 has a summary while Story 1 does not, so Story 2 leads the group.
        Assert.Equal(2, group.Lead.Id);
    }

    [Fact]
    public void Unrelated_Titles_Sharing_One_Word_Do_Not_Group()
    {
        var items = new[]
        {
            Story(1, "Linux desktop market share climbs in latest Steam hardware survey", "GamingOnLinux", "https://gamingonlinux.com/rss"),
            Story(2, "Canonical announces Ubuntu Linux support for new enterprise servers", "OMG! Ubuntu", "https://omgubuntu.co.uk/feed"),
        };

        var covered = TechQuery.GroupMostCovered(items, minSources: 2);

        Assert.Empty(covered);
    }

    [Fact]
    public void Desk_Colors_Are_Distinct_For_Technology_VideoGames_And_Linux()
    {
        Assert.Equal("mx-c-tech", PhoneColors.ForCategory("Technology"));
        Assert.Equal("mx-c-games", PhoneColors.ForCategory("Video Games"));
        Assert.Equal("mx-c-linux", PhoneColors.ForCategory("Linux"));
    }
}

