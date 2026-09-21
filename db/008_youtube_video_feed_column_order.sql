-- Rebuilds dbo.YoutubeVideoFeed so its columns are in the requested order:
--   ID, ChannelId, ChannelName, Url, DateAdded
-- (SQL Server cannot reorder columns in place, and 007 left Url after DateAdded.)
-- Rows keep their IDs, so every YouTubeVideos.YoutubeVideoFeedId link stays valid.
-- Run as a single transaction; GO separates batches.

ALTER TABLE dbo.YouTubeVideos DROP CONSTRAINT FK_YouTubeVideos_YoutubeVideoFeed;
GO

EXEC sp_rename 'dbo.PK_YoutubeVideoFeed', 'PK_YoutubeVideoFeed_old', 'OBJECT';
EXEC sp_rename 'dbo.UQ_YoutubeVideoFeed_ChannelId', 'UQ_YoutubeVideoFeed_ChannelId_old', 'OBJECT';
EXEC sp_rename 'dbo.DF_YoutubeVideoFeed_DateAdded', 'DF_YoutubeVideoFeed_DateAdded_old', 'OBJECT';
EXEC sp_rename 'dbo.YoutubeVideoFeed', 'YoutubeVideoFeed_old';
GO

CREATE TABLE dbo.YoutubeVideoFeed (
    ID          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_YoutubeVideoFeed PRIMARY KEY,
    ChannelId   nvarchar(40)   NOT NULL,
    ChannelName nvarchar(200)  NULL,
    Url         nvarchar(2048) NOT NULL,
    DateAdded   datetime2      NOT NULL CONSTRAINT DF_YoutubeVideoFeed_DateAdded DEFAULT (sysutcdatetime()),
    CONSTRAINT UQ_YoutubeVideoFeed_ChannelId UNIQUE (ChannelId)
);
GO

SET IDENTITY_INSERT dbo.YoutubeVideoFeed ON;
INSERT INTO dbo.YoutubeVideoFeed (ID, ChannelId, ChannelName, Url, DateAdded)
SELECT ID, ChannelId, ChannelName, Url, DateAdded FROM dbo.YoutubeVideoFeed_old;
SET IDENTITY_INSERT dbo.YoutubeVideoFeed OFF;
GO

ALTER TABLE dbo.YouTubeVideos ADD CONSTRAINT FK_YouTubeVideos_YoutubeVideoFeed
    FOREIGN KEY (YoutubeVideoFeedId) REFERENCES dbo.YoutubeVideoFeed (ID) ON DELETE NO ACTION;
GO

DROP TABLE dbo.YoutubeVideoFeed_old;
GO
