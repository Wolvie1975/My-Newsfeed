# Entertainment data and public views

Current contracts consolidated October 13, 2026 (Central). See
[Deployment](../DEPLOY.md) for rollout and [Release history](release-history.md)
for historical validation. Rust commands and `sql/` paths refer to the separate
scraper repository; the web app reads its stored snapshots.

## Movie releases and Digital/VOD

The In theaters view (`/movies?type=theatrical`) includes a general
[Visit AMC Theatres](https://www.amctheatres.com/) link immediately above the first release-date heading,
with a locally served AMC logo. It opens in a new tab with `noopener noreferrer`.
The Digital rental / purchase page similarly links to
[Fandango at Home](https://athome.fandango.com/) with its locally served logo,
below the source summary and above the first release-date heading. Both links
open in a new tab with `noopener noreferrer`. These page-level links are separate
from each film's Fandango action and do not change the release calendar or source
links. The logos and their source attribution are in `src/MyDailyFeed.Web/wwwroot/images/brands/`.

Movie cards on Movies, Digital and the homepage read retailer actions from
`dbo.MovieCommerce` by MovieId and Provider through the release's source link.
Theatrical releases use Fandango; digital, digital purchase and digital rental use
Fandango at Home. Subscription and disc releases have no retailer action. Verified
direct links retain the view's ticket/Buy/View label; missing or invalid actions
use an encoded title search labeled Search. HTTPS retailer hosts, paths and labels
are validated before rendering. View errors retain normal page error handling;
expiry and stale identity are handled by the scraper's view. This enrichment does
not change release selection, grouping, counts, dates, posters or source links.
On desktop (768 pixels and wider), each movie-card retailer action occupies its
own line below the source/details link. Mobile retains its existing layout.

`/movies` supports all releases, theatrical and subscription selections. Movies and
Videos page in deterministic 50-row batches; filters remain in paging URLs.
Movies and Digital share a News-style dropdown for All releases, In theaters,
Subscription streaming and Digital rental / purchase. Selection submits a GET
form; an Apply button remains available without JavaScript. The digital choice
routes to `/movies/digital`; switching views starts at the first page.
`/movies/digital` is the provider-specific confirmed rental/purchase list, not a
subscription catalog. It includes today and all known future release dates, ordered
soonest first. The old current Monday–Sunday restriction is superseded.

Prefer Movie Insider whenever enabled; otherwise select another enabled source.
The source dropdown also offers When To Stream and DVDs Release Dates when enabled,
even if that source has no matching rows. Preserve `?source=`; invalid/disabled keys
fall back to the preferred enabled source. Do not silently blend providers when a
selected source is empty. Generic digital data does not prove platform, pricing,
rental versus purchase or subscription availability.

Read Movies, MovieSourceLinks, MovieReleaseSources and MovieReleases. The core filter
is selected enabled source, US, confirmed digital types, noncanceled/nonwithdrawn
status and today-forward date:

```sql
SELECT r.ID AS ReleaseId,m.ID AS MovieId,m.Title,m.OriginalYear,m.PosterUrl,
       r.ReleaseDate,r.ReleaseType,r.Platform,r.Format,r.SourceUrl,r.LastSeenAt,
       s.SourceKey,s.Name AS SourceName,s.LastScrapedAt,s.LastError
FROM dbo.MovieReleases r
JOIN dbo.MovieSourceLinks l ON l.ID=r.MovieSourceLinkId
JOIN dbo.MovieReleaseSources s ON s.ID=l.MovieReleaseSourceId
JOIN dbo.Movies m ON m.ID=l.MovieId
WHERE s.Enabled=1 AND s.SourceKey=@SourceKey AND r.CountryCode='US'
  AND r.ReleaseType IN ('digital','digital_purchase','digital_rental')
  AND r.DateStatus='announced'
  AND r.ReleaseStatus NOT IN ('cancelled','withdrawn')
  AND r.ReleaseDate>=@TodayCentral
ORDER BY r.ReleaseDate,m.Title,r.ID;
```

ReleaseDate is a calendar DATE and is displayed directly. LastSeenAt/LastScrapedAt
are UTC; label the latter Last checked, not guaranteed successful refresh. Missing
posters, original years and platforms stay unknown. Preserve provider stable release
IDs; don't merge by title alone or put changing dates into identity keys. IMDb/TMDB
identities can support shared movie identity when supplied.

Movie Insider confirms explicit digital date paragraphs and `<time datetime>`;
calendar cards only discover candidates. Numeric `m<ID>` and `digital:first` keep
identity stable across date/slug changes. The collector scans pagination (up to 20
pages/year) because cards can be out of order. When To Stream VOD and SVOD dates
remain separate digital/subscription observations. Estimates and disc dates do not
become confirmed digital premieres. Unknown original years are not inferred from
release dates. Providers may differ without their dates being overwritten by title matches.

In the scraper, run `--configure-movieinsider` with migration credentials (or its
`sql/movieinsider_seed.sql`), explicitly enable MovieReleaseSources.SourceKey
`movieinsider`, preview `--movies --dry-run`, and retain hourly `--movies` collection.
Setup preserves existing choices; the addition reuses existing movie tables.
This repository's [movie schema check](../deployment/check-movie-schema.sql) is read-only.
Use it before deployment; the web app does not create the scraper movie schema.

Home's Today & Tonight combines today's sport fixtures, episode premieres and movie
releases, confirmed times before TBD/date-only entries. Digital/digital_rental/
digital_purchase releases are excluded from that timeline. A separate upcoming
digital preview reads the preferred provider's five earliest matching releases.
The full digital page retains the complete selected-provider list.

## Box-office weekend results

`/box-office` shows Weekend results — Top 10 from The Numbers. The News-style
section dropdown links to `/box-office/weekend-preview` for Upcoming weekend
preview — Predictions and `/box-office/movie-forecasts` for Upcoming movie
forecasts — Predictions. Each URL shows only its section. The shared GET selector
submits automatically, with an Apply button available without JavaScript.
Actual results are separate from forecasts, news Pages and RSS.
The source is [The Numbers weekend chart](https://www.the-numbers.com/weekend-box-office-chart).

```sql
SELECT Rank,WeekendStart,MovieTitle,WeekendGross,TotalDomesticGross,
       WeeklyChangePercent,Theaters,MovieUrl,SourceUrl,FetchedAt,PosterUrl,PosterLookupStatus
FROM dbo.BoxOfficeWeekend ORDER BY Rank;
```

WeekendStart is Friday, ending Sunday (+2 days). Format labels without converting
calendar dates, including both years for a New Year boundary. Display rank, linked
title, USD integer weekend/total domestic grosses and percentage change. Null change
means unavailable/new, not zero. FetchedAt is UTC; display it using the site's time
helper. Attribute The Numbers with SourceUrl and say reported totals may be revised,
not final actuals. Show a stale warning when Friday is more than ten days old.

Configure scraper schema with `--configure-boxoffice` using migration credentials,
then collect `--boxoffice` using normal ingestion credentials. Keep that flag in hourly
collection. `--boxoffice --dry-run` validates without changing content. A valid refresh
atomically replaces all ten rows. Invalid, incomplete, unordered, future-dated,
stale (>10-day) or older replacement charts preserve the last good snapshot.

The earlier Deadline/Variety box-office news implementation is superseded by this
structured chart. Do not re-enable its feeds as part of chart setup or derive totals
from headlines. Other news/movie/sports retention policies remain independent.

## Boxoffice Pro forecasts

Forecast data describes domestic USD predictions, not actual results. Link original
articles for analysis; don't copy article bodies or pros/cons text. Source discovery
uses [Boxoffice Pro forecasts](https://www.boxofficepro.com/category/forecasts-tracking/).

```sql
SELECT Kind,TargetDate,PeriodEnd,MovieTitle,Distributor,PredictedRank,
       ForecastLow,ForecastHigh,ForecastAsOf,PublishedAtUtc,UpdatedAtUtc,
       SourceUrl,FetchedAtUtc,PosterUrl,PosterLookupStatus
FROM dbo.BoxOfficeForecasts WHERE PeriodEnd>=@TodayCentral
ORDER BY Kind,TargetDate,PredictedRank,MovieTitle;

SELECT Kind,LatestPublishedAt,LastAttemptAt,LastSuccessAt,LastError,LastMovieCount
FROM dbo.BoxOfficeForecastSources;
```

| Field | Meaning |
| --- | --- |
| Kind | `weekend-preview` or `long-range` |
| TargetDate | Preview weekend start, or film's domestic release date |
| PeriodEnd | Preview end, or same date as TargetDate for long-range |
| MovieTitle / Distributor | Provider labels |
| PredictedRank | Provider preview rank; otherwise nullable |
| ForecastLow / ForecastHigh | Integer USD prediction range |
| ForecastAsOf | Date of prediction, including an older tracking-table as-of date |
| PublishedAtUtc / UpdatedAtUtc | Article publication / modification timestamps |
| SourceUrl | Original article attribution and analysis link |
| FetchedAtUtc | Last successful local import |

Primary key is `(Kind, TargetDate, MovieTitle)`. Do not join by movie-release numeric
IDs or assume titles match other catalogs. Group Upcoming weekend preview — Predictions
by TargetDate/PeriodEnd and show supplied rank, title, distributor and predicted
weekend gross. Group Upcoming movie forecasts — Predictions by release date and show
title, distributor, predicted opening-weekend range and ForecastAsOf. Format ranges
as `$25M–$35M`, preserving precision. Each row links Source: Boxoffice Pro. A preview
need not have ten rows; never fill missing ranks or supply predictions.

Filter PeriodEnd against Central today on every query, even before hourly cleanup.
An empty preview says The next weekend preview has not been published yet only when
LastSuccessAt is within 24 hours (inclusive), not future-dated, and LastError is NULL.
Otherwise it says Preview data is temporarily unavailable. Use equivalent future-film
empty states. Existing predictions remain visible with a refresh warning after failures
or stale source metadata. Raw errors stay in logs/admin. Actuals and forecasts load
on separate pages so a forecast failure does not hide the reported chart. The
results page only reads chart data; forecast pages only read the forecast snapshot.

`--configure-boxoffice` initializes actuals and forecast schemas. Hourly collection
adds `--boxoffice-forecasts`; its `--dry-run` validates without writes. The collector
selects the newest domestic preview and long-range articles by publication timestamp,
excluding U.K./Ireland/general news. Newly featured headings take precedence over
older tracking-table entries; omitted films are not reconstructed from older articles.
Both snapshots replace previous data in one transaction. Malformed ranges, missing
links, inconsistent ranks, failed requests or older publication versions preserve
last-good data; article bodies older than 60 days are rejected. An expired but valid
preview is a successful empty snapshot. Routine retention removes expired rows even
after later collection failures. Long-range films remain through their release date,
and previews through their stated end date.

## Television and streaming episodes

TVmaze tables are scraper-owned: TvSeries, TvEpisodes and TvTrackedSeries. Track by
explicit TvmazeShowId; database IDs are separate. TvTrackedSeries can exist with
SeriesId NULL before the first successful fetch. New seeds are enabled, and repeat
seeding preserves existing tracking and disabled choices. Disable a tracked show
through Enabled rather than deleting its tracking row.

Airdate is the provider's calendar date; Airtime its local clock time. StartsAtUtc is
confirmed only when the airdate, nonblank airtime and airstamp are supplied/parseable.
IsDateOnly means no confirmed time: don't show RawAirstamp placeholders (including noon
stamps with blank airtime). Network/web-channel labels describe original scheduling,
not country-specific streaming availability. Line of Fire is show 90632, NBC 2026;
no Peacock dates are inferred. Provider null/empty raw metadata is retained; treat
summary HTML as untrusted if rendering it.

```sql
SELECT s.TvmazeShowId,s.Title AS SeriesTitle,s.NetworkName,s.WebChannelName,
       s.PosterUrl,s.SourceUrl AS SeriesSourceUrl,
       e.TvmazeEpisodeId,e.SeasonNumber,e.EpisodeNumber,e.Title AS EpisodeTitle,
       e.Airdate,e.Airtime,e.StartsAtUtc,e.IsDateOnly,e.IsSpecial,
       e.RuntimeMinutes,e.ImageUrl,e.SourceUrl AS EpisodeSourceUrl
FROM dbo.TvEpisodes e
JOIN dbo.TvSeries s ON s.ID=e.SeriesId
JOIN dbo.TvTrackedSeries t ON t.SeriesId=s.ID
WHERE t.Enabled=1 AND e.Airdate>=@WeekStart AND e.Airdate<DATEADD(day,7,@WeekStart)
ORDER BY e.Airdate,s.Title,e.SeasonNumber,e.EpisodeNumber,e.TvmazeEpisodeId;
```

The collector fetches `/shows/{id}` and `/shows/{id}/episodes?specials=1`, uses a one-hour
validated cache with status/retry/lease metadata, and upserts atomically. Missing
observations never delete episodes; incomplete/shortened identity sets preserve last-good
data/cache/success metadata. Collection timestamps are UTC and foreign keys use NO ACTION.
Configure with `--configure-tv`, collect enabled shows using `--tv-from-db`, and use
`--tv-force-refresh` only for explicit cache-bypassing validation/manual refresh.

Credit TVmaze with stored original series/episode links, not collector API URLs.
The imported contract identifies API data as [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/),
requires attribution and ShareAlike for adapted redistribution, and permits image
hotlinking with attribution. Indicate date-only normalization/adaptations. Reference:
[TVmaze API documentation](https://www.tvmaze.com/api). This is the retained handoff's
licensing statement, not a new licensing review.

## Posters across movie sections

Movie releases continue using `dbo.Movies.PosterUrl`. Box-office results
(`dbo.BoxOfficeWeekend`) and upcoming previews/long-range forecasts
(`dbo.BoxOfficeForecasts`) now expose nullable `PosterUrl` and
`PosterLookupStatus` fields directly. Include these columns in the front-end
queries from the existing section handoffs.

Render a portrait image with MovieTitle as alt text. Use a neutral poster
placeholder when PosterUrl is null or the browser cannot load the image.
Do not substitute a news article hero image, logo or trailer thumbnail.
Portrait JPEG/PNG dimensions and image response type are checked before saving.
Movie title and original film year must match the source. Re-releases use the
original year from the linked The Numbers movie page URL, not the chart year.
For future forecasts, the target release year is matched against movie metadata;
ambiguous or conflicting matches leave the poster unavailable.

`PosterLookupStatus`: `matched` means a validated image, `missing` means no unique
match was found, and `error` means a lookup/validation request failed. An error
may retain the previous validated URL for the same identity. Status/error details
are for admin diagnostics; show the image or placeholder to readers.

The shared `dbo.CinemaPosterCache` uses normalized title plus original year.
It reuses validated release posters, otherwise tries the linked The Numbers
movie detail poster for actuals and the existing DVD Release Dates search.
Box-office and forecast replacement does not delete this cache. Normal hourly
imports reattach cached posters without downloading images again; successful
matches refresh after 30 days, missing matches retry after 7 days, and errors
retry after 6 hours. Entries unused for 90 days are removed by retention.

`--configure-boxoffice` applies the additive schema with migration credentials.
Hourly `--boxoffice` / `--boxoffice-forecasts` imports enrich both sections.
`--movie-posters` backfills releases and box-office/forecast posters together;
`--retry-missing-posters` forces existing release-section missing-image retries.
No new external API key is required. Images remain external HTTPS URLs.

Initial validated coverage: all 10 actual chart films and all 12 future forecasts.
The current preview dataset is empty because the last preview has expired;
newly published preview rows will use the same enrichment automatically.

Release-section backfill found one additional validated poster using The Numbers
as a fallback. Current releases: 24 of 34 movies have posters; the remaining ten
have no unambiguous title/year match and should display placeholders. Existing
release enrichment retries those missing images under its normal cache policy.

The deployed web app reads both nullable poster columns in chart/forecast queries
and uses a shared MoviePoster component for actuals, predictions, Movies, Digital
and homepage movie previews. Titles are image alt text; HTTP(S) URLs are validated
and HTTP is upgraded to HTTPS. Missing or failed images reveal the same neutral
portrait placeholder with accessible unavailable text. Lookup status is not displayed.
The current preview is empty, but future rows use the same forecast component.

## Validation

Check selected-provider isolation, confirmed digital types, today-forward boundaries,
stable identities, missing posters/metadata, collection warnings and safe links. For
box office, check USD precision, nullable change, weekend/year boundaries, stale data,
prediction versus actual labels, source health empty states and expired-row filtering.
For television, cover date-only placeholders, confirmed UTC starts and source attribution.
Recorded row counts are dated observations, never permanent expected totals.

## TVmaze schema reference

Retained verbatim from the TVmaze handoff. The authoritative migration lives in the
separate scraper repository at `sql/tvmaze.sql`; this is a reference, not an
additional web-app migration.
All collection timestamps are UTC DATETIME2. All foreign keys use NO ACTION.

```sql
-- Additive, repeatable TVmaze schema. No existing tables are altered.
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID('dbo.TvSeries','U') IS NULL
 CREATE TABLE dbo.TvSeries (
  ID INT IDENTITY(1,1) CONSTRAINT PK_TvSeries PRIMARY KEY,
  TvmazeShowId INT NOT NULL CONSTRAINT UQ_TvSeries_Show UNIQUE,
  Title NVARCHAR(500) NOT NULL,
  NetworkId INT NULL,
  NetworkName NVARCHAR(200) NULL,
  NetworkCountryCode CHAR(2) NULL,
  NetworkTimeZone NVARCHAR(100) NULL,
  WebChannelId INT NULL,
  WebChannelName NVARCHAR(200) NULL,
  WebChannelCountryCode CHAR(2) NULL,
  WebChannelTimeZone NVARCHAR(100) NULL,
  Status NVARCHAR(50) NULL,
  PremiereDate DATE NULL,
  PosterUrl NVARCHAR(2048) NULL,
  SourceUrl NVARCHAR(2048) NOT NULL,
  ImdbId VARCHAR(20) NULL,
  TheTvdbId INT NULL,
  TvRageId INT NULL,
  RawMetadata NVARCHAR(MAX) NOT NULL CONSTRAINT CK_TvSeries_JSON CHECK(ISJSON(RawMetadata)=1),
  FirstSeenAt DATETIME2 NOT NULL CONSTRAINT DF_TvSeries_First DEFAULT SYSUTCDATETIME(),
  LastSeenAt DATETIME2 NOT NULL CONSTRAINT DF_TvSeries_Last DEFAULT SYSUTCDATETIME(),
  CONSTRAINT CK_TvSeries_Show CHECK(TvmazeShowId>0)
 );
 IF OBJECT_ID('dbo.TvEpisodes','U') IS NULL
 CREATE TABLE dbo.TvEpisodes (
  ID INT IDENTITY(1,1) CONSTRAINT PK_TvEpisodes PRIMARY KEY,
  TvmazeEpisodeId INT NOT NULL CONSTRAINT UQ_TvEpisodes_Episode UNIQUE,
  SeriesId INT NOT NULL CONSTRAINT FK_TvEpisodes_Series REFERENCES dbo.TvSeries(ID),
  SeasonNumber INT NULL,
  EpisodeNumber INT NULL,
  Title NVARCHAR(500) NULL,
  EpisodeType VARCHAR(50) NULL,
  IsSpecial BIT NOT NULL,
  Airdate DATE NULL,
  Airtime TIME(0) NULL,
  StartsAtUtc DATETIME2 NULL,
  IsDateOnly BIT NOT NULL,
  RuntimeMinutes INT NULL,
  ImageUrl NVARCHAR(2048) NULL,
  SourceUrl NVARCHAR(2048) NOT NULL,
  RawAirdate NVARCHAR(50) NULL,
  RawAirtime NVARCHAR(50) NULL,
  RawAirstamp NVARCHAR(100) NULL,
  RawMetadata NVARCHAR(MAX) NOT NULL CONSTRAINT CK_TvEpisodes_JSON CHECK(ISJSON(RawMetadata)=1),
  FirstSeenAt DATETIME2 NOT NULL CONSTRAINT DF_TvEpisodes_First DEFAULT SYSUTCDATETIME(),
  LastSeenAt DATETIME2 NOT NULL CONSTRAINT DF_TvEpisodes_Last DEFAULT SYSUTCDATETIME(),
  CONSTRAINT CK_TvEpisodes_Id CHECK(TvmazeEpisodeId>0),
  CONSTRAINT CK_TvEpisodes_Time CHECK(IsDateOnly=0 OR (Airtime IS NULL AND StartsAtUtc IS NULL))
 );
 IF OBJECT_ID('dbo.TvTrackedSeries','U') IS NULL
 CREATE TABLE dbo.TvTrackedSeries (
  ID INT IDENTITY(1,1) CONSTRAINT PK_TvTrackedSeries PRIMARY KEY,
  TvmazeShowId INT NOT NULL CONSTRAINT UQ_TvTrackedSeries_Show UNIQUE,
  SeriesId INT NULL CONSTRAINT FK_TvTrackedSeries_Series REFERENCES dbo.TvSeries(ID),
  ExpectedTitle NVARCHAR(500) NOT NULL,
  Enabled BIT NOT NULL CONSTRAINT DF_TvTrackedSeries_Enabled DEFAULT 1,
  LastAttemptAt DATETIME2 NULL,
  LastSuccessAt DATETIME2 NULL,
  LastError NVARCHAR(1000) NULL,
  EpisodeCount INT NULL,
  LastIssueCount INT NULL,
  CacheExpiresAt DATETIME2 NULL,
  RetryAfter DATETIME2 NULL,
  LeaseUntil DATETIME2 NULL,
  LeaseToken UNIQUEIDENTIFIER NULL,
  CachedShow NVARCHAR(MAX) NULL CONSTRAINT CK_TvTrackedSeries_ShowJSON CHECK(CachedShow IS NULL OR ISJSON(CachedShow)=1),
  CachedEpisodes NVARCHAR(MAX) NULL CONSTRAINT CK_TvTrackedSeries_EpisodesJSON CHECK(CachedEpisodes IS NULL OR ISJSON(CachedEpisodes)=1),
  CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_TvTrackedSeries_Created DEFAULT SYSUTCDATETIME(),
  CONSTRAINT CK_TvTrackedSeries_Show CHECK(TvmazeShowId>0)
 );
 IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.TvEpisodes') AND name='IX_TvEpisodes_Schedule')
  CREATE INDEX IX_TvEpisodes_Schedule ON dbo.TvEpisodes(Airdate,SeriesId) INCLUDE(SeasonNumber,EpisodeNumber,StartsAtUtc,IsDateOnly,IsSpecial);
 IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.TvTrackedSeries') AND name='UX_TvTrackedSeries_Series')
  CREATE UNIQUE INDEX UX_TvTrackedSeries_Series ON dbo.TvTrackedSeries(SeriesId) WHERE SeriesId IS NOT NULL;
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
```

## Exact seed script

```sql
-- Explicit IDs only. Matched tracking rows retain Enabled and all existing settings.
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 MERGE dbo.TvTrackedSeries WITH(HOLDLOCK) AS t
 USING (VALUES
  (83073,N'Avatar: Seven Havens'),
  (64950,N'VisionQuest'),
  (45039,N'Slow Horses'),
  (33352,N'The Lord of the Rings: The Rings of Power'),
  (90632,N'Line of Fire (2026, NBC)')
 ) AS s(TvmazeShowId,ExpectedTitle) ON t.TvmazeShowId=s.TvmazeShowId
 WHEN NOT MATCHED THEN INSERT(TvmazeShowId,ExpectedTitle) VALUES(s.TvmazeShowId,s.ExpectedTitle);
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
```


## Entertainment browsing

Movies and TV retain their individual release/episode schedules, and TV retains
tracked-series browsing. The combined My Week agenda has been removed. Watchlist, Watch Later and
episode-progress controls were removed at the user's request; normal source,
retailer and video playback links remain. See
[personal planning](interface.md#personal-planning-and-discovery--october-19-2026).

## Where to watch

Entertainment → Where to watch (/watch) is the approved typed-search exception.
Movie/TV tabs, popular titles and common genres also work without typing. Movie
release cards link to title lookup; a search result is not a verified title match.
Titles open in a native modal with Close/Escape/focus return or a normal detail
page with JavaScript disabled. Search uses GET links/forms with at most 120 query
characters and ten pages. No local database/schema or collector change is needed.

TMDB API requests are server-side, using the existing Movies:AccessToken setting.
Never expose that credential in HTML or JavaScript. A bounded 512-entry in-memory
cache retains listing data for 15 minutes and availability for six hours; transient
listing failures are cached for 30 seconds. Up to four upstream requests run at
once. Availability checked time is the fetch time, not a promise of provider
catalog freshness. Missing credentials/provider failures show a retry message.

US offers are separated into Subscription, Free, Free with ads, Rent and Buy.
TV availability is series-level and can vary by season. Empty offers are described
as no options listed, not proof of unavailability everywhere. TMDB does not supply
full streaming deep links; use the returned HTTPS TMDB viewing-options link.
Provider names are plain text; no invented service title links or prices.

TMDB logo/API notice and JustWatch attribution are included under Data credits.
Sources: [TMDB watch providers](https://developer.themoviedb.org/reference/movie-watch-providers),
[TV watch providers](https://developer.themoviedb.org/reference/tv-series-watch-providers),
[TMDB attribution FAQ](https://developer.themoviedb.org/docs/faq).

Pending local feedback update: My services offers current US provider checkboxes
(maximum 20). Browser-local selections survive visits when storage is available;
plain GET selection also works without JavaScript. Movie/TV tabs, search, genres
and paging preserve selection. Verified subscription/free/ad-supported matches
rise to the top of each page with service badges. Rental/purchase-only access
never counts as included access. This does not filter or rerank the entire TMDB
catalog; nonmatches stay visible. Service list cache: twelve hours; per-title US
access cache: six hours (30 seconds for failures). Base cached listing order is
not mutated. Deployment is on hold pending further user feedback.

Deployment status: the service-preferences feedback update above was deployed
on October 20, 2026 after the user lifted the hold. See release history for image
and verification details.

Pending local update: theater/ticketing-only providers are excluded from Where to
watch service choices, matching and displayed offers. Fandango ticketing (provider
60) is excluded; Fandango At Home, its free service, AMC/AMC+ television services
and Cinemax remain eligible. Legacy ticketing selections are ignored. This does
not change the separate movie-release calendar or its ticketing actions.

Pending local Settings change: My services has moved from the lookup page to
Settings → My services. Where to watch retains a management shortcut. Browser
preferences and query-based matching are preserved. Settings sections can be
reordered independently of homepage sections. This update is not deployed yet.

Deployment status (October 20, 2026): Settings services, section ordering and the
theater-only provider exclusion are now live. Earlier pending/local-only notes
are superseded. See release history for verification and rollback details.

### Add a Where to watch TV title to tracked series

Open a TV title and select Add to tracked series. The public /tv/track page needs
no sign-in: confirm the matching title/premiere year/network in search results,
then select Follow. This adds to the site's shared tracked-series list. Already
tracked results show Following; disabled tracking can be re-enabled through this
flow. Movies have no tracking button. Existing admin management/removal stays
protected. A TVmaze ID is verified before writing; a TMDB ID is never substituted.
Opening/searching does not add a show. Following requires JavaScript. Episodes
appear after the separate scraper's next normal update, not immediately.

### Active series in TV browsing

Where to watch TV browse/genre pages exclude ended/canceled shows through TMDB's
upstream status filter (returning, planned, in production or pilot). The filter
applies across pages before service prioritization. Title search includes all
statuses, so ended shows remain findable. This uses provider classification rather
than show age and retains the normal 15-minute listing cache. Movies, direct title
details, episode schedules and existing tracked-series records are unaffected.
Reference: [TMDB TV discovery](https://developer.themoviedb.org/reference/discover-tv).

### Sort this page

The Where to watch selector orders the current loaded results page: Your services
first (default), Title A–Z, Newest first or Most popular. The default alphabetizes
within matched and remaining groups; with no selections all loaded titles are
alphabetical. Newest uses movie release dates/TV premiere dates, and popular uses
TMDB popularity; missing metadata sorts last. Service badges remain informational
in other modes. The GET sort query survives search, tabs, genres and paging.
This does not reorder the entire provider catalog across pages.
