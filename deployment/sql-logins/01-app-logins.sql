/*
  Least-privilege SQL logins for the myNewspaper web and admin apps. In use since November 5, 2026 (America/Chicago)
  on WebScraper and WebScraper_Test. When a migration adds a table an app writes, add it to @writes below and re-run.

  What it does (repeatable; every change is guarded):
    1. Creates logins mynewspaper_web and mynewspaper_admin if absent (CHECK_POLICY ON). An existing login keeps its
       password: rotate it with ALTER LOGIN, not by re-running this.
    2. In the target database, creates roles MyNewspaperWebApp and MyNewspaperAdminApp, grants each SELECT on schema
       dbo (both apps read across the scraper's tables and views), and INSERT/UPDATE/DELETE only on the tables that
       app writes. No DDL, no EXECUTE, no db_owner, no server roles.
    3. Creates a database user for each login and adds it to its role.

  Run with sqlcmd in error-exit mode, passwords supplied through the environment (sqlcmd reads scripting variables
  from environment variables), never on the command line or in a file:
      MNP_DATABASE=WebScraper_Test MNP_WEB_SQL_PASSWORD=… MNP_ADMIN_SQL_PASSWORD=… \
      sqlcmd -S … -U sa -C -b -i 01-app-logins.sql
  Run it against WebScraper_Test first, then 02-verify-app-logins.sql, then WebScraper (DEPLOY.md, SQL logins).

  The write lists come from reading the code (EF Add/Remove/ExecuteUpdate/ExecuteDelete, tracked SaveChanges and raw
  SQL); phase 1 of the plan confirms them by running both apps and the test suites under these logins. A missing
  table there shows up as SQL error 229 ("The INSERT permission was denied…") in the app log.
*/
:on error exit
SET NOCOUNT ON;
-- sqlcmd defaults to QUOTED_IDENTIFIER OFF; tables with filtered indexes or indexed views then fail with 1934.
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

IF DB_ID(N'$(MNP_DATABASE)') IS NULL
    THROW 50001, N'Target database $(MNP_DATABASE) does not exist.', 1;
IF LEN(N'$(MNP_WEB_SQL_PASSWORD)') < 24 OR LEN(N'$(MNP_ADMIN_SQL_PASSWORD)') < 24
    THROW 50002, N'App passwords must be at least 24 characters.', 1;
GO

USE [master];
GO
IF SUSER_ID(N'mynewspaper_web') IS NULL
    CREATE LOGIN [mynewspaper_web] WITH PASSWORD = N'$(MNP_WEB_SQL_PASSWORD)', DEFAULT_DATABASE = [$(MNP_DATABASE)],
        CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;
ELSE
    PRINT N'Login mynewspaper_web exists; password unchanged.';
IF SUSER_ID(N'mynewspaper_admin') IS NULL
    CREATE LOGIN [mynewspaper_admin] WITH PASSWORD = N'$(MNP_ADMIN_SQL_PASSWORD)', DEFAULT_DATABASE = [$(MNP_DATABASE)],
        CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;
ELSE
    PRINT N'Login mynewspaper_admin exists; password unchanged.';
GO

USE [$(MNP_DATABASE)];
GO
BEGIN TRANSACTION;

IF DATABASE_PRINCIPAL_ID(N'MyNewspaperWebApp') IS NULL CREATE ROLE [MyNewspaperWebApp] AUTHORIZATION [dbo];
IF DATABASE_PRINCIPAL_ID(N'MyNewspaperAdminApp') IS NULL CREATE ROLE [MyNewspaperAdminApp] AUTHORIZATION [dbo];

GRANT SELECT ON SCHEMA::[dbo] TO [MyNewspaperWebApp];
GRANT SELECT ON SCHEMA::[dbo] TO [MyNewspaperAdminApp];

