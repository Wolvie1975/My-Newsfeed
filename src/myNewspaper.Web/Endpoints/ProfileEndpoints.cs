using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Auth;
using myNewspaper.Web.Data;
using myNewspaper.Web.Profiles;
using myNewspaper.Web.Requests;

namespace myNewspaper.Web.Endpoints;

public sealed record PickerModeChange(string? PickerMode);
public sealed record PinChange(string? CurrentPin, string? NewPin, string? NewPinConfirm);
public sealed record RequestSubmission(string? Kind, string? Name, string? Link, string? Note);

/// <summary>
/// The public profile endpoints (profiles handoff 2.3 and 5.1). The gate middleware returns 404 for all of them while
/// profiles are switched off. Forms post here and work without JavaScript; a fetch that asks for JSON gets JSON.
/// </summary>
public static class ProfileEndpoints
{
    public static void MapProfiles(this WebApplication app)
    {
        // [FromForm] endpoints validate antiforgery through UseAntiforgery.
        var profiles = app.MapGroup("/profiles");
        profiles.AddEndpointFilter((context, next) =>
        {
            context.HttpContext.Features.Get<IStatusCodePagesFeature>()?.Enabled = false;
            return next(context);
        });
        profiles.MapPost("/{id:int}/open", OpenAsync);
        profiles.MapPost("/{id:int}/new-pin", NewPinAsync);
        profiles.MapPost("/new", CreateAsync);
        profiles.MapPost("/signout", SignOutAsync);

        var me = app.MapGroup("/api/me");
        me.AddEndpointFilter(JsonApi.Filter);
        me.MapGet("", GetMeAsync);
        me.MapPut("/settings", async (JsonObject patch, ProfileContext reader, IDbContextFactory<WebScraperContext> factory, HttpContext http) =>
        {
            await using var db = await factory.CreateDbContextAsync(http.RequestAborted);
            return Results.Ok(new { settings = await ProfileStore.PatchSettingsAsync(db, reader.Id, patch, http.RequestAborted) });
        }).WithMetadata(new RequestSizeLimitAttribute(ProfileSettings.MaxLength * 2));
        me.MapPost("/import-browser", async (JsonObject values, ProfileContext reader, IDbContextFactory<WebScraperContext> factory,
            TimeProvider clock, HttpContext http) =>
        {
            await using var db = await factory.CreateDbContextAsync(http.RequestAborted);
            var settings = await ProfileStore.ImportBrowserAsync(db, reader.Id, values, clock.GetUtcNow().UtcDateTime, http.RequestAborted);
            return Results.Ok(new { settings });
        }).WithMetadata(new RequestSizeLimitAttribute(ProfileSettings.MaxLength * 2));
        me.MapPut("/device", async (PickerModeChange change, ProfileContext reader, IDbContextFactory<WebScraperContext> factory, HttpContext http) =>
        {
            await using var db = await factory.CreateDbContextAsync(http.RequestAborted);
            await ProfileStore.SetPickerModeAsync(db, reader.Device!.Id, change.PickerMode, http.RequestAborted);
            return Results.Ok(new { pickerMode = change.PickerMode });
        });
        me.MapPost("/pin", ChangePinAsync);
        me.MapGet("/requests", async (ProfileContext reader, IDbContextFactory<WebScraperContext> factory, HttpContext http) =>
        {
            await using var db = await factory.CreateDbContextAsync(http.RequestAborted);
            return Results.Ok(new { requests = await RequestStore.MineAsync(db, reader.Id, http.RequestAborted) });
        });
        me.MapPost("/requests", async (RequestSubmission body, ProfileContext reader, IDbContextFactory<WebScraperContext> factory,
            RequestBacklog backlog, HttpContext http) =>
        {
            await using var db = await factory.CreateDbContextAsync(http.RequestAborted);
            try
            {
                var request = await RequestStore.SubmitAsync(db, reader.Id, body.Kind, body.Name, body.Link, body.Note, http.RequestAborted);
                backlog.Enqueue(request.Id);
                return Results.Ok(new { requests = await RequestStore.MineAsync(db, reader.Id, http.RequestAborted) });
            }
            catch (RequestStore.TooManyWaitingException ex) { return Results.Conflict(new { error = ex.Message }); }
        });
    }

    /// <summary>Same-site paths only, like the admin sign-in; anything else goes to the front page.</summary>
    public static string SafeReturnUrl(string? url) =>
        url is not null && !url.Any(char.IsControl) && AdminAuth.SafeReturnUrl(url) == url ? url : "/";

    private static bool WantsJson(HttpRequest request) =>
        request.Headers.Accept.Any(a => a?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);

