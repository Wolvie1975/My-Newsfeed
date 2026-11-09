-- Web-owned category routing. Existing category names/IDs/source assignments stay intact.
-- Run after confirming a recent target-database backup. Repeatable: preserves later hub edits.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.SourceCategories', N'U') IS NULL
    THROW 50000, 'SourceCategories is required.', 1;
IF COL_LENGTH(N'dbo.SourceCategories', N'NewsHub') IS NULL
BEGIN
    ALTER TABLE dbo.SourceCategories ADD NewsHub nvarchar(16) NOT NULL
        CONSTRAINT DF_SourceCategories_NewsHub DEFAULT N'headlines' WITH VALUES;
    EXEC(N'UPDATE dbo.SourceCategories SET NewsHub = CASE
        WHEN Category_Name IN (N''Sports'',N''Men''''s College Basketball News'',N''Big 12 News'',N''F1 News'') THEN N''sports''
        WHEN Category_Name IN (N''Entertainment'',N''TV/Film'') THEN N''entertainment''
        ELSE N''headlines'' END');
    EXEC(N'ALTER TABLE dbo.SourceCategories ADD CONSTRAINT CK_SourceCategories_NewsHub
        CHECK (NewsHub IN (N''headlines'',N''sports'',N''entertainment''))');
END;
COMMIT TRANSACTION;
