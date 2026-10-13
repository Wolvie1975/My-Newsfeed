# Frontend and admin optimizations

Deployed October 13, 2026 as mynewsfeed-web:frontend-admin-optimizations-20261013.

Admin header and navigation headings now render directly in Razor. The
JavaScript admin DOM-rewriting observer was removed. A ShareButton
component replaces hidden CopyLinkButton scaffolding; saved cards build
share controls directly. Obsolete copy handlers, icons and CSS were removed.

The public observer initializes only newly added controls. Saved articles
are cached in memory, updated after successful writes and invalidated by
cross-tab storage changes or storage clearing. Mutating actions clone the
cached articles before writing so failed writes do not alter the cache.
Explicit save/read actions still update all affected controls.

The homepage digital preview uses SQL Take(5), while the full digital
page retains its complete results. GetTodayTimelineAsync excludes VOD
in SQL and skips platform, refresh-metadata and total-count queries.
Movie deduplication and release-variant handling remain shared with the
existing movie page query. Queries remain sequential on one DbContext.

Sources, YouTube feeds, sports event feeds and followed TV series now
use SQL count/Skip/Take paging with 25-row pages and stable ID tie breakers.
Source category changes reset paging; deletion/reloads clamp the current
page to the remaining last page. Page changes clear pending deletions.

Validation: all 419 tests passed, including updated query regressions.
Live browser checks verified homepage timeline parity, saving/removing,
marking read, cross-tab updates/clearing, load-more share initialization,
admin paging/filter reset and navigation with JavaScript disabled.
Four share open/close cycles caused eight saved-storage reads before
and zero afterward. This is a focused browser-work measurement, not
an overall latency benchmark.

Desktop/iPhone/Android/iPad sharing checks and public light/dark checks
passed; health and public HTTP smoke checks passed. Runtime environment
and persistent key volume were preserved. Latest points to this release;
rollback: mynewsfeed-web:before-frontend-admin-optimizations.
