using System.Diagnostics;
using System.Text.RegularExpressions;

namespace myNewspaper.Tests;

/// <summary>
/// Guards from the November 4, 2026 audit (docs/coding-standards.md, "Guardrails against past drift"): the SA password
/// had been hard-coded in ten test files and, earlier, in appsettings.json.
/// </summary>
public partial class RepositoryHygieneTests
{
    // Connection-string style "Password=value" / "Pwd=value". Code that reads a setting (password = process.env…)
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
