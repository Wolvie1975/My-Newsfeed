/*
  SQL login for the .NET integration tests. DRAFT FOR REVIEW — November 5, 2026 (America/Chicago). Not yet run.

  What it does (repeatable; every change is guarded):
    1. Creates login mynewspaper_test if absent (CHECK_POLICY ON, default database WebScraper_Test). An existing login
       keeps its password: rotate it with ALTER LOGIN, not by re-running this.
    2. Creates user mynewspaper_test in WebScraper_Test and adds it to db_owner there. The tests seed scraper tables,
       so they need more than either app role, but db_owner of the test copy only.
    3. Grants CREATE ANY DATABASE (server level) for the one test that builds a scratch database of its own.
    4. Refuses if the login has a user in WebScraper, and reports any server-role membership (expects none). That
       makes "tests never touch WebScraper" true at the server, not just in TestDatabase.cs.

  The database name is fixed on purpose (no MNP_DATABASE): this login exists only for WebScraper_Test.

  Re-run it after every deployment/create-test-database.sql. That script rebuilds WebScraper_Test from a backup of
  WebScraper, which drops this user (the login survives, because it's server-wide).
      MNP_TEST_SQL_PASSWORD=… sqlcmd -S … -U sa -C -b -i 04-test-login.sql

  Then point the tests at it (README.md, phase 1 step 4): the tests read the web app's user secrets, so the test
  connection needs its own key, ConnectionStrings:TestConnection, which TestDatabase.cs prefers over DefaultConnection.
*/
:on error exit
SET NOCOUNT ON;
-- sqlcmd defaults to QUOTED_IDENTIFIER OFF; tables with filtered indexes or indexed views then fail with 1934.
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

IF DB_ID(N'WebScraper_Test') IS NULL
    THROW 50001, N'WebScraper_Test does not exist. Run deployment/create-test-database.sql first.', 1;
IF LEN(N'$(MNP_TEST_SQL_PASSWORD)') < 24
    THROW 50002, N'The test password must be at least 24 characters.', 1;
GO

USE [master];
GO
IF SUSER_ID(N'mynewspaper_test') IS NULL
    CREATE LOGIN [mynewspaper_test] WITH PASSWORD = N'$(MNP_TEST_SQL_PASSWORD)', DEFAULT_DATABASE = [WebScraper_Test],
        CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;
ELSE
    PRINT N'Login mynewspaper_test exists; password unchanged.';
-- SharedArticlesTests creates and drops its own scratch database (myNewspaperSavedTest_<guid>). The creator owns the
-- database it creates, so this is all it needs to drop it again; it gives no access to any other database.
-- Decided by the user, November 5, 2026.
GRANT CREATE ANY DATABASE TO [mynewspaper_test];
GO

-- The live database must never have a user for this login (a restore can't add one, but a mistake could).
IF DB_ID(N'WebScraper') IS NOT NULL AND EXISTS (
    SELECT 1 FROM [WebScraper].sys.database_principals AS p
    JOIN sys.server_principals AS s ON s.sid = p.sid
    WHERE s.name = N'mynewspaper_test')
    THROW 50006, N'mynewspaper_test has a user in WebScraper. Drop it there before going on.', 1;
GO

USE [WebScraper_Test];
GO
IF DB_NAME() <> N'WebScraper_Test' THROW 50007, N'Expected to be in WebScraper_Test.', 1;
BEGIN TRANSACTION;
-- A restored copy can carry an orphaned user of this name from an earlier copy; remap it rather than fail.
IF DATABASE_PRINCIPAL_ID(N'mynewspaper_test') IS NULL
    CREATE USER [mynewspaper_test] FOR LOGIN [mynewspaper_test] WITH DEFAULT_SCHEMA = [dbo];
ELSE
    ALTER USER [mynewspaper_test] WITH LOGIN = [mynewspaper_test];
IF IS_ROLEMEMBER(N'db_owner', N'mynewspaper_test') = 0
    ALTER ROLE [db_owner] ADD MEMBER [mynewspaper_test];
COMMIT TRANSACTION;
GO

-- Report: where the login has users, and any server roles (expected: WebScraper_Test only, no server roles).
USE [master];
GO
SELECT N'server role' AS kind, r.name AS name
FROM sys.server_role_members AS m
JOIN sys.server_principals AS r ON r.principal_id = m.role_principal_id
JOIN sys.server_principals AS l ON l.principal_id = m.member_principal_id
WHERE l.name = N'mynewspaper_test' AND r.name <> N'public';

-- Expected: CREATE ANY DATABASE (and CONNECT SQL) only.
SELECT N'server permission' AS kind, p.permission_name AS name
FROM sys.server_permissions AS p
JOIN sys.server_principals AS l ON l.principal_id = p.grantee_principal_id
WHERE l.name = N'mynewspaper_test' AND p.state IN ('G', 'W');

SELECT N'db_owner in' AS kind, N'WebScraper_Test' AS name
WHERE EXISTS (SELECT 1 FROM [WebScraper_Test].sys.database_principals WHERE name = N'mynewspaper_test');
GO
