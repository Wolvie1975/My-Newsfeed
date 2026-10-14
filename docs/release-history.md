# Release history and verification

Historical evidence consolidated October 13, 2026 (Central), from the former individual
handoffs and deployment reports. Counts are observations for their original checks,
not permanent acceptance totals. Recorded rollback tags describe those deployments;
verify an image still exists before using it. Runtime configuration and persistent
keys were preserved by the documented releases unless otherwise stated.

The most recent verified web release is `mynewsfeed-web:movie-section-posters-20261013`,
with rollback recorded as `mynewsfeed-web:before-movie-section-posters`. Its container
was healthy on October 13, 2026 (Central). This is a dated observation, not live
hosting status. The earlier documentation consolidation did not deploy application
code, alter the database or restart scraper collection.

## Sports releases

October 9 professional schedules provisioned nullable ScheduleStatus and enabled
Kansas City Current, WNBA and MLB on sql2025 / WebScraper. Health, schedule selection
and Results returned 200. No scraper scheduler settings changed in that web rollout.
The initial full-season provider audits (Current: 30 games; WNBA: 378 in 2026 and 705
in its earlier broader refresh) predated upcoming-only refinements and do not describe
current import counts.

Standings/Digital integration passed 394 tests and browser checks after schema/import
verification. Later Top 25/MLS work passed 411 tests with five stored polls, 125 ranking
entries, 23 Big 12 markers and 30 MLS teams in two conferences. Subsequent basketball
visibility revisions hid stale-season choices; these historical five-poll checks do
not promise five visible current choices. Compact standings and rankings passed
light/dark, mobile/desktop and no-JavaScript checks (18 standings and 10 rankings tests).

October 13 NFL mapping passed 418 tests: 32 teams in two 16-team conferences, Kansas
City included, 256 statistics matching previous stored values, provider order and
keyboard/mobile scrolling. English soccer passed 430 tests and checks of Championship's
24/EPL's 20 table teams, eight statistics per team, all 115 stored season finals,
Wrexham's 10 matches, filters, paging, both themes and no-JavaScript controls.

The college schedule retention fix passed 441 tests. Deleting past fixture evidence
no longer hides upcoming sports. Live checks showed 54 football and 94 volleyball
games, accessible basketball paging, all five sport options and Kansas football,
with and without JavaScript at 390/1440 pixels. `/healthz`, `/results` and `/box-office`
returned 200. No database migration or scraper setting changes were required.

MLS schedule and season results were subsequently deployed as
`mynewsfeed-web:mls-schedule-results-20261013`. All 448 tests passed, including
provider/status/date isolation, home perspective in schedule and Home reads,
latest-season selection, last-good retention, stable pagination, encoded/safe
rendering and shootout winner/goal separation. Production already had enabled MLS
feed 5525, standings source 9 and both nullable shootout columns, so no web migration,
grant, database write or scraper configuration changes were needed.

Live checks at 390/768/1440 pixels in both themes, with and without JavaScript,
verified 90 MLS fixtures, 421 results, source links, paging and daily-results entry.
All 421 match links were reachable across 17 pages without duplicates. MLS's 30-team
standings, Championship's 115 season finals and 54 college football fixtures remained
accessible. The MLS mobile screenshot was visually inspected. Health and affected
public routes returned 200; runtime settings, network, ports and key volume were
preserved. The previous image/container remain available for rollback. No production
MLS penalty finals were present; shootout behavior was verified with test fixtures.

## Entertainment releases

Movie Insider validation for October 5–11 found ten confirmed digital releases;
When To Stream supplied five digital and nine subscription observations. These
were provider snapshots, not fixed page totals. The former weekly bounds were later
replaced with today-forward coverage. Movie rollout used the existing scraper schema
and read-only application schema check.

Deadline/Variety news originally had 20 stored stories, publisher separation and
no-JavaScript paging checks. That `/box-office` news mode was superseded October 13
by structured The Numbers results; news stories are not weekend financial chart data.

The top-ten release passed 433 tests, including the application's read credentials,
ten ranks with one weekend, nullable changes and New Year dates. Browser checks at
390/768/1440 pixels with and without JavaScript verified links, attribution, currency,
no overflow and no JavaScript errors. The chart displayed October 9–11, 2026.

The forecasts release passed 437 tests covering source health, expired-date filtering,
precision and provider ranks. Live checks found 12 future-film forecasts, ten actuals
and a healthy unpublished-preview state across three widths, both themes and both
JavaScript modes. Every forecast had attribution/as-of information; long-range ranks
were not invented. Production health and related route checks passed.

Movie-section posters were deployed as `mynewsfeed-web:movie-section-posters-20261013`.
All 451 tests passed, including safe image URLs, encoded title alt text and missing
poster rendering. Chart/forecast database queries successfully read their added
nullable columns. Browser checks passed on Box Office, Movies, Digital and Home
at 390/768/1440 pixels in both themes: poster slots, accessible alt text, successful
HTTP responses and no horizontal overflow. The chart/forecast page had 22 portrait
slots; a sampled loaded poster was 800×1200 pixels. Forced external image failure
revealed neutral artwork and accessible unavailable text; missing release placeholders
remained visible. The mobile chart screenshot was inspected. All poster sources
remain external; lookup diagnostics are not exposed. Runtime settings and persistent
keys were preserved, and no scraper settings, schema or content were changed by the
web rollout. Rollback: `mynewsfeed-web:before-movie-section-posters`.

