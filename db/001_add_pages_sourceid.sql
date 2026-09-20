-- Pages belong to a Source. Adds Pages.SourceId -> Sources.ID.
-- Existing rows are backfilled by exact URL match, then the column is made NOT NULL.
BEGIN TRANSACTION;

ALTER TABLE dbo.Pages ADD SourceId int NULL;
GO

UPDATE p SET p.SourceId = s.ID
FROM dbo.Pages p JOIN dbo.Sources s ON s.Url = p.Url;

IF EXISTS (SELECT 1 FROM dbo.Pages WHERE SourceId IS NULL)
BEGIN
    RAISERROR('Backfill failed: some Pages have no matching Source.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END

ALTER TABLE dbo.Pages ALTER COLUMN SourceId int NOT NULL;
ALTER TABLE dbo.Pages ADD CONSTRAINT FK_Pages_Sources
    FOREIGN KEY (SourceId) REFERENCES dbo.Sources (ID) ON DELETE NO ACTION;
CREATE INDEX IX_Pages_SourceId ON dbo.Pages (SourceId);

COMMIT TRANSACTION;
