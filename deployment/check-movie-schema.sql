-- Read-only prerequisite check. Run against the database configured by production SQL_CONNECTION.
SET NOCOUNT ON;
SELECT DB_NAME() AS DatabaseName;
IF OBJECT_ID(N'dbo.Movies', N'U') IS NULL
    OR OBJECT_ID(N'dbo.MovieReleaseSources', N'U') IS NULL
    OR OBJECT_ID(N'dbo.MovieSourceLinks', N'U') IS NULL
    OR OBJECT_ID(N'dbo.MovieReleases', N'U') IS NULL
    OR OBJECT_ID(N'dbo.MovieCatalogs', N'U') IS NULL
    THROW 51000, 'Movie tables are missing. Prepare the production movie schema before deploying.', 1;

-- Compile and execute empty projections to verify every column consumed by the UI.
SELECT TOP (0) ID, Title, OriginalYear, Description, PosterUrl, Certification, RuntimeMinutes
FROM dbo.Movies;
SELECT TOP (0) ID, Name, Enabled, LastScrapedAt, LastError FROM dbo.MovieReleaseSources;
SELECT TOP (0) ID, MovieId, MovieReleaseSourceId FROM dbo.MovieSourceLinks;
SELECT TOP (0) ID, MovieSourceLinkId, CountryCode, ReleaseStatus, ReleaseDate, DateStatus,
    ReleaseType, Platform, Format, IsReRelease, SourceUrl, LastSeenAt FROM dbo.MovieReleases;
IF COL_LENGTH(N'dbo.ResultFollows', N'GroupId') IS NULL
    OR OBJECT_ID(N'dbo.ResultFollowTeams', N'U') IS NULL
    THROW 51001, 'Results conference schema is missing. Apply db/013 before deploying.', 1;
SELECT 'Movie and conference schema checks passed' AS Result;
