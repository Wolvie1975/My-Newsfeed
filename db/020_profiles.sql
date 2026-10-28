-- App-owned profiles, devices, follows and requests. The scraper never reads or writes these tables,
-- except YoutubeVideoFeed.IsHouseholdPick (web-owned column, like SourceCategories.NewsHub).
-- Run after a fresh backup, before deploying a web build that maps IsHouseholdPick. Repeatable.
-- 021 (first profile, Saved list and follows) runs separately, at switch-on.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.TvTrackedSeries', N'U') IS NULL
    THROW 50000, 'TvTrackedSeries is required.', 1;
IF OBJECT_ID(N'dbo.YoutubeVideoFeed', N'U') IS NULL
    THROW 50000, 'YoutubeVideoFeed is required.', 1;

IF OBJECT_ID(N'dbo.MyDailyFeedProfiles', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedProfiles (
        Id               int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MyDailyFeedProfiles PRIMARY KEY,
        Name             nvarchar(40)  NOT NULL,
        Color            char(7)       NOT NULL CONSTRAINT DF_MyDailyFeedProfiles_Color DEFAULT '#0051BA',
        Hidden           bit           NOT NULL CONSTRAINT DF_MyDailyFeedProfiles_Hidden DEFAULT 0,
        SortOrder        int           NOT NULL CONSTRAINT DF_MyDailyFeedProfiles_Sort DEFAULT 0,
        PinHash          nvarchar(200) NULL,          -- PasswordHasher output; NULL = must choose a PIN on next open
        FailedPinCount   int           NOT NULL CONSTRAINT DF_MyDailyFeedProfiles_Failed DEFAULT 0,
        LockedAt         datetime2     NULL,          -- set on the 5th wrong PIN; cleared only by the admin
        LockedByDeviceId uniqueidentifier NULL,
        Settings         nvarchar(max) NOT NULL CONSTRAINT DF_MyDailyFeedProfiles_Settings DEFAULT N'{}',
        BrowserImportAt  datetime2     NULL,          -- set when localStorage settings were imported once
        CreatedAt        datetime2     NOT NULL CONSTRAINT DF_MyDailyFeedProfiles_Created DEFAULT SYSUTCDATETIME(),
        LastReadAt       datetime2     NULL,
        CONSTRAINT UQ_MyDailyFeedProfiles_Name UNIQUE (Name),
        CONSTRAINT CK_MyDailyFeedProfiles_Settings CHECK (ISJSON(Settings) = 1)
    );

IF OBJECT_ID(N'dbo.MyDailyFeedProfileDevices', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedProfileDevices (
        Id          uniqueidentifier NOT NULL CONSTRAINT PK_MyDailyFeedProfileDevices PRIMARY KEY,
        ProfileId   int              NULL,            -- NULL = device known, no profile chosen yet
        TokenHash   varbinary(32)    NOT NULL,        -- SHA-256 of the cookie secret
        Label       nvarchar(100)    NOT NULL,        -- "Living room iPad"; editable by the admin
        UserAgent   nvarchar(300)    NULL,
        PickerMode  nvarchar(16)     NOT NULL CONSTRAINT DF_MyDailyFeedProfileDevices_Mode DEFAULT N'every-visit',
        CreatedAt   datetime2        NOT NULL CONSTRAINT DF_MyDailyFeedProfileDevices_Created DEFAULT SYSUTCDATETIME(),
        LastSeenAt  datetime2        NOT NULL CONSTRAINT DF_MyDailyFeedProfileDevices_Seen DEFAULT SYSUTCDATETIME(),
        RevokedAt   datetime2        NULL,
        CONSTRAINT FK_MyDailyFeedProfileDevices_Profile FOREIGN KEY (ProfileId)
            REFERENCES dbo.MyDailyFeedProfiles(Id) ON DELETE SET NULL,
        CONSTRAINT CK_MyDailyFeedProfileDevices_Mode CHECK (PickerMode IN (N'every-visit', N'daily', N'until-signout'))
    );

IF OBJECT_ID(N'dbo.MyDailyFeedProfileSaved', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedProfileSaved (
        ProfileId int           NOT NULL CONSTRAINT PK_MyDailyFeedProfileSaved PRIMARY KEY,
        Articles  nvarchar(max) NOT NULL CONSTRAINT DF_MyDailyFeedProfileSaved_Articles DEFAULT N'[]',
        CONSTRAINT FK_MyDailyFeedProfileSaved_Profile FOREIGN KEY (ProfileId)
            REFERENCES dbo.MyDailyFeedProfiles(Id) ON DELETE CASCADE,
        CONSTRAINT CK_MyDailyFeedProfileSaved_Json CHECK (ISJSON(Articles) = 1)
    );

IF OBJECT_ID(N'dbo.MyDailyFeedProfileTvFollows', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedProfileTvFollows (
        ProfileId         int       NOT NULL,
        TvTrackedSeriesId int       NOT NULL,
        FollowedAt        datetime2 NOT NULL CONSTRAINT DF_MyDailyFeedProfileTvFollows_At DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_MyDailyFeedProfileTvFollows PRIMARY KEY (ProfileId, TvTrackedSeriesId),
        CONSTRAINT FK_MyDailyFeedProfileTvFollows_Profile FOREIGN KEY (ProfileId)
            REFERENCES dbo.MyDailyFeedProfiles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_MyDailyFeedProfileTvFollows_Series FOREIGN KEY (TvTrackedSeriesId)
            REFERENCES dbo.TvTrackedSeries(ID) ON DELETE CASCADE
    );

IF OBJECT_ID(N'dbo.MyDailyFeedProfileChannelFollows', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedProfileChannelFollows (
        ProfileId          int       NOT NULL,
        YoutubeVideoFeedId int       NOT NULL,
        FollowedAt         datetime2 NOT NULL CONSTRAINT DF_MyDailyFeedProfileChannelFollows_At DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_MyDailyFeedProfileChannelFollows PRIMARY KEY (ProfileId, YoutubeVideoFeedId),
        CONSTRAINT FK_MyDailyFeedProfileChannelFollows_Profile FOREIGN KEY (ProfileId)
            REFERENCES dbo.MyDailyFeedProfiles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_MyDailyFeedProfileChannelFollows_Feed FOREIGN KEY (YoutubeVideoFeedId)
            REFERENCES dbo.YoutubeVideoFeed(ID) ON DELETE CASCADE
    );

-- Household channels already turned on for every profile. A row here means "rolled out"; removing a profile's
-- follow afterwards is that profile turning the channel off, and the rollout never re-adds it.
IF OBJECT_ID(N'dbo.MyDailyFeedChannelRollouts', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedChannelRollouts (
        YoutubeVideoFeedId int       NOT NULL CONSTRAINT PK_MyDailyFeedChannelRollouts PRIMARY KEY,
        RolledOutAt        datetime2 NOT NULL CONSTRAINT DF_MyDailyFeedChannelRollouts_At DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_MyDailyFeedChannelRollouts_Feed FOREIGN KEY (YoutubeVideoFeedId)
            REFERENCES dbo.YoutubeVideoFeed(ID) ON DELETE CASCADE
    );

IF OBJECT_ID(N'dbo.MyDailyFeedContentRequests', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedContentRequests (
        Id                 int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MyDailyFeedContentRequests PRIMARY KEY,
        ProfileId          int            NOT NULL,
        Kind               nvarchar(16)   NOT NULL,      -- youtube | news
        Name               nvarchar(200)  NOT NULL,
        Link               nvarchar(2048) NULL,
        Note               nvarchar(500)  NULL,
        Status             nvarchar(16)   NOT NULL CONSTRAINT DF_MyDailyFeedContentRequests_Status DEFAULT N'waiting',
        CheckOk            bit            NULL,          -- NULL = not checked yet
        CheckSummary       nvarchar(300)  NULL,          -- "Channel ID found from the link · feed responds"
        ResolvedChannelId  nvarchar(40)   NULL,
        ResolvedFeedUrls   nvarchar(max)  NULL,          -- JSON array of discovered RSS/Atom URLs
        AdminReply         nvarchar(500)  NULL,          -- shown to the requester when declined
        YoutubeVideoFeedId int            NULL,
        SourceId           int            NULL,
        CreatedAt          datetime2      NOT NULL CONSTRAINT DF_MyDailyFeedContentRequests_Created DEFAULT SYSUTCDATETIME(),
        DecidedAt          datetime2      NULL,
        CONSTRAINT FK_MyDailyFeedContentRequests_Profile FOREIGN KEY (ProfileId)
            REFERENCES dbo.MyDailyFeedProfiles(Id) ON DELETE CASCADE,
        CONSTRAINT CK_MyDailyFeedContentRequests_Kind CHECK (Kind IN (N'youtube', N'news')),
        CONSTRAINT CK_MyDailyFeedContentRequests_Status CHECK (Status IN (N'waiting', N'added', N'declined'))
    );

IF OBJECT_ID(N'dbo.MyDailyFeedHousehold', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedHousehold (
        Id                int           NOT NULL CONSTRAINT PK_MyDailyFeedHousehold PRIMARY KEY,
        DefaultPickerMode nvarchar(16)  NOT NULL CONSTRAINT DF_MyDailyFeedHousehold_Mode DEFAULT N'every-visit',
        ForgetDevicesDays int           NULL,           -- NULL = never
        RequestsSeenAt    datetime2     NULL,           -- admin's "Mark all seen"
        BaseSettings      nvarchar(max) NOT NULL CONSTRAINT DF_MyDailyFeedHousehold_Base DEFAULT N'{}',  -- base edition
        CONSTRAINT CK_MyDailyFeedHousehold_Id CHECK (Id = 1),
        CONSTRAINT CK_MyDailyFeedHousehold_Base CHECK (ISJSON(BaseSettings) = 1)
    );
IF NOT EXISTS (SELECT 1 FROM dbo.MyDailyFeedHousehold WHERE Id = 1)
    INSERT dbo.MyDailyFeedHousehold(Id) VALUES (1);

IF COL_LENGTH(N'dbo.YoutubeVideoFeed', N'IsHouseholdPick') IS NULL
    ALTER TABLE dbo.YoutubeVideoFeed ADD IsHouseholdPick bit NOT NULL
        CONSTRAINT DF_YoutubeVideoFeed_HouseholdPick DEFAULT 0 WITH VALUES;
COMMIT TRANSACTION;
