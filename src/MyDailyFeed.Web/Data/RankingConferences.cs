using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace MyDailyFeed.Web.Data;

// Supplement rankings with season-specific ESPN conference membership, cached across visitors.
public sealed class RankingConferences(IHttpClientFactory clients, IMemoryCache cache, ILogger<RankingConferences> log)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private sealed record Snapshot(DateTime Checked, IReadOnlyDictionary<string, string> Names);
    public async Task<IReadOnlyDictionary<string, string>> GetAsync(string sport, int season)
    {
        var path = sport switch
        {
            "Football" => "football/college-football", "Men's Basketball" => "basketball/mens-college-basketball",
            "Women's Basketball" => "basketball/womens-college-basketball", "Volleyball" => "volleyball/womens-college-volleyball",
            "Soccer" => "soccer/usa.ncaa.w.1", _ => null
        };
        if (path is null) return new Dictionary<string, string>();
        var key = $"ranking-conferences:{path}:{season}";
        await gate.WaitAsync();
        try
        {
            cache.TryGetValue<Snapshot>(key, out var previous);
            if (previous is not null && DateTime.UtcNow - previous.Checked < TimeSpan.FromHours(6)) return previous.Names;
            try
            {
                using var response = await clients.CreateClient("ranking-conferences").GetAsync($"https://site.api.espn.com/apis/v2/sports/{path}/standings?season={season}&level=3");
                response.EnsureSuccessStatusCode();
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var names = Parse(json.RootElement, season);
                cache.Set(key, new Snapshot(DateTime.UtcNow, names), TimeSpan.FromDays(7));
                return names;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
            {
                log.LogWarning(ex, "Conference lookup unavailable for {Sport}, season {Season}.", sport, season);
                var names = previous?.Names ?? new Dictionary<string, string>();
                // Short retry delay; preserve a previously successful season-specific lookup.
                cache.Set(key, new Snapshot(DateTime.UtcNow - TimeSpan.FromHours(6) + TimeSpan.FromMinutes(5), names), TimeSpan.FromDays(7));
                return names;
            }
        }
        finally { gate.Release(); }
    }

    public static IReadOnlyDictionary<string, string> Parse(JsonElement root, int season)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("season", out var info) || info.ValueKind != JsonValueKind.Object || !info.TryGetProperty("year", out var year) || year.ValueKind != JsonValueKind.Number || !year.TryGetInt32(out var actual) || actual != season) return result;
        static string? Text(JsonElement e, string key) => e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        void Visit(JsonElement node, string? conference)
        {
            var name = Text(node, "name");
            var isConference = node.TryGetProperty("isConference", out var flag) && flag.ValueKind == JsonValueKind.True;
            if (isConference || (!node.TryGetProperty("children", out _) && node.TryGetProperty("standings", out _) && name is not null && !name.Contains("Division", StringComparison.OrdinalIgnoreCase)))
                conference = Text(node, "shortName") ?? Text(node, "abbreviation") ?? name;
            if (conference is not null && node.TryGetProperty("standings", out var standings) && standings.TryGetProperty("entries", out var entries) && entries.ValueKind == JsonValueKind.Array)
                foreach (var entry in entries.EnumerateArray())
                    if (entry.TryGetProperty("team", out var team) && Text(team, "id") is { } id) result[id] = conference;
            if (node.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array)
                foreach (var child in children.EnumerateArray()) Visit(child, conference);
        }
        Visit(root, null);
        return result;
    }
}
