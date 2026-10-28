using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;

namespace myNewspaper.Web.Requests;

/// <summary>Request IDs waiting for <see cref="RequestCheckerService"/>. In memory; the service re-queues on startup.</summary>
public sealed class RequestBacklog
{
    private readonly Channel<int> channel = Channel.CreateBounded<int>(new BoundedChannelOptions(1000)
    {
        FullMode = BoundedChannelFullMode.DropWrite,   // anything dropped is still unchecked and re-queued on restart
        SingleReader = true,
    });

    public void Enqueue(int requestId) => channel.Writer.TryWrite(requestId);

    public IAsyncEnumerable<int> ReadAllAsync(CancellationToken ct) => channel.Reader.ReadAllAsync(ct);
}

/// <summary>One background loop that checks each new request and writes CheckOk and CheckSummary (handoff 5.2).</summary>
public sealed class RequestCheckerService(RequestBacklog queue, RequestChecks checks, IDbContextFactory<WebScraperContext> factory,
    ILogger<RequestCheckerService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var db = await factory.CreateDbContextAsync(stoppingToken);
            foreach (var id in await db.ContentRequests.Where(r => r.Status == "waiting" && r.CheckOk == null)
                         .OrderBy(r => r.Id).Select(r => r.Id).ToListAsync(stoppingToken))
                queue.Enqueue(id);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            log.LogError(ex, "Could not re-queue unchecked requests.");
        }

        await foreach (var id in queue.ReadAllAsync(stoppingToken))
        {
            try { await CheckAsync(id, stoppingToken); }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                log.LogError(ex, "Checking request {Id} failed.", id);
            }
        }
    }

    public async Task CheckAsync(int id, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequestStore.CheckAsync(db, checks, id, ct);
    }
}
