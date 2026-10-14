# Maintenance and review notes

Consolidated October 13, 2026 (Central). Review scope is the first-party web app:
public/admin pages, routing/authentication, EF/query helpers, score sync, browser
scripts, PWA and Docker configuration. The earlier audit excluded Rust scraper
implementation and vendored Bootstrap; it was not a penetration or load test.

## Completed audit repairs

| Original finding | Current repair |
| --- | --- |
| Shared sports-event deletion could violate source foreign keys | Protected controls, ownership rechecks and recoverable delete/save rejection; legacy records remain editable |
| Event edits could mismatch provider/feed attribution | Provider/shared ownership is read-only or validated at save |
| Video edits could mismatch channel/feed labels and deletion ownership | Matching-channel choices and save-time validation |
| Overlapping result syncs could race on inserts | Process-wide serialization for admin/background runs |
| Category forms could submit natively during async handlers | Native browser submission prevented |
| Removed schedule controls still applied invisible filters | Retired from/to/q parameters ignored |
| Movie/video caps made later entries unreachable | Deterministic 50-row batches and Previous/Next controls |
| Healthy empty provider schedules appeared failed | Successful empty state separated from never fetched/failed/stale; counts align with public statuses |

These repairs were deployed with the personal-features release; later interface
revisions removed favorites/My teams but retained read/unread and pagination.
The college schedule's past-fixture season gate was subsequently removed and
regression-tested for all five supported sports. MLS schedule and season-results
integration is also now deployed, including shootout handling; see [Sports](sports.md).

## Remaining engineering follow-ups

- Result synchronization's gate is process-local. Multiple web instances need
  database-level coordination or conflict-safe upserts before concurrent deployment.
- YoutubeFeedEdit captures its channel lock when loading. Recheck linked videos in
  the save transaction so an intervening scraper insert cannot invalidate that state.
- Some edit components retain `notFound` across parameter changes. Reset request-specific
  state when navigating from a missing ID to a valid ID of the same component.
- Standings and basketball rankings still require a stored past opening fixture.
  The schedule fix did not remove those gates; upcoming-only retention can make their
  visibility fail for the same reason. Review season/publication visibility independently
  rather than documenting the gates as reliable season evidence.
- Interactive admin submission and overlapping sync scenarios deserve targeted
  browser/concurrency coverage in addition to helper and row-rendering tests.

Current implementation evidence is in EventsQuery, StandingsQuery, RankingsQuery,
ScoresSync and the relevant admin components. The existing two xUnit2031 warnings in
EventsTests remain analyzer warnings, not build failures.

## Big 12 design constraints retained from the expansion review

The October 9 expansion proposal described an earlier Kansas-only schema and is not
an outstanding deployment plan. All 16 member feeds are now enabled and canonical
SportsEvents/SportsEventSources support is implemented. Preserve the following
provider lessons when extending or repairing ingestion:

- Use one five-sport school feed per verified full member, not conference-wide RSS
  alone: broad RSS/JSON requests omitted later fixtures present in school feeds.
  Enrich with bounded monthly metadata covering observed dates.
- Big 12 sport IDs are Football 4, men's basketball 7, women's basketball 15, women's
  soccer 31 and women's volleyball 37. Oklahoma State had no volleyball observations
  in the verified snapshot. Affiliate sport membership is not automatic full membership.
- Official school IDs: Arizona 34, Arizona State 35, Baylor 1, BYU 32, UCF 31,
  Cincinnati 30, Colorado 11, Houston 33, Iowa State 2, Kansas 3, Kansas State 4,
  Oklahoma State 6, TCU 7, Texas Tech 9, Utah 36 and West Virginia 10.
- Link mirrored fixtures only through explicit reciprocal provider game identities,
  validating sport and participants; names/dates alone would collapse doubleheaders.
  Preserve existing public IDs, FirstSeenAt and primary perspective. Reconciliation
  conflicts must be reported rather than resolved by deleting canonical records.
- Canonical identity examples are `big12:pair:<sorted IDs>` and `big12:game:<ID>`.
  School/source attribution belongs in the junction, not repeated canonical rows or
  a last-writer-wins primary feed assignment. Source identity is feed plus ProviderGameId.
- JSON supplies participant/neutral/media enrichment; timed RSS remains the established
  timing source when provider UTC fields disagree. Preserve source calendar day and
  timezone labels; unknown/date-only starts are not midnight confirmed starts.
- Do not treat every RSS “vs” as home. Neutral games need IsNeutral and nullable IsAway.
  Missing venues/logos/TV/watch links remain unknown. Preserve supplied radio/audio and
  nonempty media from either source with provenance.
- Per-feed failures must not erase last-good observations. Missing rows alone do not
  prove cancellation. Self-opponents/unresolved partners need review, not guessed merges.

The historical snapshot had 1,434 school observations and 498 explicit mirror pairs;
these are original review evidence, not permanent totals. Validate isolated staging
migrations/upserts, repeated/concurrent imports, mirrored identities, neutral/TBD cases,
feed enablement preservation and failure isolation before broadening ingestion.

## Documentation consolidation

The seven maintained Markdown files are README, DEPLOY and the five topic guides
linked from README. The former handoffs/reports were reviewed, consolidated and
removed; SQL scripts, images and application source were not removed. Current contracts
replace superseded requests; dated counts and image tags move to release history.
The TVmaze schema and seed SQL were retained verbatim as a reference in Entertainment.

| Former document group | Maintained location |
| --- | --- |
| Professional schedules, NWSL/Current, WNBA, English soccer | Sports; deployment steps in DEPLOY |
| Standings base/NFL/MLS, Top 25 and compact-table guides | Sports |
| Big 12 expansion proposal | Design constraints and historical context here |
| Digital/VOD, movie deployment, TVmaze, box-office handoffs | Entertainment; deployment steps in DEPLOY |
| Personal features, UI review, admin optimization, sharing/card guides | Interface; completed/remaining audit work here |
| Individual deployment reports | Release history |
| PWA/Tailscale deployment | DEPLOY |

Old individual reports may describe multiple consecutive revisions. Resolve conflicts
using current source and the latest verified behavior rather than treating every
historical “latest” tag or “pending deployment” statement as current. Numeric source
IDs and snapshot row counts are environment/date-specific. Scraper-only migrations,
fixtures and reports are identified as belonging to the other repository instead of
being linked to nonexistent local files.
