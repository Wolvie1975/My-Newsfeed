using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;
using myNewspaper.Web.Profiles;
using myNewspaper.Web.Requests;

namespace myNewspaper.Tests;

// Live-database checks for profiles (handoff F2 and Phase 7). Each test runs inside a transaction that is rolled back.
public class ProfileDataTests
{
    private static readonly ProfilePins Pins = new(new PasswordHasher<Profile>());
    private static readonly DateTime Now = new(2026, 10, 28, 14, 0, 0, DateTimeKind.Utc);

    private static WebScraperContext Create() =>
        new(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(TestDatabase.ConnectionString).Options);

    private static string Tag() => Guid.NewGuid().ToString("N")[..10];

    private static Task<Profile> NewProfile(WebScraperContext db, string pin = "1234", JsonObject? choices = null) =>
        ProfileStore.CreateAsync(db, Pins, "zz " + Tag(), "#0051ba", pin, choices);

    private static Task<ProfileDevice> NewDevice(WebScraperContext db) =>
        ProfileStore.CreateDeviceAsync(db, ProfileCookies.NewDevice(), "tests", ProfileGate.EveryVisit);

    [Fact]
    public async Task Five_wrong_pins_lock_the_profile_until_the_admin_acts()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var profile = await NewProfile(db);
        var device = await NewDevice(db);

        for (var left = 4; left >= 1; left--)
            Assert.Equal(new OpenResult(OpenOutcome.WrongPin, left), await ProfileStore.OpenAsync(db, Pins, device.Id, profile.Id, "0000", Now));
        Assert.Equal(OpenOutcome.Locked, (await ProfileStore.OpenAsync(db, Pins, device.Id, profile.Id, "9999", Now)).Outcome);

        // No timed unlock: the right PIN still fails after 15 minutes and after a day.
        Assert.Equal(OpenOutcome.Locked, (await ProfileStore.OpenAsync(db, Pins, device.Id, profile.Id, "1234", Now.AddMinutes(15))).Outcome);
        Assert.Equal(OpenOutcome.Locked, (await ProfileStore.OpenAsync(db, Pins, device.Id, profile.Id, "1234", Now.AddDays(1))).Outcome);
        var row = Assert.Single(await ProfileAdmin.ListAsync(db), p => p.Id == profile.Id);
        Assert.Equal(device.Label, row.LockedByDevice);
        Assert.Contains(await ProfileAdmin.DevicesAsync(db, profile.Id) ?? [], d => d.Id == device.Id && d.CausedLock);

