-- Game results from ESPN's public scoreboard data, synced by the web app itself rather than the scraper.
--
--   dbo.ResultFollows   one row per league or team the site follows, named by ESPN's sport and league slugs
--                       ("football" / "college-football") and, for a team, ESPN's team id. The web app's background
--                       sync reads each enabled row every few hours, with LastSyncedAt / LastError bookkeeping like
--                       dbo.YoutubeVideoFeed.
--   dbo.ResultGames     one row per game, keyed by ESPN's event id within its league, upserted by the sync. The public
--                       results page shows the finished ones.
--
-- Notes
--   * ESPN ids are strings in its data, so they are stored as strings.
--   * A league follow has no TeamId. UQ_ResultFollows_Team treats NULLs as equal, so a league can be followed only once.
--   * Games are not linked to follows: one game can belong to a followed team and a followed league at once, and the
--     results page matches games to follows by league and team id instead. Removing a follow hides its results
--     without deleting them; a game another follow still covers keeps showing.
--   * Scores are whole numbers in every sport ESPN covers here (a volleyball "score" is sets won). WinnerSide comes
--     from ESPN, so a soccer shoot-out or a forfeit is decided the way ESPN reports it.
-- Run as a single transaction; GO separates batches.

CREATE TABLE dbo.ResultFollows (
    ID           int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResultFollows PRIMARY KEY,
    Kind         nvarchar(10)   NOT NULL,
    Sport        nvarchar(40)   NOT NULL,
    League       nvarchar(60)   NOT NULL,
    TeamId       nvarchar(20)   NULL,
    Name         nvarchar(200)  NOT NULL,
    LogoUrl      nvarchar(2048) NULL,
    Enabled      bit            NOT NULL CONSTRAINT DF_ResultFollows_Enabled DEFAULT (1),
    CreatedAt    datetime2      NOT NULL CONSTRAINT DF_ResultFollows_CreatedAt DEFAULT (sysutcdatetime()),
    LastSyncedAt datetime2      NULL,
    LastError    nvarchar(1000) NULL,
    CONSTRAINT CK_ResultFollows_Kind CHECK ((Kind = N'team' AND TeamId IS NOT NULL) OR (Kind = N'league' AND TeamId IS NULL)),
    CONSTRAINT UQ_ResultFollows_Team UNIQUE (Sport, League, TeamId)
);
GO

CREATE TABLE dbo.ResultGames (
    ID            int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResultGames PRIMARY KEY,
    Sport         nvarchar(40)   NOT NULL,
    League        nvarchar(60)   NOT NULL,
    EventId       nvarchar(20)   NOT NULL,
    StartsAtUtc   datetime2      NOT NULL,
    State         nvarchar(10)   NOT NULL,   -- ESPN status state: pre, in, post
    Completed     bit            NOT NULL,
    StatusText    nvarchar(60)   NULL,       -- ESPN short detail: "Final", "Final/OT", "FT", "Postponed"
    HomeTeamId    nvarchar(20)   NOT NULL,
    HomeName      nvarchar(200)  NOT NULL,
    HomeLogoUrl   nvarchar(2048) NULL,
    HomeScore     int            NULL,
    HomeShootout  int            NULL,
    AwayTeamId    nvarchar(20)   NOT NULL,
    AwayName      nvarchar(200)  NOT NULL,
    AwayLogoUrl   nvarchar(2048) NULL,
    AwayScore     int            NULL,
    AwayShootout  int            NULL,
    WinnerSide    nvarchar(4)    NULL,       -- home, away, or NULL for a draw or an unfinished game
    Venue         nvarchar(300)  NULL,       -- "City, State / Venue name", the events page's location shape
    FirstSeenAt   datetime2      NOT NULL CONSTRAINT DF_ResultGames_FirstSeenAt DEFAULT (sysutcdatetime()),
    LastSeenAt    datetime2      NOT NULL CONSTRAINT DF_ResultGames_LastSeenAt DEFAULT (sysutcdatetime()),
    CONSTRAINT UQ_ResultGames_Event UNIQUE (Sport, League, EventId)
);
GO

CREATE INDEX IX_ResultGames_StartsAtUtc ON dbo.ResultGames (StartsAtUtc);
CREATE INDEX IX_ResultGames_HomeTeam ON dbo.ResultGames (Sport, League, HomeTeamId);
CREATE INDEX IX_ResultGames_AwayTeam ON dbo.ResultGames (Sport, League, AwayTeamId);
GO
