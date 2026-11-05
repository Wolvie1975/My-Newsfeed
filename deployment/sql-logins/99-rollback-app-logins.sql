/*
  Remove what 01-app-logins.sql created (November 4, 2026, America/Chicago).
  Put both app containers back on a sysadmin connection first (DEPLOY.md, SQL logins), or they lose the database.
      MNP_DATABASE=WebScraper_Test sqlcmd -S … -U sa -C -b -i 99-rollback-app-logins.sql
*/
:on error exit
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM sys.dm_exec_sessions WHERE login_name IN (N'mynewspaper_web', N'mynewspaper_admin', N'mynewspaper_test'))
    THROW 50005, N'An app or test login still has open sessions; switch the apps (and tests) back first.', 1;
GO

USE [$(MNP_DATABASE)];
GO
BEGIN TRANSACTION;
IF DATABASE_PRINCIPAL_ID(N'mynewspaper_web') IS NOT NULL DROP USER [mynewspaper_web];
IF DATABASE_PRINCIPAL_ID(N'mynewspaper_admin') IS NOT NULL DROP USER [mynewspaper_admin];
IF DATABASE_PRINCIPAL_ID(N'MyNewspaperWebApp') IS NOT NULL DROP ROLE [MyNewspaperWebApp];
IF DATABASE_PRINCIPAL_ID(N'MyNewspaperAdminApp') IS NOT NULL DROP ROLE [MyNewspaperAdminApp];
-- The test login's user (04-test-login.sql) exists only in WebScraper_Test.
IF DB_NAME() = N'WebScraper_Test' AND DATABASE_PRINCIPAL_ID(N'mynewspaper_test') IS NOT NULL
    DROP USER [mynewspaper_test];
COMMIT TRANSACTION;
GO

USE [master];
GO
-- The logins are server-wide and may still have users in the other database (WebScraper vs WebScraper_Test), so
-- they are only dropped when asked: MNP_DROP_LOGINS=yes, after running this against every database that has them.
IF N'$(MNP_DROP_LOGINS)' = N'yes'
BEGIN
    IF SUSER_ID(N'mynewspaper_web') IS NOT NULL DROP LOGIN [mynewspaper_web];
    IF SUSER_ID(N'mynewspaper_admin') IS NOT NULL DROP LOGIN [mynewspaper_admin];
    IF SUSER_ID(N'mynewspaper_test') IS NOT NULL DROP LOGIN [mynewspaper_test];
END
ELSE
    PRINT N'Logins kept (set MNP_DROP_LOGINS=yes to drop them).';
GO
