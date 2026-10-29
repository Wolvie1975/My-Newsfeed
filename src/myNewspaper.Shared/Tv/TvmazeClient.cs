using System.Net;
using System.Text.Json;

namespace myNewspaper.Web.Tv;

public sealed record TvmazeShow(int Id, string Name, string? Premiere, string? Platform, string? Poster, string? Url);
public sealed record TvmazeResponse(IReadOnlyList<TvmazeShow> Shows, string? Error);

/// <summary>Admin search/identity lookup only. The separate scraper owns episode collection.</summary>
public sealed class TvmazeClient(HttpClient http)
{
    public async Task<TvmazeResponse> SearchAsync(string query, CancellationToken ct = default) =>
        await ReadAsync("search/shows?q=" + Uri.EscapeDataString(query.Trim()), true, ct);

    public async Task<TvmazeResponse> GetShowAsync(int id, CancellationToken ct = default) =>
        id <= 0 ? new([], "Invalid TVmaze show ID.") : await ReadAsync($"shows/{id}", false, ct);

    private async Task<TvmazeResponse> ReadAsync(string path, bool search, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync(path, ct);
            if (response.StatusCode == HttpStatusCode.TooManyRequests) return new([], "TVmaze is busy. Please wait before trying again.");
            if (!response.IsSuccessStatusCode) return new([], $"TVmaze answered HTTP {(int)response.StatusCode}. Please try again later.");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var elements = search ? json.RootElement.EnumerateArray().Select(e => e.GetProperty("show")) : [json.RootElement];
            return new(elements.Take(30).Select(ParseShow).ToList(), null);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or KeyNotFoundException || ex is TaskCanceledException && !ct.IsCancellationRequested)
        { return new([], "Could not read TVmaze. Please try again later."); }
    }

    public static TvmazeShow ParseShow(JsonElement show)
    {
        static string? Text(JsonElement e, string key) => e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        static string? Child(JsonElement e, string parent, string key) => e.TryGetProperty(parent, out var child) && child.ValueKind == JsonValueKind.Object ? Text(child, key) : null;
        return new(show.GetProperty("id").GetInt32(), Text(show, "name") ?? "Untitled series", Text(show, "premiered"),
            Child(show, "webChannel", "name") ?? Child(show, "network", "name"), Child(show, "image", "medium"), Text(show, "url"));
    }
}
