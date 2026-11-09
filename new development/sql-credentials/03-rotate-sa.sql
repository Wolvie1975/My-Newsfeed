/*
  Rotate the sa password. DRAFT FOR REVIEW — November 4, 2026 (America/Chicago).
  The current sa password is in Git history until October 25, 2026 (checked November 4: the live value matches one
  of the committed values). Run only in phase 4 of README.md, after both apps run on their own logins and the
  pre-check below shows no sa sessions except the one running this script.
      MNP_NEW_SA_PASSWORD=… sqlcmd -S … -U sa -C -b -i 03-rotate-sa.sql
  (sqlcmd prompts for, or reads SQLCMDPASSWORD for, the current password; don't pass it with -P.)
*/
:on error exit
SET NOCOUNT ON;
USE [master];
GO

IF LEN(N'$(MNP_NEW_SA_PASSWORD)') < 24
    THROW 50003, N'The new sa password must be at least 24 characters.', 1;

-- Pre-check: who still connects as sa? Expect only this session.
SELECT host_name, LEFT(program_name, 60) AS program, COUNT(*) AS sessions
FROM sys.dm_exec_sessions
WHERE is_user_process = 1 AND login_name = N'sa' AND session_id <> @@SPID
GROUP BY host_name, LEFT(program_name, 60);

IF EXISTS (SELECT 1 FROM sys.dm_exec_sessions WHERE is_user_process = 1 AND login_name = N'sa' AND session_id <> @@SPID)
    THROW 50004, N'Other sessions are still using sa (listed above). Move them to their own logins first.', 1;

ALTER LOGIN [sa] WITH PASSWORD = N'$(MNP_NEW_SA_PASSWORD)';
PRINT N'sa password changed. Existing connections are unaffected; new ones need the new password.';
GO