    private static IResult PickerWith(int id, string error, string? returnUrl, int? triesLeft = null)
    {
        var query = new Dictionary<string, string?> { ["profile"] = id.ToString(System.Globalization.CultureInfo.InvariantCulture), ["error"] = error };
        if (triesLeft is { } tries) query["triesLeft"] = tries.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (returnUrl is not null) query["returnUrl"] = SafeReturnUrl(returnUrl);
        return Results.LocalRedirect(QueryHelpers.AddQueryString(ProfileGate.PickerPath, query));
    }

    private static IResult Outcome(HttpContext http, OpenResult result, int id, string? returnUrl, ProfileCookies cookies, Guid deviceId,
        DateTime nowUtc)
    {
        var json = WantsJson(http.Request);
        var target = SafeReturnUrl(returnUrl);
        switch (result.Outcome)
        {
            case OpenOutcome.Opened:
                cookies.WriteGate(http, new GatePass(deviceId, id, nowUtc));
                return json ? Results.Ok(new { opened = true, returnUrl = target }) : Results.LocalRedirect(target);
            case OpenOutcome.WrongPin:
                return json ? Results.Json(new { wrongPin = true, triesLeft = result.TriesLeft }, statusCode: StatusCodes.Status401Unauthorized)
                    : PickerWith(id, "wrong-pin", returnUrl, result.TriesLeft);
            case OpenOutcome.Locked:
                return json ? Results.Json(new { locked = true }, statusCode: StatusCodes.Status423Locked) : PickerWith(id, "locked", returnUrl);
            case OpenOutcome.NeedsPin:
                var href = $"{ProfileGate.PickerPath}/{id}/new-pin?returnUrl={Uri.EscapeDataString(target)}";
                return json ? Results.Json(new { needsPin = true, href }, statusCode: StatusCodes.Status409Conflict) : Results.LocalRedirect(href);
            case OpenOutcome.NoDevice:
                // The middleware identifies the device on /profiles/* POSTs, so this only happens with cookies blocked.
                return Results.BadRequest(new { error = "Turn on cookies for this site to open a profile." });
            default:
                return Results.NotFound();
        }
    }

    private static async Task<IResult> OpenAsync(int id, [FromForm] string? pin, [FromForm] string? returnUrl, HttpContext http,
        ProfileContext reader, IDbContextFactory<WebScraperContext> factory, ProfilePins pins, ProfileCookies cookies, TimeProvider clock)
    {
        if (reader.Device is null) return Outcome(http, OpenResult.NoDevice, id, returnUrl, cookies, Guid.Empty, default);
        var now = clock.GetUtcNow().UtcDateTime;
        await using var db = await factory.CreateDbContextAsync(http.RequestAborted);
        var result = await ProfileStore.OpenAsync(db, pins, reader.Device.Id, id, pin, now, http.RequestAborted);
        return Outcome(http, result, id, returnUrl, cookies, reader.Device.Id, now);
    }

    private static async Task<IResult> NewPinAsync(int id, [FromForm] string? pin, [FromForm] string? pinConfirm, [FromForm] string? returnUrl,
        HttpContext http, ProfileContext reader, IDbContextFactory<WebScraperContext> factory, ProfilePins pins, ProfileCookies cookies,
        TimeProvider clock)
    {
        if (reader.Device is null) return Outcome(http, OpenResult.NoDevice, id, returnUrl, cookies, Guid.Empty, default);
        string? error = !ProfileRules.IsValidPin(pin) ? "A PIN is 4 digits." : pin != pinConfirm ? "The two PINs don't match." : null;
        if (error is not null)
            return WantsJson(http.Request) ? Results.BadRequest(new { error })
                : Results.LocalRedirect($"{ProfileGate.PickerPath}/{id}/new-pin?error={Uri.EscapeDataString(error)}&returnUrl={Uri.EscapeDataString(SafeReturnUrl(returnUrl))}");
        await using var db = await factory.CreateDbContextAsync(http.RequestAborted);
        var result = await ProfileStore.SetFirstPinAsync(db, pins, reader.Device.Id, id, pin, http.RequestAborted);
        return Outcome(http, result, id, returnUrl, cookies, reader.Device.Id, clock.GetUtcNow().UtcDateTime);
    }

