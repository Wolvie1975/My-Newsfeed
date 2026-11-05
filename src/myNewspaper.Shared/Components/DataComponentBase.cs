using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace myNewspaper.Shared.Components;

/// <summary>
/// Base for components that read the database or call other services. <see cref="Ct"/> is the token to pass to reads:
/// during static rendering it is the request's <see cref="HttpContext.RequestAborted"/>, so a page the visitor has
/// left stops querying; in an interactive component it is cancelled when the component is disposed (navigating away,
/// closing the circuit). Writes a person started (save, delete, approve) pass <see cref="CancellationToken.None"/>
/// instead, so they finish even if the person navigates away straight after clicking (dotnet-coding-standard.md §4).
/// </summary>
public abstract class DataComponentBase : ComponentBase, IDisposable
{
    private CancellationTokenSource? source;
    private bool disposed;

    [CascadingParameter] private HttpContext? RequestContext { get; set; }

    /// <summary>Cancelled when the request is aborted (static rendering) or the component is disposed.</summary>
    protected CancellationToken Ct
    {
        get
        {
            if (disposed) return new CancellationToken(canceled: true);
            source ??= RequestContext is { } http
                ? CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted)
                : new CancellationTokenSource();
            return source.Token;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed || !disposing) return;
        disposed = true;
        source?.Cancel();
        source?.Dispose();
    }
}
