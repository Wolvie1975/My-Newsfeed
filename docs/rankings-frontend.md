# Top 25 and MLS frontend

/rankings displays the full selected national poll. The dropdown is populated from
all enabled ranking sources, including sources without a current poll. Invalid or
disabled source IDs fall back to the first enabled source, and selection is retained
in ?sourceId=. Both poll and team IsCurrent flags are required. Scraper-owned numeric
membership flags supply Big 12 badges; ESPN team IDs are never resolved through
conference IDs. Ties are retained without renumbering or limiting the table to 25 rows.

Previous rank, newly unranked and unknown previous rank remain distinct. Movement
uses previous minus current rank. Missing points/votes/records remain unavailable,
while real zero values remain zero. All-missing points/votes columns are omitted.
Provider names, records, poll dates, seasons and phase/period labels are retained.
Old polls receive a Latest available poll dated message, separate from scraper
freshness. Basketball's April 2026 / 2025–26 snapshots remain explicitly labeled
on Top 25 as required by the ranking handoff; the college-season hiding rule still
applies separately to schedules and conference standings.

MLS uses the soccer statistic mappings, retains the full East/West conference tables,
and only displays supplied ranks. No MLS schedule or results ownership is changed.

The schema and enabled sources have been verified in the shared database. Queries
are read-only; browser requests do not call providers. Regression tests use separate
rollback-only ranking fixture tables. The full suite passes 411 tests. Browser checks
verify all five polls (125 rows, 23 Big 12 highlights), MLS's 30 teams in two groups,
correct soccer columns and mobile/desktop layout. Frontend deployment is pending.

Pending VOD today-forward and college-season visibility changes are included in this
working tree; production remains on the preceding release until explicitly deployed.

MLS display ordering now sorts points descending within each conference. Equal points
retain provider Position order; unavailable points sort last. Official Rank values
are retained rather than renumbered. Other standings keep their provider order.

Basketball visibility revision: men’s/women’s basketball poll choices are hidden
until a current-season opening fixture date has arrived and the provider has a
current-season poll. Last season’s polls cannot be reached via sourceId. Other
national polls retain their existing full-table and age-label behavior.
