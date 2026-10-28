using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using myNewspaper.Web.Data;
using myNewspaper.Web.Profiles;
using myNewspaper.Web.Requests;

namespace myNewspaper.Web.Endpoints;

public sealed record AdminProfileAdd(string? Name, string? Color, string? Pin, string? PinConfirm);
public sealed record AdminProfileEdit(string? Name, string? Color, bool Hidden);
public sealed record AdminPinReset(string? Pin, string? PinConfirm);
public sealed record AdminProfileOrder(int[]? Ids);
public sealed record AdminDeviceLabel(string? Label);
public sealed record AdminYoutubeApproval(string? ChannelId, string? ChannelName, int? CategoryId, bool FollowForRequester = true);
public sealed record AdminCollectedApproval(string? ChannelId);
public sealed record AdminNewsApproval(string? FeedUrl, string? Name, int? CategoryId);
public sealed record AdminDecline(string? Reply);
public sealed record AdminFeedTest(string? Url);
public sealed record AdminFeedSearch(string? Link);

/// <summary>
/// The admin APIs behind Admin · Profiles, the base edition and Admin · Requests (profiles handoff 5.3, 5.4, 6.4).
/// They need the admin sign-in and work whether or not profiles are switched on. Per decision 3 they return only who
/// each profile is, its lock and hidden state, its devices and its requests: never follows, settings, Saved or
/// reading times, and nothing here opens a profile.
/// </summary>
public static class AdminProfileEndpoints
{
    public static void MapAdminProfiles(this WebApplication app)
    {
        var api = app.MapGroup("/admin/api").RequireAuthorization();
        api.AddEndpointFilter(JsonApi.Filter);
        api.MapGet("/token", (HttpContext http, IAntiforgery antiforgery) => Results.Ok(new { token = antiforgery.GetAndStoreTokens(http).RequestToken }));

        var profiles = api.MapGroup("/profiles");
        profiles.MapGet("", async (Db db) =>
            Results.Ok(new { profiles = await ProfileAdmin.ListAsync(db.Context, db.Ct), totals = await ProfileAdmin.TotalsAsync(db.Context, db.Ct) }));
        profiles.MapPost("", async (AdminProfileAdd body, Db db, ProfilePins pins) =>
        {
            if (body.Pin != body.PinConfirm) throw new ArgumentException("The two PINs don't match.");
            var profile = await ProfileStore.CreateAsync(db.Context, pins, body.Name, body.Color, body.Pin, null, db.Ct);
            return Results.Ok(new { id = profile.Id });
        });
        profiles.MapPut("/order", async (AdminProfileOrder body, Db db) =>
        {
            await ProfileAdmin.ReorderAsync(db.Context, body.Ids ?? [], db.Ct);
            return Results.Ok(new { profiles = await ProfileAdmin.ListAsync(db.Context, db.Ct) });
        });
        profiles.MapPut("/{id:int}", async (int id, AdminProfileEdit body, Db db) =>
            Found(await ProfileAdmin.UpdateAsync(db.Context, id, body.Name, body.Color, body.Hidden, db.Now, db.Ct)));
        profiles.MapDelete("/{id:int}", async (int id, Db db) => Found(await ProfileAdmin.DeleteAsync(db.Context, id, db.Ct)));
        profiles.MapPost("/{id:int}/unlock", async (int id, Db db) => Found(await ProfileAdmin.UnlockAsync(db.Context, id, db.Ct)));
        profiles.MapPost("/{id:int}/reset-pin", async (int id, AdminPinReset body, Db db, ProfilePins pins) =>
        {
            if (body.Pin != body.PinConfirm) throw new ArgumentException("The two PINs don't match.");
            return Found(await ProfileAdmin.ResetPinAsync(db.Context, pins, id, body.Pin, db.Now, db.Ct));
        });
        profiles.MapGet("/{id:int}/devices", async (int id, Db db) =>
            await ProfileAdmin.DevicesAsync(db.Context, id, db.Ct) is { } devices ? Results.Ok(new { devices }) : Results.NotFound());
        profiles.MapPut("/{id:int}/devices/{deviceId:guid}", async (int id, Guid deviceId, AdminDeviceLabel body, Db db) =>
            Found(await ProfileAdmin.RenameDeviceAsync(db.Context, id, deviceId, body.Label, db.Ct)));
        profiles.MapPost("/{id:int}/devices/{deviceId:guid}/signout", async (int id, Guid deviceId, Db db) =>
            Found(await ProfileAdmin.SignOutDeviceAsync(db.Context, id, deviceId, db.Now, db.Ct)));
        profiles.MapPost("/{id:int}/signout-all", async (int id, Db db) =>
            Results.Ok(new { signedOut = await ProfileAdmin.SignOutEverywhereAsync(db.Context, id, db.Now, db.Ct) }));

        var household = api.MapGroup("/household");
        household.MapGet("", async (Db db) => Results.Ok(await ProfileAdmin.GetHouseholdAsync(db.Context, db.Ct)));
        household.MapPut("", async (HouseholdSettings body, Db db, IMemoryCache cache) =>
        {
            await ProfileAdmin.SaveHouseholdAsync(db.Context, body, db.Ct);
            ProfileGateMiddleware.ForgetHousehold(cache);
            return Results.Ok(await ProfileAdmin.GetHouseholdAsync(db.Context, db.Ct));
        });
        household.MapGet("/base-edition", async (Db db) => Results.Ok(new { settings = await ProfileAdmin.GetBaseEditionAsync(db.Context, db.Ct) }));
        household.MapPut("/base-edition", async (JsonObject settings, Db db) =>
            Results.Ok(new { settings = await ProfileAdmin.SaveBaseEditionAsync(db.Context, settings, db.Ct) }));
        // Reset to site defaults.
        household.MapDelete("/base-edition", async (Db db) => Results.Ok(new { settings = await ProfileAdmin.SaveBaseEditionAsync(db.Context, [], db.Ct) }));

        var requests = api.MapGroup("/requests");
        requests.MapGet("", async (string? status, int? profileId, int? id, Db db) => Results.Ok(new
        {
            requests = await RequestStore.ListAsync(db.Context, status, profileId, id, ct: db.Ct),
            counts = await RequestStore.CountsAsync(db.Context, profileId, db.Ct),
        }));
        requests.MapGet("/summary", async (Db db) => Results.Ok(await RequestStore.SummaryAsync(db.Context, db.Ct)));
        requests.MapPost("/mark-seen", async (Db db) =>
        {
            await RequestStore.MarkSeenAsync(db.Context, db.Now, db.Ct);
            return Results.Ok(new { count = await RequestStore.AlertCountAsync(db.Context, db.Ct) });
        });
        requests.MapPost("/{id:int}/approve-youtube", async (int id, AdminYoutubeApproval body, Db db) => await Decided(db, id,
            await RequestStore.ApproveYoutubeAsync(db.Context, id, body.ChannelId, body.ChannelName, body.CategoryId, body.FollowForRequester, db.Now, db.Ct)));
        requests.MapPost("/{id:int}/approve-collected", async (int id, AdminCollectedApproval body, Db db) => await Decided(db, id,
            await RequestStore.ApproveCollectedYoutubeAsync(db.Context, id, body.ChannelId, db.Now, db.Ct)));
        requests.MapPost("/{id:int}/approve-news", async (int id, AdminNewsApproval body, Db db) => await Decided(db, id,
            await RequestStore.ApproveNewsAsync(db.Context, id, body.FeedUrl, body.Name, body.CategoryId, db.Now, db.Ct)));
        requests.MapPost("/{id:int}/decline", async (int id, AdminDecline body, Db db) => await Decided(db, id,
            await RequestStore.DeclineAsync(db.Context, id, body.Reply, db.Now, db.Ct)));
        // Runs the checker again on demand and returns the refreshed row.
        requests.MapPost("/{id:int}/check", async (int id, Db db, RequestChecks checks) =>
        {
            await RequestStore.CheckAsync(db.Context, checks, id, db.Ct);
            return Row(await RequestStore.ListAsync(db.Context, id: id, ct: db.Ct));
        });
        requests.MapPost("/test-feed", async (AdminFeedTest body, RequestChecks checks, HttpContext http) =>
            Results.Ok(await checks.TestFeedAsync(body.Url, http.RequestAborted)));
        requests.MapPost("/find-feeds", async (AdminFeedSearch body, Db db, RequestChecks checks) =>
            Results.Ok(await checks.CheckNewsAsync(db.Context, body.Link, db.Ct)));
    }

