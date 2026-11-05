using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace myNewspaper.Shared.Hosting;

/// <summary>
/// Which build is running: the commit it was built from and when. Served at <c>/version</c> on both sites so a deploy
/// can confirm the live build is the one it just built (coding-standards.md, Releases). Nothing else is exposed: no
/// settings, environment or paths.
/// </summary>
public sealed record BuildInfo(string Commit, string? BuiltAt)
{
    public const string Unknown = "unknown";

    public static BuildInfo Current { get; } = From(Assembly.GetEntryAssembly());

    public static BuildInfo From(Assembly? assembly)
    {
        var informational = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var builtAt = assembly?.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "BuildTime")?.Value;
        return new BuildInfo(CommitOf(informational), string.IsNullOrWhiteSpace(builtAt) ? null : builtAt);
    }

    /// <summary>The SDK appends <c>+&lt;SourceRevisionId&gt;</c> to the informational version: the git commit, from the
    /// repository locally or from the GIT_COMMIT build argument in Docker.</summary>
    public static string CommitOf(string? informationalVersion)
    {
        var plus = informationalVersion?.IndexOf('+', StringComparison.Ordinal) ?? -1;
        return plus >= 0 && plus < informationalVersion!.Length - 1 ? informationalVersion[(plus + 1)..] : Unknown;
    }
}

public static class VersionEndpoints
{
    /// <summary>GET /version → <c>{ "commit": "…", "builtAt": "…" }</c>. Anonymous, like /healthz.</summary>
    public static RouteHandlerBuilder MapVersion(this IEndpointRouteBuilder app) =>
        app.MapGet("/version", () => Results.Ok(BuildInfo.Current)).ExcludeFromDescription();
}
