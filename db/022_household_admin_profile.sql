-- Which profile is the household admin's: only that profile's pages show the "Admin sign in" link (an open admin
-- session shows "Admin" as before). Purely cosmetic: /login still works by address and still needs the password.
-- NULL means nobody, so no reader gets the link. Deleting the profile clears it. Repeatable.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.MyDailyFeedHousehold', N'U') IS NULL
    THROW 50000, 'Run 020_profiles.sql first.', 1;

IF COL_LENGTH(N'dbo.MyDailyFeedHousehold', N'AdminProfileId') IS NULL
    ALTER TABLE dbo.MyDailyFeedHousehold ADD AdminProfileId int NULL;

IF OBJECT_ID(N'dbo.FK_MyDailyFeedHousehold_AdminProfile', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.MyDailyFeedHousehold ADD CONSTRAINT FK_MyDailyFeedHousehold_AdminProfile
        FOREIGN KEY (AdminProfileId) REFERENCES dbo.MyDailyFeedProfiles (Id) ON DELETE SET NULL;');
COMMIT TRANSACTION;
