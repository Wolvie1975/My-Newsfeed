using myNewspaper.Shared.Data;
using Xunit;

namespace myNewspaper.Tests;

public sealed class TrendingQueryTests
{
    private static readonly DateTime BaseTime = new(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);

    private static FeedItem Story(int id, string title, string sourceLabel = "KCTV5") =>
        new(
            Id: id,
            Title: title,
            Url: $"https://example.com/story/{id}",
            Description: null,
            ImageUrl: null,
            SourceLabel: sourceLabel,
            SourceUrl: "https://example.com",
            CategoryId: 1,
            CategoryName: "Top Headlines",
            Date: BaseTime.AddMinutes(id));

    [Fact]
    public void Board_Of_Education_Counts_As_One_Phrase()
    {
        var items = new[]
        {
            Story(1, "The Board of Education approved the new budget"),
            Story(2, "Community rallies outside Board of Education meeting"),
            Story(3, "Local Board of Education members face tough questions"),
        };

        var phrases = TrendingQuery.Phrases(items);

        var board = Assert.Single(phrases, p => p.Text.Contains("Board", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Board of Education", board.Text);
        Assert.Equal(3, board.Stories);
    }

    [Fact]
    public void Sentence_Start_Why_Is_Not_A_Phrase()
    {
        var items = new[]
        {
            Story(1, "Why voters rejected the stadium sales tax"),
            Story(2, "Why prices are still rising in grocery stores"),
            Story(3, "Why city leaders delayed the housing vote"),
        };

        var phrases = TrendingQuery.Phrases(items);

        Assert.DoesNotContain(phrases, p => p.Text.Equals("Why", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Phrase_In_2_Stories_Is_Dropped_And_One_In_3_Is_Kept()
    {
        var items = new[]
        {
            Story(1, "Mayor Lucas speaks at city hall today"),
            Story(2, "Mayor Lucas meets with local business leaders"),
            Story(3, "Council votes down Mayor Lucas initiative"),
            Story(4, "Taylor Swift spotted in Kansas City restaurant"),
            Story(5, "Fans gather as Taylor Swift arrives downtown"),
        };

        var phrases = TrendingQuery.Phrases(items);

        Assert.Contains(phrases, p => p.Text == "Mayor Lucas" && p.Stories == 3);
        // "Taylor Swift" appears in only 2 stories, so it is dropped
        Assert.DoesNotContain(phrases, p => p.Text.Contains("Taylor Swift", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Source_Label_Is_Not_A_Phrase()
    {
        var items = new[]
        {
            Story(1, "Breaking news reported by Kansas City Star reporters", sourceLabel: "Kansas City Star"),
            Story(2, "Investigation published by Kansas City Star reveals fraud", sourceLabel: "Kansas City Star"),
            Story(3, "Editorial from Kansas City Star sparks public debate", sourceLabel: "Kansas City Star"),
        };

        var phrases = TrendingQuery.Phrases(items);

        Assert.DoesNotContain(phrases, p => p.Text.Equals("Kansas City Star", StringComparison.OrdinalIgnoreCase));
    }
}
