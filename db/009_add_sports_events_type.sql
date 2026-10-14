-- Lookup table of sports-event feeds ("events types"), and a link from each event to the feed that supplied it.
--
--   dbo.SportsEventsType   ID, RssUrl, EventsTypeName, DateAdded
--                          One row per events calendar feed the scraper reads with --events-feed.
--   dbo.SportsEvents       gains SportsEventsTypeId -> SportsEventsType.ID.
--
-- Why this fits: SportsEvents had no column recording which feed a row came from, and the scraper accepts several
-- feeds. The feed in use today (sport_id=0) returns EVERY sport of a school, so a type here identifies a feed, not a
-- sport; the sport of each game stays in SportsEvents.Sport.
--
-- Notes
--   * RssUrl is nvarchar(450): the longest a UNIQUE index key can be (900 bytes of the 1,700-byte limit), so the
--     same feed cannot be added twice. Feed addresses are far shorter than that.
--   * The link column is NULLable on purpose: the scraper's event upsert does not set it yet, so a NOT NULL column
--     would make every insert fail. Existing events are linked below. ON DELETE NO ACTION: a type that still has
--     events cannot be deleted.
-- Run as a single transaction; GO separates batches.

CREATE TABLE dbo.SportsEventsType (
    ID             int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SportsEventsType PRIMARY KEY,
    RssUrl         nvarchar(450)  NOT NULL,
    EventsTypeName nvarchar(200)  NOT NULL,
    DateAdded      datetime2      NOT NULL CONSTRAINT DF_SportsEventsType_DateAdded DEFAULT (sysutcdatetime()),
    CONSTRAINT UQ_SportsEventsType_RssUrl UNIQUE (RssUrl)
);
GO

ALTER TABLE dbo.SportsEvents ADD SportsEventsTypeId int NULL;
GO

-- The scraper log shows this one feed saved all 38 existing events (school_id=3 is Kansas, sport_id=0 is all sports).
INSERT INTO dbo.SportsEventsType (RssUrl, EventsTypeName)
VALUES (N'https://big12sports.com/services/responsive-calendar-subscription.ashx/calendar.rss?sport_id=0&school_id=3&schedule_id=0',
        N'Kansas – all sports');
GO

UPDATE e SET e.SportsEventsTypeId = t.ID
FROM dbo.SportsEvents e
CROSS JOIN dbo.SportsEventsType t
WHERE t.RssUrl LIKE N'https://big12sports.com/%'
  AND e.Url LIKE N'http%://big12sports.com/%';
GO

ALTER TABLE dbo.SportsEvents ADD CONSTRAINT FK_SportsEvents_SportsEventsType
    FOREIGN KEY (SportsEventsTypeId) REFERENCES dbo.SportsEventsType (ID) ON DELETE NO ACTION;
CREATE INDEX IX_SportsEvents_SportsEventsTypeId ON dbo.SportsEvents (SportsEventsTypeId);
GO
