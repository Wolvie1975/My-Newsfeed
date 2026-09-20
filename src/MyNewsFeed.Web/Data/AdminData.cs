using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data;

public static class AdminData
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;
    private const int ForeignKeyViolation = 547;

    /// <summary>True when the save failed on a unique index (for example a source URL or category name that already exists).</summary>
    public static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation };

    public static bool IsForeignKeyViolation(Exception ex) =>
        ex is SqlException { Number: ForeignKeyViolation } || ex.InnerException is SqlException { Number: ForeignKeyViolation };

    /// <summary>
    /// Deletes a source. Pages reference sources with ON DELETE NO ACTION, so unless <paramref name="includePages"/>
    /// is set, the database rejects the delete while any page still points at the source.
    /// Returns false when the source no longer exists.
    /// </summary>
    public static async Task<bool> DeleteSourceAsync(WebScraperContext db, int id, bool includePages)
    {
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var tx = ownsTransaction ? await db.Database.BeginTransactionAsync() : null;

        if (includePages)
        {
            await db.Pages.Where(p => p.SourceId == id).ExecuteDeleteAsync();
        }

        var deleted = await db.Sources.Where(s => s.Id == id).ExecuteDeleteAsync();

        if (tx is not null)
        {
            await tx.CommitAsync();
        }

        return deleted > 0;
    }

    public static string Utc(DateTime? value) =>
        value is null ? "—" : value.Value.ToString("yyyy-MM-dd HH:mm") + " UTC";
}
