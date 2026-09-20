using MyNewsFeed.Web.Auth;

namespace MyNewsFeed.Tests;

public class AdminAuthTests
{
    [Fact]
    public void Correct_password_matches() => Assert.True(AdminAuth.PasswordMatches("s3cret", "s3cret"));

    [Theory]
    [InlineData("s3cret", "wrong")]
    [InlineData("s3cret", "S3CRET")]
    [InlineData("s3cret", "")]
    [InlineData("s3cret", null)]
    public void Wrong_password_is_rejected(string configured, string? supplied) =>
        Assert.False(AdminAuth.PasswordMatches(configured, supplied));

    [Theory]
    [InlineData(null, "")]
    [InlineData(null, "anything")]
    [InlineData("", "")]
    public void Unset_configured_password_never_matches(string? configured, string? supplied) =>
        Assert.False(AdminAuth.PasswordMatches(configured, supplied));

    [Theory]
    [InlineData("/admin/pages", "/admin/pages")]
    [InlineData("/admin/pages?sourceId=2", "/admin/pages?sourceId=2")]
    [InlineData("/", "/")]
    public void Local_return_urls_are_kept(string url, string expected) =>
        Assert.Equal(expected, AdminAuth.SafeReturnUrl(url));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("//evil.example")]
    [InlineData("/\\evil.example")]
    [InlineData("https://evil.example")]
    [InlineData("admin/pages")]
    public void Unsafe_return_urls_fall_back_to_the_landing_page(string? url) =>
        Assert.Equal(AdminAuth.DefaultLandingPath, AdminAuth.SafeReturnUrl(url));
}
