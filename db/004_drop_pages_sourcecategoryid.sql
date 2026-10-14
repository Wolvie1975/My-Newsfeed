-- Removes Pages.SourceCategoryId (added in 003). It duplicated Sources.SourceCategoryId, which is the single
-- place a category is set; a page's category is read through its Source.
-- Run as a single transaction; GO separates batches.

DROP INDEX IX_Pages_SourceCategoryId ON dbo.Pages;
GO

ALTER TABLE dbo.Pages DROP CONSTRAINT FK_Pages_SourceCategories;
GO

ALTER TABLE dbo.Pages DROP COLUMN SourceCategoryId;
GO
