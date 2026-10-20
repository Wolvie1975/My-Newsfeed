-- App-owned shared bookmarks. Run once before deploying shared Saved support.
-- No scraper schema or article rows are changed. Keep these tables on rollback.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.MyDailyFeedSavedList', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MyDailyFeedSavedList (
        Id int NOT NULL CONSTRAINT PK_MyDailyFeedSavedList PRIMARY KEY,
        Articles nvarchar(max) NOT NULL,
        CONSTRAINT CK_MyDailyFeedSavedList_Id CHECK (Id = 1),
        CONSTRAINT CK_MyDailyFeedSavedList_Json CHECK (ISJSON(Articles) = 1)
    );
END;
IF NOT EXISTS (SELECT 1 FROM dbo.MyDailyFeedSavedList WHERE Id = 1)
    INSERT dbo.MyDailyFeedSavedList(Id, Articles) VALUES (1, N'[]');
IF OBJECT_ID(N'dbo.MyDailyFeedSavedImports', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedSavedImports (
        ClientId uniqueidentifier NOT NULL CONSTRAINT PK_MyDailyFeedSavedImports PRIMARY KEY,
        ImportedAt datetime2 NOT NULL CONSTRAINT DF_MyDailyFeedSavedImports_Date DEFAULT SYSUTCDATETIME()
    );
COMMIT;
