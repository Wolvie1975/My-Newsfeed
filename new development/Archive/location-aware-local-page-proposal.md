# Proposal: location-aware Local page

Saved October 22, 2026. Status: deferred idea; suggestions only. No implementation or deployment is authorized by this proposal.

## Goal and current behavior

Make the Local page adapt its weather, news, and community events to a selected location. Currently, **Use my location** changes only the weather forecast; the page title, news, and event calendar links remain tied to Kansas City.

The main challenge is obtaining useful local news and event coverage for each area. Getting browser coordinates is the smaller part of the work.

## Work involved

| Area | Proposed work |
| --- | --- |
| Location selection | Request browser location permission and translate coordinates into a city and region. Offer manual city or ZIP entry when permission is declined or location is unavailable. |
| Saved preference | Remember the selected area in the browser, display it clearly, and allow users to change it or return to Kansas City. |
| Weather | Make the existing forecast follow the page's selected location. Evaluate provider coverage before promising forecasts outside the current service area. |
| News | Map supported locations to relevant publishers or evaluate a location-aware news provider. Keyword matching can supplement coverage, but may return unrelated stories. |
| Events | Evaluate geographic event providers or maintain city-specific calendar links. A collected event feed also needs handling for dates, duplicates, cancellations, and coverage gaps. |
| Page behavior | Update titles, headings, links, and preferences together. Reset news pagination when changing location and clearly identify unavailable coverage. |

## Suggested first version

Start with a defined set of supported cities rather than promising local content everywhere:

- Keep Kansas City as the default.
- Add manual location selection.
- Let **Use my location** suggest or select the nearest supported area, with its coverage clearly identified.
- Allow weather to follow the actual selected location where supported, while clearly labeling the area used for news and events.
- Start with curated local publishers and community calendar links.
- Show an honest unavailable state when an area has no news or event coverage.

Treat location as a saved choice that changes when the user asks. Traveling should not automatically replace their hometown news.

## Larger follow-up option

Broad geographic coverage would require provider research, possible subscriptions, geographic content mapping, and additional collection infrastructure. Reliable news and event coverage should be established before expanding the interface's promises.

## Decisions to revisit

- Which cities should be supported initially?
- Should news and events use one selected city, or allow a nearby coverage area?
- Which publishers, calendar links, or providers offer adequate coverage, and at what cost?
- Should the first version keep events as external calendar links or collect individual listings?
- Is a browser-local preference sufficient, or should location eventually sync across devices?

## Resume point

Review this proposal, select the initial cities, and inspect the existing Local page, weather code, and news collection model before designing implementation changes. No provider options or pricing were researched for this discussion.

Relevant source files:

- `src/MyDailyFeed.Web/Components/Pages/Local.razor`
- `src/MyDailyFeed.Web/wwwroot/personal.js`
