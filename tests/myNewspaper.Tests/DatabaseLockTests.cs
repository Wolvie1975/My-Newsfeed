using myNewspaper.Shared.Data;

namespace myNewspaper.Tests;

/// <summary>
/// The lock ScoresSync takes so Web's background sync and Admin's Sync now (separate processes) never upsert the same
/// games at once. Two contexts stand in for the two processes: each has its own connection, so its own SQL session.
/// </summary>
public class DatabaseLockTests
{
    private static string Resource() => "myNewspaper.tests." + Guid.NewGuid().ToString("N");

    [Fact]
    public async Task A_second_session_cannot_take_the_lock_until_the_first_releases_it()
    {
        var resource = Resource();
        await using var first = TestDatabase.CreateContext();
        await using var second = TestDatabase.CreateContext();

        var held = await DatabaseLock.TryAcquireAsync(first, resource, TimeSpan.Zero);
        Assert.NotNull(held);
        Assert.Null(await DatabaseLock.TryAcquireAsync(second, resource, TimeSpan.FromMilliseconds(500)));

        await held.DisposeAsync();
        var next = await DatabaseLock.TryAcquireAsync(second, resource, TimeSpan.Zero);
        Assert.NotNull(next);
        await next.DisposeAsync();
    }

    [Fact]
    public async Task A_waiting_session_gets_the_lock_as_soon_as_it_is_released()
    {
        var resource = Resource();
        await using var first = TestDatabase.CreateContext();
        await using var second = TestDatabase.CreateContext();
        var held = await DatabaseLock.TryAcquireAsync(first, resource, TimeSpan.Zero);
        Assert.NotNull(held);

        var waiting = DatabaseLock.TryAcquireAsync(second, resource, TimeSpan.FromSeconds(10));
        await Task.Delay(300);
        Assert.False(waiting.IsCompleted);
        await held.DisposeAsync();

        var next = await waiting.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.NotNull(next);
        await next.DisposeAsync();
    }
}