    private static IResult Found(bool found) => found ? Results.Ok(new { ok = true }) : Results.NotFound();

    private static IResult Row(IReadOnlyList<AdminRequestRow> rows) => rows.Count == 1 ? Results.Ok(new { request = rows[0] }) : Results.NotFound();

    private static async Task<IResult> Decided(Db db, int id, ApproveOutcome outcome) => outcome switch
    {
        ApproveOutcome.Approved => Row(await RequestStore.ListAsync(db.Context, id: id, ct: db.Ct)),
        ApproveOutcome.NotWaiting => Results.Conflict(new { error = "That request was already answered." }),
        ApproveOutcome.AlreadyCollected => Results.Conflict(new { alreadyCollected = true, error = "That channel is already collected. Approve and follow it instead." }),
        ApproveOutcome.NotCollected => Results.Conflict(new { error = "That channel isn't collected yet. Add it with its channel ID." }),
        _ => Results.NotFound(),
    };

    /// <summary>A database context for one admin request, disposed with the request, plus its clock and cancellation.</summary>
    public sealed class Db(WebScraperContext context, DateTime now, CancellationToken ct) : IAsyncDisposable
    {
        public WebScraperContext Context { get; } = context;
        public DateTime Now { get; } = now;
        public CancellationToken Ct { get; } = ct;

        public static async ValueTask<Db?> BindAsync(HttpContext http)
        {
            var factory = http.RequestServices.GetRequiredService<IDbContextFactory<WebScraperContext>>();
            var clock = http.RequestServices.GetRequiredService<TimeProvider>();
            var db = new Db(await factory.CreateDbContextAsync(http.RequestAborted), clock.GetUtcNow().UtcDateTime, http.RequestAborted);
            http.Response.RegisterForDisposeAsync(db);
            return db;
        }

        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }
}
