using System.Reflection;
using myNewspaper.Shared.Hosting;

namespace myNewspaper.Tests;

public class BuildInfoTests
{
    [Theory]
    [InlineData("1.0.0+0a988e157268abc", "0a988e157268abc")]
    [InlineData("1.0.0", BuildInfo.Unknown)]
    [InlineData("1.0.0+", BuildInfo.Unknown)]
    [InlineData(null, BuildInfo.Unknown)]
    public void The_commit_is_whatever_follows_the_plus_in_the_informational_version(string? version, string commit) =>
        Assert.Equal(commit, BuildInfo.CommitOf(version));

    [Fact]
    public void A_local_build_of_the_shared_library_carries_its_git_commit()
    {
        // Built inside the repository, the SDK sets SourceRevisionId from git; without a build time it stays null.
        var info = BuildInfo.From(typeof(BuildInfo).Assembly);
        Assert.Matches("^[0-9a-f]{40}$", info.Commit);
        Assert.Null(info.BuiltAt);
    }

    [Fact]
    public void No_assembly_means_unknown() => Assert.Equal(new BuildInfo(BuildInfo.Unknown, null), BuildInfo.From((Assembly?)null));
}
