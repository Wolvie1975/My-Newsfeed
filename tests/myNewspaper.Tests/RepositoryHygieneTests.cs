using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Shared.Data;

namespace myNewspaper.Tests;

/// <summary>
/// Guards from the November 4, 2026 audit (docs/coding-standards.md, "Guardrails against past drift"): the SA password
/// had been hard-coded in ten test files and, earlier, in appsettings.json; null checks were written two ways; and some
/// classes had role-less names (BoxScoresRepository, AdminData); and about 25 components queried the context inline.
/// </summary>
public partial class RepositoryHygieneTests
{
    // Connection-string style "Password=<value>" / "Pwd=<value>". Code that reads a setting (password = process.env…)
    // has spaces around '=' and isn't matched; CHANGE_ME is the documented placeholder in .env.example.
    [GeneratedRegex(@"(?i)(?<![\w])(password|pwd)=(?!CHANGE_ME\b)([^;""'`\s<>*{}]{3,})")]
    private static partial Regex LiteralPassword();

    [Fact]
    public void No_tracked_file_contains_a_literal_connection_string_password()
    {
        var root = RepositoryRoot();
        var offenders = TrackedFiles(root)
            .Where(f => !f.Contains("/wwwroot/lib/", StringComparison.Ordinal))
            .SelectMany(f =>
            {
                var path = Path.Combine(root, f);
                if (!File.Exists(path) || new FileInfo(path).Length > 2_000_000) return [];
                return File.ReadLines(path).Select((line, i) => (f, line: i + 1, text: line))
                    .Where(l => LiteralPassword().IsMatch(l.text))
                    .Select(l => $"{l.f}:{l.line}");
            })
            .ToList();

        Assert.True(offenders.Count == 0,
            "Literal passwords in tracked files (use user secrets or environment variables): " + string.Join(", ", offenders));
    }

    // Null checks use `is null` / `is not null` (general standard section 3). The only equality null comparisons left
    // are inside EF expression trees, where C# can't use an `is` pattern (CS8122). No analyzer tells the two apart, so
    // the total is a ratchet: it may go down, never up. Moving a query between files doesn't change it. If a new
    // LINQ-to-EF predicate needs one, raise the constant in the same change and say why in the commit.
    private const int AllowedExpressionTreeNullComparisons = 160;

    [GeneratedRegex(@"[!=]=\s*null\b")]
    private static partial Regex EqualityNullCheck();

    [Fact]
    public void Null_checks_use_is_null_outside_expression_trees()
    {
        var root = RepositoryRoot();
        var hits = TrackedFiles(root)
            .Where(f => (f.StartsWith("src/", StringComparison.Ordinal) || f.StartsWith("tests/", StringComparison.Ordinal))
                && (f.EndsWith(".cs", StringComparison.Ordinal) || f.EndsWith(".razor", StringComparison.Ordinal))
                && !f.EndsWith("/RepositoryHygieneTests.cs", StringComparison.Ordinal))
            .Where(f => File.Exists(Path.Combine(root, f)))
            .SelectMany(f => File.ReadLines(Path.Combine(root, f)).Select((line, i) => (f, line: i + 1, text: line)))
            .Where(l => EqualityNullCheck().IsMatch(l.text))
            .Select(l => $"{l.f}:{l.line}")
            .ToList();

        Assert.True(hits.Count <= AllowedExpressionTreeNullComparisons,
            $"{hits.Count} lines compare with == null / != null; at most {AllowedExpressionTreeNullComparisons} are allowed (EF " +
            "expression trees, where `is null` doesn't compile). Use `is null` / `is not null` everywhere else. Lines: " +
            string.Join(", ", hits));
    }

    // Class names say their role (general standard section 2): XxxQuery, XxxStore, XxxEndpoints, XxxService,
    // XxxClient, XxxOptions. Records are DTOs and aren't checked.
    [GeneratedRegex(@"\b(class|interface)\s+(?<name>\w*(Repository|Manager|Helpers?|Data|Utils?|Utilities))\b")]
    private static partial Regex RolelessTypeName();

    [Fact]
    public void Classes_are_not_named_repository_manager_helper_or_data()
    {
        var root = RepositoryRoot();
        var offenders = TrackedFiles(root)
            .Where(f => f.StartsWith("src/", StringComparison.Ordinal)
                && (f.EndsWith(".cs", StringComparison.Ordinal) || f.EndsWith(".razor", StringComparison.Ordinal)))
            .Where(f => File.Exists(Path.Combine(root, f)))
            .SelectMany(f => File.ReadLines(Path.Combine(root, f))
                .Select(l => RolelessTypeName().Match(l))
                .Where(m => m.Success)
                .Select(m => $"{f}: {m.Groups["name"].Value}"))
            .ToList();

        Assert.True(offenders.Count == 0,
            "Name classes by role (XxxQuery, XxxStore, XxxEndpoints, XxxService, XxxClient, XxxOptions): " + string.Join(", ", offenders));
    }

    // UI code creates the context and calls an XxxQuery/XxxStore method (general standard section 6): no LINQ against a
    // DbSet in .razor files or in the hosts' endpoint and startup code. The DbSet names come from WebScraperContext, so
    // a new table is covered without editing this test.
    [Fact]
    public void Components_and_endpoints_call_query_and_store_classes_instead_of_the_context()
    {
        var sets = typeof(WebScraperContext).GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => Regex.Escape(p.Name))
            .ToList();
        Assert.NotEmpty(sets);
        var pattern = new Regex(@"\bdb(\.Context)?\.(" + string.Join("|", sets) + @")\b", RegexOptions.CultureInvariant);
        var root = RepositoryRoot();
        var offenders = TrackedFiles(root)
            .Where(f => f.StartsWith("src/", StringComparison.Ordinal)
                && (f.EndsWith(".razor", StringComparison.Ordinal)
                    || (f.EndsWith(".cs", StringComparison.Ordinal)
                        && (f.StartsWith("src/myNewspaper.Web/", StringComparison.Ordinal) || f.StartsWith("src/myNewspaper.Admin/", StringComparison.Ordinal)))))
            .Where(f => File.Exists(Path.Combine(root, f)))
            .SelectMany(f => File.ReadLines(Path.Combine(root, f)).Select((line, i) => (f, line: i + 1, text: line)))
            .Where(l => pattern.IsMatch(l.text))
            .Select(l => $"{l.f}:{l.line}")
            .ToList();

        Assert.True(offenders.Count == 0,
            "Move these queries into an XxxQuery or XxxStore class in Shared and call it from the component: " + string.Join(", ", offenders));
    }

    private static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "myNewspaper.sln"))) return dir.FullName;
        throw new InvalidOperationException("Could not find the repository root (myNewspaper.sln).");
    }

    // git's own list, so ignored files such as .env are never read.
    private static IEnumerable<string> TrackedFiles(string root)
    {
        using var git = Process.Start(new ProcessStartInfo("git", "ls-files -z")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException("git is required for this check.");
        var output = git.StandardOutput.ReadToEnd();
        git.WaitForExit();
        Assert.Equal(0, git.ExitCode);
        return output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }
}
