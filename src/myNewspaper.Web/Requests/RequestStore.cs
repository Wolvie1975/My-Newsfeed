using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;
using myNewspaper.Web.Profiles;

namespace myNewspaper.Web.Requests;

/// <summary>What a requester sees of their own request: never the checker's findings, only the status and any reply.</summary>
public sealed record MyRequest(int Id, string Kind, string Name, string Status, DateTime CreatedAt, DateTime? DecidedAt, string? AdminReply);

public sealed record AdminRequestRow(int Id, string Kind, string Name, string? Link, string? Note, string Status,
    bool? CheckOk, string? CheckSummary, string? ResolvedChannelId, IReadOnlyList<string> ResolvedFeedUrls, string? AdminReply,
    DateTime CreatedAt, DateTime? DecidedAt, int ProfileId, string ProfileName, string ProfileColor, bool AlreadyCollected,
    string NextAction);

public sealed record AdminRequestCounts(int Waiting, int Added, int Declined, int All);

public sealed record LockedProfileAlert(int ProfileId, string Name, string Color, DateTime LockedAt, string? Device);

/// <summary>The admin's quick look (5.4): locked profiles first, then waiting requests, and the header count.</summary>
public sealed record RequestSummary(int Count, IReadOnlyList<LockedProfileAlert> Locked, IReadOnlyList<AdminRequestRow> Waiting);

public enum ApproveOutcome { Approved, NotFound, NotWaiting, AlreadyCollected, NotCollected }

/// <summary>YouTube channel and news source requests (profiles handoff 5.1, 5.3, 5.4). Nothing is auto-approved.</summary>
public static class RequestStore
{
    public const string Waiting = "waiting";
    public const string Added = "added";
    public const string Declined = "declined";
    public const string Youtube = "youtube";
    public const string News = "news";
    public const int MaxWaitingPerProfile = 5;
    private const string AlreadyASource = "Already a source";

    public sealed class TooManyWaitingException() : InvalidOperationException(
        $"You have {MaxWaitingPerProfile} requests waiting. Wait for the admin to answer one before sending another.");

    private static string Text(string? value, int max, string what, bool required)
    {
        var text = (value ?? "").Trim();
        if (text.Length == 0 && required) throw new ArgumentException($"{what} is required.");
        if (text.Length > max) throw new ArgumentException($"{what} can be at most {max} characters.");
        if (text.Any(c => char.IsControl(c) && c is not ('\n' or '\r'))) throw new ArgumentException($"{what} can't contain control characters.");
        return text;
    }

    public static async Task<ContentRequest> SubmitAsync(WebScraperContext db, int profileId, string? kind, string? name, string? link,
        string? note, CancellationToken ct = default)
    {
        if (kind is not (Youtube or News)) throw new ArgumentException("Choose a YouTube channel or a news source.");
        var request = new ContentRequest
        {
            ProfileId = profileId,
            Kind = kind,
            Name = Text(name, 200, "A name", required: true).ReplaceLineEndings(" "),
            Link = Text(link, 2048, "The link", required: false) is { Length: > 0 } l ? l.ReplaceLineEndings("") : null,
            Note = Text(note, 500, "The note", required: false) is { Length: > 0 } n ? n : null,
            Status = Waiting,
        };
        await using var tx = await ProfileStore.BeginAsync(db, ct);
        // Holds the profile row, so parallel submits can't both pass the limit.
        _ = await ProfileStore.LockAsync(db, profileId, ct) ?? throw new InvalidOperationException("No profile selected.");
        if (await db.ContentRequests.CountAsync(r => r.ProfileId == profileId && r.Status == Waiting, ct) >= MaxWaitingPerProfile)
            throw new TooManyWaitingException();
        db.ContentRequests.Add(request);
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return request;
    }

    public static async Task<IReadOnlyList<MyRequest>> MineAsync(WebScraperContext db, int profileId, CancellationToken ct = default) =>
        await db.ContentRequests.AsNoTracking().Where(r => r.ProfileId == profileId)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Select(r => new MyRequest(r.Id, r.Kind, r.Name, r.Status, r.CreatedAt, r.DecidedAt, r.Status == Declined ? r.AdminReply : null))
            .ToListAsync(ct);

