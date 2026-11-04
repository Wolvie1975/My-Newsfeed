using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using myNewspaper.Shared.Data;

namespace myNewspaper.Tests;

/// <summary>
/// The integration tests' database. The server and credentials come from the web app's user secrets or the
/// ConnectionStrings__DefaultConnection environment variable, so no credentials live in the repository; the database is
/// always the dedicated test copy (<see cref="DatabaseName"/>, or TEST_DATABASE), never the live WebScraper database.
/// </summary>
public static class TestDatabase
{
    /// <summary>The test copy of WebScraper on the same server. Tests insert and delete rows here.</summary>
    public const string DatabaseName = "WebScraper_Test";

    private const string LiveDatabaseName = "WebScraper";
    private const string WebUserSecretsId = "c579816a-020e-445a-92f6-efdd7002106c";

    public static string ConnectionString { get; } = Build();

    /// <summary>A new context on the test database. Dispose it (<c>await using</c>) like any other.</summary>
    public static WebScraperContext CreateContext() =>
        new(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(ConnectionString).Options);

    private static string Build()
    {
        var configured = new ConfigurationBuilder()
            .AddUserSecrets(WebUserSecretsId)
            .AddEnvironmentVariables()
            .Build()
            .GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings:DefaultConnection with `dotnet user-secrets set` in src/myNewspaper.Web, "
                + "or the ConnectionStrings__DefaultConnection environment variable.");
        var database = Environment.GetEnvironmentVariable("TEST_DATABASE") is { Length: > 0 } name ? name : DatabaseName;
        if (database.Equals(LiveDatabaseName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Integration tests never run against the live {LiveDatabaseName} database.");
        return new SqlConnectionStringBuilder(configured) { InitialCatalog = database }.ConnectionString;
    }
}
