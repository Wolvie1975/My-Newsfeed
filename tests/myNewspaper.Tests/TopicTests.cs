using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;
using myNewspaper.Web.Profiles;

namespace myNewspaper.Tests;

/// <summary>Your topics (docs/news-section-handoff.md, phase 5).</summary>
public class TopicTests
{
    private static readonly DateTime Now = new(2026, 10, 28, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Alternatives_are_split_on_commas_trimmed_and_kept_to_the_rules()
    {
        Assert.Equal(["Chiefs stadium", "Arrowhead"], TopicQuery.Split(" Chiefs   stadium , Arrowhead,, ", 5));
        // 2–60 characters, no repeats in any capitals, at most five.
        Assert.Equal(["KC", "Royals"], TopicQuery.Split("K, KC, kc, Royals, " + new string('x', 61), 5));
        Assert.Equal(["a1", "a2", "a3", "a4", "a5"], TopicQuery.Split("a1,a2,a3,a4,a5,a6", 5));
        Assert.Empty(TopicQuery.Split(null, 5));
    }

    [Fact]
    public void Matches_are_whole_words_in_any_capitals_and_phrases_as_written()
    {
        Assert.Equal([new("Royals ", false), new("Kansas City", true), new(" win; ", false), new("kansas  city", true), new(" again", false)],
            TopicQuery.Highlight("Royals Kansas City win; kansas  city again", "Kansas City"));
        // "Chiefs" is a whole word in "Chiefs'" but not in "Mischiefs".
        Assert.Equal([new("Mischiefs and ", false), new("Chiefs", true), new("' win", false)],
            TopicQuery.Highlight("Mischiefs and Chiefs' win", "Chiefs"));
        Assert.Equal([new("Nothing here", false)], TopicQuery.Highlight("Nothing here", "Royals"));
        // Regex characters in a term are taken literally.
        Assert.Equal([new("Learn ", false), new("C++", true)], TopicQuery.Highlight("Learn C++", "C++, (x"));
    }

    [Theory]
    [InlineData(0, "none")]
    [InlineData(1, "good")]
    [InlineData(100, "good")]
    [InlineData(101, "broad")]
    public void A_week_of_matches_gets_one_of_three_verdicts(int perWeek, string verdict) =>
        Assert.Equal(verdict, TopicQuery.VerdictFor(perWeek));

    [Theory]
    [InlineData("x", null, null)]
    [InlineData("Royals", null, "politics")]
    [InlineData("", null, null)]
    public void The_form_needs_a_word_of_two_or_more_and_a_known_section(string terms, string? except, string? hub) =>
        Assert.Throws<ArgumentException>(() => TopicQuery.Normalize(terms, except, hub));

    [Fact]
    public void The_form_is_stored_tidied()
    {
        Assert.Equal(("Chiefs stadium, Arrowhead", "Chiefs", "sports"), TopicQuery.Normalize("Chiefs stadium ,Arrowhead", " Chiefs ,", "sports"));
        Assert.Equal(("Royals", (string?)null, (string?)null), TopicQuery.Normalize("Royals", "", ""));
    }

    private static WebScraperContext Create() =>
        new(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(TestDatabase.ConnectionString).Options);

    private static Task<Profile> NewProfile(WebScraperContext db) =>
        ProfileStore.CreateAsync(db, new ProfilePins(new PasswordHasher<Profile>()), "zz " + Guid.NewGuid().ToString("N")[..10], "#0051ba", "1234", null);

    [Fact]
    public async Task Topics_belong_to_one_profile_stop_at_twenty_and_go_with_the_profile()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var mine = await NewProfile(db);
        var theirs = await NewProfile(db);

        var topic = await TopicQuery.CreateAsync(db, mine.Id, "Kansas City", "Chiefs", null, Now);
        Assert.Equal(("Kansas City", "Chiefs", (string?)null), (topic.Terms, topic.ExceptTerms, topic.Hub));
        // Another profile can't open, change, mark or remove it: each answers as an unknown ID does.
        Assert.Null(await TopicQuery.GetAsync(db, theirs.Id, topic.Id));
        Assert.False(await TopicQuery.UpdateAsync(db, theirs.Id, topic.Id, "Royals", null, null));
        Assert.False(await TopicQuery.MarkReadAsync(db, theirs.Id, topic.Id, Now));
        Assert.False(await TopicQuery.DeleteAsync(db, theirs.Id, topic.Id));
        Assert.Empty(await TopicQuery.ListAsync(db, theirs.Id));

        // Its owner can.
        Assert.True(await TopicQuery.UpdateAsync(db, mine.Id, topic.Id, "Royals, KC Royals", null, "sports"));
        Assert.True(await TopicQuery.MarkReadAsync(db, mine.Id, topic.Id, Now.AddHours(1)));
        var saved = (await TopicQuery.GetAsync(db, mine.Id, topic.Id))!;
        Assert.Equal(("Royals, KC Royals", (string?)null, "sports", Now.AddHours(1)), (saved.Terms, saved.ExceptTerms, saved.Hub, TopicQuery.NewSince(saved)));

        for (var i = 1; i < TopicQuery.Limit; i++) await TopicQuery.CreateAsync(db, mine.Id, $"topic {i}", null, null, Now);
        await Assert.ThrowsAsync<TopicQuery.TooManyTopicsException>(() => TopicQuery.CreateAsync(db, mine.Id, "one more", null, null, Now));
        Assert.Equal(TopicQuery.Limit, await TopicQuery.CountAsync(db, mine.Id));

        // Deleting the profile deletes its topics.
        Assert.True(await ProfileAdmin.DeleteAsync(db, mine.Id));
        Assert.Equal(0, await db.ProfileTopics.CountAsync(t => t.ProfileId == mine.Id));
    }
}
