using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using myNewspaper.Web.Profiles;

namespace myNewspaper.Web.Endpoints;

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
