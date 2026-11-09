-- The Tech section (docs/tech/). Additive and repeatable; run after 019 and 023.
-- Part 1: allow NewsHub = 'tech' and move the three tech categories there. The move happens on the first run only
--         (when the constraint doesn't allow 'tech' yet), so a re-run never overrides a later Admin change, including
--         a category moved back to 'headlines'. (Before November 5, 2026 a re-run moved such a category again.)
-- Part 2: allow Your topics to be limited to Tech.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.SourceCategories', N'U') IS NULL OR COL_LENGTH(N'dbo.SourceCategories', N'NewsHub') IS NULL
    THROW 50000, 'Run 019_news_category_hubs.sql first.', 1;
IF OBJECT_ID(N'dbo.MyDailyFeedProfileTopics', N'U') IS NULL
    THROW 50000, 'Run 023_news_section.sql first.', 1;

-- Part 1 of 2
DECLARE @firstRun bit = CASE WHEN EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_SourceCategories_NewsHub'
                                          AND definition LIKE N'%tech%') THEN 0 ELSE 1 END;
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_SourceCategories_NewsHub'
           AND definition NOT LIKE N'%tech%')
BEGIN
    ALTER TABLE dbo.SourceCategories DROP CONSTRAINT CK_SourceCategories_NewsHub;
    ALTER TABLE dbo.SourceCategories ADD CONSTRAINT CK_SourceCategories_NewsHub
        CHECK (NewsHub IN (N'headlines', N'sports', N'entertainment', N'tech'));
END;
IF @firstRun = 1
    UPDATE dbo.SourceCategories SET NewsHub = N'tech'
    WHERE NewsHub = N'headlines' AND Category_Name IN (N'Technology', N'Video Games', N'Linux');

-- Part 2 of 2
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_MyDailyFeedProfileTopics_Hub'
           AND definition NOT LIKE N'%tech%')
BEGIN
    ALTER TABLE dbo.MyDailyFeedProfileTopics DROP CONSTRAINT CK_MyDailyFeedProfileTopics_Hub;
    ALTER TABLE dbo.MyDailyFeedProfileTopics ADD CONSTRAINT CK_MyDailyFeedProfileTopics_Hub
        CHECK (Hub IS NULL OR Hub IN (N'headlines', N'sports', N'entertainment', N'tech'));
END;
COMMIT TRANSACTION;

