using Microsoft.Extensions.Configuration;

namespace myNewspaper.Tests;

/// <summary>
/// Connection string for the integration tests. Resolved from the web app's user secrets or the
/// ConnectionStrings__DefaultConnection environment variable, so no credentials live in the repository.
/// </summary>
public static class TestDatabase
{
    private const string WebUserSecretsId = "c579816a-020e-445a-92f6-efdd7002106c";

    public static string ConnectionString { get; } = new ConfigurationBuilder()
        .AddUserSecrets(WebUserSecretsId)
        .AddEnvironmentVariables()
        .Build()
        .GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Set ConnectionStrings:DefaultConnection with `dotnet user-secrets set` in src/myNewspaper.Web, "
            + "or the ConnectionStrings__DefaultConnection environment variable.");
}
