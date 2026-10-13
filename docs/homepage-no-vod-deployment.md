# Today/Tonight VOD filter

Deployed October 13, 2026 as mynewsfeed-web:homepage-no-vod-20261013.
TodayTonight.Build excludes digital, digital_rental and digital_purchase
movie releases from the homepage timeline. Other movie release types,
sports and TV entries retain their existing behavior. The separate
Upcoming Digital / VOD section and digital releases page remain available.

All 20 personal-feature tests passed, including a regression for the
three excluded types and the retained theatrical/subscription/disc types.
Live browser checks compared the timeline before and after deployment,
verified non-movie entries, the separate digital section, page overflow
at phone/desktop widths, and successful health/digital/rankings/standings
responses. Runtime environment and key volume were preserved.
Rollback: mynewsfeed-web:before-homepage-no-vod. Latest points to this
release. No scraper settings or release data were changed.
