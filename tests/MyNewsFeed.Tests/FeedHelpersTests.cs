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

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    // Sunday 20 Sep 2026, 14:00 UTC.
    private static readonly FixedClock Noon = new(new DateTimeOffset(2026, 9, 20, 14, 0, 0, TimeSpan.Zero));

    [Theory]
    [InlineData("2026-09-20 01:00", "Today")]
    [InlineData("2026-09-20 23:59", "Today")]
    [InlineData("2026-09-19 23:00", "Yesterday")]
    [InlineData("2026-09-19 00:00", "Yesterday")]
    [InlineData("2026-09-18 12:00", "Sep 18")]
    [InlineData("2026-01-02 12:00", "Jan 2")]
    [InlineData("2025-12-31 12:00", "Dec 31, 2025")]
    public void Short_labels_use_today_yesterday_then_a_date(string when, string expected)
    {
        var dates = FeedDates.Create("UTC", Noon);
        Assert.Equal(expected, dates.Short(DateTime.Parse(when)));
    }

    [Fact]
    public void Group_labels_name_the_day()
    {
        var dates = FeedDates.Create("UTC", Noon);
        Assert.Equal("Today · Sun Sep 20", dates.GroupLabel("2026-09-20"));
        Assert.Equal("Yesterday · Sat Sep 19", dates.GroupLabel("2026-09-19"));
        Assert.Equal("Fri Sep 18", dates.GroupLabel("2026-09-18"));
        Assert.Equal("Wed Dec 31, 2025", dates.GroupLabel("2025-12-31"));
        Assert.Equal("garbage", dates.GroupLabel("garbage"));
    }

    [Fact]
    public void Day_key_is_the_calendar_day_in_the_display_zone()
    {
        // 02:00 UTC on 20 Sep is 21:00 on 19 Sep in Chicago (UTC-5 in September).
        var utc = new DateTime(2026, 9, 20, 2, 0, 0);
        Assert.Equal("2026-09-20", FeedDates.Create("UTC", Noon).DayKey(utc));
        Assert.Equal("2026-09-19", FeedDates.Create("America/Chicago", Noon).DayKey(utc));
    }

    [Fact]
    public void Today_is_decided_in_the_display_zone_not_in_utc()
    {
        // The clock reads 03:00 UTC on 20 Sep, which is still the evening of 19 Sep in Chicago.
        var clock = new FixedClock(new DateTimeOffset(2026, 9, 20, 3, 0, 0, TimeSpan.Zero));
        var chicago = FeedDates.Create("America/Chicago", clock);

        Assert.Equal("Today", chicago.Short(new DateTime(2026, 9, 20, 2, 0, 0)));      // 21:00 on the 19th, local
        Assert.Equal("Yesterday", chicago.Short(new DateTime(2026, 9, 19, 2, 0, 0)));  // 21:00 on the 18th, local
        Assert.Equal("Today · Sat Sep 19", chicago.GroupLabel("2026-09-19"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Not/AZone")]
    public void An_unknown_time_zone_falls_back_to_utc(string? id) =>
        Assert.Equal(TimeZoneInfo.Utc, FeedDates.Create(id, Noon).Zone);
}
