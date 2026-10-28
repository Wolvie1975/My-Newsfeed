using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;

namespace myNewspaper.Web.Requests;

public sealed record FetchResult(Uri Url, string Body, string? ContentType);

/// <summary>What the checker found. Only the admin sees it; the requester only ever sees Waiting.</summary>
public sealed record CheckResult(bool Ok, string Summary, string? ChannelId = null, IReadOnlyList<string>? FeedUrls = null,
    int? Entries = null, string? NewestTitle = null);

/// <summary>
/// Fills in the details of a YouTube or news request for the admin (profiles handoff 5.2). Never approves anything,
/// and treats any failure as "needs details", so a changed YouTube page never blocks a request.
/// </summary>
public sealed partial class RequestChecks(IHttpClientFactory clients)
{
    public const string ClientName = "request-checker";
    private const int MaxRedirects = 5;
    public const int MaxBytes = 5_000_000;   // a YouTube channel page is about 2.6 MB

    [GeneratedRegex(@"^UC[A-Za-z0-9_-]{22}\z")]
    private static partial Regex ChannelIdPattern();

    [GeneratedRegex(@"<link[^>]+rel=[""']canonical[""'][^>]*href=[""']https?://(?:www\.|m\.)?youtube\.com/channel/(UC[A-Za-z0-9_-]{22})", RegexOptions.IgnoreCase)]
    private static partial Regex CanonicalChannel();

    [GeneratedRegex(@"<meta[^>]+itemprop=[""'](?:identifier|channelId)[""'][^>]*content=[""'](UC[A-Za-z0-9_-]{22})[""']", RegexOptions.IgnoreCase)]
    private static partial Regex MetaChannel();

    [GeneratedRegex(@"""(?:channelId|externalId)""\s*:\s*""(UC[A-Za-z0-9_-]{22})""")]
    private static partial Regex ScriptChannel();

    [GeneratedRegex(@"<link\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex LinkTag();

    [GeneratedRegex(@"\b(rel|type|href)\s*=\s*(?:""([^""]*)""|'([^']*)')", RegexOptions.IgnoreCase)]
    private static partial Regex Attribute();

    public static bool IsChannelId(string? value) => value is not null && ChannelIdPattern().IsMatch(value);

    public static string FeedUrl(string channelId) => AdminData.YoutubeFeedUrl(channelId);

    /// <summary>Same comparison as the UQ_Sources_UrlHash index: SHA-256 of the exact nvarchar text.</summary>
    public static byte[] SourceUrlHash(string url) => SHA256.HashData(Encoding.Unicode.GetBytes(url));

    public async Task<CheckResult> CheckYoutubeAsync(WebScraperContext db, string? link, CancellationToken ct = default)
    {
        string? channelId;
        try { channelId = await ResolveChannelAsync(link, ct); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            channelId = null;
        }
        if (string.IsNullOrWhiteSpace(link)) return new(false, "No link given · needs a channel ID");
        if (channelId is null) return new(false, "Couldn't find the channel from the link · needs a channel ID");

        var collected = await db.YoutubeVideoFeeds.AnyAsync(f => f.ChannelId == channelId, ct);
        var feed = await TestFeedAsync(FeedUrl(channelId), ct);
        var summary = "Channel ID found from the link · " + (feed.Ok ? "feed responds" : "feed didn't respond");
        if (collected) summary += " · Already collected";
        return new(feed.Ok, summary, channelId, [FeedUrl(channelId)], feed.Entries, feed.NewestTitle);
    }

    /// <summary>The channel ID behind a youtube.com or youtu.be link: from the address itself, or from the page.</summary>
    public async Task<string?> ResolveChannelAsync(string? link, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(link)) return null;
        var value = link.Trim();
        if (IsChannelId(value)) return value;
        if (!value.Contains("://")) value = "https://" + value;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !SsrfGuard.IsAllowedUri(uri)) return null;
        var host = uri.Host.ToLowerInvariant();
        var youtube = host is "youtube.com" or "www.youtube.com" or "m.youtube.com";
        if (!youtube && host != "youtu.be") return null;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (youtube && segments is ["channel", var id, ..] && IsChannelId(id)) return id;
        if (!youtube && segments.Length == 0) return null;

        // @handle, /c/, /user/, watch?v= and youtu.be links all name the channel on their page.
        var page = await FetchAsync(new Uri("https://www.youtube.com" + (youtube ? uri.PathAndQuery : "/watch?v=" + Uri.EscapeDataString(segments[0]))), ct);
        foreach (var pattern in new[] { CanonicalChannel(), MetaChannel(), ScriptChannel() })
            if (pattern.Match(page.Body) is { Success: true } match) return match.Groups[1].Value;
        return null;
    }

