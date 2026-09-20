-- Adds Pages.SourceCategoryId -> SourceCategories.ID and backfills it from each page's Source.
-- Nullable (uncategorized pages, and scraper inserts that don't set it); ON DELETE NO ACTION.
-- Run as a single transaction; GO separates batches.

ALTER TABLE dbo.Pages ADD SourceCategoryId int NULL;
GO

UPDATE p SET p.SourceCategoryId = s.SourceCategoryId
FROM dbo.Pages p JOIN dbo.Sources s ON s.ID = p.SourceId;
GO

ALTER TABLE dbo.Pages ADD CONSTRAINT FK_Pages_SourceCategories
    FOREIGN KEY (SourceCategoryId) REFERENCES dbo.SourceCategories (ID) ON DELETE NO ACTION;
CREATE INDEX IX_Pages_SourceCategoryId ON dbo.Pages (SourceCategoryId);
GO
