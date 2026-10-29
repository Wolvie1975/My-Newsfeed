using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;

namespace myNewspaper.Web.Endpoints;

/// <summary>A database context for one admin request, disposed with the request, plus its clock and cancellation.</summary>
public sealed class RequestDb(WebScraperContext context, DateTime now, CancellationToken ct) : IAsyncDisposable
{
    public WebScraperContext Context { get; } = context;
    public DateTime Now { get; } = now;
    public CancellationToken Ct { get; } = ct;

    public static async ValueTask<RequestDb?> BindAsync(HttpContext http)
    {
        var factory = http.RequestServices.GetRequiredService<IDbContextFactory<WebScraperContext>>();
        var clock = http.RequestServices.GetRequiredService<TimeProvider>();
        var db = new RequestDb(await factory.CreateDbContextAsync(http.RequestAborted), clock.GetUtcNow().UtcDateTime, http.RequestAborted);
        http.Response.RegisterForDisposeAsync(db);
        return db;
    }

    public ValueTask DisposeAsync() => Context.DisposeAsync();
}
