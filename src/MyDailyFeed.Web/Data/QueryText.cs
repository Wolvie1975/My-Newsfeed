namespace MyDailyFeed.Web.Data;

/// <summary>Bound user-entered text used by individual listing filters.</summary>
public static class QueryText
{
    public static string Term(string? value)
    {
        var term = (value ?? "").Trim();
        return term.Length > 120 ? term[..120] : term;
    }
}
