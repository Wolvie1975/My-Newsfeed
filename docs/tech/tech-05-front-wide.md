# Tech front on wide desktops: Latest headlines beside the lead, desks bottom rail

Updated November 7, 2026 (America/Chicago). Reverses the October "category lists beside the lead" layout for Tech, adopting option A (matching Entertainment's middle-tier structure in `d360a37`).

## Layout from 1920px

```
┌ The day so far ┬ Lead ───────────┬ Latest headlines (5) ┬ Just released ─┐
│ (timeline)     │ photo, title,   │ kicker / headline     │ Tech videos    │
│                │ Most covered    │ All tech headlines →  │                │
└────────────────┴─────────────────┴───────────────────────┴────────────────┘
═══ (double rule) ═════════════════════════════════════════════════════════
┌ Technology ──── All → ┬ Video Games ──── All → ┬ Linux ──────── All → ──┐
│ stories, one column    │ stories, one column     │ stories, one column    │
└────────────────────────┴─────────────────────────┴────────────────────────┘
This week in Tech: … →
```

| Column / Row | Content |
|---|---|
| Column 1 (360px @ 1920, 400px @ 2560) | The day so far (vertical timeline, `ol.spf-timeline.nf-day__line`) |
| Column 2 (`minmax(0, 1.5fr)`) | Lead story · Most covered |
| Column 3 (`minmax(0, 1fr)`) | **Latest headlines** (5 stories with category kickers, `section.ef-headlines-col.tc-headlines-col`) · All tech headlines link |
| Column 4 (`minmax(0, 1fr)`) | **Just released** (when releases exist in last 48h) · **Tech videos** |
| Row 3 (`grid-column: 1 / -1`) | **Technology · Video Games · Linux** desks, 3 across under double rule (`border-top: 3px double var(--text)`) |
| Row 4 (`grid-column: 1 / -1`) | **This week in Tech** banner |

The briefing tiles and "This week in Tech" stay full width, above and below.

## How

`TechFront.razor`'s desktop markup:

```
.ef.spf.nf.tc-front
├── section.nf-brief                          (briefing tiles)
├── div.spf-cols
│   ├── section.spf-upcoming.nf-day           (The day so far timeline)
│   ├── div.ef-main.spf-main                  (lead, Most covered)
│   ├── section.ef-headlines-col.tc-headlines-col (Latest headlines, 5 stories)
│   └── aside.ef-side.spf-side                (TechJustReleased, Tech videos)
├── div.nf-desks                              (Technology, Video Games, Linux)
└── p.tc-week-banner                          (This week in Tech)
```

- Below 1920px: `.tc-headlines-col` sits under the lead in column 2 (`grid-column: 2; grid-row: 2`), matching Entertainment at 1024–1279.
- At 1920px+: `.tc-front` becomes the grid and `.spf-cols` dissolves (`display: contents`), placing its four children across the four columns. Desks sit in row 3 full width (`grid-column: 1 / -1; grid-row: 3`) three across (`grid-template-columns: repeat(3, minmax(0, 1fr))`).
