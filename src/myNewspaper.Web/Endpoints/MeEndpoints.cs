using System.Globalization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using myNewspaper.Web.Data;
using myNewspaper.Web.Profiles;
using myNewspaper.Web.Requests;
using myNewspaper.Web.Tv;

namespace myNewspaper.Web.Endpoints;

/// <summary>
/// The Settings page's forms for the open profile (handoff 6.3): its name and color, Change PIN, this device's picker,
/// TV and channel follows, and requests. They live under /me, which the gate covers, so a device whose gate pass has
/// lapsed is sent to the picker before anything changes; [FromForm] binding checks antiforgery. Each one redirects back
/// to its Settings section with a fixed message code: names never go in the address.
/// </summary>
public static class MeEndpoints
{
    public const string SettingsPath = "/settings";

    public static void MapMe(this WebApplication app)
    {
        var me = app.MapGroup("/me");
        me.AddEndpointFilter((context, next) =>
        {
            context.HttpContext.Features.Get<IStatusCodePagesFeature>()?.Enabled = false;
            return next(context);
        });

        me.MapPost("/profile", async ([FromForm] string? name, [FromForm] string? color, ProfileContext reader, AdminProfileEndpoints.Db db) =>
        {
            try
            {
                await ProfileAdmin.UpdateAsync(db.Context, reader.Id, name, color, hidden: false, db.Now, db.Ct);
                return Back("profile", done: "profile-saved");
            }
            catch (ProfileNameTakenException) { return Back("profile", error: "name-taken"); }
            catch (ArgumentException) { return Back("profile", error: "profile-invalid"); }
        });

        me.MapPost("/pin", async ([FromForm] string? currentPin, [FromForm] string? newPin, [FromForm] string? newPinConfirm,
            ProfileContext reader, ProfilePins pins, AdminProfileEndpoints.Db db) =>
        {
            if (!ProfileRules.IsValidPin(newPin)) return Back("profile", error: "pin-invalid");
            if (newPin != newPinConfirm) return Back("profile", error: "pin-mismatch");
            var result = await ProfileStore.ChangePinAsync(db.Context, pins, reader.Device!.Id, reader.Id, currentPin, newPin, db.Now, db.Ct);
            return result.Outcome switch
            {
                OpenOutcome.Opened => Back("profile", done: "pin-changed"),
                OpenOutcome.WrongPin => Back("profile", error: "pin-wrong", triesLeft: result.TriesLeft),
                // Locked: the next page goes to the picker, which says so.
                _ => Results.LocalRedirect(ProfileGate.PickerPath),
            };
        });

        me.MapPost("/device", async ([FromForm] string? pickerMode, ProfileContext reader, AdminProfileEndpoints.Db db) =>
        {
            try
            {
                await ProfileStore.SetPickerModeAsync(db.Context, reader.Device!.Id, pickerMode, db.Ct);
                return Back("profile", done: "device-saved");
            }
            catch (ArgumentException) { return Back("profile", error: "device-invalid"); }
        });

        me.MapPost("/tv/follow", async ([FromForm] int? show, [FromForm] string? returnUrl, ProfileContext reader, TvmazeClient tvmaze, AdminProfileEndpoints.Db db) =>
        {
            var back = ProfileEndpoints.SafeReturnUrl(returnUrl);
            if (show is not > 0) return To(back, "shows", error: "show-invalid");
            // Check the show with TVmaze rather than trusting the title in the form.
            var response = await tvmaze.GetShowAsync(show.Value);
            if (response.Error is not null || response.Shows.Count != 1) return To(back, "shows", error: "show-unavailable");
            await MyFollows.FollowShowAsync(db.Context, reader.Id, show.Value, response.Shows[0].Name, db.Ct);
            return To(back, "shows", done: "show-followed");
        });

        me.MapPost("/tv/{trackingId:int}/unfollow", async (int trackingId, ProfileContext reader, AdminProfileEndpoints.Db db) =>
        {
            await MyFollows.UnfollowShowAsync(db.Context, reader.Id, trackingId, db.Ct);
            return Back("shows", done: "show-unfollowed");
        });

        me.MapPost("/channels/{feedId:int}", async (int feedId, [FromForm] string? follow, ProfileContext reader, AdminProfileEndpoints.Db db) =>
        {
            var on = follow == "on";
            return await MyFollows.SetChannelAsync(db.Context, reader.Id, feedId, on, db.Ct)
                ? Back("channels", done: on ? "channel-on" : "channel-off")
                : Back("channels", error: "channel-unknown");
        });

        me.MapPost("/requests", async ([FromForm] string? kind, [FromForm] string? name, [FromForm] string? link, [FromForm] string? note,
            ProfileContext reader, RequestBacklog backlog, AdminProfileEndpoints.Db db) =>
        {
            try
            {
                var request = await RequestStore.SubmitAsync(db.Context, reader.Id, kind, name, link, note, db.Ct);
                backlog.Enqueue(request.Id);
                return Back("requests", done: "request-sent");
            }
            catch (RequestStore.TooManyWaitingException) { return Back("requests", error: "too-many-requests"); }
            catch (ArgumentException) { return Back("requests", error: "request-invalid"); }
        });

        // Your topics (docs/news-section-handoff.md, phase 5). Every call is for the open profile's own topics.
        me.MapGet("/topics/preview", async (string? terms, string? except, string? hub, HttpContext http, FeedDates dates,
            AdminProfileEndpoints.Db db) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            var preview = await TopicQuery.PreviewAsync(db.Context, dates, terms, except, hub, db.Now, db.Ct);
            return Results.Json(new
            {
                total = preview.Total,
                verdict = preview.Verdict,
                perDay = preview.PerDay.Select(d => new { day = d.Day.ToString("ddd", CultureInfo.InvariantCulture), count = d.Count }),
                latest = preview.Latest.Select(i => new
                {
                    url = FeedQuery.SafeHref(i.Url),
                    title = TopicQuery.Highlight(FeedText.StripEmoji(i.Title), terms).Select(s => new { text = s.Text, match = s.Match }),
                    meta = string.Join(" · ", new[] { FeedText.SourceName(i.SourceLabel, i.SourceUrl), i.CategoryName, i.Undated ? null : dates.Published(i.Date) }
                        .Where(p => !string.IsNullOrEmpty(p))),
                }),
                hiddenByExcept = preview.HiddenByExcept,
                except = TopicQuery.Split(except, TopicQuery.MaxExcept),
            });
        });

