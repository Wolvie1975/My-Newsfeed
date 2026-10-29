# Public Requests alert and read-only overview

October 29, 2026 (UTC). Implements the design in `new development/requests-view-handoff.md`, adapted to the separate public/admin websites.

## Current behavior

Only the household admin's selected profile sees the header alert when waiting requests or locked profiles exist. The mobile menu always offers Requests for that profile. `/requests` lists locked profiles first and up to 20 waiting requests, including requester names, notes, checks and New labels. Counts cover the entire backlog. Mark all seen does not clear the public waiting counter.

This page has no management actions and does not change request, lock or seen state. Management links use the configured absolute admin address and retain the destination through admin login. Admin authentication is independent: signing in there does not replace or redirect the public overview. Existing admin panel actions remain unchanged.

Other selected profiles receive HTTP 404 before the overview query. Devices without an open profile follow the existing edition picker. With profiles disabled, the page returns 404. Database read failures hide the header counter and show a generic page error. No schema migration is needed. Readers who can open a PINless household admin profile can read these notes; set a profile PIN to restrict access.

## Implementation progress

- Added read queries in `src/myNewspaper.Shared/Requests/RequestStore.cs`; the new-since-seen count is independent of the 20-row display limit.
- Added public `RequestsViewLink` and `RequestsView`, header/menu wiring and responsive styles. No shared admin authentication or public management endpoint was added.
- Build succeeds without warnings; all 1,006 existing .NET tests pass. Frontend lint passes.
- Browser verification uses temporary profiles and a real submitted request, restoring the previous household admin profile and removing temporary profiles afterwards.
- Screenshot review identified a cramped 320px masthead; the alert now uses a compact icon/badge with a 44px link target. Final verification and deployment results follow below.

## Deployment and verification

Deployed public image `mynewspaper-web:requests-20261029` (`f057f45e0be1`) to `mynewsfeed-web`, preserving runtime settings, ports and persistent keys. The separate admin app was unchanged. Previous public container retained as `mynewsfeed-web-before-requests-20261029` for rollback. Live HTTPS `/healthz` returns HTTP 200 and Docker reports healthy.

Public address: `https://mynewsfeed-prod.tailbf31bc.ts.net/requests`. Select the household admin's edition first. Admin destinations use `https://mynewsfeed-prod.tailbf31bc.ts.net:8443/admin`.

Verification: 1,006 existing .NET tests passed, plus the new database regression test covering more than 20 waiting requests and preserving the seen marker. Five JavaScript unit tests passed; lint, CSS/document formatting and diff whitespace checks passed. Local browser flow and screenshot checks passed at 320, 390, 430, 744, 1024 and 1440px in light/dark. Live page assertions and 36 screenshots cover those widths, both themes, and ribbon/Kansas/monochrome schemes. Checks exercised a real submitted request, household-admin visibility, non-admin HTTP 404, no forms/buttons inside the overview, absolute admin links and no horizontal overflow. Representative phone screenshots were visually inspected in light/dark and another scheme.

Limits: no physical device check or deliberate production database outage; locked-profile cards, an empty backlog, no-JavaScript navigation and completing the full cross-site login round trip were not separately exercised during this release. Database failures are handled in code. The browser cleanup's admin switch needed separate verification; cleanup results are recorded below.

Cleanup verified with a database check: Jason is restored as the household admin; all temporary `zz Requests check` profiles and their requests were removed. The cleanup helper was removed from the test suite. No real profiles or requests were deleted.
