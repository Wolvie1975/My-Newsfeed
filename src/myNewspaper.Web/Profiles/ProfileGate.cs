namespace myNewspaper.Web.Profiles;

/// <summary>The gate's rules, separate from HTTP so they can be tested with a fake clock (profiles handoff 2.1 and 2.2).</summary>
public static class ProfileGate
{
    public const string EveryVisit = "every-visit";
    public const string Daily = "daily";
    public const string UntilSignOut = "until-signout";
    public static readonly IReadOnlyList<string> PickerModes = [EveryVisit, Daily, UntilSignOut];

    public const string PickerPath = "/editions";

    /// <summary>Paths that never see the gate and aren't identified: static files and framework plumbing.</summary>
    public static bool IsPassThrough(PathString path) =>
        path.StartsWithSegments("/_framework") || path.StartsWithSegments("/_blazor") || path.StartsWithSegments("/_content")
        || path.Equals("/healthz") || path.Equals("/service-worker.js") || path.Equals("/manifest.webmanifest")
        || path.Equals("/offline.html") || path.Equals("/favicon.ico");

    /// <summary>Identified, but never sent to the picker: the picker itself, profile endpoints, sign-in, admin, errors.</summary>
    public static bool IsExempt(PathString path) =>
        path.StartsWithSegments(PickerPath) || path.StartsWithSegments("/profiles") || path.StartsWithSegments("/account")
        || path.StartsWithSegments("/login") || path.StartsWithSegments("/admin")
        || path.StartsWithSegments("/Error") || path.StartsWithSegments("/not-found");

    /// <summary>The public profile routes, which don't exist while profiles are switched off.</summary>
    public static bool IsProfileRoute(PathString path) =>
        path.StartsWithSegments(PickerPath) || path.StartsWithSegments("/profiles") || path.StartsWithSegments("/api/me");

    /// <summary>A browser asking for a page, as opposed to fetch calls, fragments, probes and bots asking for data.</summary>
    public static bool IsPageRequest(HttpRequest request) =>
        (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
        && request.Headers.Accept.Any(a => a?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);

    /// <summary>
    /// Whether this device must pick a profile now. <paramref name="profileId"/> is null without a usable profile
    /// (none chosen, signed out, hidden, deleted, or the device revoked).
    /// </summary>
    public static bool NeedsPicker(int? profileId, string pickerMode, GatePass? pass, Guid deviceId, DateTime nowUtc, TimeZoneInfo zone)
    {
        if (profileId is null) return true;
        if (pickerMode == UntilSignOut) return false;
        // A gate cookie only counts for the device and profile that earned it.
        if (pass is null || pass.DeviceId != deviceId || pass.ProfileId != profileId) return true;
        if (pickerMode == Daily)
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(pass.PassedAtUtc, zone))
                != DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zone));
        return false;   // every-visit: the session cookie being there is the whole test
    }

    /// <summary>A short description for the admin's device list ("iPad · Safari"); never stored with more detail.</summary>
    public static string DescribeDevice(string? userAgent)
    {
        var ua = userAgent ?? "";
        var device = ua.Contains("iPad") ? "iPad" : ua.Contains("iPhone") ? "iPhone" : ua.Contains("Android") ? "Android"
            : ua.Contains("CrOS") ? "Chromebook" : ua.Contains("Windows") ? "Windows" : ua.Contains("Macintosh") ? "Mac"
            : ua.Contains("Linux") ? "Linux" : "Device";
        var browser = ua.Contains("Edg/") ? "Edge" : ua.Contains("Firefox/") || ua.Contains("FxiOS/") ? "Firefox"
            : ua.Contains("Chrome/") || ua.Contains("CriOS/") ? "Chrome" : ua.Contains("Safari/") ? "Safari" : null;
        return browser is null ? device : $"{device} · {browser}";
    }
}
