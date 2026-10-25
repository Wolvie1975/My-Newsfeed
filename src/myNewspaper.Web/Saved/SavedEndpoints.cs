using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace myNewspaper.Web.Saved;

public static class SavedEndpoints
{
    public static void MapSharedArticles(this WebApplication app)
    {
        var api = app.MapGroup("/api/saved");
        api.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            http.Response.Headers.CacheControl = "no-store";
            try
            {
                if (http.Request.Method != "GET")
                    await http.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(http);
                return await next(context);
            }
            catch (AntiforgeryValidationException) { return Results.BadRequest(new { error = "Refresh the page and try again." }); }
            catch (ArgumentException) { return Results.BadRequest(new { error = "Invalid saved content." }); }
            catch (SavedListFullException) { return Results.Conflict(new { error = "Your shared list is full (500 items). Remove some items and try again." }); }
        });
        api.MapGet("", async (HttpContext http, IAntiforgery antiforgery, SharedArticles saved) =>
            Results.Ok(new { articles = await saved.Read(http.RequestAborted), token = antiforgery.GetAndStoreTokens(http).RequestToken }));
        api.MapPost("/add", async (SavedArticle article, SharedArticles saved, HttpContext http) =>
            Results.Ok(new { articles = await saved.Add(article, http.RequestAborted) }))
            .WithMetadata(new RequestSizeLimitAttribute(2_000_000));
        api.MapPost("/remove", async (SavedRemoval article, SharedArticles saved, HttpContext http) =>
            Results.Ok(new { articles = await saved.Remove(article.Url, http.RequestAborted) }))
            .WithMetadata(new RequestSizeLimitAttribute(2_000_000));
        api.MapPost("/import", async (SavedImport import, SharedArticles saved, HttpContext http) =>
            Results.Ok(new { articles = await saved.Import(import, http.RequestAborted) }))
            .WithMetadata(new RequestSizeLimitAttribute(2_000_000));
    }
}
