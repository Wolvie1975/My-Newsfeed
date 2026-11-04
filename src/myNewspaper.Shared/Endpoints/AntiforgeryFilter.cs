using Microsoft.AspNetCore.Antiforgery;

namespace myNewspaper.Web.Endpoints;

/// <summary>
/// Antiforgery for form posts that bind no form fields. UseAntiforgery only records the result for such endpoints, and
/// form binding is what normally rejects a bad token, so a POST that binds nothing would otherwise go unchecked.
/// </summary>
public static class AntiforgeryFilter
{
    /// <summary>Validates the antiforgery token before the handler runs; a missing or invalid token returns 400.</summary>
    public static TBuilder RequireAntiforgery<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            try { await http.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(http); }
            catch (AntiforgeryValidationException) { return Results.BadRequest(new { error = "Refresh the page and try again." }); }
            return await next(context);
        });
}
