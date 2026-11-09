using Microsoft.EntityFrameworkCore;

namespace myNewspaper.Shared.Data;

/// <summary>
/// A named lock held in SQL Server (sp_getapplock), so work that must not overlap is serialized across processes:
/// Web and Admin are separate containers, and an in-process SemaphoreSlim only covers one of them. The lock belongs
/// to the context's connection, which stays open until the handle is disposed. Dispose releases it explicitly: a pooled
/// connection's session outlives Close. If the connection has dropped, the server ended the session and the lock.
/// </summary>
public sealed class DatabaseLock : IAsyncDisposable
{
    private readonly WebScraperContext db;
    private readonly string resource;

    private DatabaseLock(WebScraperContext db, string resource)
    {
        this.db = db;
        this.resource = resource;
    }

    /// <summary>
    /// Takes the lock, waiting at most <paramref name="wait"/> (keep it under the 30 s command timeout); null when another
    /// session still holds it. <paramref name="db"/> must not be used for anything else while the lock is held.
    /// </summary>
    public static async Task<DatabaseLock?> TryAcquireAsync(WebScraperContext db, string resource, TimeSpan wait, CancellationToken ct = default)
    {
        await db.Database.OpenConnectionAsync(ct);
        var milliseconds = (int)wait.TotalMilliseconds;
        // 0 granted, 1 granted after waiting; negative: timed out, cancelled, deadlock victim or error.
        var result = (await db.Database.SqlQuery<int>($"""
            DECLARE @result int;
            EXEC @result = sp_getapplock @Resource = {resource}, @LockMode = N'Exclusive', @LockOwner = N'Session', @LockTimeout = {milliseconds};
            SELECT @result AS Value
            """).ToListAsync(ct)).Single();
        if (result >= 0) return new DatabaseLock(db, resource);
        await db.Database.CloseConnectionAsync();
        return null;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await db.Database.ExecuteSqlAsync($"EXEC sp_releaseapplock @Resource = {resource}, @LockOwner = N'Session'");
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
