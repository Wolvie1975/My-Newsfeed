# Compact rankings layout

Deployed October 13, 2026 as mynewsfeed-web:rankings-layout-20261013.
Default columns are Rank, Team, Record and Change on all screen sizes.
The native checkbox Show full poll details reveals Previous, Points and
First-place votes, when present. About this poll contains the period,
season phase, refresh timestamp and source attribution/link. Season and
poll date remain visible; stale/error warnings remain visible. All teams,
Big 12 highlights and provider values remain available.

Mobile cells and logos use compact spacing, team names can wrap, and
the team column is no longer sticky. Expanded tables retain horizontal
scrolling. Both disclosure controls work without JavaScript.

All 10 rankings tests passed. Authenticated production settings and the
key volume were preserved. Browser checks passed for all three enabled
polls (25 teams each) in both themes at 320/390/768/1280 pixels. Compact
tables fit at 390 pixels and above; there is no page overflow at any
checked width. Expanded columns, unchanged team data, metadata disclosure
and no-JavaScript operation were verified. Mobile screenshots were
visually inspected. The broader public theme regression and HTTP checks
passed after deployment.

Rollback: mynewsfeed-web:before-rankings-layout. The latest tag points
to the deployed release. No scraper settings or ranking data changed.
