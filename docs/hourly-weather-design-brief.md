# Design brief: next 8 hours of weather

## What we want

Add a view of the **next 8 hours** of weather to the forecast at the top of **News › Kansas City** (`/news/kansas-city`, `#local-weather`). It should sit with the existing forecast cards, not replace them, and answer two questions quickly: *what will it be like when I go out later?* and *will it rain?*

Please explore 3 or 4 different concepts. Ideas to try:
- a compact strip of 8 columns
- a temperature line or sparkline with conditions shown along it
- a rain-chance bar row under the temperatures
- an expandable "Hourly" panel attached to the first forecast card

## The page today

The site is myNewspaper, a personal news site laid out like a newspaper broadsheet.

At the top of the page:
- A **Use my location** button. It changes the forecast to the viewer's location; there's also a **Use Kansas City** button to switch back.
- A status line, for example: *National Weather Service · updated 11/6/2026, 3:22:09 PM CST*.
- **Four forecast cards** in a row on desktop, each covering a 12-hour National Weather Service (NWS) period:

| Card | Example |
|---|---|
| Period name (small, uppercase label) | THIS AFTERNOON |
| Icon + temperature (large serif) | ☁ 49°F |
| Short forecast (bold) | Cloudy |
| Wind (small, muted) | Wind 5 mph NNW |

- A thin rule closes the forecast area. Below it are **Kansas City News** (main column) and **Find something to do** (side column).

### Layouts
- **Desktop (1024px and wider):** the forecast area has no card background. A 4-column grid of cards with a 1px border, 14px rounded corners and 18px padding, closed by a 1px rule.
- **Phone:** the forecast is one rounded card (16px corners). Inside it, the first period is a large feature block, then a short row of tiles for the next 3 periods. No sideways scrolling anywhere on the page.

### Visual style
- **Fonts:** Newsreader (serif) for headlines and large temperatures; Schibsted Grotesk for body text and labels.
- **Light theme:** page `#f4f6fa`, accent and header royal blue `#0051ba`.
- **Dark theme (shown in the screenshot):** page navy `#09182b`, accent light blue `#8dcbff`, header `#0b346b`. Cards are slightly lighter navy panels with a subtle border.
- **Labels:** small uppercase with wide letter spacing (12px, weight 800, 0.06em).
- **Icons:** the existing set of line icons for conditions such as cloudy, partly sunny and clear night, with day and night versions. Partly sunny uses a yellow sun; the other icons use the accent color.
- **Sizes:** every tap target is at least 44px. Both light and dark themes have to work.

## Data available for each hour

The NWS hourly forecast gives these fields for each hour, so concepts can use any of them:

| Field | Example | Notes |
|---|---|---|
| Start time | 4 PM | Shown in Central time, or the viewer's local time |
| Day or night | true / false | Picks the day or night icon |
| Temperature | 48°F | Whole degrees |
| Short forecast | "Mostly Cloudy", "Chance Rain Showers" | Picks the icon |
| Chance of rain or snow | 20% | Often 0% |
| Wind speed and direction | 5 mph NNW | |
| Dew point | 38°F | Optional |
| Relative humidity | 72% | Optional |

There is **no** "feels like" temperature, UV index or sunrise and sunset time in this data, so please don't design around them.

## Constraints and edge cases
- **Exactly 8 hours**, starting at the next hour.
- **Hourly data can fail on its own.** The 4 forecast cards must still look complete when the hourly view is missing. Show either a quiet "Hourly forecast unavailable" line or nothing at all.
- **Labels can be long.** "Slight Chance Rain Showers then Mostly Cloudy" has to fit, or be shortened to an icon plus a tooltip.
- **Phone width (about 360px):** all 8 hours must be visible without sideways scrolling. For example, use 2 rows of 4, or a vertical list.
- **Accessibility:** the hours need to read correctly to a screen reader (time, temperature, conditions, rain chance), and nothing can be shown by color alone.
- The view refreshes whenever the forecast does, about every 15 minutes, and again when the viewer switches location.

## What to deliver
For each concept, show desktop and phone mockups in both dark and light themes. Include a short note on what the concept emphasizes (temperature trend, rain timing or at-a-glance conditions).
