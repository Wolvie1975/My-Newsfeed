-- The public events page shows each game as "away team vs home team". Only the opponent is stored on an event, so the
-- school whose calendar a feed is (Kansas) is kept on its type. Nullable and optional: the scraper does not write it,
-- and a type without one just shows a neutral label on the events page.
-- Run as a single transaction; GO separates batches.

ALTER TABLE dbo.SportsEventsType ADD SchoolName nvarchar(100) NULL;
GO

-- The one feed in use is Kansas's (school_id=3 in its address).
UPDATE dbo.SportsEventsType SET SchoolName = N'Kansas' WHERE SchoolName IS NULL AND RssUrl LIKE N'%school_id=3%';
GO
