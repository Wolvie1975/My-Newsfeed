using System.Globalization;
using System.Text.RegularExpressions;

namespace myNewspaper.Web.Data;

/// <summary>Display-time text clean-up for the public feed. The stored data is never changed.</summary>
public static partial class FeedText
{
    // Emoji and pictographs: the symbol/dingbat blocks, common single emoji in the technical block, the joiner and
    // variation selectors that glue emoji sequences together, and every astral emoji block (surrogate pairs).
    [GeneratedRegex(@"(?:[⌚⌛⏩-⏳⏸-⏺☀-➿⬀-⯿️‍⃣]|[\uD83C-\uD83E][\uDC00-\uDFFF])+")]
    private static partial Regex EmojiPattern();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"^\s*title\s*:\s*", RegexOptions.IgnoreCase)]
    private static partial Regex TitleLabel();

    /// <summary>Drops a leading "Title:" label some feeds put in the headline (Al Jazeera, October 29, 2026).</summary>
    public static string StripTitleLabel(string title) =>
        TitleLabel().Replace(title ?? "", "") is { Length: > 0 } stripped ? stripped : title ?? "";

    /// <summary>Removes emoji and tidies the spaces they leave behind. Text without emoji is returned unchanged.</summary>
    public static string StripEmoji(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        var stripped = EmojiPattern().Replace(text, " ");
        return ReferenceEquals(stripped, text) || stripped == text
            ? text
            : Whitespace().Replace(stripped, " ").Trim();
    }

    /// <summary>The publication name: the label when set, otherwise the site's host without a leading "www.".</summary>
    public static string SourceName(string? label, string? url)
    {
        if (!string.IsNullOrWhiteSpace(label))
        {
            return label.Trim();
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var host = uri.Host;
            return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
        }

        return url ?? "";
    }

    private static readonly HashSet<string> MinorWords = new(StringComparer.OrdinalIgnoreCase) { "the", "of", "and", "a", "an", "&" };

    /// <summary>Publisher initials for P8: an opening acronym (up to four letters), otherwise up to three initials/digits.</summary>
    public static string SourceInitials(string? name)
    {
        var main = Regex.Split(name ?? "", @"\s+[—–|·]\s+")[0];
        var words = Regex.Split(main, @"[\s\-]+").Where(w => w.Length > 0).ToArray();
        if (words.Length > 0 && Regex.IsMatch(words[0], "^[A-Z0-9]{2,4}$")) return words[0];
        var initials = string.Concat(words
            .Where(w => !MinorWords.Contains(w) && char.IsLetterOrDigit(w[0]))
            .Select(w => w.All(char.IsDigit) ? w : char.ToUpper(w[0], CultureInfo.InvariantCulture).ToString()));
        return initials.Length == 0 ? "•" : initials[..Math.Min(3, initials.Length)];
    }

    /// <summary>The single character shown in the small source badge.</summary>
    public static string SourceMark(string name)
    {
        var first = name.FirstOrDefault(char.IsLetterOrDigit);
        return first == default ? "•" : char.ToUpper(first, CultureInfo.InvariantCulture).ToString();
    }
}