DECLARE @writes TABLE (RoleName sysname, TableName sysname, Why nvarchar(200));
INSERT @writes (RoleName, TableName, Why) VALUES
    -- Public site: profiles, devices, follows, topics, Saved, requests (profile gate, /me endpoints, picker)
    (N'MyNewspaperWebApp', N'MyDailyFeedProfiles', N'create edition, PIN, settings, lockout, PreviousVisitAt'),
    (N'MyNewspaperWebApp', N'MyDailyFeedProfileDevices', N'profile gate creates devices; LastSeenAt; sign-out'),
    (N'MyNewspaperWebApp', N'MyDailyFeedHousehold', N'picker mode and household settings'),
    (N'MyNewspaperWebApp', N'MyDailyFeedProfileChannelFollows', N'Settings → channels'),
    (N'MyNewspaperWebApp', N'MyDailyFeedProfileTvFollows', N'follow / unfollow a show'),
    (N'MyNewspaperWebApp', N'MyDailyFeedProfileTopics', N'topics follow, read, delete'),
    (N'MyNewspaperWebApp', N'MyDailyFeedProfileSaved', N'per-profile Saved'),
    (N'MyNewspaperWebApp', N'MyDailyFeedSavedList', N'shared Saved list'),
    (N'MyNewspaperWebApp', N'MyDailyFeedSavedImports', N'browser Saved import receipts'),
    (N'MyNewspaperWebApp', N'MyDailyFeedContentRequests', N'submit requests; RequestCheckerService results'),
    (N'MyNewspaperWebApp', N'MyDailyFeedChannelRollouts', N'ChannelRolloutService'),
    (N'MyNewspaperWebApp', N'TvTrackedSeries', N'SeriesFollowForm (profiles off) and following a show'),
    -- Public site background work: ScoresSyncService
    (N'MyNewspaperWebApp', N'ResultFollows', N'sync status columns'),
    (N'MyNewspaperWebApp', N'ResultFollowTeams', N'conference membership refresh'),
    (N'MyNewspaperWebApp', N'ResultGames', N'synced games'),

    -- Admin: everything the household admin edits, plus the same app-owned tables
    (N'MyNewspaperAdminApp', N'MyDailyFeedProfiles', N'Profiles: add, edit, hide, delete, unlock'),
    (N'MyNewspaperAdminApp', N'MyDailyFeedProfileDevices', N'profile delete removes devices'),
    (N'MyNewspaperAdminApp', N'MyDailyFeedHousehold', N'household settings, base edition, admin profile'),
    (N'MyNewspaperAdminApp', N'MyDailyFeedProfileChannelFollows', N'approve request follows; profile delete'),
    (N'MyNewspaperAdminApp', N'MyDailyFeedProfileTvFollows', N'profile delete'),
    (N'MyNewspaperAdminApp', N'MyDailyFeedProfileTopics', N'profile delete'),
    (N'MyNewspaperAdminApp', N'MyDailyFeedProfileSaved', N'profile delete'),
    (N'MyNewspaperAdminApp', N'MyDailyFeedSavedList', N'profile delete / base edition'),
    (N'MyNewspaperAdminApp', N'MyDailyFeedSavedImports', N'profile delete'),
    (N'MyNewspaperAdminApp', N'MyDailyFeedContentRequests', N'Requests: approve, decline, check again'),
    (N'MyNewspaperAdminApp', N'MyDailyFeedChannelRollouts', N'channel rollout on approve'),
    (N'MyNewspaperAdminApp', N'Sources', N'Sources: add, edit, delete'),
    (N'MyNewspaperAdminApp', N'SourceCategories', N'Categories: add, edit, delete, hub'),
    (N'MyNewspaperAdminApp', N'Pages', N'PageEdit; delete a source with its pages'),
    (N'MyNewspaperAdminApp', N'YoutubeVideoFeed', N'YouTube feeds: add, edit, delete, household pick'),
    (N'MyNewspaperAdminApp', N'YouTubeVideos', N'delete videos with their feed; assign feed'),
    (N'MyNewspaperAdminApp', N'SportsEvents', N'events: link, assign type, delete'),
    (N'MyNewspaperAdminApp', N'SportsEventsType', N'event types: add, edit, delete'),
    (N'MyNewspaperAdminApp', N'TvTrackedSeries', N'tracked series list'),
    (N'MyNewspaperAdminApp', N'ResultFollows', N'Results follows: add, edit, delete; Sync now'),
    (N'MyNewspaperAdminApp', N'ResultFollowTeams', N'Results follows; Sync now'),
    (N'MyNewspaperAdminApp', N'ResultGames', N'Sync now');

IF EXISTS (SELECT 1 FROM @writes w WHERE OBJECT_ID(N'dbo.' + QUOTENAME(w.TableName), N'U') IS NULL)
BEGIN
    DECLARE @missing nvarchar(2000) = (SELECT STRING_AGG(TableName, N', ') FROM (SELECT DISTINCT TableName FROM @writes w
        WHERE OBJECT_ID(N'dbo.' + QUOTENAME(w.TableName), N'U') IS NULL) m);
    RAISERROR(N'Tables missing in this database (grant skipped, investigate before production): %s', 10, 1, @missing);
END;

DECLARE @role sysname, @table sysname, @sql nvarchar(max);
DECLARE g CURSOR LOCAL FAST_FORWARD FOR
    SELECT RoleName, TableName FROM @writes WHERE OBJECT_ID(N'dbo.' + QUOTENAME(TableName), N'U') IS NOT NULL;
OPEN g; FETCH NEXT FROM g INTO @role, @table;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'GRANT INSERT, UPDATE, DELETE ON dbo.' + QUOTENAME(@table) + N' TO ' + QUOTENAME(@role) + N';';
    EXEC sys.sp_executesql @sql;
    FETCH NEXT FROM g INTO @role, @table;
END;
CLOSE g; DEALLOCATE g;

IF DATABASE_PRINCIPAL_ID(N'mynewspaper_web') IS NULL CREATE USER [mynewspaper_web] FOR LOGIN [mynewspaper_web];
IF DATABASE_PRINCIPAL_ID(N'mynewspaper_admin') IS NULL CREATE USER [mynewspaper_admin] FOR LOGIN [mynewspaper_admin];
IF IS_ROLEMEMBER(N'MyNewspaperWebApp', N'mynewspaper_web') = 0 ALTER ROLE [MyNewspaperWebApp] ADD MEMBER [mynewspaper_web];
IF IS_ROLEMEMBER(N'MyNewspaperAdminApp', N'mynewspaper_admin') = 0 ALTER ROLE [MyNewspaperAdminApp] ADD MEMBER [mynewspaper_admin];

COMMIT TRANSACTION;

SELECT r.name AS role_name, COUNT(DISTINCT CASE WHEN p.permission_name IN (N'INSERT', N'UPDATE', N'DELETE') THEN p.major_id END) AS writable_tables
FROM sys.database_principals r
LEFT JOIN sys.database_permissions p ON p.grantee_principal_id = r.principal_id AND p.class = 1
WHERE r.name IN (N'MyNewspaperWebApp', N'MyNewspaperAdminApp')
GROUP BY r.name;
GO
