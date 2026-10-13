# News card formatting deployment

Deployed October 13, 2026 to the existing mynewsfeed-web container.
Source names and copy/save actions occupy the first footer row; publication
dates occupy a separate row. Long source names wrap within their column.
News and saved cards share this styling.

The release image, mynewsfeed-web:news-card-formatting-20261013, derives from
the previous production image and changes only the news footer CSS and its
static asset metadata/compressed variants. The stylesheet fingerprint is
f4c2e6bc4c. Unrelated pending workspace changes were not included.
Runtime environment, port mapping, network and the persistent key volume
were preserved. The latest image tag also points to this release.
Rollback image: mynewsfeed-web:before-news-card-formatting.

Validation: all 19 FeedArticlesRenderTests passed; production /healthz,
/news, /saved and /results returned HTTP 200. The news page references
the new stylesheet fingerprint, and identity, gzip and Brotli requests
serve the updated CSS. Visual verification on the user's tablet remains
to be performed.

## Date below headline

Later on October 13, the date/time row moved directly below the headline,
before the summary. Source and copy/save actions remain in the footer.
The shared CSS grid also applies to saved cards. Deployed as
mynewsfeed-web:headline-date-20261013 (stylesheet da99d04e76), preserving
production runtime settings. Rollback: mynewsfeed-web:before-headline-date.
All 19 article rendering tests passed; /healthz, /news, /saved and /results
returned HTTP 200, and all three stylesheet encodings served the new layout.
Tablet visual verification remains pending.