    /// <summary>
    /// The setup flow's submit (decision 10: anyone with the link). Steps 2 to 4 arrive either as the <c>settings</c>
    /// JSON document or, from the setup page (<c>setup=1</c>), as plain fields that work without JavaScript; both are laid
    /// over the base edition. The new profile opens on this device.
    /// </summary>
    private static async Task<IResult> CreateAsync([FromForm] string? name, [FromForm] string? color, [FromForm] string? pin,
        [FromForm] string? pinConfirm, [FromForm] string? settings, [FromForm] string? setup, [FromForm] string? returnUrl, HttpContext http,
        ProfileContext reader, IDbContextFactory<WebScraperContext> factory, ProfilePins pins, ProfileCookies cookies, TimeProvider clock)
    {
        if (reader.Device is null) return Outcome(http, OpenResult.NoDevice, 0, returnUrl, cookies, Guid.Empty, default);
        try
        {
            if (pin != pinConfirm) throw new ArgumentException("The two PINs don't match.");
            await using var db = await factory.CreateDbContextAsync(http.RequestAborted);
            JsonObject choices;
            if (setup == "1")
            {
                // The repeated fields (team, service, show, order); the form is already read for the fields above.
                var form = await http.Request.ReadFormAsync(http.RequestAborted);
                var services = form["service"].Select(s => int.TryParse(s, out var id) ? id : throw new ArgumentException("Invalid services."));
                choices = ProfileSettings.FromSetup(await ProfileAdmin.GetBaseEditionAsync(db, http.RequestAborted),
                    form["team"].OfType<string>(), services, form["show"].OfType<string>(), form["order"].OfType<string>());
            }
            else choices = ProfileSettings.ParseInput(settings);
            var profile = await ProfileStore.CreateAsync(db, pins, name, color, pin, choices, http.RequestAborted);
            await ProfileStore.AssignDeviceAsync(db, reader.Device.Id, profile.Id, http.RequestAborted);
            return Outcome(http, OpenResult.Opened, profile.Id, returnUrl, cookies, reader.Device.Id, clock.GetUtcNow().UtcDateTime);
        }
        catch (ArgumentException ex)
        {
            return WantsJson(http.Request) ? Results.BadRequest(new { error = ex.Message })
                : Results.LocalRedirect($"{ProfileGate.PickerPath}/new?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    private static async Task<IResult> SignOutAsync(HttpContext http, ProfileContext reader, IDbContextFactory<WebScraperContext> factory,
        IAntiforgery antiforgery)
    {
        // No form fields to bind, so antiforgery is checked here rather than by form binding.
        try { await antiforgery.ValidateRequestAsync(http); }
        catch (AntiforgeryValidationException) { return Results.BadRequest(new { error = "Refresh the page and try again." }); }
        if (reader.Device is not null)
        {
            await using var db = await factory.CreateDbContextAsync(http.RequestAborted);
            await ProfileStore.SignOutAsync(db, reader.Device.Id, http.RequestAborted);
        }
        ProfileCookies.ClearGate(http);
        return WantsJson(http.Request) ? Results.Ok(new { signedOut = true }) : Results.LocalRedirect(ProfileGate.PickerPath);
    }

    private static IResult GetMeAsync(ProfileContext reader, HttpContext http, IAntiforgery antiforgery)
    {
        var profile = reader.Profile ?? throw new InvalidOperationException("No profile selected.");
        return Results.Ok(new
        {
            name = profile.Name,
            color = profile.Color,
            settings = ProfileSettings.Parse(profile.Settings),
            browserImportAt = profile.BrowserImportAt,
            pickerMode = reader.Device?.PickerMode,
            token = antiforgery.GetAndStoreTokens(http).RequestToken,
        });
    }

    private static async Task<IResult> ChangePinAsync(PinChange change, ProfileContext reader, IDbContextFactory<WebScraperContext> factory,
        ProfilePins pins, TimeProvider clock, HttpContext http)
    {
        if (change.NewPin != change.NewPinConfirm) return Results.BadRequest(new { error = "The two new PINs don't match." });
        await using var db = await factory.CreateDbContextAsync(http.RequestAborted);
        var result = await ProfileStore.ChangePinAsync(db, pins, reader.Device!.Id, reader.Id, change.CurrentPin, change.NewPin,
            clock.GetUtcNow().UtcDateTime, http.RequestAborted);
        return result.Outcome switch
        {
            OpenOutcome.Opened => Results.Ok(new { changed = true }),
            OpenOutcome.WrongPin => Results.Json(new { wrongPin = true, triesLeft = result.TriesLeft }, statusCode: StatusCodes.Status401Unauthorized),
            OpenOutcome.Locked => Results.Json(new { locked = true }, statusCode: StatusCodes.Status423Locked),
            _ => Results.NotFound(),
        };
    }
}

/// <summary>Shared by the JSON APIs: no caching, antiforgery on every write, and safe messages for bad input.</summary>
public static class JsonApi
{
    public static async ValueTask<object?> Filter(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        http.Response.Headers.CacheControl = "no-store";
        // API callers get the status code itself, never the HTML not-found page.
        http.Features.Get<IStatusCodePagesFeature>()?.Enabled = false;
        try
        {
            if (!HttpMethods.IsGet(http.Request.Method) && !HttpMethods.IsHead(http.Request.Method))
                await http.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(http);
            return await next(context);
        }
        catch (AntiforgeryValidationException) { return Results.BadRequest(new { error = "Refresh the page and try again." }); }
        catch (ProfileNameTakenException ex) { return Results.Conflict(new { error = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }
}