## Interface and audit releases

The original codebase audit had 362 passing tests. Protected sports/video ownership
repairs raised coverage to 364. The personal-features/audit release passed 376 tests;
its favorites/My teams controls were later removed, while saved reading status,
paging and serialized result synchronization remained.

News cards initially moved dates into a footer row, then below headlines. Both
changes passed all 19 article-rendering checks. Admin CSS/navigation changes passed
seven authenticated page checks at 390/768/1024/1440 pixels. Admin Home was corrected
to `/admin`, followed by public/admin theme isolation. Later optimization replaced
DOM-rewritten admin structure with Razor markup, added 25-row SQL admin paging and
saved-state caching; all 419 tests passed. Four share open/close cycles changed
saved-storage reads from eight to zero in that focused browser measurement, not an
application-wide latency benchmark.

Sharing evolved from multiple copy actions to one Share dialog with Copy link,
phone-only SMS and supported native sharing. Desktop/iPhone/Android/iPad profiles,
focus/dismissal and mocked native APIs passed; physical app delivery was not tested.
The homepage VOD exclusion passed all 20 personal-feature tests and live timeline
comparison while keeping the separate upcoming digital preview.

The original public UI review covered 78 route/theme/width combinations. Its seven
main recommendations were implemented in the polish release, passing 430 tests and
15 public-view checks. Mobile first-table positions improved: Events 655→569 pixels,
NFL 619→498, Rankings 634→480, Wrexham results 703→547. Counts, heading focus, grouped
navigation, 44-pixel actions, Saved recovery and movie labels/filters were verified.
Lower-priority suggestions remain in [Interface](interface.md).

## Recorded images and rollback tags

The following inventory preserves image names from individual reports. “latest points
to this release” statements in those files applied at that time; they are superseded
by later rollouts. Some early updates patched static assets; subsequent full builds
included current source. Rollback must always use a compatible preserved runtime.

| Original report | Recorded image tags |
| --- | --- |
| deployment/MOVIES.md | `mynewsfeed-web:before-movies` |
| docs/admin-layout-deployment.md | `mynewsfeed-web:admin-layout-20261013`, `mynewsfeed-web:before-admin-layout`, `mynewsfeed-web:admin-home-20261013`, `mynewsfeed-web:before-admin-home`, `mynewsfeed-web:dark-scope-20261013`, `mynewsfeed-web:before-dark-scope` |
| docs/article-sharing-deployment.md | `mynewsfeed-web:article-sharing-20261013`, `mynewsfeed-web:before-article-sharing`, `mynewsfeed-web:share-menu-20261013`, `mynewsfeed-web:before-share-menu-cleanup`, `mynewsfeed-web:unified-share-20261013`, `mynewsfeed-web:before-unified-share` |
| docs/box-office-frontend.md | `mynewsfeed-web:before-box-office` |
| docs/boxoffice-forecasts-deployment.md | `mynewsfeed-web:boxoffice-forecasts-20261013`, `mynewsfeed-web:before-boxoffice-forecasts` |
| docs/boxoffice-top-10-deployment.md | `mynewsfeed-web:boxoffice-top10-20261013`, `mynewsfeed-web:before-boxoffice-top10` |
| docs/digital-vod-frontend.md | `mynewsfeed-web:before-standings-digital` |
| docs/english-soccer-deployment.md | `mynewsfeed-web:english-soccer-20261013`, `mynewsfeed-web:before-english-soccer` |
| docs/frontend-admin-optimizations.md | `mynewsfeed-web:frontend-admin-optimizations-20261013`, `mynewsfeed-web:before-frontend-admin-optimizations` |
| docs/homepage-no-vod-deployment.md | `mynewsfeed-web:homepage-no-vod-20261013`, `mynewsfeed-web:before-homepage-no-vod` |
| docs/my-newsfeed-nfl-standings.md | `mynewsfeed-web:nfl-standings-20261013`, `mynewsfeed-web:before-nfl-standings` |
| docs/news-card-formatting-deployment.md | `mynewsfeed-web:news-card-formatting-20261013`, `mynewsfeed-web:before-news-card-formatting`, `mynewsfeed-web:headline-date-20261013`, `mynewsfeed-web:before-headline-date` |
| docs/personal-features.md | `mynewsfeed-web:before-personal-features` |
| docs/professional-schedule-deployment.md | `mynewsfeed-web:before-professional-schedules`, `mynewsfeed-web:college-schedule-fix-20261013`, `mynewsfeed-web:before-college-schedule-fix` |
| docs/public-ui-polish-deployment.md | `mynewsfeed-web:public-ui-polish-20261013`, `mynewsfeed-web:before-public-ui-polish` |
| docs/rankings-layout-deployment.md | `mynewsfeed-web:rankings-layout-20261013`, `mynewsfeed-web:before-rankings-layout` |
| docs/standings-deployment.md | `mynewsfeed-web:before-standings-digital` |
| docs/standings-layout-deployment.md | `mynewsfeed-web:standings-layout-20261013`, `mynewsfeed-web:before-standings-layout` |
| MLS schedule/results update | `mynewsfeed-web:mls-schedule-results-20261013`, `mynewsfeed-web:before-mls-schedule-results` |
| Movie-section posters update | `mynewsfeed-web:movie-section-posters-20261013`, `mynewsfeed-web:before-movie-section-posters` |
