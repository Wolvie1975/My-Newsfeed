-- Categories for Sources. Adds dbo.SourceCategories and Sources.SourceCategoryId -> SourceCategories.ID.
-- The FK is nullable (existing and scraper-created sources have no category) and ON DELETE NO ACTION,
-- so a category that is still in use cannot be deleted.
-- Run as a single transaction; GO separates batches.

CREATE TABLE dbo.SourceCategories (
    ID            int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SourceCategories PRIMARY KEY,
    Category_Name nvarchar(100)     NOT NULL,
    DateAdded     datetime2         NOT NULL CONSTRAINT DF_SourceCategories_DateAdded DEFAULT (sysutcdatetime()),
    CONSTRAINT UQ_SourceCategories_Category_Name UNIQUE (Category_Name)
);
GO

ALTER TABLE dbo.Sources ADD SourceCategoryId int NULL;
GO

ALTER TABLE dbo.Sources ADD CONSTRAINT FK_Sources_SourceCategories
    FOREIGN KEY (SourceCategoryId) REFERENCES dbo.SourceCategories (ID) ON DELETE NO ACTION;
CREATE INDEX IX_Sources_SourceCategoryId ON dbo.Sources (SourceCategoryId);
GO
