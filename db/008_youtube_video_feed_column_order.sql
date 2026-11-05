-- Rebuilds dbo.YoutubeVideoFeed so its columns are in the requested order:
--   ID, ChannelId, ChannelName, Url, DateAdded
-- (SQL Server cannot reorder columns in place, and 007 left Url after DateAdded.)
-- Rows keep their IDs, so every YouTubeVideos.YoutubeVideoFeedId link stays valid.
-- Run after 007. Repeatable: it only rebuilds 007's layout (Url after DateAdded), so a re-run does nothing, and it
-- never rebuilds the table once later migrations have added columns (020's IsHouseholdPick). Back up first.
-- One transaction with XACT_ABORT: a failure leaves the table exactly as it was. (Before November 5, 2026 this script
-- said so but ran as separate GO batches, so a failure part-way left the table renamed.)
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.YoutubeVideoFeed', N'U') IS NULL OR COL_LENGTH(N'dbo.YoutubeVideoFeed', N'Url') IS NULL
    OR COL_LENGTH(N'dbo.YoutubeVideoFeed', N'DateAdded') IS NULL
    THROW 50000, 'Run 006_add_youtube_video_feed.sql and 007_reshape_youtube_video_feed.sql first.', 1;

IF COLUMNPROPERTY(OBJECT_ID(N'dbo.YoutubeVideoFeed'), N'Url', 'ColumnId')
       > COLUMNPROPERTY(OBJECT_ID(N'dbo.YoutubeVideoFeed'), N'DateAdded', 'ColumnId')
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.YoutubeVideoFeed')
                   AND name NOT IN (N'ID', N'ChannelId', N'ChannelName', N'Url', N'DateAdded'))
BEGIN
    ALTER TABLE dbo.YouTubeVideos DROP CONSTRAINT FK_YouTubeVideos_YoutubeVideoFeed;

    EXEC sp_rename N'dbo.PK_YoutubeVideoFeed', N'PK_YoutubeVideoFeed_old', N'OBJECT';
    EXEC sp_rename N'dbo.UQ_YoutubeVideoFeed_ChannelId', N'UQ_YoutubeVideoFeed_ChannelId_old', N'OBJECT';
    EXEC sp_rename N'dbo.DF_YoutubeVideoFeed_DateAdded', N'DF_YoutubeVideoFeed_DateAdded_old', N'OBJECT';
    EXEC sp_rename N'dbo.YoutubeVideoFeed', N'YoutubeVideoFeed_old';

    -- EXEC: these compile after the renames above.
    EXEC (N'CREATE TABLE dbo.YoutubeVideoFeed (
        ID          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_YoutubeVideoFeed PRIMARY KEY,
        ChannelId   nvarchar(40)   NOT NULL,
        ChannelName nvarchar(200)  NULL,
        Url         nvarchar(2048) NOT NULL,
        DateAdded   datetime2      NOT NULL CONSTRAINT DF_YoutubeVideoFeed_DateAdded DEFAULT (sysutcdatetime()),
        CONSTRAINT UQ_YoutubeVideoFeed_ChannelId UNIQUE (ChannelId)
    );');

    EXEC (N'SET IDENTITY_INSERT dbo.YoutubeVideoFeed ON;
        INSERT INTO dbo.YoutubeVideoFeed (ID, ChannelId, ChannelName, Url, DateAdded)
        SELECT ID, ChannelId, ChannelName, Url, DateAdded FROM dbo.YoutubeVideoFeed_old;
        SET IDENTITY_INSERT dbo.YoutubeVideoFeed OFF;');

    EXEC (N'ALTER TABLE dbo.YouTubeVideos ADD CONSTRAINT FK_YouTubeVideos_YoutubeVideoFeed
        FOREIGN KEY (YoutubeVideoFeedId) REFERENCES dbo.YoutubeVideoFeed (ID) ON DELETE NO ACTION;');

    DROP TABLE dbo.YoutubeVideoFeed_old;
END;
COMMIT TRANSACTION;
