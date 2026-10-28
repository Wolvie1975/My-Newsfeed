using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace myNewspaper.Web.Profiles;

public sealed record DeviceToken(Guid DeviceId, byte[] Secret)
{
    public byte[] SecretHash => SHA256.HashData(Secret);
}

/// <summary>When this device last passed the picker, and for which profile.</summary>
public sealed record GatePass(Guid DeviceId, int ProfileId, DateTime PassedAtUtc);

/// <summary>
/// The two profile cookies (profiles handoff 2.1). <c>mnp.device</c> is long-lived and names the device; the database
/// keeps only a hash of its secret, so a copied database can't mint cookies. <c>mnp.gate</c> is a session cookie that
/// records passing the picker. Both are protected with Data Protection, which persists to the /keys volume.
/// </summary>
public sealed class ProfileCookies(IDataProtectionProvider provider)
{
    public const string DeviceCookie = "mnp.device";
    public const string GateCookie = "mnp.gate";

    // Browsers cap cookie lifetimes at about 400 days.
    private static readonly TimeSpan DeviceLifetime = TimeSpan.FromDays(400);

    private readonly IDataProtector device = provider.CreateProtector("myNewspaper.Profiles.Device.v1");
    private readonly IDataProtector gate = provider.CreateProtector("myNewspaper.Profiles.Gate.v1");

    public static DeviceToken NewDevice() => new(Guid.NewGuid(), RandomNumberGenerator.GetBytes(32));

    public DeviceToken? ReadDevice(HttpContext http)
    {
        var raw = Unprotect(device, http.Request.Cookies[DeviceCookie]);
        var parts = raw?.Split('.');
        if (parts is not { Length: 2 } || !Guid.TryParseExact(parts[0], "N", out var id)) return null;
        try
        {
            var secret = WebEncoders.Base64UrlDecode(parts[1]);
            return secret.Length == 32 ? new(id, secret) : null;
        }
        catch (FormatException) { return null; }
    }

    public void WriteDevice(HttpContext http, DeviceToken token) =>
        http.Response.Cookies.Append(DeviceCookie, device.Protect($"{token.DeviceId:N}.{WebEncoders.Base64UrlEncode(token.Secret)}"),
            Options(http, DateTimeOffset.UtcNow.Add(DeviceLifetime)));

    public GatePass? ReadGate(HttpContext http)
    {
        var parts = Unprotect(gate, http.Request.Cookies[GateCookie])?.Split('|');
        return parts is { Length: 3 } && Guid.TryParseExact(parts[0], "N", out var id)
            && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var profile)
            && long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            && ticks <= DateTime.MaxValue.Ticks
                ? new(id, profile, new DateTime(ticks, DateTimeKind.Utc)) : null;
    }

    /// <summary>No expiry, so the browser drops it when the session ends.</summary>
    public void WriteGate(HttpContext http, GatePass pass) =>
        http.Response.Cookies.Append(GateCookie,
            gate.Protect(string.Create(CultureInfo.InvariantCulture, $"{pass.DeviceId:N}|{pass.ProfileId}|{pass.PassedAtUtc.Ticks}")),
            Options(http, null));

    public static void ClearGate(HttpContext http) => http.Response.Cookies.Delete(GateCookie, Options(http, null));

    private static CookieOptions Options(HttpContext http, DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        // LAN HTTP is still served (DEPLOY.md), so Secure follows the request instead of being forced.
        Secure = http.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Expires = expires,
        IsEssential = true,
    };

    private static string? Unprotect(IDataProtector protector, string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 2048) return null;
        try { return protector.Unprotect(value); }
        catch (CryptographicException) { return null; }
    }
}
