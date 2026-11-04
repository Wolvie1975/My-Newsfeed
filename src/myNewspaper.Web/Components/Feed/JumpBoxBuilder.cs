using System.Globalization;
using myNewspaper.Web.Components.Phone;
using myNewspaper.Shared.Data;

namespace myNewspaper.Web.Components.Feed;

public sealed record JumpGroup(string Key, string Label, string ShortLabel, int Count, string Href);
public sealed record JumpCategory(int Id, string Label, int Count, string Href, string ColorClass, bool Active);
public sealed record JumpBoxModel(
    string GroupBy,
    string CategoryAriaLabel,
    bool IsTech,
    IReadOnlyList<JumpGroup> Groups,
    IReadOnlyList<JumpCategory> Categories);

public static class JumpBoxBuilder
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static JumpBoxModel Build(
        IReadOnlyList<(DateTime Date, int? CategoryId)> recent,
        IReadOnlyList<(DateTime Date, int? CategoryId)> hubRecent,
        IReadOnlyList<FeedCategory> hubCategories,
        IReadOnlyList<FeedItem> firstPageItems,
        string groupBy,
        string hub,
        int? selectedCategoryId,
        Func<int?, string?, string> makeHref,
        FeedDates dates,
        DateTime nowUtc)
    {
        var today = dates.TodayAt(nowUtc);
        var todayStartUtc = dates.StartOfDayUtc(today);
        var firstPageHeadingKeys = firstPageItems
            .Skip(8)
            .Select(i => dates.CardGroupKey(i, groupBy, nowUtc))
            .ToHashSet(StringComparer.Ordinal);
        var topGroupKey = firstPageItems.Count > 0 ? dates.CardGroupKey(firstPageItems[0], groupBy, nowUtc) : null;

        var groups = new List<JumpGroup>();
        if (groupBy == "hour")
        {
            var todayRows = recent
                .Select(r => (Utc: DateTime.SpecifyKind(r.Date, DateTimeKind.Utc), Local: TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(r.Date, DateTimeKind.Utc), dates.Zone)))
                .Where(r => DateOnly.FromDateTime(r.Local) == today)
                .ToList();

            var hourGroups = todayRows
                .GroupBy(r => new DateTime(r.Local.Year, r.Local.Month, r.Local.Day, r.Local.Hour, 0, 0, DateTimeKind.Unspecified))
                .OrderByDescending(g => g.Key)
                .Take(4)
                .ToList();

            foreach (var (g, idx) in hourGroups.Select((g, i) => (g, i)))
            {
                var key = "h-" + g.Key.ToString("yyyy-MM-dd-HH", Culture);
                var label = g.Key.ToString("h tt", Culture) + " hour";
                var shortLabel = g.Key.ToString("h tt", Culture);
                var hourEndUtc = TimeZoneInfo.ConvertTimeToUtc(g.Key.AddHours(1), dates.Zone);
                var href = idx == 0 && key == topGroupKey
                    ? "#top-of-list"
                    : firstPageHeadingKeys.Contains(key)
                        ? "#hc-" + key
                        : makeHref(selectedCategoryId, new FeedCursor(hourEndUtc, int.MaxValue, false).ToToken());
                groups.Add(new JumpGroup(key, label, shortLabel, g.Count(), href));
            }

            if (hourGroups.Count > 0)
            {
                var oldestHourStart = hourGroups[^1].Key;
                var oldestHourStartUtc = TimeZoneInfo.ConvertTimeToUtc(oldestHourStart, dates.Zone);
                var earlierCount = recent.Count(r => DateTime.SpecifyKind(r.Date, DateTimeKind.Utc) < oldestHourStartUtc);
                if (earlierCount > 0)
                {
                    var earlierHref = makeHref(selectedCategoryId, new FeedCursor(oldestHourStartUtc, int.MaxValue, false).ToToken());
                    groups.Add(new JumpGroup("earlier", "Earlier", "Earlier", earlierCount, earlierHref));
                }
            }
        }
        else
        {
            var dayGroups = recent
                .Select(r => (Utc: DateTime.SpecifyKind(r.Date, DateTimeKind.Utc), Day: DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(r.Date, DateTimeKind.Utc), dates.Zone))))
                .GroupBy(r => r.Day)
                .OrderByDescending(g => g.Key)
                .Take(4)
                .ToList();

            foreach (var (g, idx) in dayGroups.Select((g, i) => (g, i)))
            {
                var key = "d-" + g.Key.ToString("yyyy-MM-dd", Culture);
                var label = g.Key == today ? "Today"
                    : g.Key == today.AddDays(-1) ? "Yesterday"
                    : g.Key.ToString("ddd, MMM d", Culture);
                var shortLabel = g.Key == today ? "Today"
                    : g.Key == today.AddDays(-1) ? "Yesterday"
                    : g.Key.ToString("MMM d", Culture);
                var dayEndUtc = dates.StartOfDayUtc(g.Key.AddDays(1));
                var href = idx == 0 && key == topGroupKey
                    ? "#top-of-list"
                    : firstPageHeadingKeys.Contains(key)
                        ? "#hc-" + key
                        : makeHref(selectedCategoryId, new FeedCursor(dayEndUtc, int.MaxValue, false).ToToken());
                groups.Add(new JumpGroup(key, label, shortLabel, g.Count(), href));
            }

            if (dayGroups.Count > 0)
            {
                var oldestDay = dayGroups[^1].Key;
                var oldestDayStartUtc = dates.StartOfDayUtc(oldestDay);
                var earlierCount = recent.Count(r => DateTime.SpecifyKind(r.Date, DateTimeKind.Utc) < oldestDayStartUtc);
                if (earlierCount > 0)
                {
                    var earlierHref = makeHref(selectedCategoryId, new FeedCursor(oldestDayStartUtc, int.MaxValue, false).ToToken());
                    groups.Add(new JumpGroup("earlier", "Earlier", "Earlier", earlierCount, earlierHref));
                }
            }
        }

        var todayCountsByCat = hubRecent
            .Where(r => r.CategoryId is not null && DateTime.SpecifyKind(r.Date, DateTimeKind.Utc) >= todayStartUtc)
            .GroupBy(r => r.CategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var categories = new List<JumpCategory>();
        if (hub == "tech")
        {
            foreach (var cat in hubCategories.OrderBy(NewsSections.Order).ThenBy(c => c.Name))
            {
                var count = todayCountsByCat.GetValueOrDefault(cat.Id, 0);
                var href = makeHref(cat.Id, null);
                categories.Add(new JumpCategory(cat.Id, NewsSections.LabelFor(cat.Name), count, href, PhoneColors.ForCategory(cat.Name), selectedCategoryId == cat.Id));
            }
        }
        else
        {
            var orderedCats = hubCategories
                .Select(c => (Cat: c, Count: todayCountsByCat.GetValueOrDefault(c.Id, 0)))
                .Where(x => x.Count > 0 || hub is "sports" or "entertainment")
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Cat.Name)
                .Take(5);
            foreach (var (cat, count) in orderedCats)
            {
                var href = hub == "headlines"
                    ? makeHref(cat.Id, null)
                    : (NewsSections.RouteFor(cat) ?? makeHref(cat.Id, null));
                categories.Add(new JumpCategory(cat.Id, NewsSections.LabelFor(cat.Name), count, href, PhoneColors.ForCategory(cat.Name), selectedCategoryId == cat.Id));
            }
        }

        return new JumpBoxModel(
            groupBy,
            hub == "tech" ? "Today by desk" : "Today by category",
            hub == "tech",
            groups,
            categories);
    }
}

