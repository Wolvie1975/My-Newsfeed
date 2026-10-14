-- Lookup table of YouTube channel feeds, and a link from each video to its feed.
--
--   dbo.YoutubeVideoFeed   one row per YouTube channel feed (the same channels passed to the scraper's
--                          --youtube-feed option), with Enabled / LastScrapedAt / LastError bookkeeping like dbo.Sources.
--   dbo.YouTubeVideos      gains YoutubeVideoFeedId -> YoutubeVideoFeed.ID.
--
-- The new column is NULLable on purpose: the scraper's video upsert does not set it yet, so a NOT NULL column would
-- make every insert fail. Existing videos are linked by channel. ChannelId and ChannelName stay on YouTubeVideos
-- because the scraper still writes them. ON DELETE NO ACTION: a feed that still has videos cannot be deleted.
-- Run as a single transaction; GO separates batches.

CREATE TABLE dbo.YoutubeVideoFeed (
    ID            int IDENTITY(1,1) NOT NULL CONSTRAINT PK_YoutubeVideoFeed PRIMARY KEY,
    ChannelId     nvarchar(40)   NOT NULL,
    Label         nvarchar(200)  NULL,
    -- Derived, so it can never disagree with ChannelId: the Atom feed the scraper reads.
    FeedUrl       AS (CONVERT(nvarchar(120), N'https://www.youtube.com/feeds/videos.xml?channel_id=' + ChannelId)) PERSISTED,
    Enabled       bit            NOT NULL CONSTRAINT DF_YoutubeVideoFeed_Enabled DEFAULT (1),
    CreatedAt     datetime2      NOT NULL CONSTRAINT DF_YoutubeVideoFeed_CreatedAt DEFAULT (sysutcdatetime()),
    LastScrapedAt datetime2      NULL,
    LastError     nvarchar(1000) NULL,
    CONSTRAINT UQ_YoutubeVideoFeed_ChannelId UNIQUE (ChannelId)
);
GO

-- One feed per channel already present in the videos table.
INSERT INTO dbo.YoutubeVideoFeed (ChannelId, Label)
SELECT v.ChannelId, MAX(v.ChannelName)
FROM dbo.YouTubeVideos v
GROUP BY v.ChannelId;
GO

ALTER TABLE dbo.YouTubeVideos ADD YoutubeVideoFeedId int NULL;
GO

UPDATE v SET v.YoutubeVideoFeedId = f.ID
FROM dbo.YouTubeVideos v JOIN dbo.YoutubeVideoFeed f ON f.ChannelId = v.ChannelId;
GO

ALTER TABLE dbo.YouTubeVideos ADD CONSTRAINT FK_YouTubeVideos_YoutubeVideoFeed
    FOREIGN KEY (YoutubeVideoFeedId) REFERENCES dbo.YoutubeVideoFeed (ID) ON DELETE NO ACTION;
CREATE INDEX IX_YouTubeVideos_YoutubeVideoFeedId ON dbo.YouTubeVideos (YoutubeVideoFeedId);
GO
