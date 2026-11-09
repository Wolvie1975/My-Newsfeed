-- Switch-on (profiles handoff stage U1): gives the household's first profile today's shared Saved list and makes it
-- follow every enabled TV series and every YouTube channel, so nothing disappears when profiles turn on. Every channel
-- that exists now counts as a household channel that has been rolled out (decision 11).
-- Create your own profile, with its PIN, in Admin · Profiles first. This script never creates one: it stops unless
-- exactly one profile exists, so the Saved list and follows can't land on the wrong person.
-- Run after a fresh backup and before setting Profiles:Enabled=true. Repeatable.
-- Keep dbo.MyDailyFeedSavedList until the release is proven, so rolling the web container back still finds it.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.MyDailyFeedProfiles', N'U') IS NULL
    THROW 50000, 'Run 020_profiles.sql first.', 1;
IF (SELECT COUNT(*) FROM dbo.MyDailyFeedProfiles) <> 1
    THROW 50000, 'Expected exactly one profile. Create yours in Admin · Profiles (and remove any others) first.', 1;

DECLARE @first int = (SELECT Id FROM dbo.MyDailyFeedProfiles);

IF NOT EXISTS (SELECT 1 FROM dbo.MyDailyFeedProfileSaved WHERE ProfileId = @first)
    INSERT dbo.MyDailyFeedProfileSaved (ProfileId, Articles)
    SELECT @first, ISNULL((SELECT Articles FROM dbo.MyDailyFeedSavedList WHERE Id = 1), N'[]');

INSERT dbo.MyDailyFeedProfileTvFollows (ProfileId, TvTrackedSeriesId)
SELECT @first, t.ID FROM dbo.TvTrackedSeries t
WHERE t.Enabled = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.MyDailyFeedProfileTvFollows f WHERE f.ProfileId = @first AND f.TvTrackedSeriesId = t.ID);

INSERT dbo.MyDailyFeedProfileChannelFollows (ProfileId, YoutubeVideoFeedId)
SELECT @first, y.ID FROM dbo.YoutubeVideoFeed y
WHERE NOT EXISTS (SELECT 1 FROM dbo.MyDailyFeedProfileChannelFollows f WHERE f.ProfileId = @first AND f.YoutubeVideoFeedId = y.ID);

INSERT dbo.MyDailyFeedChannelRollouts (YoutubeVideoFeedId)
SELECT y.ID FROM dbo.YoutubeVideoFeed y
WHERE NOT EXISTS (SELECT 1 FROM dbo.MyDailyFeedChannelRollouts r WHERE r.YoutubeVideoFeedId = y.ID);

COMMIT TRANSACTION;
