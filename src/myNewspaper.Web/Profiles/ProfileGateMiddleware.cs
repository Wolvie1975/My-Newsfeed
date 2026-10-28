using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.StaticAssets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Web.Profiles;

/// <summary>
/// Identifies the device, loads its profile into <see cref="ProfileContext"/>, and sends anyone without a profile to
/// the picker (profiles handoff 2.2). There's no Guest: anything not exempt needs a profile. While profiles are
/// switched off it does nothing except hide the public profile routes.
/// </summary>
public sealed class ProfileGateMiddleware(RequestDelegate next, IOptions<ProfilesOptions> options, IDbContextFactory<WebScraperContext> factory,
    ProfileCookies cookies, IMemoryCache cache, FeedDates dates, TimeProvider clock)
{
    private const string HouseholdCacheKey = "profiles.household";

    /// <summary>Call after the admin changes household settings, so the next request sees them.</summary>
    public static void ForgetHousehold(IMemoryCache cache) => cache.Remove(HouseholdCacheKey);

    public async Task InvokeAsync(HttpContext http, ProfileContext context)
    {
        var path = http.Request.Path;
        if (!options.Value.Enabled)
        {
            if (ProfileGate.IsProfileRoute(path))
            {
                // A plain 404: re-running a POST through the not-found page would answer 400 for its antiforgery check.
                http.Features.Get<IStatusCodePagesFeature>()?.Enabled = false;
                http.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            await next(http);
            return;
        }

        // UseAuthentication runs before MapStaticAssets, so static files are recognized by their endpoint, not by path.
        // The not-found and error pages re-run the pipeline for a request that was already identified (and both are
        // exempt), so they pass too; otherwise a browser without a cookie would get a second device row.
        if (ProfileGate.IsPassThrough(path) || http.GetEndpoint()?.Metadata.GetMetadata<StaticAssetDescriptor>() is not null
            || http.Features.Get<IStatusCodeReExecuteFeature>() is not null || http.Features.Get<IExceptionHandlerPathFeature>() is not null)
        {
            await next(http);
            return;
        }

        var ct = http.RequestAborted;
        var now = clock.GetUtcNow().UtcDateTime;
        var household = await cache.GetOrCreateAsync(HouseholdCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            await using var db = await factory.CreateDbContextAsync(ct);
            return await ProfileStore.GetHouseholdAsync(db, ct);
        }) ?? new Household();

        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            ProfileDevice? device = null;
            Profile? profile = null;
            if (cookies.ReadDevice(http) is { } token)
                (device, profile) = await ProfileStore.LoadDeviceAsync(db, token, household.ForgetDevicesDays, now, ct);

            // Only browsers opening a page, or posting to the profile endpoints, get a device row: probes, bots and
            // API calls without a cookie never create one.
            var canCreate = ProfileGate.IsPageRequest(http.Request)
                || (HttpMethods.IsPost(http.Request.Method) && path.StartsWithSegments("/profiles"));
            if (device is null && canCreate)
            {
                var fresh = ProfileCookies.NewDevice();
                device = await ProfileStore.CreateDeviceAsync(db, fresh, http.Request.Headers.UserAgent, household.DefaultPickerMode, ct);
                cookies.WriteDevice(http, fresh);
            }

            context.Device = device;
            context.Profile = profile;

            if (!ProfileGate.IsExempt(path) && (device is null
                || ProfileGate.NeedsPicker(profile?.Id, device.PickerMode, cookies.ReadGate(http), device.Id, now, dates.Zone)))
            {
                context.Profile = null;
                await SendToPickerAsync(http);
                return;
            }

            if (device is not null) await ProfileStore.TouchAsync(db, device, ProfileGate.IsExempt(path) ? null : profile, now, ct);
        }
        await next(http);
    }

    /// <summary>Pages redirect to the picker; everything else gets 401 and the picker's address, for the shared fetch helper.</summary>
    private static Task SendToPickerAsync(HttpContext http)
    {
        http.Response.Headers.CacheControl = "no-store";
        if (ProfileGate.IsPageRequest(http.Request))
        {
            var returnUrl = http.Request.PathBase + http.Request.Path + http.Request.QueryString;
            http.Response.Redirect($"{ProfileGate.PickerPath}?returnUrl={Uri.EscapeDataString(returnUrl)}");
            return Task.CompletedTask;
        }
        http.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return http.Response.WriteAsJsonAsync(new { gate = ProfileGate.PickerPath });
    }
}

public static class ProfileGateExtensions
{
    public static IApplicationBuilder UseProfileGate(this IApplicationBuilder app) => app.UseMiddleware<ProfileGateMiddleware>();
}
