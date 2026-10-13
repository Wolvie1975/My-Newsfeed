# Personal features and same-day results

Home combines today's sports fixtures, episode premieres and movie releases under
Today & Tonight. Confirmed times come first; date-only and TBD entries follow.
Existing news, movie, episode and video sections remain available.

My teams (/teams) shows the last five collected results and next ten fixtures for a
chosen team. Favorite choices stay in browser localStorage and are shared across
this browser's tabs. The My teams filter on Home, Sports Events and Results filters
sports rows in the current view; Home's television and movie entries remain visible.
The status line reports matching games in that view. The individual team page can
find fixtures outside the first schedule batch. College school names are matched
to explicit known ESPN aliases; professional team names use exact name matching.

Saved articles can be marked read or unread and filtered accordingly. Older saved
records start as unread. Favorites and read status are not synchronized to other
devices and disappear if site storage is cleared. Blocked storage leaves the
server-rendered pages usable without preference controls.

Movies and Videos use batches of 50 with Previous/Next controls. Changing channel
starts on its first page. Out-of-range page numbers fall back to the last available
page. Query ordering includes database IDs for deterministic pagination; newly
imported records can shift page boundaries between requests.

Results defaults to today in the display timezone (Central in production). Previous
and Next navigation retains the selected follow. Today's visible Results tab reloads
every two minutes; background tabs do not reload. The background score service checks
every ten minutes, with Results:SyncIntervalHours defaulting to one sixth of an hour.
Finals become visible after ESPN reports them and the next successful sync completes.
Games are grouped by their start date; a game ending after midnight stays with the
previous day's starts and can be reached with Previous day.

The existing results sync uses a process-wide gate to prevent overlapping admin and
background runs within this single-instance application. Multiple application
instances would require database-level coordination instead. The Rust scraper still
owns sports fixtures; no calendar integration or alternate fixture importer is added.

Admin → Schedule coverage lists schedule feeds/sports, matching enabled results
follows, missing coverage, unmapped sports and sync errors. It does not enable feeds
or follows automatically. Pending protected-delete and assignment fixes are included.
Category form submission, retired schedule filter parameters and empty-feed health
classification have also been corrected.

Deployment requires no additional schema migrations beyond the previously installed
ScheduleStatus column. Preserve existing environment configuration and set
Results__SyncIntervalHours=0.16666666666666666 for the ten-minute refresh interval.

## Deployment verification

The release is deployed to mynewsfeed-web with existing runtime settings preserved.
All public routes return HTTP 200; unauthenticated coverage access redirects to login.
Authenticated coverage and protected-delete controls passed browser checks. Favorites,
read/unread state, historical results and pagination passed live browser checks.
376 automated tests passed before deployment, and mobile/desktop layout checks passed.
The container is healthy. The rollback image is mynewsfeed-web:before-personal-features.

## Interface revision

Team favorites and the My teams page have been removed. The public pages show
all followed leagues and retain their schedule/sport/league dropdowns. Results
always shows today, ignores retired day parameters, and no longer offers previous-day
navigation. Ten-minute score collection and automatic Results refresh remain enabled.
Saved-article reading status and movie/video pagination are unchanged.

Sports Events and Results group games by league within each calendar date.
League headings are alphabetical; games run from earliest confirmed start to latest,
with TBD schedule starts last. Results rows display Central start times.
