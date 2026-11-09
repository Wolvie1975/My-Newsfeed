using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

namespace myNewspaper.Tests;

/// <summary>
/// Home is assembled from many independent reads; when the database can't be read, each section degrades on its own
/// and the page still renders (dotnet-coding-standard, error handling). The app points at a database that doesn't
/// exist on the test server, so every read fails quickly with a login error instead of waiting for a timeout.
/// </summary>
public class HomeDegradationTests
{
    private sealed class UnreadableDatabaseApp : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var connection = new SqlConnectionStringBuilder(TestDatabase.ConnectionString)
            {
                InitialCatalog = "myNewspaper_missing_" + Guid.NewGuid().ToString("N")[..8],
                ConnectTimeout = 5,
            };
            builder.UseSetting("ConnectionStrings:DefaultConnection", connection.ConnectionString);
            builder.UseSetting("Profiles:Enabled", "false");
            builder.UseSetting("Results:BackgroundSyncEnabled", "false");
            builder.UseSetting("BackgroundJobs:Enabled", "false");
        }
    }

    [Fact]
    public async Task Home_renders_with_each_section_unavailable_when_the_database_cannot_be_read()
    {
        await using var app = new UnreadableDatabaseApp();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var request = new HttpRequestMessage(HttpMethod.Get, "/") { Headers = { { "Accept", "text/html" } } };

        var response = await client.SendAsync(request);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Today’s scoreboard is temporarily unavailable.", html);
        Assert.Contains("Today’s games are temporarily unavailable.", html);
        Assert.Contains("More headlines are temporarily unavailable.", html);
        Assert.Contains("Sports headlines are temporarily unavailable.", html);
        Assert.Contains("Entertainment headlines are temporarily unavailable.", html);
        Assert.Contains("Tech headlines are temporarily unavailable.", html);
        Assert.Contains("Today’s episodes are temporarily unavailable.", html);
        Assert.Contains("Videos are temporarily unavailable.", html);
        // An unavailable section never claims there's nothing to show.
        Assert.DoesNotContain("No new episodes from followed series today.", html);
        Assert.DoesNotContain("No videos yet.", html);
        Assert.DoesNotContain("No games on today’s scoreboard.", html);
    }
}