    public async Task<CheckResult> CheckNewsAsync(WebScraperContext db, string? link, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(link)) return new(false, "No link given · needs a feed address");
        var value = link.Trim();
        if (!value.Contains("://")) value = "https://" + value;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !SsrfGuard.IsAllowedUri(uri))
            return new(false, "The link isn't a web address · needs a feed address");

        List<string> found;
        try { found = await FindFeedsAsync(uri, ct); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            return new(false, "The site didn't respond · needs a feed address");
        }
        if (found.Count == 0) return new(false, $"No feed found on {uri.Host} · needs a feed address");

        var hashes = found.Select(SourceUrlHash).ToList();
        var existing = await db.Sources.AsNoTracking().Where(s => s.UrlHash != null).Select(s => s.UrlHash!).ToListAsync(ct);
        var already = hashes.Any(h => existing.Any(e => e.AsSpan().SequenceEqual(h)));
        var summary = $"{found.Count} {(found.Count == 1 ? "feed" : "feeds")} found on {uri.Host}" + (already ? " · Already a source" : "");
        return new(true, summary, FeedUrls: found);
    }

    /// <summary>The link itself if it's a feed; otherwise the page's advertised feeds, then /feed, /rss and /feed.xml.</summary>
    public async Task<List<string>> FindFeedsAsync(Uri link, CancellationToken ct = default)
    {
        var page = await FetchAsync(link, ct);
        if (ParseFeed(page.Body) is not null) return [page.Url.AbsoluteUri];

        var candidates = new List<Uri>();
        foreach (Match tag in LinkTag().Matches(page.Body))
        {
            var attributes = Attribute().Matches(tag.Value)
                .ToDictionary(m => m.Groups[1].Value.ToLowerInvariant(), m => WebUtility.HtmlDecode(m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Value),
                    StringComparer.Ordinal);
            if (attributes.GetValueOrDefault("rel")?.Split(' ').Contains("alternate", StringComparer.OrdinalIgnoreCase) == true
                && attributes.GetValueOrDefault("type")?.ToLowerInvariant() is "application/rss+xml" or "application/atom+xml"
                && attributes.GetValueOrDefault("href") is { Length: > 0 } href && Uri.TryCreate(page.Url, href, out var feed))
                candidates.Add(feed);
        }
        foreach (var path in new[] { "/feed", "/rss", "/feed.xml" }) candidates.Add(new Uri(page.Url, path));

        var found = new List<string>();
        foreach (var candidate in candidates.DistinctBy(c => c.AbsoluteUri).Take(8))
        {
            if (!SsrfGuard.IsAllowedUri(candidate)) continue;
            try
            {
                var response = await FetchAsync(candidate, ct);
                if (ParseFeed(response.Body) is not null && !found.Contains(response.Url.AbsoluteUri)) found.Add(response.Url.AbsoluteUri);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested) { }
            if (found.Count >= 5) break;
        }
        return found;
    }

    /// <summary>Admin "Test feed": the entry count and the newest title, or why it failed.</summary>
    public async Task<CheckResult> TestFeedAsync(string? url, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) || !SsrfGuard.IsAllowedUri(uri))
            return new(false, "Enter a full http or https feed address.");
        try
        {
            var response = await FetchAsync(uri, ct);
            if (ParseFeed(response.Body) is not { } feed) return new(false, "That address isn't an RSS or Atom feed.");
            return new(feed.Entries > 0, feed.Entries > 0 ? $"Feed responds · {feed.Entries} entries" : "Feed responds but has no entries",
                FeedUrls: [response.Url.AbsoluteUri], Entries: feed.Entries, NewestTitle: feed.NewestTitle);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            return new(false, "The feed didn't respond.");
        }
    }

    /// <summary>GET with every redirect target checked like the first address, at most 5 MB.</summary>
    public async Task<FetchResult> FetchAsync(Uri url, CancellationToken ct = default)
    {
        var client = clients.CreateClient(ClientName);
        for (var hop = 0; ; hop++)
        {
            if (!SsrfGuard.IsAllowedUri(url)) throw new HttpRequestException("Only public http and https addresses can be checked.");
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is { } location)
            {
                if (hop >= MaxRedirects) throw new HttpRequestException("Too many redirects.");
                url = location.IsAbsoluteUri ? location : new Uri(url, location);
                continue;
            }
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > MaxBytes) throw new HttpRequestException("The response is too large.");
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(chunk, ct)) > 0)
            {
                if (buffer.Length + read > MaxBytes) throw new HttpRequestException("The response is too large.");
                buffer.Write(chunk, 0, read);
            }
            var charset = response.Content.Headers.ContentType?.CharSet;
            Encoding encoding;
            try { encoding = string.IsNullOrEmpty(charset) ? Encoding.UTF8 : Encoding.GetEncoding(charset.Trim('"')); }
            catch (ArgumentException) { encoding = Encoding.UTF8; }
            return new(url, encoding.GetString(buffer.GetBuffer(), 0, (int)buffer.Length), response.Content.Headers.ContentType?.MediaType);
        }
    }

    /// <summary>RSS 2.0, RSS 1.0 (RDF) or Atom: the number of entries and the first one's title. Null when it isn't a feed.</summary>
    public static (int Entries, string? NewestTitle)? ParseFeed(string body)
    {
        XDocument doc;
        try
        {
            // No DTDs and no resolver: a feed can't make the parser fetch or expand anything.
            using var reader = XmlReader.Create(new StringReader(body.TrimStart('﻿', ' ', '\r', '\n', '\t')),
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxBytes });
            doc = XDocument.Load(reader);
        }
        catch (XmlException) { return null; }
        var root = doc.Root;
        if (root is null) return null;
        XNamespace atom = "http://www.w3.org/2005/Atom";
        List<XElement>? entries = root.Name == atom + "feed" ? root.Elements(atom + "entry").ToList()
            : root.Name.LocalName == "rss" ? root.Element("channel")?.Elements("item").ToList() ?? []
            : root.Name.LocalName == "RDF" ? root.Elements().Where(e => e.Name.LocalName == "item").ToList()
            : null;
        if (entries is null) return null;
        var title = entries.FirstOrDefault()?.Elements().FirstOrDefault(e => e.Name.LocalName == "title")?.Value.Trim();
        return (entries.Count, string.IsNullOrEmpty(title) ? null : title.Length > 300 ? title[..300] : title);
    }

    public static string? FeedUrlsJson(IReadOnlyList<string>? urls) => urls is { Count: > 0 } ? JsonSerializer.Serialize(urls) : null;
}