        me.MapPost("/topics", async ([FromForm] string? terms, [FromForm] string? except, [FromForm] string? hub,
            ProfileContext reader, AdminProfileEndpoints.Db db) =>
        {
            try
            {
                var topic = await TopicQuery.CreateAsync(db.Context, reader.Id, terms, except, hub, db.Now, db.Ct);
                return Results.LocalRedirect($"/news/topics/{topic.Id}?done=topic-followed");
            }
            catch (TopicQuery.TooManyTopicsException) { return Results.LocalRedirect(TopicFollowPath + "?error=too-many"); }
            catch (ArgumentException) { return Results.LocalRedirect(TopicFollowPath + "?error=topic-invalid"); }
        });

        me.MapPost("/topics/{id:int}", async (int id, [FromForm] string? terms, [FromForm] string? except, [FromForm] string? hub,
            ProfileContext reader, AdminProfileEndpoints.Db db) =>
        {
            try
            {
                return await TopicQuery.UpdateAsync(db.Context, reader.Id, id, terms, except, hub, db.Ct)
                    ? Results.LocalRedirect($"/news/topics/{id}?done=topic-saved")
                    : Results.NotFound();
            }
            catch (ArgumentException) { return Results.LocalRedirect($"{TopicFollowPath}?edit={id}&error=topic-invalid"); }
        });

        me.MapPost("/topics/{id:int}/read", async (int id, ProfileContext reader, AdminProfileEndpoints.Db db) =>
            await TopicQuery.MarkReadAsync(db.Context, reader.Id, id, db.Now, db.Ct)
                ? Results.LocalRedirect($"/news/topics/{id}?done=topic-read")
                : Results.NotFound());

        me.MapPost("/topics/{id:int}/delete", async (int id, ProfileContext reader, AdminProfileEndpoints.Db db) =>
            await TopicQuery.DeleteAsync(db.Context, reader.Id, id, db.Ct)
                ? Results.LocalRedirect("/news?done=topic-removed")
                : Results.NotFound());
    }

    public const string TopicFollowPath = "/news/topics/follow";

    private static IResult Back(string section, string? done = null, string? error = null, int? triesLeft = null) =>
        To(SettingsPath, section, done, error, triesLeft);

    private static IResult To(string path, string section, string? done = null, string? error = null, int? triesLeft = null)
    {
        var query = new List<string>();
        if (done is not null) query.Add("done=" + done);
        if (error is not null) query.Add("error=" + error);
        if (triesLeft is { } n) query.Add("triesLeft=" + n.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var bare = path.Split('#')[0];
        var separator = bare.Contains('?') ? '&' : '?';
        return Results.LocalRedirect(bare + (query.Count > 0 ? separator + string.Join('&', query) : "") + "#" + section);
    }
}