    /// <summary>Runs the checker for one waiting request and stores what it found.</summary>
    public static async Task CheckAsync(WebScraperContext db, RequestChecks checks, int id, CancellationToken ct = default)
    {
        var request = await db.ContentRequests.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, ct);
        if (request is null || request.Status != Waiting) return;
        var result = request.Kind == Youtube ? await checks.CheckYoutubeAsync(db, request.Link, ct) : await checks.CheckNewsAsync(db, request.Link, ct);
        var summary = result.Summary.Length > 300 ? result.Summary[..300] : result.Summary;
        await db.ContentRequests.Where(r => r.Id == id && r.Status == Waiting).ExecuteUpdateAsync(s => s
            .SetProperty(r => r.CheckOk, result.Ok)
            .SetProperty(r => r.CheckSummary, summary)
            .SetProperty(r => r.ResolvedChannelId, result.ChannelId)
            .SetProperty(r => r.ResolvedFeedUrls, RequestChecks.FeedUrlsJson(result.FeedUrls)), ct);
    }

    public static async Task<IReadOnlyList<AdminRequestRow>> ListAsync(WebScraperContext db, string? status = null, int? profileId = null,
        int? id = null, int take = 200, CancellationToken ct = default)
    {
        var query = db.ContentRequests.AsNoTracking();
        if (status is Waiting or Added or Declined) query = query.Where(r => r.Status == status);
        if (profileId is not null) query = query.Where(r => r.ProfileId == profileId);
        if (id is not null) query = query.Where(r => r.Id == id);
        var rows = await query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).Take(take)
            .Select(r => new
            {
                Request = r,
                r.Profile!.Name,
                r.Profile.Color,
                Collected = r.ResolvedChannelId != null && db.YoutubeVideoFeeds.Any(f => f.ChannelId == r.ResolvedChannelId),
            }).ToListAsync(ct);
        return rows.Select(r => Row(r.Request, r.Name, r.Color, r.Collected)).ToList();
    }

    public static async Task<AdminRequestCounts> CountsAsync(WebScraperContext db, int? profileId = null, CancellationToken ct = default)
    {
        var query = db.ContentRequests.AsNoTracking();
        if (profileId is not null) query = query.Where(r => r.ProfileId == profileId);
        var counts = await query.GroupBy(r => r.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        int Of(string s) => counts.SingleOrDefault(c => c.Key == s)?.Count ?? 0;
        return new(Of(Waiting), Of(Added), Of(Declined), counts.Sum(c => c.Count));
    }

    private static AdminRequestRow Row(ContentRequest r, string profileName, string profileColor, bool collected)
    {
        IReadOnlyList<string> feeds = [];
        try { if (r.ResolvedFeedUrls is { } json) feeds = JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch (JsonException) { }
        var already = r.Kind == Youtube ? collected : r.CheckSummary?.Contains(AlreadyASource, StringComparison.Ordinal) == true;
        var next = r.Status != Waiting ? "" : already ? "Approve"
            : r.CheckOk != true ? "Fill in details"
            : r.Kind == News && feeds.Count > 1 ? "Pick a feed" : "Review and approve";
        return new(r.Id, r.Kind, r.Name, r.Link, r.Note, r.Status, r.CheckOk, r.CheckSummary, r.ResolvedChannelId, feeds, r.AdminReply,
            r.CreatedAt, r.DecidedAt, r.ProfileId, profileName, profileColor, already, next);
    }

    private static async Task<ContentRequest?> LockAsync(WebScraperContext db, int id, CancellationToken ct)
    {
        var request = await db.ContentRequests.FromSql($"SELECT * FROM dbo.MyDailyFeedContentRequests WITH (UPDLOCK, ROWLOCK) WHERE Id = {id}")
            .SingleOrDefaultAsync(ct);
        if (request is not null && db.Entry(request).State == EntityState.Unchanged) await db.Entry(request).ReloadAsync(ct);
        return request;
    }

    private static async Task<int?> CategoryAsync(WebScraperContext db, int? categoryId, bool youtube, CancellationToken ct)
    {
        if (categoryId is null) return null;
        var exists = youtube ? await db.YoutubeCategories.AnyAsync(c => c.Id == categoryId, ct) : await db.SourceCategories.AnyAsync(c => c.Id == categoryId, ct);
        return exists ? categoryId : throw new ArgumentException("That category doesn't exist.");
    }

    /// <summary>
    /// Add channel and approve. The feed, the requester's follow and the request's YoutubeVideoFeedId are written in
    /// one transaction, so the household rollout can never see the new feed without its request and turn it on for
    /// everyone (4.2). A channel that's already collected goes through <see cref="ApproveCollectedYoutubeAsync"/>.
    /// </summary>
    public static async Task<ApproveOutcome> ApproveYoutubeAsync(WebScraperContext db, int id, string? channelId, string? channelName,
        int? categoryId, bool followForRequester, DateTime nowUtc, CancellationToken ct = default)
    {
        var channel = (channelId ?? "").Trim();
        if (!RequestChecks.IsChannelId(channel)) throw new ArgumentException("A channel ID starts with UC and is 24 characters.");
        var name = Text(channelName, 200, "The channel name", required: false).ReplaceLineEndings(" ");
        await using var tx = await ProfileStore.BeginAsync(db, ct);
        var request = await LockAsync(db, id, ct);
        if (request is null || request.Kind != Youtube) return ApproveOutcome.NotFound;
        if (request.Status != Waiting) return ApproveOutcome.NotWaiting;
        if (await db.YoutubeVideoFeeds.AnyAsync(f => f.ChannelId == channel, ct)) return ApproveOutcome.AlreadyCollected;
        var feed = new YoutubeVideoFeed
        {
            ChannelId = channel,
            ChannelName = name.Length > 0 ? name : null,
            Url = RequestChecks.FeedUrl(channel),
            YoutubeCategoryId = await CategoryAsync(db, categoryId, youtube: true, ct),
        };
        db.YoutubeVideoFeeds.Add(feed);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (AdminData.IsUniqueViolation(ex)) { return ApproveOutcome.AlreadyCollected; }
        if (followForRequester) db.ProfileChannelFollows.Add(new ProfileChannelFollow { ProfileId = request.ProfileId, YoutubeVideoFeedId = feed.Id });
        request.Status = Added;
        request.YoutubeVideoFeedId = feed.Id;
        request.ResolvedChannelId = channel;
        request.DecidedAt = nowUtc;
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return ApproveOutcome.Approved;
    }

    /// <summary>
    /// Approve and follow for the requester, for a channel that's already collected. Only the follow is added. The
    /// request's YoutubeVideoFeedId stays empty on purpose: setting it would mark an existing household channel as a
    /// requested one and keep the rollout from turning it on for anyone else.
    /// </summary>
    public static async Task<ApproveOutcome> ApproveCollectedYoutubeAsync(WebScraperContext db, int id, string? channelId, DateTime nowUtc,
        CancellationToken ct = default)
    {
        await using var tx = await ProfileStore.BeginAsync(db, ct);
        var request = await LockAsync(db, id, ct);
        if (request is null || request.Kind != Youtube) return ApproveOutcome.NotFound;
        if (request.Status != Waiting) return ApproveOutcome.NotWaiting;
        var channel = string.IsNullOrWhiteSpace(channelId) ? request.ResolvedChannelId : channelId.Trim();
        var feedId = await db.YoutubeVideoFeeds.Where(f => f.ChannelId == channel).Select(f => (int?)f.Id).SingleOrDefaultAsync(ct);
        if (feedId is null) return ApproveOutcome.NotCollected;
        if (!await db.ProfileChannelFollows.AnyAsync(f => f.ProfileId == request.ProfileId && f.YoutubeVideoFeedId == feedId, ct))
            db.ProfileChannelFollows.Add(new ProfileChannelFollow { ProfileId = request.ProfileId, YoutubeVideoFeedId = feedId.Value });
        request.Status = Added;
        request.DecidedAt = nowUtc;
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return ApproveOutcome.Approved;
    }

    /// <summary>Add source and approve. News sources aren't followed: the source appears for everyone in its section.</summary>
    public static async Task<ApproveOutcome> ApproveNewsAsync(WebScraperContext db, int id, string? feedUrl, string? label, int? categoryId,
        DateTime nowUtc, CancellationToken ct = default)
    {
        var url = (feedUrl ?? "").Trim();
        if (url.Length > 2048 || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || !SsrfGuard.IsAllowedUri(uri))
            throw new ArgumentException("Enter a full URL that starts with http:// or https://.");
        var name = Text(label, 200, "The source name", required: false).ReplaceLineEndings(" ");
        await using var tx = await ProfileStore.BeginAsync(db, ct);
        var request = await LockAsync(db, id, ct);
        if (request is null || request.Kind != News) return ApproveOutcome.NotFound;
        if (request.Status != Waiting) return ApproveOutcome.NotWaiting;
        var hash = RequestChecks.SourceUrlHash(url);
        var sourceId = await db.Sources.Where(s => s.UrlHash == hash).Select(s => (int?)s.Id).SingleOrDefaultAsync(ct);
        if (sourceId is null)
        {
            var source = new Source
            {
                Url = url,
                Label = name.Length > 0 ? name : null,
                Enabled = true,
                SourceCategoryId = await CategoryAsync(db, categoryId, youtube: false, ct)
            };
            db.Sources.Add(source);
            await db.SaveChangesAsync(ct);
            sourceId = source.Id;
        }
        request.Status = Added;
        request.SourceId = sourceId;
        request.DecidedAt = nowUtc;
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return ApproveOutcome.Approved;
    }

    /// <summary>Decline, with an optional reason the requester sees.</summary>
    public static async Task<ApproveOutcome> DeclineAsync(WebScraperContext db, int id, string? reply, DateTime nowUtc, CancellationToken ct = default)
    {
        var text = Text(reply, 500, "The reply", required: false);
        var request = await db.ContentRequests.SingleOrDefaultAsync(r => r.Id == id, ct);
        if (request is null) return ApproveOutcome.NotFound;
        if (request.Status != Waiting) return ApproveOutcome.NotWaiting;
        var changed = await db.ContentRequests.Where(r => r.Id == id && r.Status == Waiting).ExecuteUpdateAsync(s => s
            .SetProperty(r => r.Status, Declined)
            .SetProperty(r => r.AdminReply, text.Length > 0 ? text : null)
            .SetProperty(r => r.DecidedAt, nowUtc), ct);
        return changed > 0 ? ApproveOutcome.Approved : ApproveOutcome.NotWaiting;
    }

    /// <summary>
    /// The header count: waiting requests newer than Mark all seen, plus every locked profile. Locks keep counting until
    /// the admin unlocks or resets them.
    /// </summary>
    public static async Task<int> AlertCountAsync(WebScraperContext db, CancellationToken ct = default)
    {
        var seen = (await ProfileStore.GetHouseholdAsync(db, ct)).RequestsSeenAt;
        return await db.ContentRequests.CountAsync(r => r.Status == Waiting && (seen == null || r.CreatedAt > seen), ct)
            + await db.Profiles.CountAsync(p => p.LockedAt != null, ct);
    }

    public static async Task<RequestSummary> SummaryAsync(WebScraperContext db, CancellationToken ct = default)
    {
        var locked = await db.Profiles.AsNoTracking().Where(p => p.LockedAt != null).OrderBy(p => p.LockedAt)
            .Select(p => new LockedProfileAlert(p.Id, p.Name, p.Color, p.LockedAt!.Value,
                db.ProfileDevices.Where(d => d.Id == p.LockedByDeviceId).Select(d => d.Label).FirstOrDefault()))
            .ToListAsync(ct);
        return new(await AlertCountAsync(db, ct), locked, await ListAsync(db, Waiting, take: 20, ct: ct));
    }

    public static async Task MarkSeenAsync(WebScraperContext db, DateTime nowUtc, CancellationToken ct = default) =>
        await db.Household.Where(h => h.Id == 1).ExecuteUpdateAsync(s => s.SetProperty(h => h.RequestsSeenAt, nowUtc), ct);
}
