# Where to watch handoff

Updated October 25, 2026 (America/Chicago). These are recorded observations;
verify current repository and deployment state before making further changes.

## Latest: Featured rows and search results tidy — October 25, 2026

The user approved the rows mockup (https://claude.ai/artifact/PXei2FLrtRijKXiJ6YCHnw):
Featured is the default view with four rows of up to 12 titles, and the services row
mixes movies and TV. From a results-page review they chose a Best match default for
searches and tidier links/headings. They declined folding Rent/Buy and merging
"with ads" tiers; don't revisit those unless asked. Both deployed this session with
the user's approval:

- `mynewspaper-web:watch-featured-20261025` (`9c16d8f0b3ab`), then
  `mynewspaper-web:watch-search-20261025` (`5b88f6d4b6fb`), the current release.
- Rollback for the latest: `mynewsfeed-web:before-mynewspaper-watch-search-20261025`
  (= the Featured release); previous containers retained.
- Details, checks and limits: release history entries for both releases.
- Browser matrix helper: `/tmp/mynewspaper-watch-flow/check.cjs` (BASE/OUT env). It
  waits for network idle after in-page navigation to avoid Blazor URL-before-DOM races.

No outstanding Where to watch request. Service hyperlinks remain deferred.

## Earlier: flow redesign deployed October 25, 2026

The user asked for suggestions to improve the page's look and flow, kept the search
results layout as is, and approved: Movies | TV toggle, instant sort, service logo
badges on browse posters, selected-service logos with an "Only my services" filter,
a smaller phone header and a tighter desktop row. They asked to **remove genre
browsing altogether** rather than convert it to chips. Deployment was authorized for
this session only.

- Release: `mynewspaper-web:watch-flow-20261025` (`a85358c2c77e`), healthy.
- Rollback image: `mynewsfeed-web:before-mynewspaper-watch-flow-20261025`; previous
  container retained as `mynewsfeed-web-before-mynewspaper-watch-flow-20261025`
  (it ran `watch-compact-20261024`).
- Details, verification and limits: release history → “Where to watch flow”;
  behavior: `docs/entertainment.md` → “Where to watch flow — October 25, 2026”.
- The "My services" filter is URL-only (`only=1`), not remembered across visits.
  Offer persistence if the user wants it.
- About 200 stopped `mynewsfeed-web-before-*` containers have accumulated.
  Cleanup was not requested; ask before removing any.

### Streaming-style rows

Superseded: approved and deployed; see the latest section above.

## Earlier: compact layout (October 24, 2026)

## User direction

The user likes the existing title-search results layout. Preserve its poster,
title and grouped provider rows. The requested work focused on making the search
and browse controls compact. Implementation and deployment are complete.

The user considered linking service logos/names directly to the title on each
service to avoid extra clicks, then explicitly deferred that decision. Do not
implement service hyperlinks until the user revisits it.

## Implemented and deployed

- Search, My services settings link and browse controls share one compact panel.
- Content type and genre sit side by side above 600px and stack on phones.
- Search field and button stack at widths of 360px and below.
- The results note and sorting form share a wrapping toolbar.
- Existing browse poster cards, search provider rows, modal details, query
  parameters and browser-local service preferences retain their behavior.

Files changed for the compact layout:

- `src/myNewspaper.Web/Components/Pages/Watch.razor`
- `src/myNewspaper.Web/wwwroot/public.css`
- `docs/entertainment.md`
- `docs/release-history.md`

## Deployment and rollback

- Container: `mynewsfeed-web`.
- Release image: `mynewspaper-web:watch-compact-20261024`.
- Image ID: `sha256:cbe552dcd9b66754e0592ab8d9e187371ff9004d464be6954336fba057bd24dd`.
- Rollback image: `mynewsfeed-web:before-mynewspaper-watch-compact-20261024`.
- Retained previous container:
  `mynewsfeed-web-before-mynewspaper-watch-compact-20261024`.
- Host port 8085, container port 8080, network `dev-bridge`.
- Live checks used `http://172.18.0.5:8080`; inspect the container before reusing
  that address because container IPs can change.

Container health was healthy and `/healthz` returned 200. Environment, persistent
mounts, port bindings, network mode, health check and restart policy were compared
with the prior container and preserved. No database migration, collector update
or credential change was made. Follow `DEPLOY.md` for future operations; keep
runtime configuration private and never print full environment/connection data.

## Validation performed

- Docker build, publish and Blazor static-asset manifest guard passed.
- All 27 watch-related .NET tests passed for this deployment.
- Earlier in this session, before the compact markup/CSS changes, the full suite
  passed all 700 tests. This was not a full-suite rerun of the compact release.
- Preview and deployed build each passed 16 combinations: widths 320, 390, 430
  and 1440px; light/dark themes; JavaScript enabled/disabled.
- Exercised movie/TV browsing, genre selection, title search, sort submission,
  refresh, clear search and service query preservation; checked no horizontal
  overflow and controls at least 44px tall.
- Inspected phone screenshots at all three required widths and desktop images.
- Additional preview/live checks passed at 768px in both themes, including
  browser-local saved-service restoration, refresh and details modal/Escape.
- Main route checks passed: Home, News, Events, Standings, Rankings, Movies,
  Box Office and Watch returned 200. Results, Movies/digital and unauthenticated
  Admin returned expected redirects.
- `git diff --check` passed.

Limits: real-device checks were unavailable. Live browser checks used direct
container HTTP; this release did not separately verify the tailnet HTTPS entry.
Existing System.Security.Cryptography.Pkcs dependency advisories remain.

## Evidence and helper scripts

Temporary evidence is local and may disappear; it is not committed:

- Preview: `/tmp/mynewspaper-watch-compact/`.
- Deployment/live: `/tmp/mynewspaper-watch-compact-deploy/`.
- Live browser log: `live-check.log`; screenshots: `live/`.
- Deployment helpers: `build.cjs`, `deploy.cjs`, `verify.cjs`.
- Browser helpers: `check.cjs`, `preferences.cjs`.

Runtime JSON files in the deployment evidence contain private settings. Do not
commit, publish or print them. Inspect only specific nonsecret fields as needed.

## Git status at handoff

Repository: `/workspaces/DotNet_Projects/My-Newsfeed`, branch `develop`.
HEAD: `c4a2cbe` (`Add watch provider results and branded article fallbacks`).
Local HEAD and the locally recorded `origin/develop` currently match; no fresh
fetch was performed for this handoff. The initial push attempt in this chat failed
because HTTPS credentials were unavailable; GitHub CLI was not installed.

The October 24 compact layout and all three October 25 releases (flow, Featured,
search tidy) are uncommitted; commit/push has not been requested. This handoff adds documentation only. Review current `git status`
before committing; preserve all existing changes.

## Deferred service-link decision

TMDB's current watch-provider integration supplies provider IDs, names and logos
plus one regional TMDB viewing-options URL, not individual service title URLs.
`WatchResultCard.razor` currently renders service logos/names without links and
uses the validated regional URL for “Open viewing options”.

Options discussed, neither selected:

1. Integrate a data source supplying verified direct title links.
2. Use supported service search URLs as a fallback, with the remaining title
   selection step clearly understood.

Do not fabricate direct title URLs or make each logo point to the same intermediate
page while presenting it as a direct service link. Preserve the current results
layout and existing TMDB/JustWatch attribution. Reference:
https://developer.themoviedb.org/reference/movie-watch-providers

## Next steps

The requested compact implementation and deployment have no remaining required
steps. Gather the user's visual feedback on the live layout. Commit/push the
pending work when requested and authentication is available. Revisit service
hyperlinks only when the user is ready. All future rendered changes require the
mobile checks in `AGENTS.md` and `DEPLOY.md`, including post-deployment checks.
