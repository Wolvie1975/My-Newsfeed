# MyNewsFeed

ASP.NET Core / Blazor newsfeed with sports schedules, results, standings and polls;
movie releases, television episodes, videos, saved articles and administration.
The web app and Rust scraper share SQL Server data but have separate collection
and deployment responsibilities.

| Guide | Contents |
| --- | --- |
| [Deployment](DEPLOY.md) | Database prerequisites, Docker rollout, rollback, runtime settings and private HTTPS/PWA |
| [Sports](docs/sports.md) | Schedules/results, team records, F1, standings, Top 25 and SQL read contracts |
| [Entertainment](docs/entertainment.md) | Movies, digital releases, TVmaze, box-office actuals and forecasts |
| [Interface and administration](docs/interface.md) | Settings, appearance, news filtering, F1 News, saved articles, accessibility and admin protections |
| [Maintenance](docs/maintenance.md) | Review findings, completed repairs, remaining work and documentation provenance |
| [Release history](docs/release-history.md) | Historical verification, release tags and recorded rollback images |

College Sports has a shared sport selector across Schedule, Results, Standings
and Rankings. Football, men’s basketball, women’s volleyball and women’s soccer
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
dotnet build MyNewsFeed.sln
dotnet test MyNewsFeed.sln
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
