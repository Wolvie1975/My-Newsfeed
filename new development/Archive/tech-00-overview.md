# Tech section: overview (read first)

Written October 31, 2026 (America/Chicago), against `develop` at `3a7ec71`.

**Save as:** `docs/tech/tech-00-overview.md`

**Mockups:** the myNewspaper canvas, **Tech section** page (https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5):

- the plan board;
- the Tech front (desktop and phone);
- All headlines;
- the Technology, Video Games and Linux desks;
- the Linux desk on a phone;
- the weekly roundup.

Bracketed headlines and versions on the boards are placeholders.

## What we're building

A fourth section beside Sports and Entertainment: **Tech**. It's built on the Technology, Video Games and Linux
categories you already collect, plus three small new feeds from the scraper.

| Piece | Data | Guide |
|---|---|---|
| The Tech hub: navigation, routes, moving the three categories out of News, Admin and Your topics support | Existing | **01** |
| Tech front (`/tech`): briefing, The day so far, lead + Latest, **Most covered**, **Your topics**, Tech videos, desks | Existing | **02** |
| All headlines (`/tech/news`) and the three desk pages (`/tech/technology`, `/tech/video-games`, `/tech/linux`) | Existing | **03** |
| **Release tracker** (Linux desk) and **Just released** (front), **What developers are reading** (Technology desk), **Service status** (front) | **New scraper feeds** | **04** (after the scraper work) |
| **Weekly roundup** (`/tech/week`) | Existing (plus the tracker once 04 is in) | **05** |
| The three new feeds, in the Rss-Rust-Scrape repo | New | `scraper-tech-handoff.md` |

**Not included,** by your decisions:

- game release tracking (games appear as ordinary headlines on the Video Games desk);
- Security this week;
- Listen (podcasts);
- Tech calendar.

## Order

1. **01 → 02 → 03** can ship before any scraper work. The front and desks hide the sections that need new feeds
   until their tables exist (each guide says how).
2. **The scraper handoff** runs in the scraper project, in parallel with 01–03 or after them.
3. **04** once the scraper's tables are created and populated in the target database.
4. **05** any time after 02 (it uses Most covered). Its "Released this week" box appears once 04 is in.

Stop after each guide and check it, including the mobile checks in `AGENTS.md`.

## Who owns what

- **Web (this repo):**
  - migration `db/024_tech_hub.sql` (hub values);
  - all pages and queries.
- **Scraper (Rss-Rust-Scrape):**
  - the three new tables and their `sql/` migrations;
  - collection;
  - enable/disable flags.

  The web app only **reads** those tables, and never fetches from the internet itself (see `DEPLOY.md`).

## Words used in these guides

- **Hub:** the `SourceCategories.NewsHub` value that decides which section a category's headlines appear in. Today
  it's `headlines` (News), `sports` or `entertainment`. These guides add `tech`.
- **Desk:** one of Tech's three categories (Technology, Video Games, Linux), each with its own page.
- **Tech colours:** Tech purple `--mx-tech` (#6d28d9). Video Games uses the amber `--mx-business` (#b45309), and Linux
  the teal `--mx-local` (#0f766e). These tokens already exist in `mobile.css`, with dark-mode values.
