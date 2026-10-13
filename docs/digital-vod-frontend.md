# Weekly Digital / VOD frontend

The dedicated /movies/digital page is linked from Movies and Home. It reads the
scraper-owned movie tables, preferring Movie Insider whenever that source is enabled.
The source dropdown includes enabled Movie Insider, When To Stream and DVDs Release
Dates rows even for an empty week. ?source= retains the selected provider; invalid or
disabled keys fall back to the preferred enabled source. Providers are never blended.

The page includes confirmed digital/digital_rental/digital_purchase observations from
Monday through Sunday of the current display-zone week, including earlier weekdays.
Canceled/withdrawn, estimated, disc and subscription observations are excluded. Dates
are shown directly; the collection timestamp is converted from UTC and labeled Last
checked. Public collection failures retain stored rows and show a generic message.

Each provider's stable release IDs are retained. No title-based or date-key deduplication
is performed. Unknown posters, years and platforms remain unknown. The page does not
infer rental/purchase pricing or subscription availability from generic digital data.
Home shows five newest-first observations from the preferred source's same weekly list,
with a link to the full week. Friday theatrical and other Movies views are unchanged.

Real-data preview checks found all eight standings sources populated, including full
NWSL, and 10 Movie Insider / 5 When To Stream digital observations for October 5–11,
2026. These are validation snapshots, not fixed expected totals. Mobile and desktop
browser checks passed. No frontend production deployment has been performed yet.

## Published verification

The frontend is now deployed. All eight standings sources, source-specific weekly
digital lists, Home, Movies, schedule and results passed live checks. The container
is healthy. Existing runtime settings were preserved; rollback image:
mynewsfeed-web:before-standings-digital.

## Date-range revision

Digital/VOD and its Home preview now include today and every known future confirmed
release date, ordered soonest first. The former Monday–Sunday bounds and past weekdays
are no longer used. Source preference, source separation and confirmation rules remain.