        // Unlock keeps the PIN.
        Assert.True(await ProfileAdmin.UnlockAsync(db, profile.Id));
        Assert.Equal(OpenOutcome.Opened, (await ProfileStore.OpenAsync(db, Pins, device.Id, profile.Id, "1234", Now)).Outcome);
        Assert.Equal(profile.Id, (await db.ProfileDevices.AsNoTracking().SingleAsync(d => d.Id == device.Id)).ProfileId);
    }

    [Fact]
    public async Task Reset_pin_replaces_it_clears_the_lock_and_signs_out_every_device()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var profile = await NewProfile(db);
        var device = await NewDevice(db);
        Assert.Equal(OpenOutcome.Opened, (await ProfileStore.OpenAsync(db, Pins, device.Id, profile.Id, "1234", Now)).Outcome);
        var other = await NewDevice(db);
        for (var i = 0; i < ProfileRules.MaxWrongPins; i++) await ProfileStore.OpenAsync(db, Pins, other.Id, profile.Id, "0000", Now);

        Assert.True(await ProfileAdmin.ResetPinAsync(db, Pins, profile.Id, "5678", Now));
        Assert.NotNull((await db.ProfileDevices.AsNoTracking().SingleAsync(d => d.Id == device.Id)).RevokedAt);
        Assert.Null((await db.Profiles.AsNoTracking().SingleAsync(p => p.Id == profile.Id)).LockedAt);
        Assert.Equal(OpenOutcome.WrongPin, (await ProfileStore.OpenAsync(db, Pins, other.Id, profile.Id, "1234", Now)).Outcome);
        Assert.Equal(OpenOutcome.Opened, (await ProfileStore.OpenAsync(db, Pins, other.Id, profile.Id, "5678", Now)).Outcome);
        // The revoked device can't open anything until it comes back as a new device.
        Assert.Equal(OpenOutcome.NoDevice, (await ProfileStore.OpenAsync(db, Pins, device.Id, profile.Id, "5678", Now)).Outcome);
        await Assert.ThrowsAsync<ArgumentException>(() => ProfileAdmin.ResetPinAsync(db, Pins, profile.Id, "12345", Now));
    }

    [Fact]
    public async Task Changing_the_pin_needs_the_current_one_and_wrong_ones_count()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var profile = await NewProfile(db);
        var device = await NewDevice(db);
        Assert.Equal(new OpenResult(OpenOutcome.WrongPin, 4), await ProfileStore.ChangePinAsync(db, Pins, device.Id, profile.Id, "0000", "4321", Now));
        Assert.Equal(OpenOutcome.Opened, (await ProfileStore.ChangePinAsync(db, Pins, device.Id, profile.Id, "1234", "4321", Now)).Outcome);
        Assert.Equal(OpenOutcome.Opened, (await ProfileStore.OpenAsync(db, Pins, device.Id, profile.Id, "4321", Now)).Outcome);
        await Assert.ThrowsAsync<ArgumentException>(() => ProfileStore.ChangePinAsync(db, Pins, device.Id, profile.Id, "4321", "", Now));
    }

    [Fact]
    public async Task A_profile_without_a_pin_must_choose_one_first_and_only_once()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var seeded = new Profile { Name = "zz " + Tag(), Color = "#0051BA" };
        db.Profiles.Add(seeded);
        await db.SaveChangesAsync();
        var device = await NewDevice(db);
        Assert.Equal(OpenOutcome.NeedsPin, (await ProfileStore.OpenAsync(db, Pins, device.Id, seeded.Id, null, Now)).Outcome);
        await Assert.ThrowsAsync<ArgumentException>(() => ProfileStore.SetFirstPinAsync(db, Pins, device.Id, seeded.Id, "12"));
        Assert.Equal(OpenOutcome.Opened, (await ProfileStore.SetFirstPinAsync(db, Pins, device.Id, seeded.Id, "2468")).Outcome);
        Assert.Equal(OpenOutcome.NotFound, (await ProfileStore.SetFirstPinAsync(db, Pins, device.Id, seeded.Id, "1111")).Outcome);
        Assert.Equal(OpenOutcome.Opened, (await ProfileStore.OpenAsync(db, Pins, device.Id, seeded.Id, "2468", Now)).Outcome);
    }

    [Fact]
    public async Task New_profiles_need_a_pin_and_a_free_name()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var profile = await NewProfile(db);
        await Assert.ThrowsAsync<ArgumentException>(() => ProfileStore.CreateAsync(db, Pins, "zz " + Tag(), "#0051BA", null, null));
        await Assert.ThrowsAsync<ArgumentException>(() => ProfileStore.CreateAsync(db, Pins, "zz " + Tag(), "red", "1234", null));
        await Assert.ThrowsAsync<ProfileNameTakenException>(() => ProfileStore.CreateAsync(db, Pins, profile.Name, "#0051BA", "1234", null));
    }

    [Fact]
    public async Task New_profiles_copy_the_base_edition_once_and_start_with_household_channels_on()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var tag = Tag();
        var household = new YoutubeVideoFeed { ChannelId = $"UC{tag}", Url = $"https://{tag}.test.invalid/feed" };
        db.YoutubeVideoFeeds.Add(household);
        await db.SaveChangesAsync();
        db.ChannelRollouts.Add(new ChannelRollout { YoutubeVideoFeedId = household.Id });
        await db.SaveChangesAsync();

        await ProfileAdmin.SaveBaseEditionAsync(db, JsonNode.Parse("""{"teams":["big12:kansas"],"services":[8],"theme":"dark"}""")!.AsObject());
        var profile = await NewProfile(db, choices: JsonNode.Parse("""{"services":[15],"theme":null}""")!.AsObject());
        Assert.Equal("""{"teams":["big12:kansas"],"services":[15]}""", (await ProfileStore.GetSettingsAsync(db, profile.Id)).ToJsonString());
        Assert.True(await db.ProfileChannelFollows.AnyAsync(f => f.ProfileId == profile.Id && f.YoutubeVideoFeedId == household.Id));

        // Changing the base edition later never changes an existing profile.
        await ProfileAdmin.SaveBaseEditionAsync(db, []);
        Assert.Equal("""{"teams":["big12:kansas"],"services":[15]}""", (await ProfileStore.GetSettingsAsync(db, profile.Id)).ToJsonString());
        await Assert.ThrowsAsync<ArgumentException>(() => ProfileAdmin.SaveBaseEditionAsync(db, JsonNode.Parse("""{"theme":"neon"}""")!.AsObject()));
    }

    [Fact]
    public async Task Settings_patch_by_key_and_the_browser_import_happens_once()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var profile = await NewProfile(db);
        await ProfileStore.PatchSettingsAsync(db, profile.Id, JsonNode.Parse("""{"theme":"dark"}""")!.AsObject());
        var imported = await ProfileStore.ImportBrowserAsync(db, profile.Id, JsonNode.Parse("""{"theme":"light","teams":["a"]}""")!.AsObject(), Now);
        Assert.Equal("""{"theme":"dark","teams":["a"]}""", imported.ToJsonString());
        var again = await ProfileStore.ImportBrowserAsync(db, profile.Id, JsonNode.Parse("""{"services":[8]}""")!.AsObject(), Now);
        Assert.Equal("""{"theme":"dark","teams":["a"]}""", again.ToJsonString());
        await Assert.ThrowsAsync<ArgumentException>(() => ProfileStore.PatchSettingsAsync(db, profile.Id, JsonNode.Parse("""{"theme":"neon"}""")!.AsObject()));
    }

    [Fact]
    public async Task Hidden_profiles_and_revoked_or_forgotten_devices_count_as_no_profile()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var profile = await NewProfile(db);
        var token = ProfileCookies.NewDevice();
        var device = await ProfileStore.CreateDeviceAsync(db, token, "tests", ProfileGate.Daily);
        await ProfileStore.AssignDeviceAsync(db, device.Id, profile.Id);
        var (found, open) = await ProfileStore.LoadDeviceAsync(db, token, null, DateTime.UtcNow);
        Assert.Equal(ProfileGate.Daily, found!.PickerMode);
        Assert.Equal(profile.Id, open!.Id);

        // A cookie with the right ID but another secret is nobody.
        Assert.Null((await ProfileStore.LoadDeviceAsync(db, token with { Secret = new byte[32] }, null, DateTime.UtcNow)).Device);
        // Unused for longer than the household limit: forgotten.
        Assert.Null((await ProfileStore.LoadDeviceAsync(db, token, 90, DateTime.UtcNow.AddDays(91))).Device);

        await ProfileAdmin.UpdateAsync(db, profile.Id, profile.Name, profile.Color, hidden: true, Now);
        Assert.Null((await ProfileStore.LoadDeviceAsync(db, token, null, DateTime.UtcNow)).Device);   // hiding revokes devices
    }

    [Fact]
    public async Task Another_profiles_follows_never_appear_and_look_like_unknown_ids()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var tag = Tag();
        var dates = FeedDates.Create("America/Chicago");
        var showId = Random.Shared.Next(1_500_000_000, 2_000_000_000);
        var series = new TvSeries
        {
            TvmazeShowId = showId,
            Title = $"zz {tag} show",
            SourceUrl = "https://www.tvmaze.com/shows/1",
            RawMetadata = "{}",
            FirstSeenAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };
        db.Add(series);
        await db.SaveChangesAsync();
        var tracked = new TvTrackedSeries { TvmazeShowId = showId, ExpectedTitle = series.Title, SeriesId = series.Id, Enabled = true };
        db.Add(tracked);
        db.Add(new TvEpisode
        {
            TvmazeEpisodeId = showId,
            SeriesId = series.Id,
            SeasonNumber = 1,
            EpisodeNumber = 1,
            Airdate = dates.Today,
            IsDateOnly = true,
            RawAirstamp = "x",
            SourceUrl = "https://www.tvmaze.com/episodes/1",
            RawMetadata = "{}",
            FirstSeenAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        });
        var category = new YoutubeCategory { CategoryName = $"Zz {tag}", DateAdded = DateTime.UtcNow };
        var feed = new YoutubeVideoFeed
        {
            ChannelId = $"UC{tag}",
            ChannelName = $"zz {tag} channel",
            Url = $"https://{tag}.test.invalid/feed",
            YoutubeCategory = category
        };
        db.Add(new YouTubeVideo
        {
            VideoId = tag[..10] + "x",
            ChannelId = feed.ChannelId,
            Title = $"zz {tag} video",
            Url = "https://www.youtube.com/watch?v=x",
            PublishedAt = DateTime.UtcNow,
            FirstSeenAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            YoutubeVideoFeed = feed
        });
        await db.SaveChangesAsync();

        var a = await NewProfile(db);
        var b = await NewProfile(db);
        db.ProfileTvFollows.Add(new ProfileTvFollow { ProfileId = a.Id, TvTrackedSeriesId = tracked.Id });
        db.ProfileChannelFollows.Add(new ProfileChannelFollow { ProfileId = a.Id, YoutubeVideoFeedId = feed.Id });
        await db.SaveChangesAsync();
        var viewerA = Viewer.ForProfile(a.Id);
        var viewerB = Viewer.ForProfile(b.Id);

        // A sees its own follows; so does switch-off mode.
        Assert.Contains((await TvQuery.GetAsync(db, viewerA, dates, dates.Today, showId)).Items, e => e.ShowId == showId);
        Assert.Equal(1, (await VideosQuery.GetVideosAsync(db, viewerA, feed.ChannelId, null)).Total);
        Assert.Equal(1, (await VideosQuery.GetVideosAsync(db, Viewer.Household, feed.ChannelId, null)).Total);

        // B: the show ID, channel ID and searches behave exactly like IDs that don't exist.
        var unknownShow = await TvQuery.GetAsync(db, viewerB, dates, dates.Today, int.MaxValue);
        var followedShow = await TvQuery.GetAsync(db, viewerB, dates, dates.Today, showId);
        Assert.Equal(unknownShow.Items, followedShow.Items);
        Assert.Equal(unknownShow.Total, followedShow.Total);
        Assert.Equal(unknownShow.Unannounced, followedShow.Unannounced);
        Assert.DoesNotContain(followedShow.Shows, s => s.Id == showId);
        Assert.Empty((await TvQuery.GetAsync(db, viewerB, dates, dates.Today, null, search: tag)).Shows);
        Assert.DoesNotContain(await TvQuery.GetOnDateAsync(db, viewerB, dates, dates.Today), e => e.ShowId == showId);
        Assert.Equal(0, (await VideosQuery.GetVideosAsync(db, viewerB, feed.ChannelId, null)).Total);
        Assert.Equal(0, (await VideosQuery.GetVideosAsync(db, viewerB, null, tag)).Total);
        Assert.DoesNotContain(await VideosQuery.GetChannelsAsync(db, viewerB), c => c.ChannelId == feed.ChannelId);
        Assert.DoesNotContain(await VideosQuery.GetCategoriesAsync(db, viewerB), c => c.Name == category.CategoryName);
        Assert.Contains(await VideosQuery.GetCategoriesAsync(db, viewerA), c => c.Name == category.CategoryName);
        Assert.DoesNotContain(await VideosQuery.GetLatestPerChannelAsync(db, viewerB), v => v.ChannelId == feed.ChannelId);
        Assert.DoesNotContain(await VideosQuery.GetHomeVideosAsync(db, viewerB, take: 1000), v => v.ChannelId == feed.ChannelId);

        // With profiles on and nobody open, nothing at all.
        Assert.Empty((await TvQuery.GetAsync(db, Viewer.Nobody, dates, dates.Today, null)).Shows);
        Assert.Equal(0, (await VideosQuery.GetVideosAsync(db, Viewer.Nobody, null, null)).Total);
        // B starts with the household channels (rolled out at switch-on), but never with A's show or channel.
        var follows = await FollowQuery.ForProfile(db, b.Id);
        Assert.DoesNotContain(tracked.Id, follows.TvTrackedSeriesIds);
        Assert.DoesNotContain(feed.Id, follows.YoutubeVideoFeedIds);
    }

    [Fact]
    public async Task The_rollout_turns_household_channels_on_once_and_never_touches_requested_ones()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var tag = Tag();
        var a = await NewProfile(db);
        var b = await NewProfile(db);
        var household = new YoutubeVideoFeed { ChannelId = $"UC{tag}H", Url = $"https://{tag}h.test.invalid/feed" };
        db.YoutubeVideoFeeds.Add(household);
        await db.SaveChangesAsync();
        var request = await RequestStore.SubmitAsync(db, a.Id, RequestStore.Youtube, "My channel", null, null);
        var requestedId = $"UC{tag}R".PadRight(24, 'x');
        Assert.Equal(ApproveOutcome.Approved, await RequestStore.ApproveYoutubeAsync(db, request.Id, requestedId, "Requested", null, true, Now));
        var requested = await db.YoutubeVideoFeeds.SingleAsync(f => f.ChannelId == requestedId);

        Assert.True(await HouseholdChannels.RunOnceAsync(db) >= 1);
        Assert.True(await db.ProfileChannelFollows.AnyAsync(f => f.ProfileId == b.Id && f.YoutubeVideoFeedId == household.Id));
        Assert.True(await db.ProfileChannelFollows.AnyAsync(f => f.ProfileId == a.Id && f.YoutubeVideoFeedId == requested.Id));
        Assert.False(await db.ProfileChannelFollows.AnyAsync(f => f.ProfileId == b.Id && f.YoutubeVideoFeedId == requested.Id));
        Assert.False(await db.ChannelRollouts.AnyAsync(r => r.YoutubeVideoFeedId == requested.Id));

        // B turns the household channel off; the next run leaves it off, and the feed stays as it was.
        await db.ProfileChannelFollows.Where(f => f.ProfileId == b.Id && f.YoutubeVideoFeedId == household.Id).ExecuteDeleteAsync();
        Assert.Equal(0, await HouseholdChannels.RunOnceAsync(db));
        Assert.False(await db.ProfileChannelFollows.AnyAsync(f => f.ProfileId == b.Id && f.YoutubeVideoFeedId == household.Id));
        Assert.True(await db.YoutubeVideoFeeds.AnyAsync(f => f.Id == household.Id));

        // Profiles created later start with household channels on, but not with anyone's requested one.
        var c = await NewProfile(db);
        Assert.True(await db.ProfileChannelFollows.AnyAsync(f => f.ProfileId == c.Id && f.YoutubeVideoFeedId == household.Id));
        Assert.False(await db.ProfileChannelFollows.AnyAsync(f => f.ProfileId == c.Id && f.YoutubeVideoFeedId == requested.Id));
    }

    [Fact]
    public async Task Requests_wait_for_the_admin_and_the_requester_sees_only_status_and_reply()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var a = await NewProfile(db);
        var first = await RequestStore.SubmitAsync(db, a.Id, RequestStore.News, "Local paper", "https://example.com", "please");
        for (var i = 1; i < RequestStore.MaxWaitingPerProfile; i++) await RequestStore.SubmitAsync(db, a.Id, RequestStore.Youtube, $"Channel {i}", null, null);
        await Assert.ThrowsAsync<RequestStore.TooManyWaitingException>(() => RequestStore.SubmitAsync(db, a.Id, RequestStore.Youtube, "One more", null, null));
        await Assert.ThrowsAsync<ArgumentException>(() => RequestStore.SubmitAsync(db, a.Id, "podcast", "x", null, null));

        await db.ContentRequests.Where(r => r.Id == first.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.CheckSummary, "1 feed found on example.com · Already a source").SetProperty(r => r.CheckOk, true));
        var adminRow = Assert.Single(await RequestStore.ListAsync(db, id: first.Id));
        Assert.True(adminRow.AlreadyCollected);
        Assert.Equal("Approve", adminRow.NextAction);
        Assert.Equal(a.Name, adminRow.ProfileName);

        Assert.Equal(ApproveOutcome.Approved, await RequestStore.DeclineAsync(db, first.Id, "We already have it under Local.", Now));
        Assert.Equal(ApproveOutcome.NotWaiting, await RequestStore.DeclineAsync(db, first.Id, null, Now));
        var mine = Assert.Single(await RequestStore.MineAsync(db, a.Id), r => r.Id == first.Id);
        Assert.Equal(("declined", "We already have it under Local."), (mine.Status, mine.AdminReply));
        // Another profile never sees it.
        var b = await NewProfile(db);
        Assert.Empty(await RequestStore.MineAsync(db, b.Id));
    }

    [Fact]
    public async Task Approving_a_news_request_adds_one_enabled_source_and_reuses_an_existing_one()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var tag = Tag();
        var a = await NewProfile(db);
        var category = await db.SourceCategories.OrderBy(c => c.Id).Select(c => (int?)c.Id).FirstOrDefaultAsync();
        var url = $"https://{tag}.test.invalid/feed";
        var first = await RequestStore.SubmitAsync(db, a.Id, RequestStore.News, "Local paper", url, null);
        await Assert.ThrowsAsync<ArgumentException>(() => RequestStore.ApproveNewsAsync(db, first.Id, "ftp://example.com/feed", null, null, Now));
        Assert.Equal(ApproveOutcome.Approved, await RequestStore.ApproveNewsAsync(db, first.Id, url, "Local paper", category, Now));
        var source = await db.Sources.SingleAsync(s => s.Url == url);
        Assert.True(source.Enabled);
        Assert.Equal(("Local paper", category), (source.Label, source.SourceCategoryId));
        var row = Assert.Single(await RequestStore.ListAsync(db, id: first.Id));
        Assert.Equal(RequestStore.Added, row.Status);
        Assert.Equal(ApproveOutcome.NotWaiting, await RequestStore.ApproveNewsAsync(db, first.Id, url, null, null, Now));

        // A second request for the same feed points at the same source instead of adding another.
        var second = await RequestStore.SubmitAsync(db, a.Id, RequestStore.News, "Same paper", url, null);
        Assert.Equal(ApproveOutcome.Approved, await RequestStore.ApproveNewsAsync(db, second.Id, url, null, null, Now));
        Assert.Equal(1, await db.Sources.CountAsync(s => s.Url == url));
        Assert.Equal(source.Id, await db.ContentRequests.Where(r => r.Id == second.Id).Select(r => r.SourceId).SingleAsync());
    }

    [Fact]
    public async Task A_profile_can_follow_household_channels_picks_and_its_own_requests_but_never_anyone_elses()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var tag = Tag();
        var a = await NewProfile(db);
        var b = await NewProfile(db);
        var request = await RequestStore.SubmitAsync(db, a.Id, RequestStore.Youtube, "A's channel", null, null);
        var requestedId = $"UC{tag}Q".PadRight(24, 'x');
        Assert.Equal(ApproveOutcome.Approved, await RequestStore.ApproveYoutubeAsync(db, request.Id, requestedId, $"zz {tag} requested", null, false, Now));
        var requested = await db.YoutubeVideoFeeds.SingleAsync(f => f.ChannelId == requestedId);
        var pick = new YoutubeVideoFeed { ChannelId = $"UC{tag}P", ChannelName = $"zz {tag} pick", Url = $"https://{tag}p.test.invalid/feed", IsHouseholdPick = true };
        db.YoutubeVideoFeeds.Add(pick);
        await db.SaveChangesAsync();
        db.ChannelRollouts.Add(new ChannelRollout { YoutubeVideoFeedId = pick.Id });   // keep the rollout from treating the pick as new
        await db.SaveChangesAsync();
        await db.ProfileChannelFollows.Where(f => f.YoutubeVideoFeedId == pick.Id).ExecuteDeleteAsync();

        // B can't follow A's requested channel, and never sees it listed; the answer matches an unknown ID.
        Assert.False(await MyFollows.SetChannelAsync(db, b.Id, requested.Id, follow: true));
        Assert.False(await MyFollows.SetChannelAsync(db, b.Id, int.MaxValue, follow: true));
        var bPage = await MyFollows.GetAsync(db, b.Id);
        Assert.DoesNotContain(bPage.Household.Concat(bPage.Requested).Concat(bPage.Picks), c => c.FeedId == requested.Id);
        // A can follow its own (approved without the follow), and lists it under its other channels.
        Assert.True(await MyFollows.SetChannelAsync(db, a.Id, requested.Id, follow: true));
        Assert.Contains((await MyFollows.GetAsync(db, a.Id)).Requested, c => c.FeedId == requested.Id && c.Following);
        // Household channels switch off and on.
        Assert.Contains(bPage.Household, c => c.FeedId == pick.Id && !c.Following);
        Assert.True(await MyFollows.SetChannelAsync(db, b.Id, pick.Id, follow: true));
        Assert.True(await MyFollows.SetChannelAsync(db, b.Id, pick.Id, follow: true));   // a double tap is fine
        Assert.Contains((await MyFollows.GetAsync(db, b.Id)).Household, c => c.FeedId == pick.Id && c.Following);
        Assert.True(await MyFollows.SetChannelAsync(db, b.Id, pick.Id, follow: false));
        Assert.False(await db.ProfileChannelFollows.AnyAsync(f => f.ProfileId == b.Id && f.YoutubeVideoFeedId == pick.Id));
    }

    [Fact]
    public async Task Unfollowing_a_show_stops_collecting_it_only_when_nobody_follows_it()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var a = await NewProfile(db);
        var b = await NewProfile(db);
        var showId = Random.Shared.Next(1_500_000_000, 2_000_000_000);
        await MyFollows.FollowShowAsync(db, a.Id, showId, "zz show");
        await MyFollows.FollowShowAsync(db, b.Id, showId, "zz show");
        await MyFollows.FollowShowAsync(db, b.Id, showId, "zz show");   // following twice adds one row
        var tracked = await db.TvTrackedSeries.SingleAsync(t => t.TvmazeShowId == showId);
        Assert.True(tracked.Enabled);
        Assert.Equal(2, await db.ProfileTvFollows.CountAsync(f => f.TvTrackedSeriesId == tracked.Id));
        Assert.Contains((await MyFollows.GetAsync(db, a.Id)).Shows, s => s.TvmazeShowId == showId);

        Assert.True(await MyFollows.UnfollowShowAsync(db, a.Id, tracked.Id));
        Assert.True(await db.TvTrackedSeries.Where(t => t.Id == tracked.Id).Select(t => t.Enabled).SingleAsync());
        Assert.False(await MyFollows.UnfollowShowAsync(db, a.Id, tracked.Id));
        Assert.True(await MyFollows.UnfollowShowAsync(db, b.Id, tracked.Id));
        Assert.False(await db.TvTrackedSeries.Where(t => t.Id == tracked.Id).Select(t => t.Enabled).SingleAsync());
        // Following again turns collection back on.
        await MyFollows.FollowShowAsync(db, a.Id, showId, "zz show");
        Assert.True(await db.TvTrackedSeries.Where(t => t.Id == tracked.Id).Select(t => t.Enabled).SingleAsync());
    }

    [Fact]
    public async Task The_alert_counts_new_waiting_requests_and_every_lock_and_mark_seen_clears_only_requests()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        await RequestStore.MarkSeenAsync(db, DateTime.UtcNow.AddMinutes(-1));
        var baseline = await RequestStore.AlertCountAsync(db);
        var a = await NewProfile(db);
        await RequestStore.SubmitAsync(db, a.Id, RequestStore.Youtube, "Channel", null, null);
        var device = await NewDevice(db);
        for (var i = 0; i < ProfileRules.MaxWrongPins; i++) await ProfileStore.OpenAsync(db, Pins, device.Id, a.Id, "0000", Now);
        Assert.Equal(baseline + 2, await RequestStore.AlertCountAsync(db));
        var summary = await RequestStore.SummaryAsync(db);
        Assert.Contains(summary.Locked, l => l.ProfileId == a.Id && l.Device == device.Label);

        await RequestStore.MarkSeenAsync(db, DateTime.UtcNow.AddMinutes(1));
        Assert.Contains(await db.Profiles.Where(p => p.Id == a.Id).Select(p => p.LockedAt).ToListAsync(), l => l != null);
        Assert.True(await RequestStore.AlertCountAsync(db) >= 1);
        await ProfileAdmin.UnlockAsync(db, a.Id);
        Assert.Equal(0, await RequestStore.AlertCountAsync(db) - await db.Profiles.CountAsync(p => p.LockedAt != null));
    }

    [Fact]
    public async Task Deleting_a_profile_removes_its_devices_and_the_channels_only_it_requested()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var tag = Tag();
        var a = await NewProfile(db);
        var b = await NewProfile(db);
        var aDevice = await NewDevice(db);
        var bDevice = await NewDevice(db);
        await ProfileStore.AssignDeviceAsync(db, aDevice.Id, a.Id);
        await ProfileStore.AssignDeviceAsync(db, bDevice.Id, b.Id);

        // A's own channel, with a stored video; and a channel both A and B asked for.
        var own = await RequestStore.SubmitAsync(db, a.Id, RequestStore.Youtube, "A's channel", null, null);
        var ownId = $"UC{tag}O".PadRight(24, 'x');
        Assert.Equal(ApproveOutcome.Approved, await RequestStore.ApproveYoutubeAsync(db, own.Id, ownId, $"zz {tag} own", null, false, Now));
        var ownFeed = await db.YoutubeVideoFeeds.SingleAsync(f => f.ChannelId == ownId);
        db.YouTubeVideos.Add(new YouTubeVideo
        {
            VideoId = $"v{tag}",
            ChannelId = ownId,
            Title = "zz video",
            Url = $"https://{tag}.test.invalid/v",
            PublishedAt = Now,
            FirstSeenAt = Now,
            LastSeenAt = Now,
            YoutubeVideoFeedId = ownFeed.Id,
        });
        await db.SaveChangesAsync();
        var shared = await RequestStore.SubmitAsync(db, b.Id, RequestStore.Youtube, "Shared channel", null, null);
        var sharedId = $"UC{tag}S".PadRight(24, 'x');
        Assert.Equal(ApproveOutcome.Approved, await RequestStore.ApproveYoutubeAsync(db, shared.Id, sharedId, $"zz {tag} shared", null, false, Now));
        var sharedFeed = await db.YoutubeVideoFeeds.SingleAsync(f => f.ChannelId == sharedId);
        var aShared = await RequestStore.SubmitAsync(db, a.Id, RequestStore.Youtube, "Shared channel", null, null);
        await db.ContentRequests.Where(r => r.Id == aShared.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.YoutubeVideoFeedId, sharedFeed.Id).SetProperty(r => r.Status, RequestStore.Added));

        Assert.True(await ProfileAdmin.DeleteAsync(db, a.Id));

        Assert.False(await db.Profiles.AnyAsync(p => p.Id == a.Id));
        Assert.False(await db.ProfileDevices.AnyAsync(d => d.Id == aDevice.Id));
        Assert.False(await db.ContentRequests.AnyAsync(r => r.ProfileId == a.Id));
        Assert.False(await db.YoutubeVideoFeeds.AnyAsync(f => f.Id == ownFeed.Id));
        Assert.False(await db.YouTubeVideos.AnyAsync(v => v.YoutubeVideoFeedId == ownFeed.Id));
        // B, its device and the channel B asked for stay.
        Assert.True(await db.YoutubeVideoFeeds.AnyAsync(f => f.Id == sharedFeed.Id));
        Assert.Equal(b.Id, await db.ProfileDevices.Where(d => d.Id == bDevice.Id).Select(d => d.ProfileId).SingleAsync());
        Assert.True(await db.ContentRequests.AnyAsync(r => r.Id == shared.Id));
        // A second delete finds nothing.
        Assert.False(await ProfileAdmin.DeleteAsync(db, a.Id));
    }
}
