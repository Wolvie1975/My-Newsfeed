# Professional schedule deployment

The schedule reads scraper-owned SportsEvents directly. Kansas City Current uses
espn-nwsl / ProviderSchoolId 20907; league-wide WNBA uses espn-wnba / ProviderSchoolId 0. MLB uses espn-mlb / ProviderSchoolId 0.
Team names and logos come from each event; ESPN participant IDs are never resolved
through Big 12 metadata. Existing school-source observations continue to work.
The existing NWSL results importer is unchanged.

## Deploy in this order

1. Deploy the updated Rust scraper. Run its --configure-nwsl and --configure-wnba
   commands against the shared database. They register disabled feeds and initialize
   the nullable ScheduleStatus column. Alternatively apply db/016_add_schedule_status.sql
   before deploying the web app; it is safe when the scraper already added the column.
2. Enable Kansas City Current and WNBA in Admin → Sports event types, or run:

   ```sql
   UPDATE dbo.SportsEventsType SET Enabled=1
   WHERE (ProviderKey='espn-nwsl' AND ProviderSchoolId=20907)
      OR (ProviderKey='espn-wnba' AND ProviderSchoolId=0);
   ```

3. Keep WNBA's registered URL league-wide (no team parameter), and remove fixed
   season restrictions for ongoing collection. Run the scraper's --events-from-db
   preview/import commands from the provider guides and verify its hourly scheduler
   uses the deployed binary.
4. Deploy My-Newsfeed and open /events. The Schedule selector includes enabled
   Current/WNBA/MLB feeds. Schedule and sport dropdowns narrow today-forward
   games. Retired date/search URL parameters are ignored. All followed leagues
   remain visible; there is no favorites-only view.

Completed and canceled statuses are hidden, including today's finished games.
Legacy NULL statuses remain visible. Postponed, delayed and suspended games show
status plus Time TBD. Unknown starts use the scraper's stored EventDate (including
its Eastern-midnight placeholder correction), rather than inferring a day from an
unknown UTC start. Confirmed UTC starts use the existing Central display conversion.

## Verified deployment (2026-10-09)

The shared database (sql2025 / WebScraper) has ScheduleStatus and enabled Current,
WNBA and MLB feeds. The updated mynewsfeed-web production container was rebuilt
and deployed, preserving its runtime settings. It is healthy; /healthz, the MLB
schedule selection and /results return HTTP 200. MLB selection shows 22 games.
The previous image is retained as mynewsfeed-web:before-professional-schedules.
No scraper scheduler settings were changed by this deployment.

For future deployments, the required column must exist before the new app runs.

## Results coverage

Apply db/017_seed_schedule_result_coverage.sql when setting up this schedule coverage.
It adds WNBA league results and Big 12 women's basketball conference results using
the existing ESPN score sync. Existing follows and enabled settings are preserved.
MLB, NWSL, Big 12 football, men's basketball, women's soccer and volleyball are
already covered by the earlier result seeds. Schedule fixtures remain scraper-owned.

Sports Events shows today-forward fixtures; Results defaults to today's finished
games in Central time, with navigation limited to today and yesterday. A fixture appears in Results after it finishes, syncs, and
falls within that page's date window. A postponed or canceled fixture has no final
score. ESPN coverage and delayed score reporting can still affect individual games.
