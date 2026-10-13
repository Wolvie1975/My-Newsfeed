# Standings frontend

The public /standings page reads scraper-owned SportsStandingsSources and SportsStandings.
It makes no provider HTTP requests and performs no database writes. Both desktop and
mobile navigation include Standings next to Results. Source selection is retained in
?sourceId= and defaults to the alphabetically first enabled source for invalid or
disabled IDs. Enabled empty sources stay in the dropdown.

The frontend uses the contract in my-newsfeed-standings-frontend.md. SQL BIGINT
standing IDs are long; ranks are nullable and never derived from Position. Only
IsCurrent rows from the selected enabled source are visible. Provider groups remain
separate, with Position determining row order. NWSL retains the full league while
highlighting Kansas City Current. Big 12 uses positional statistic keys and preserves
published column order and repeated labels. Missing statistics show an em dash.

Published titles and seasons are retained. Last-refreshed timestamps use the existing
Central-time helper. Old snapshots remain visible with a freshness warning, and raw
errors are restricted to server logs. Wide tables scroll within a keyboard-focusable
region; team names remain sticky and readable without logos. Notes and statistic
values use normal HTML encoding; links and logos accept HTTP(S) only.

## Prerequisites before real-data preview or deployment

1. Obtain sports-standings.md and sql/standings.sql from the Rust scraper repository.
   They are referenced by the handoff but are not present in this web checkout.
2. Apply the scraper-owned migration using its documented deployment order.
   The web app does not invent or initialize the schema.
3. Register and enable the eight standings sources, deploy the updated scraper,
   and run its standings collection. Confirm the hourly scheduler refreshes them.
4. Grant the web database principal SELECT on SportsStandingsSources and SportsStandings
   if it does not already have access.
5. Start the local preview, check each source's groups/statistics/season and empty or
   failure states against the provider snapshots, then deploy the web image.

If the tables are missing, the page displays an availability message and Try again,
with the error logged server-side. Existing public and admin pages keep working.
No production deployment or scraper configuration is performed by this feature change.

## Validation

StandingsTests cover enabled/disabled/default selection, empty sources, historical
filtering, BIGINT IDs, group/Position order, sport-specific columns, duplicate Big 12
headers, season spans, invalid JSON, missing statistics, unsafe links, encoded notes,
and freshness states. Query tests use rollback-only fixture tables under separate
names, never the scraper's standings tables. Provider network calls are not used.

The complete suite passes 394 tests. Browser checks confirm the friendly missing-schema
state, navigation links, keyboard focus, and horizontal table scrolling at mobile
and desktop widths without page overflow. The table layout was checked with clearly
labeled fixture data because scraper standings tables are not yet available here.

The scraper migration/import is now verified in the shared database: all eight
enabled sources return their expected snapshot team counts, and the NWSL Current
highlight and real-data mobile/desktop layouts pass browser checks. Frontend
production deployment remains pending.

## Published verification

The frontend is now deployed. All eight standings sources, source-specific weekly
digital lists, Home, Movies, schedule and results passed live checks. The container
is healthy. Existing runtime settings were preserved; rollback image:
mynewsfeed-web:before-standings-digital.

## College season visibility

College sources are shown only when their published season matches the current
calendar/academic season and the stored schedule has reached an opening-game date.
Basketball spans July–June for season-label matching; other supported Big 12 sports
use calendar-year labels. These windows identify season labels, not guessed start
dates: the opening date comes from stored non-canceled fixtures. Old standings
remain stored and are not relabeled. Schedule sport options and games use the same
opening-fixture check. Professional sources are unaffected. Results already restricts
visible games to finished games from today/yesterday.

MLB and WNBA are temporarily hidden from the public Standings page through
Standings:HiddenProviders in appsettings.json during playoffs. Remove their provider
keys from that setting to show them again. Scraper source enablement and collected
rows are unchanged, and their schedules/results remain available. Hidden source IDs
fall back to a visible source.
