/*
  Read-only verification for 01-app-logins.sql (November 4, 2026, America/Chicago).
  Run as sa (or another sysadmin) against the same database:
      MNP_DATABASE=WebScraper_Test sqlcmd -S … -U sa -C -b -i 02-verify-app-logins.sql
  Each probe runs as the app login inside a transaction that is rolled back. Write probes are DELETE … WHERE 1 = 0, so
  they touch no rows even when allowed. A "deny" must come with a permission error (229, 262), not some other error,
  or the probe reports FAIL.
*/
:on error exit
SET NOCOUNT ON;
-- sqlcmd defaults to QUOTED_IDENTIFIER OFF; tables with filtered indexes or indexed views then fail with 1934.
SET QUOTED_IDENTIFIER ON;
USE [$(MNP_DATABASE)];
GO

DECLARE @probes TABLE (Login sysname, Probe nvarchar(200), Expected nvarchar(10), Sql nvarchar(max));
INSERT @probes VALUES
    (N'mynewspaper_web', N'read Pages', N'allow', N'DECLARE @x int; SELECT TOP (1) @x = 1 FROM dbo.Pages;'),
    (N'mynewspaper_web', N'read a scraper view (SportsScoreboard)', N'allow', N'DECLARE @x int; SELECT TOP (1) @x = 1 FROM dbo.SportsScoreboard;'),
    (N'mynewspaper_web', N'write own table (MyDailyFeedProfileDevices)', N'allow', N'DELETE FROM dbo.MyDailyFeedProfileDevices WHERE 1 = 0;'),
    (N'mynewspaper_web', N'write ResultGames (scores sync)', N'allow', N'DELETE FROM dbo.ResultGames WHERE 1 = 0;'),
    (N'mynewspaper_web', N'write Sources', N'deny', N'DELETE FROM dbo.Sources WHERE 1 = 0;'),
    (N'mynewspaper_web', N'write Pages', N'deny', N'DELETE FROM dbo.Pages WHERE 1 = 0;'),
    (N'mynewspaper_web', N'create a table', N'deny', N'CREATE TABLE dbo.zz_probe (Id int);'),
    (N'mynewspaper_admin', N'read Pages', N'allow', N'DECLARE @x int; SELECT TOP (1) @x = 1 FROM dbo.Pages;'),
    (N'mynewspaper_admin', N'write Sources', N'allow', N'DELETE FROM dbo.Sources WHERE 1 = 0;'),
    (N'mynewspaper_admin', N'write own table (MyDailyFeedProfiles)', N'allow', N'DELETE FROM dbo.MyDailyFeedProfiles WHERE 1 = 0;'),
    (N'mynewspaper_admin', N'write scraper-only table (ScraperRuns)', N'deny', N'DELETE FROM dbo.ScraperRuns WHERE 1 = 0;'),
    (N'mynewspaper_admin', N'create a table', N'deny', N'CREATE TABLE dbo.zz_probe (Id int);');

DECLARE @results TABLE (Login sysname, Probe nvarchar(200), Expected nvarchar(10), Actual nvarchar(10), Error int NULL);
DECLARE @login sysname, @probe nvarchar(200), @expected nvarchar(10), @sql nvarchar(max), @error int;
DECLARE p CURSOR LOCAL FAST_FORWARD FOR SELECT Login, Probe, Expected, Sql FROM @probes;
OPEN p; FETCH NEXT FROM p INTO @login, @probe, @expected, @sql;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @error = NULL;
    BEGIN TRANSACTION;
    BEGIN TRY
        EXECUTE AS LOGIN = @login;
        EXEC sys.sp_executesql @sql;
        REVERT;
    END TRY
    BEGIN CATCH
        SET @error = ERROR_NUMBER();
        -- Roll back first: a failed probe can doom the transaction, and REVERT inside a doomed transaction fails
        -- (3930), leaving the session impersonating the app login for every later probe.
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        REVERT;
    END CATCH;
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    INSERT @results VALUES (@login, @probe, @expected,
        CASE WHEN @error IS NULL THEN N'allow' WHEN @error IN (229, 262) THEN N'deny' ELSE N'error' END, @error);
    FETCH NEXT FROM p INTO @login, @probe, @expected, @sql;
END;
CLOSE p; DEALLOCATE p;
IF ORIGINAL_LOGIN() <> SUSER_SNAME() THROW 50020, N'Still impersonating after the probes; results are not valid.', 1;

SELECT CASE WHEN Expected = Actual THEN 'PASS' ELSE 'FAIL' END AS result, Login, Probe, Expected, Actual, Error FROM @results;
SELECT name, is_disabled, is_policy_checked FROM sys.sql_logins WHERE name IN (N'mynewspaper_web', N'mynewspaper_admin');
-- Expect no rows: neither app login is in any server role.
SELECT r.name AS server_role, m.name AS member FROM sys.server_role_members rm
JOIN sys.server_principals r ON r.principal_id = rm.role_principal_id
JOIN sys.server_principals m ON m.principal_id = rm.member_principal_id
WHERE m.name IN (N'mynewspaper_web', N'mynewspaper_admin');
GO
