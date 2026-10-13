# English soccer frontend deployment

Deployed October 13, 2026 as mynewsfeed-web:english-soccer-20261013.
Runtime environment, shared network, port mapping and persistent key
volume were preserved. Latest points to this release; rollback image:
mynewsfeed-web:before-english-soccer. No scraper jobs, feed settings,
production data or database schema were changed.

Enabled espn-championship and espn-epl feeds now appear in Schedule
selection and receive separate league headings. Their home/away names
come from the provider rows, with the home-team perspective applied
explicitly. Final/canceled/unnecessary games are excluded. Stored
EventDate remains authoritative for TBD fixtures; known UTC instants
use the existing Central display conversion. Schedule paging makes
all upcoming fixtures accessible beyond the existing 200-row page size.

Both tables use P/W/D/L/GF/GA/GD/Pts display values, preserve provider
order and season/title, and retain the compact/full-statistics controls.
Wrexham (TeamId 352) is highlighted only within Championship; all 24
teams remain visible. League navigation resolves enabled source IDs by
ProviderKey. Premier League has Schedule/Table links; Championship
also links to Season results.

Wrexham-only schedule and results filters use team=352 and match both
home and away identities. The new /championship/results page reads
scraper-owned SportsMatchResults independently of ResultGames/Follows.
It selects the latest validated stored season, retains last-good data
on feed refresh errors, excludes nonfinal/invalid scores, and pages
25 matches at a time. Outcomes use Wrexham's home/away perspective;
penalty finals are labeled without inferring shootout winners. Source
refresh warnings, safe match links and provider logos are displayed.
The ordinary Results page links to this independent season view when
the Championship feed is enabled; existing result synchronization and
today/yesterday behavior remain intact.

Database inspection confirmed the application has SELECT permission
on SportsMatchResults. No new grant or migration was required here.
Other environments must provision the scraper-owned table and SELECT
permission before using the season-results page.

Validation: all 430 tests passed, including English soccer regressions
for provider-scoped schedule identities, disabled/final/past filtering,
TBD dates, paging, score validation, home/away outcomes, penalties,
season replacement and last-good preservation. Fixture changes were
transactional and rolled back. Live browser checks compared all 24
Championship and 20 EPL teams and their eight statistics with stored
data, verified provider order/Wrexham highlighting, schedule paging,
Wrexham filters and all 115 current-season final scores across pages.
Wrexham results currently include 10 matches. Light/dark phone/tablet/
desktop checks, no-JavaScript filters, existing-page HTTP checks, sharing
and seven-page admin checks passed. The results mobile section label
was corrected and verified after the final rebuild.
