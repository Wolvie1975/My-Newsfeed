# Compact standings layout

Deployed October 13, 2026 as mynewsfeed-web:standings-layout-20261013.
Default columns: NFL W/L/T/Pct; MLB/WNBA W/L/Pct; MLS/NWSL W/D/L/Pts;
other sources retain their first three provider columns. Show all
statistics reveals remaining columns and optional soccer rank across
all tables. About these standings contains refresh, source and ordering
information; seasons and refresh warnings remain visible. Team notes
use native disclosures. All controls work without JavaScript.

Provider group names, ordering and displayed values remain intact.
Mobile tables use smaller logos and padding, wrapping team names, and
no sticky team column. Full tables retain horizontal scrolling.

All 18 standings tests passed. Browser checks covered the six currently
enabled sources (Big 12 football/soccer/volleyball, MLS, NFL and NWSL) in
light and dark mode at 320/390/768/1280 pixels, expansion, unchanged team
data, metadata disclosure and no-JavaScript operation. Mobile NFL
screenshot was visually inspected. Public theme and HTTP smoke checks
passed; runtime settings and the key volume were preserved.
Rollback: mynewsfeed-web:before-standings-layout. Latest points to this
release. No scraper configuration or stored standings were changed.
