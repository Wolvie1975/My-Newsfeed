# Box Office news section

/box-office is linked in public navigation and from News. It reads the existing
Box Office category by name, never deployment-specific category or publisher IDs.
The regular News dropdown also includes the category, and enabled categories/sources
remain selectable before articles arrive.

The dedicated page reuses FeedArticles, FeedCursor and existing first/more batch
sizes. Its publisher dropdown supports All publishers or an enabled source belonging
to Box Office, retaining source/category/cursor in URLs and enhanced Load more requests.
Invalid, disabled or cross-category source selections fall back to All publishers;
articles from disabled sources never become public. No publisher HTTP requests are
made by the frontend, and no new schema or ingestion job is introduced.

Articles retain ordinary provider attribution and publication dates. No box-office
totals or weekday-derived estimate/actual badges are generated. Missing publication
dates are explicitly identified; scrape times are not substituted as publication.
Images/links accept HTTP(S), provider text stays HTML-encoded and excerpts remain
bounded. Collection metadata is labeled Last checked, with generic stale/failure
messages; stored stories remain visible and raw errors stay server-side.

The initial real-data browser check loaded 20 stories without duplicate article IDs,
verified each publisher's ten-story list, the News category option, no-JavaScript
pagination, and mobile/desktop layouts. Those counts are snapshots, not fixed totals.
Database tests use rollback fixtures and make no live publisher requests.

Frontend production deployment is pending. Existing scraper source enablement,
schedules, results and movie collection are unchanged.

## Deployment verification

Published to mynewsfeed-web. Live publisher filtering, enhanced and no-JavaScript
pagination, all 20 current stories, and mobile/desktop layouts passed browser checks.
All checked public routes return 200; the container is healthy. Runtime settings
were preserved, and rollback image mynewsfeed-web:before-box-office is retained.
