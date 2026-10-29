using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;
using myNewspaper.Web.Profiles;
using myNewspaper.Web.Requests;

namespace myNewspaper.Tests;

public class RequestOverviewTests
{
    [Fact]
    public async Task Overview_counts_new_requests_beyond_the_display_limit_without_marking_seen()
    {
        await using var db = new WebScraperContext(new DbContextOptionsBuilder<WebScraperContext>()
            .UseSqlServer(TestDatabase.ConnectionString).Options);
        var profile = await ProfileStore.CreateAsync(db, new ProfilePins(new PasswordHasher<Profile>()),
            "zz requests " + Guid.NewGuid().ToString("N")[..8], "#0051BA", "1470", null);
        try
        {
            var seen = (await ProfileStore.GetHouseholdAsync(db)).RequestsSeenAt;
            var created = (seen is { } at && at > DateTime.UtcNow ? at : DateTime.UtcNow).AddMinutes(1);
            db.ContentRequests.AddRange(Enumerable.Range(0, 25).Select(i => new ContentRequest
            {
                ProfileId = profile.Id,
                Kind = RequestStore.News,
                Name = "Overview fixture " + i,
                Status = RequestStore.Waiting,
                CreatedAt = created
            }));
            await db.SaveChangesAsync();
            var overview = await RequestStore.OverviewAsync(db);
            Assert.Equal(20, overview.Requests.Count);
            Assert.True(overview.Waiting >= 25);
            Assert.True(overview.NewSinceSeen >= 25);
            Assert.Equal(seen, overview.SeenAt);
            Assert.Equal(overview.Waiting + overview.Locked, await RequestStore.OpenItemsCountAsync(db));
        }
        finally
        {
            await db.ContentRequests.Where(r => r.ProfileId == profile.Id).ExecuteDeleteAsync();
            await db.Profiles.Where(p => p.Id == profile.Id).ExecuteDeleteAsync();
        }
    }
}
