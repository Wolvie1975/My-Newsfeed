using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MyNewsFeed.Web.Auth;

public static class AdminAuth
{
    public const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string DefaultLandingPath = "/admin/sources";

    /// <summary>Fails closed: an unset or empty configured password never matches.</summary>
    public static bool PasswordMatches(string? configured, string? supplied)
    {
        if (string.IsNullOrEmpty(configured) || supplied is null)
        {
            return false;
        }

        // Hash both sides so the comparison is fixed-length and constant-time.
        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    /// <summary>Only same-site absolute paths are allowed as post-login targets (prevents open redirects).</summary>
    public static string SafeReturnUrl(string? url)
    {
        var isLocalPath = !string.IsNullOrEmpty(url)
            && url[0] == '/'
            && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
        return isLocalPath ? url! : DefaultLandingPath;
    }
}
