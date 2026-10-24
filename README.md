# myNewspaper

myNewspaper (formerly MyDailyFeed / MyNewsFeed) is an ASP.NET Core / Blazor daily dashboard with sports schedules, results, standings and polls;
movie releases, television episodes, a separate YouTube section, saved articles
and administration.
The web app and Rust scraper share SQL Server data but have separate collection
and deployment responsibilities.

**Product direction:** “This is a tap to link site and not a typing one.”
Build public browsing around links, cards, tabs, dropdowns and simple selections;
avoid features that require typed input. See the
[standing interaction guideline](docs/interface.md#standing-product-guideline-tap-to-link).

**Required development check:** Every feature and fix must be verified on mobile
before completion and again after deployment. Desktop checks alone are insufficient.
See [mobile acceptance checks](docs/interface.md#required-mobile-verification) and
[project instructions](AGENTS.md).

**Approved exception (October 20, 2026):** Where to watch supports typed movie/TV title search, alongside tap-to-browse genres and popular titles. This does not restore site-wide Search.

| Guide | Contents |
| --- | --- |
| [Deployment](DEPLOY.md) | Database prerequisites, Docker rollout, rollback, runtime settings and private HTTPS/PWA |
| [Sports](docs/sports.md) | Schedules/results, team records, F1, standings, Top 25 and SQL read contracts |
| [Entertainment](docs/entertainment.md) | Movies, digital releases, TVmaze, box-office actuals and forecasts |
| [Interface and administration](docs/interface.md) | Settings, appearance, news filtering, F1 News, saved articles, accessibility and admin protections |
| [Maintenance](docs/maintenance.md) | Review findings, completed repairs, remaining work and documentation provenance |
| [Release history](docs/release-history.md) | Historical verification, release tags and recorded rollback images |

College Sports has a shared sport selector across Schedule, Results, Standings,
Rankings and conference-wide News. Football, men’s basketball, women’s volleyball and women’s soccer
reuse existing coverage; see [the college hub](docs/sports.md#college-sports-hub).

F1 has its own Sports section with Schedule, Results, Drivers/Constructors
standings and publisher News, plus a separate home-page F1 card outside Today & Tonight.
See [F1 coverage and routes](docs/sports.md#f1-section).

Team-record updates (October 18, 2026, Central): Home, Events and Results now show
current team records, including compact overall (conference) totals such as
`12-4 (5-2)` for covered college teams. Home keeps the record tooltips but omits
the long explanatory paragraph. MLS and Championship results also include records. Missing records remain
blank. Records are hidden league-wide during playoffs, including on past results;
see [coverage and freshness](docs/sports.md#team-records-beside-matchups).

Recent production changes (October 17, 2026, Central):

- Home's Today & Tonight hides completed sports events on the next page load once
  a matching final result is stored. Stored fixtures and results are preserved. See [home-page result matching](docs/sports.md#home-page-completed-events).
- Settings → Light and dark mode → **Automatic · Kansas City sunrise/sunset**
  enables dark mode at sunset and light mode at sunrise in Kansas City, Missouri.
  Existing browser preferences are preserved; automatic mode is optional. See
  [automatic appearance](docs/interface.md#automatic-light-and-dark-mode).
- Settings uses a clearer, symmetrical gear icon in the desktop and mobile header.
- News defaults to **Top Headlines**, listed first below All categories, and offers an enabled-source
  selector scoped to the chosen category. Filtering survives reload and pagination.
  See [Top Headlines and source filtering](docs/interface.md#top-headlines-and-news-source-filtering).

These features are deployed. The October 17 Top Headlines collection check
remains a dated [follow-up](docs/maintenance.md#top-headlines-collection-follow-up--october-17-2026),
not a current article-count report. Deployment verification and rollback images
are recorded in [release history](docs/release-history.md).

These guides consolidate the former individual handoffs and deployment reports.
Current behavior takes precedence over historical proposals and release snapshots.
Scraper `sql/` paths and Rust commands refer to the separate Rss-Rust-Scrape project;
this repository's application migrations are in `db/`.

Build and run the existing checks from the repository root:

```bash
dotnet build myNewspaper.sln
dotnet test myNewspaper.sln
```

The solar calculation checks run independently of SQL Server:

```bash
node --test tests/browser/solar-times.test.cjs
```

For the Playwright appearance checks, see the
[interface guide](docs/interface.md#automatic-light-and-dark-mode).

Integration tests use the configured SQL Server database and application credentials;
fixtures use rollback transactions or connection-local temporary tables. Use an
appropriate development/test database.
Production configuration is supplied privately through runtime settings; see the
deployment guide before rebuilding a running container.

Deployed October 19, 2026: homepage and news reading preferences, and a Kansas
City Local section with weather forecasts and icons. See
[personal planning and discovery](docs/interface.md#personal-planning-and-discovery--october-19-2026)
for routes, data scope and browser-local storage behavior.

Sports → [Scoreboard](docs/sports.md#live-scoreboard) now provides sampled live scores,
upcoming games and finals, with a separate homepage card and tap-based date/competition
filters. Grouped Show and Final scores filters, current team records and season-results
links consolidate the former daily Results page; old `/results` bookmarks redirect
to Scoreboard with their date/follow selections. Scores are stored by the independent collector and refresh approximately
every ten minutes; browser reads check every five minutes while visible.

Sports results also support [cached box scores and game-stat modals](docs/sports.md#cached-box-scores-and-game-stat-modals)
when collected detail is available, with ordinary detail-page links as a fallback.

## Project identity

The solution is myNewspaper.sln; the application and test projects are
src/myNewspaper.Web/myNewspaper.Web.csproj and
tests/myNewspaper.Tests/myNewspaper.Tests.csproj. The existing My-Newsfeed checkout
and GitHub repository name are retained for link compatibility. The October 24 rename updates assemblies and namespaces to myNewspaper. Saved
articles, browser preferences, app id/scope and authentication identifiers retain
their existing identities. See docs/interface.md for the new logo and icon assets.
