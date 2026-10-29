using System.Data.Common;

namespace myNewspaper.Web.Data;

public static class DataFailures
{
    /// <summary>
    /// A database read that should degrade to an "unavailable" state instead of failing the page:
    /// connection/command errors, or EF reporting the store is unusable.
    /// </summary>
    public static bool IsReadFailure(Exception ex) => ex is DbException or InvalidOperationException;
}
