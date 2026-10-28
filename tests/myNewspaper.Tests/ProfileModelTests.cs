using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Tests;

// Model-only checks for the db/020 mapping; building the model never opens a connection.
public class ProfileModelTests
{
    private static readonly IModel Model = new WebScraperContext(
        new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer("Server=model.invalid").Options).Model;

    [Theory]
    [InlineData(typeof(Profile), "MyDailyFeedProfiles")]
    [InlineData(typeof(ProfileDevice), "MyDailyFeedProfileDevices")]
    [InlineData(typeof(ProfileTvFollow), "MyDailyFeedProfileTvFollows")]
    [InlineData(typeof(ProfileChannelFollow), "MyDailyFeedProfileChannelFollows")]
    [InlineData(typeof(ChannelRollout), "MyDailyFeedChannelRollouts")]
    [InlineData(typeof(ContentRequest), "MyDailyFeedContentRequests")]
    [InlineData(typeof(Household), "MyDailyFeedHousehold")]
    public void Profile_tables_use_the_app_owned_prefix(Type entity, string table) =>
        Assert.Equal(table, Model.FindEntityType(entity)!.GetTableName());

    [Theory]
    [InlineData(typeof(ChannelRollout), nameof(ChannelRollout.YoutubeVideoFeedId))]
    [InlineData(typeof(Household), nameof(Household.Id))]
    public void Keys_the_app_supplies_are_not_identity_columns(Type entity, string key) =>
        Assert.Equal(ValueGenerated.Never, Model.FindEntityType(entity)!.FindProperty(key)!.ValueGenerated);

    [Theory]
    [InlineData(typeof(Profile), nameof(Profile.CreatedAt))]
    [InlineData(typeof(ProfileDevice), nameof(ProfileDevice.CreatedAt))]
    [InlineData(typeof(ProfileDevice), nameof(ProfileDevice.LastSeenAt))]
    [InlineData(typeof(ProfileTvFollow), nameof(ProfileTvFollow.FollowedAt))]
    [InlineData(typeof(ProfileChannelFollow), nameof(ProfileChannelFollow.FollowedAt))]
    [InlineData(typeof(ChannelRollout), nameof(ChannelRollout.RolledOutAt))]
    [InlineData(typeof(ContentRequest), nameof(ContentRequest.CreatedAt))]
    public void Timestamps_come_from_the_database(Type entity, string property) =>
        Assert.Equal("sysutcdatetime()", Model.FindEntityType(entity)!.FindProperty(property)!.GetDefaultValueSql());

    [Fact]
    public void Follow_keys_are_profile_then_item()
    {
        Assert.Equal([nameof(ProfileTvFollow.ProfileId), nameof(ProfileTvFollow.TvTrackedSeriesId)],
            Model.FindEntityType(typeof(ProfileTvFollow))!.FindPrimaryKey()!.Properties.Select(p => p.Name));
        Assert.Equal([nameof(ProfileChannelFollow.ProfileId), nameof(ProfileChannelFollow.YoutubeVideoFeedId)],
            Model.FindEntityType(typeof(ProfileChannelFollow))!.FindPrimaryKey()!.Properties.Select(p => p.Name));
    }

    [Fact]
    public void Removing_a_profile_leaves_its_devices_without_one() =>
        Assert.Equal(DeleteBehavior.SetNull, Model.FindEntityType(typeof(ProfileDevice))!.GetForeignKeys().Single().DeleteBehavior);

    [Fact]
    public void Profile_color_is_char_7() =>
        Assert.Equal("char(7)", Model.FindEntityType(typeof(Profile))!.FindProperty(nameof(Profile.Color))!.GetColumnType());

    [Fact]
    public void Youtube_feeds_map_the_household_pick_column() =>
        Assert.NotNull(Model.FindEntityType(typeof(YoutubeVideoFeed))!.FindProperty(nameof(YoutubeVideoFeed.IsHouseholdPick)));
}
