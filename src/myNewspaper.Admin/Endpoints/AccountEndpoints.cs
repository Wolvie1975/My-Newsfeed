using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using myNewspaper.Web.Auth;

namespace myNewspaper.Web.Endpoints;

public static class AccountEndpoints
{
    /// <summary>Form posts from the static login page and the nav-menu logout button (antiforgery is validated for [FromForm] endpoints).</summary>
    public static void MapAccount(this WebApplication app)
    {
        var account = app.MapGroup("/account");

        account.MapPost("/login", async (HttpContext http, IConfiguration config,
            [FromForm] string? password, [FromForm] string? returnUrl) =>
        {
            var target = AdminAuth.SafeReturnUrl(returnUrl);
            if (!AdminAuth.PasswordMatches(config["Admin:Password"], password))
            {
                return Results.LocalRedirect($"/login?error=1&returnUrl={Uri.EscapeDataString(target)}");
            }

            var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], AdminAuth.Scheme);
            await http.SignInAsync(AdminAuth.Scheme, new ClaimsPrincipal(identity));
            return Results.LocalRedirect(target);
        });

        account.MapPost("/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(AdminAuth.Scheme);
            return Results.LocalRedirect("/login");
        });
    }
}
