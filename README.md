# MyNewsFeed

ASP.NET Core / Blazor newsfeed with sports schedules, results, standings and polls;
movie releases, television episodes, videos, saved articles and administration.
The web app and Rust scraper share SQL Server data but have separate collection
and deployment responsibilities.

| Guide | Contents |
| --- | --- |
| [Deployment](DEPLOY.md) | Database prerequisites, Docker rollout, rollback, runtime settings and private HTTPS/PWA |
| [Sports](docs/sports.md) | Schedule/results ownership, provider identities, standings, Top 25 and SQL read contracts |
| [Entertainment](docs/entertainment.md) | Movies, digital releases, TVmaze, box-office actuals and forecasts |
| [Interface and administration](docs/interface.md) | Navigation, saved articles, sharing, accessibility, paging and admin protections |
| [Maintenance](docs/maintenance.md) | Review findings, completed repairs, remaining work and documentation provenance |
| [Release history](docs/release-history.md) | Historical verification, release tags and recorded rollback images |

These guides consolidate the former individual handoffs and deployment reports.
Current behavior takes precedence over historical proposals and release snapshots.
Scraper `sql/` paths and Rust commands refer to the separate Rss-Rust-Scrape project;
this repository's application migrations are in `db/`.

Build and run the existing checks from the repository root:

```bash
dotnet build MyNewsFeed.sln
dotnet test MyNewsFeed.sln
```

Integration tests use the configured SQL Server database and application credentials;
fixture writes are transactional. Use an appropriate development/test database.
Production configuration is supplied privately through runtime settings; see the
deployment guide before rebuilding a running container.
