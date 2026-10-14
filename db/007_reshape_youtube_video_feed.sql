-- Reshapes dbo.YoutubeVideoFeed (created in 006) to the requested five columns:
--   ID, ChannelId, ChannelName, Url, DateAdded
--
--   Label          -> renamed ChannelName
--   FeedUrl        -> replaced by a plain, editable Url column (existing values copied across)
--   CreatedAt      -> renamed DateAdded
--   Enabled, LastScrapedAt, LastError -> dropped (not part of the requested design)
--
-- The existing row and every YouTubeVideos link to it are kept. YouTubeVideos.YoutubeVideoFeedId still points at
-- YoutubeVideoFeed.ID, and stays NULLable so the scraper's current video upsert keeps working.
-- Run as a single transaction; GO separates batches.

ALTER TABLE dbo.YoutubeVideoFeed ADD Url nvarchar(2048) NULL;
GO

UPDATE dbo.YoutubeVideoFeed SET Url = FeedUrl;
GO

ALTER TABLE dbo.YoutubeVideoFeed ALTER COLUMN Url nvarchar(2048) NOT NULL;
ALTER TABLE dbo.YoutubeVideoFeed DROP COLUMN FeedUrl;
GO

ALTER TABLE dbo.YoutubeVideoFeed DROP CONSTRAINT DF_YoutubeVideoFeed_Enabled;
ALTER TABLE dbo.YoutubeVideoFeed DROP COLUMN Enabled, LastScrapedAt, LastError;
GO

EXEC sp_rename 'dbo.YoutubeVideoFeed.Label', 'ChannelName', 'COLUMN';
GO

EXEC sp_rename 'dbo.YoutubeVideoFeed.CreatedAt', 'DateAdded', 'COLUMN';
GO

EXEC sp_rename 'dbo.DF_YoutubeVideoFeed_CreatedAt', 'DF_YoutubeVideoFeed_DateAdded', 'OBJECT';
GO
