using MyNewsFeed.Web.Data;

namespace MyNewsFeed.Tests;

// Pure logic: no database needed.
public class FeedHelpersTests
{
    // ---- SafeHref -----------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("https://example.com/a", "https://example.com/a")]
    [InlineData("http://example.com/", "http://example.com/")]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("JaVaScRiPt:alert(1)", null)]
    [InlineData("data:text/html,<script>alert(1)</script>", null)]
    [InlineData("ftp://example.com/file", null)]
    [InlineData("/relative/path", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void SafeHref_only_allows_http_and_https(string? url, string? expected) =>
        Assert.Equal(expected, FeedQuery.SafeHref(url));

    // ---- Excerpt ------------------------------------------------------------------------------------------------

    [Fact]
    public void Excerpt_returns_short_text_unchanged_and_null_for_blank()
    {
        Assert.Equal("Short text", FeedQuery.Excerpt("  Short text  "));
        Assert.Null(FeedQuery.Excerpt(null));
        Assert.Null(FeedQuery.Excerpt("   "));
    }

    [Fact]
    public void Excerpt_cuts_long_text_at_a_word_boundary()
    {
        var text = string.Join(' ', Enumerable.Repeat("word", 100));
        var excerpt = FeedQuery.Excerpt(text, 50)!;

        Assert.EndsWith("…", excerpt);
        Assert.True(excerpt.Length <= 51);
        Assert.DoesNotContain("wor…", excerpt);
    }

    [Fact]
    public void Excerpt_hard_cuts_text_without_spaces() =>
        Assert.Equal(101, FeedQuery.Excerpt(new string('x', 500), 100)!.Length);

    // ---- Query string -------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(null, null, null, "")]
    [InlineData(8, null, null, "?category=8")]
    [InlineData(null, "rust lang", null, "?q=rust%20lang")]
    [InlineData(8, "a&b", "123.4", "?category=8&q=a%26b&after=123.4")]
    [InlineData(null, "   ", null, "")]
    public void QueryString_keeps_only_the_filters_that_are_set(int? category, string? search, string? after, string expected) =>
        Assert.Equal(expected, FeedQuery.QueryString(category, search, after));

    // ---- Cursor -------------------------------------------------------------------------------------------------

    [Fact]
    public void Cursor_round_trips_through_its_token()
    {
        var cursor = new FeedCursor(new DateTime(2026, 9, 20, 11, 39, 27, 123).AddTicks(4567), 542);
        Assert.Equal(cursor, FeedCursor.Parse(cursor.ToToken()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1.2.3")]
    [InlineData("12345")]
    [InlineData("-5.1")]
    [InlineData("99999999999999999999.1")]
    [InlineData("3155378975999999999.99999999999")]
    [InlineData("3155378976000000000.1")]
    [InlineData("1.x")]
    public void Cursor_rejects_malformed_tokens(string? token) => Assert.Null(FeedCursor.Parse(token));

    // ---- Emoji --------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("😈 Kentucky's jab after TAMU upset", "Kentucky's jab after TAMU upset")]
    [InlineData("Big news 🔥🔥 today", "Big news today")]
    [InlineData("Family 👨‍👩‍👧 photo", "Family photo")]
    [InlineData("Flag 🇺🇸 day", "Flag day")]
    [InlineData("Thumbs 👍🏽 up", "Thumbs up")]
    [InlineData("✅ Done ⭐", "Done")]
    [InlineData("Heart ❤️ works", "Heart works")]
    [InlineData("Time ⏰ check", "Time check")]
    public void StripEmoji_removes_emoji_and_tidies_spaces(string input, string expected) =>
        Assert.Equal(expected, FeedText.StripEmoji(input));

    [Theory]
    [InlineData("No emoji here")]
    [InlineData("Pokémon Champions — ‘quoted’ “text” … é ü ñ")]
    [InlineData("Rust™ 1.0 © 2026 ®")]
    [InlineData("Paramount+ and 100% of $5 & more")]
    [InlineData("Arrows → and ← stay, as do ± and ×")]
    [InlineData("日本語のタイトル")]
    public void StripEmoji_leaves_ordinary_text_alone(string input) => Assert.Equal(input, FeedText.StripEmoji(input));

    [Fact]
    public void StripEmoji_handles_null_and_empty()
    {
        Assert.Equal("", FeedText.StripEmoji(null));
        Assert.Equal("", FeedText.StripEmoji(""));
        Assert.Equal("", FeedText.StripEmoji("🔥🔥"));
    }

    // ---- Source names -------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("ESPN", "https://www.espn.com/espn/rss/news", "ESPN")]
    [InlineData("  It's FOSS  ", "https://itsfoss.com/", "It's FOSS")]
    [InlineData(null, "https://www.espn.com/espn/rss/news", "espn.com")]
    [InlineData("", "https://9to5linux.com/", "9to5linux.com")]
    [InlineData("   ", "https://WWW.Example.COM/x", "example.com")]
    [InlineData(null, "not a url", "not a url")]
    [InlineData(null, null, "")]
    public void SourceName_prefers_the_label_then_the_host(string? label, string? url, string expected) =>
        Assert.Equal(expected, FeedText.SourceName(label, url));

    [Theory]
    [InlineData("ESPN", "E")]
    [InlineData("9to5Linux", "9")]
    [InlineData("/Film", "F")]
    [InlineData("it's FOSS", "I")]
    [InlineData("", "•")]
    [InlineData("---", "•")]
    public void SourceMark_is_the_first_letter_or_digit(string name, string expected) =>
        Assert.Equal(expected, FeedText.SourceMark(name));

    // ---- Dates --------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("2026-09-20 01:00", "Sep 20, 2026")]
    [InlineData("2026-09-20 23:59", "Sep 20, 2026")]
    [InlineData("2026-09-19 00:00", "Sep 19, 2026")]
    [InlineData("2026-01-02 12:00", "Jan 2, 2026")]
    [InlineData("2025-12-31 12:00", "Dec 31, 2025")]
    public void Published_shows_the_actual_date(string when, string expected) =>
        Assert.Equal(expected, FeedDates.Create("UTC").Published(DateTime.Parse(when)));

    [Fact]
    public void Published_never_uses_relative_words_even_for_today()
    {
        var dates = FeedDates.Create("UTC");
        foreach (var moment in new[] { DateTime.UtcNow, DateTime.UtcNow.AddDays(-1) })
        {
            var text = dates.Published(moment);
            Assert.DoesNotContain("Today", text);
            Assert.DoesNotContain("Yesterday", text);
            Assert.Matches(@"^[A-Z][a-z]{2} \d{1,2}, \d{4}$", text);
        }
    }

    [Fact]
    public void Group_labels_are_real_dates()
    {
        var dates = FeedDates.Create("UTC");
        Assert.Equal("Sun Sep 20, 2026", dates.GroupLabel("2026-09-20"));
        Assert.Equal("Sat Sep 19, 2026", dates.GroupLabel("2026-09-19"));
        Assert.Equal("Wed Dec 31, 2025", dates.GroupLabel("2025-12-31"));
        Assert.Equal("garbage", dates.GroupLabel("garbage"));
    }

    [Fact]
    public void The_date_follows_the_display_zone()
    {
        // 02:00 UTC on 20 Sep is 21:00 on 19 Sep in Chicago (UTC-5 in September).
        var utc = new DateTime(2026, 9, 20, 2, 0, 0);
        var chicago = FeedDates.Create("America/Chicago");

        Assert.Equal("Sep 20, 2026", FeedDates.Create("UTC").Published(utc));
        Assert.Equal("Sep 19, 2026", chicago.Published(utc));
        Assert.Equal("2026-09-20", FeedDates.Create("UTC").DayKey(utc));
        Assert.Equal("2026-09-19", chicago.DayKey(utc));
    }

    [Fact]
    public void The_tooltip_gives_the_full_date_time_and_offset()
    {
        var utc = new DateTime(2026, 9, 20, 9, 52, 0);

        Assert.Equal("Sep 20, 2026, 9:52 AM (UTC+0)", FeedDates.Create("UTC").PublishedFull(utc));
        Assert.Equal("Sep 20, 2026, 4:52 AM (UTC-5)", FeedDates.Create("America/Chicago").PublishedFull(utc));
        Assert.Equal("Jan 15, 2026, 9:00 AM (UTC-6)", FeedDates.Create("America/Chicago").PublishedFull(new DateTime(2026, 1, 15, 15, 0, 0)));
        Assert.Equal("Sep 20, 2026, 3:22 PM (UTC+5:30)", FeedDates.Create("Asia/Kolkata").PublishedFull(utc));
    }

    [Theory]
    [InlineData("2026-09-20 09:52", "9:52 AM")]
    [InlineData("2026-09-20 00:05", "12:05 AM")]
    [InlineData("2026-09-20 12:30", "12:30 PM")]
    [InlineData("2026-09-20 13:07", "1:07 PM")]
    [InlineData("2026-09-20 23:59", "11:59 PM")]
    public void Time_is_a_12_hour_clock_time(string when, string expected) =>
        Assert.Equal(expected, FeedDates.Create("UTC").Time(DateTime.Parse(when)));

    [Fact]
    public void Time_follows_the_display_zone_including_daylight_saving()
    {
        var chicago = FeedDates.Create("America/Chicago");
        Assert.Equal("4:52 AM", chicago.Time(new DateTime(2026, 9, 20, 9, 52, 0)));   // CDT, UTC-5
        Assert.Equal("3:52 AM", chicago.Time(new DateTime(2026, 1, 20, 9, 52, 0)));   // CST, UTC-6
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Not/AZone")]
    public void An_unknown_time_zone_falls_back_to_utc(string? id) =>
        Assert.Equal(TimeZoneInfo.Utc, FeedDates.Create(id).Zone);
}
