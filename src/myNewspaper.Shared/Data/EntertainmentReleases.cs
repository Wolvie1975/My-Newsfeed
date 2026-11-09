namespace myNewspaper.Shared.Data;

public static class EntertainmentReleases
{
    private static readonly System.Text.RegularExpressions.Regex YearRegex =
        new(@"\s*\(\d{4}\)$", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>Strips any trailing " (YYYY)" year from a movie title for duplicate comparisons.</summary>
    public static string NormalizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "";
        var trimmed = title.Trim();
        return YearRegex.Replace(trimmed, "").Trim();
    }

    /// <summary>True when the title ends with a four-digit year in parentheses.</summary>
    public static bool HasYear(string? title) =>
        !string.IsNullOrWhiteSpace(title) && YearRegex.IsMatch(title.Trim());

    /// <summary>
    /// Merges duplicate releases on the same day when their titles match ignoring a trailing (YYYY).
    /// Prefers the copy with a poster; if tied, prefers the copy whose title includes the year.
    /// Preserves original sequence order.
    /// </summary>
    public static IReadOnlyList<T> Deduplicate<T>(
        IEnumerable<T> items,
        Func<T, string> getTitle,
        Func<T, DateOnly?> getDay,
        Func<T, string?> getPoster,
        Func<T, bool>? isTonight = null)
    {
        var result = new List<T>();
        var groups = new Dictionary<(string NormTitle, DateOnly? Day, bool Tonight), T>();

        foreach (var item in items)
        {
            var title = getTitle(item);
            var norm = NormalizeTitle(title).ToLowerInvariant();
            var day = getDay(item);
            var tonight = isTonight?.Invoke(item) ?? false;
            var key = (norm, day, tonight);

            if (!groups.TryGetValue(key, out var existing))
            {
                groups[key] = item;
                result.Add(item);
            }
            else
            {
                var existingHasPoster = !string.IsNullOrWhiteSpace(getPoster(existing));
                var newHasPoster = !string.IsNullOrWhiteSpace(getPoster(item));

                var replace = false;
                if (newHasPoster && !existingHasPoster)
                {
                    replace = true;
                }
                else if (newHasPoster == existingHasPoster)
                {
                    var existingHasYear = HasYear(getTitle(existing));
                    var newHasYear = HasYear(title);
                    if (newHasYear && !existingHasYear)
                    {
                        replace = true;
                    }
                }

                if (replace)
                {
                    groups[key] = item;
                    var idx = result.IndexOf(existing);
                    if (idx >= 0) result[idx] = item;
                }
            }
        }

        return result;
    }
}
