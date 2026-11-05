-- Creates (or recreates) WebScraper_Test, the database the .NET integration tests use (tests/myNewspaper.Tests/
-- TestDatabase.cs), as a copy of WebScraper with the household's app-owned state reset to defaults.
-- Run against master on the same SQL Server as WebScraper, in error-on-failure mode, for example:
--   sqlcmd -S <server> -d master -U <login> -C -b -i deployment/create-test-database.sql
-- The copy-only backup doesn't affect the regular backup chain. Afterwards delete the backup file, e.g.
--   docker exec sql2025 rm /var/opt/mssql/data/WebScraper_copy_for_test.bak
-- Repeatable: drops only a database named WebScraper_Test. Never point the tests at WebScraper itself.
-- Then re-run deployment/sql-logins/04-test-login.sql: the copy has WebScraper's users, not mynewspaper_test.
SET NOCOUNT ON;
IF DB_ID(N'WebScraper') IS NULL THROW 50000, 'WebScraper is missing on this server.', 1;

IF DB_ID(N'WebScraper_Test') IS NOT NULL
BEGIN
    ALTER DATABASE WebScraper_Test SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE WebScraper_Test;
END;

DECLARE @dir nvarchar(260) = CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(260));
DECLARE @data nvarchar(260) = CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(260));
DECLARE @bak nvarchar(400) = @dir + CASE WHEN RIGHT(@dir, 1) = N'/' THEN N'' ELSE N'/' END + N'WebScraper_copy_for_test.bak';
DECLARE @mdf nvarchar(400) = @data + CASE WHEN RIGHT(@data, 1) = N'/' THEN N'' ELSE N'/' END + N'WebScraper_Test.mdf';
DECLARE @ldf nvarchar(400) = @data + CASE WHEN RIGHT(@data, 1) = N'/' THEN N'' ELSE N'/' END + N'WebScraper_Test_log.ldf';
DECLARE @rowsFile sysname = (SELECT name FROM WebScraper.sys.database_files WHERE type_desc = N'ROWS');
DECLARE @logFile sysname = (SELECT name FROM WebScraper.sys.database_files WHERE type_desc = N'LOG');

BACKUP DATABASE WebScraper TO DISK = @bak WITH COPY_ONLY, INIT, CHECKSUM;
RESTORE DATABASE WebScraper_Test FROM DISK = @bak WITH MOVE @rowsFile TO @mdf, MOVE @logFile TO @ldf, CHECKSUM;
ALTER DATABASE WebScraper_Test SET RECOVERY SIMPLE;
GO

-- The tests create their own profiles and expect a default household: no profiles or devices (so no PIN hashes or
-- device tokens are copied), an empty base edition and no admin profile.
USE WebScraper_Test;
IF DB_NAME() <> N'WebScraper_Test' THROW 50000, 'Expected to be in WebScraper_Test.', 1;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
UPDATE dbo.MyDailyFeedHousehold SET AdminProfileId = NULL, BaseSettings = N'{}', RequestsSeenAt = NULL;
DELETE FROM dbo.MyDailyFeedProfileDevices;
DELETE FROM dbo.MyDailyFeedProfiles;   -- cascades: saved lists, follows, topics, requests
COMMIT TRANSACTION;
SELECT DB_NAME() AS [Database], (SELECT COUNT(*) FROM dbo.MyDailyFeedProfiles) AS Profiles,
       (SELECT BaseSettings FROM dbo.MyDailyFeedHousehold) AS BaseSettings;
