-- The News section (docs/news-section-handoff.md, phases 4 and 5). Additive and repeatable; run after 020.
--
-- Part 1: Profiles.PreviousVisitAt, the start of the profile's previous visit. LastReadAt moves every minute while
-- someone reads, so it can't say what's new since they were last here; PreviousVisitAt only moves when a new visit
-- starts (a return after 30 minutes away). NULL until the profile's second visit.
--
-- Part 2: MyDailyFeedProfileTopics, the words or phrases a profile follows ("Your topics"). Private to the profile,
-- like its TV and channel follows: the admin sees counts only. Deleting the profile deletes its topics.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.MyDailyFeedProfiles', N'U') IS NULL
    THROW 50000, 'Run 020_profiles.sql first.', 1;

-- Part 1 of 2
IF COL_LENGTH(N'dbo.MyDailyFeedProfiles', N'PreviousVisitAt') IS NULL
    ALTER TABLE dbo.MyDailyFeedProfiles ADD PreviousVisitAt datetime2 NULL;

-- Part 2 of 2
IF OBJECT_ID(N'dbo.MyDailyFeedProfileTopics', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedProfileTopics (
        Id            int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MyDailyFeedProfileTopics PRIMARY KEY,
        ProfileId     int            NOT NULL,
        Terms         nvarchar(300)  NOT NULL,   -- "Chiefs stadium, Arrowhead": comma-separated alternatives
        ExceptTerms   nvarchar(200)  NULL,       -- "Chiefs": comma-separated
        Hub           nvarchar(16)   NULL,       -- NULL = all sections; else headlines | sports | entertainment
        CreatedAt     datetime2      NOT NULL CONSTRAINT DF_MyDailyFeedProfileTopics_Created DEFAULT SYSUTCDATETIME(),
        LastViewedAt  datetime2      NULL,
        CONSTRAINT FK_MyDailyFeedProfileTopics_Profile FOREIGN KEY (ProfileId)
            REFERENCES dbo.MyDailyFeedProfiles(Id) ON DELETE CASCADE,
        CONSTRAINT CK_MyDailyFeedProfileTopics_Hub CHECK (Hub IS NULL OR Hub IN (N'headlines', N'sports', N'entertainment'))
    );
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MyDailyFeedProfileTopics_Profile'
               AND object_id = OBJECT_ID(N'dbo.MyDailyFeedProfileTopics'))
    CREATE INDEX IX_MyDailyFeedProfileTopics_Profile ON dbo.MyDailyFeedProfileTopics (ProfileId);
COMMIT TRANSACTION;
