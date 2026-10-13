-- Shared contract with the Rust scraper's sql/nwsl_events.sql.
-- Apply before deploying the web app, or let the updated scraper initialize it first.
IF COL_LENGTH('dbo.SportsEvents', 'ScheduleStatus') IS NULL
    ALTER TABLE dbo.SportsEvents ADD ScheduleStatus nvarchar(100) NULL;
