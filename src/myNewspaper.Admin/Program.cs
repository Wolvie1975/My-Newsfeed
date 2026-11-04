using Microsoft.AspNetCore.DataProtection;
using myNewspaper.Admin.Components;
using myNewspaper.Hosting;
using myNewspaper.Web.Auth;
using myNewspaper.Web.Endpoints;

namespace myNewspaper.Admin;

public partial class AdminProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-Saved-CSRF";
            options.Cookie.Name = "myNewspaper.Admin.Csrf";
        });
        builder.Services.AddAuthentication(AdminAuth.Scheme).AddCookie(AdminAuth.Scheme, options =>
        {
            options.Cookie.Name = "myNewspaper.Admin.Session";
            options.Cookie.HttpOnly = true;
            // Admin is served to browsers over HTTPS (Tailscale, port 8443): the session cookie is never sent in cleartext.
            // Only the session cookie: antiforgery refuses to run at all with SecurePolicy.Always when the app itself
            // sees plain HTTP behind the proxy.
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/login";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.Events.OnRedirectToLogin = context => AdminStatus(context, StatusCodes.Status401Unauthorized);
            options.Events.OnRedirectToAccessDenied = context => AdminStatus(context, StatusCodes.Status403Forbidden);
        });
        builder.Services.AddAuthorization();
        builder.Services.AddCascadingAuthenticationState();
        var protection = builder.Services.AddDataProtection().SetApplicationName("myNewspaper.Admin");
        if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keys)
            protection.PersistKeysToFileSystem(new DirectoryInfo(keys));
        // Shared with the public host. Only explicit admin actions use the sync and request services here; scheduled
        // workers remain in the public host.
        builder.Services.AddMyNewspaperCore(builder.Configuration, CoreHostLabels.Admin);
        var app = builder.Build();
        app.UseForwardedHeaders();
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();
        app.Use(async (http, next) =>
        {
            if (!Path.HasExtension(http.Request.Path)) http.Response.Headers.CacheControl = "no-store";
            await next();
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapAccount();
        app.MapAdminProfiles();
        app.MapGet("/", () => Results.LocalRedirect("/admin"));
        app.MapHealthChecks("/healthz");
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        app.Run();
    }

    private static Task AdminStatus(Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions> context, int status)
    {
        if (context.Request.Path.StartsWithSegments("/admin/api"))
        {
            context.HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodePagesFeature>()?.Enabled = false;
            context.Response.StatusCode = status;
        }
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }
}
