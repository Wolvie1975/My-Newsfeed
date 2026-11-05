// Live score cards keep the score inside the card when a team name is long (November 5, 2026: "Chicago Blackhawks"
// pushed the second card's scores past its edge at 390px on Home and Scoreboard). Renders mobile.css with fixed
// markup, so it doesn't depend on games being live: the strip in the dark lead block (170px cards), the strip on a
// light band (150px) and the grid, at 320/390/430px. Needs no server; BASE_URL is ignored.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const css = fs.readFileSync(path.resolve(__dirname, '../../src/myNewspaper.Web/wwwroot/mobile.css'), 'utf8');
const card = (away, home) => `
    <a class="mx-score" href="#">
        <span class="mx-score__league">NHL</span>
        <span class="mx-score__clock">3P 16:24</span>
        <span class="mx-score__team"><span>${away}</span><b>1</b></span>
        <span class="mx-score__team"><span>${home}</span><b>13</b></span>
    </a>`;
// One name too long for a card in any font, so the check doesn't depend on which font loads.
const cards =
    card('Ottawa Senators', 'Buffalo Sabres') + card('Chicago Blackhawks', 'Columbus Blue Jackets Hockey Club');
const layouts = {
    'dark block strip': `<div class="mx-block"><div class="mx-scores mx-scores--strip">${cards}</div></div>`,
    'light band strip': `<div class="mx-scores mx-scores--strip">${cards}</div>`,
    grid: `<div class="mx-scores">${cards}${card('Vegas Golden Knights', 'Utah Mammoth')}</div>`,
};

(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage();
        for (const width of [320, 390, 430]) {
            await page.setViewportSize({ width, height: 800 });
            for (const [name, markup] of Object.entries(layouts)) {
                await page.setContent(
                    // border-box, as the site's base stylesheet sets.
                    `<!doctype html><html><head><style>*,*::before,*::after{box-sizing:border-box}${css}</style></head>` +
                        `<body class="pub" style="margin:0;padding:0 16px">${markup}</body></html>`,
                );
                const outside = await page.$$eval('.mx-score', (tiles) =>
                    tiles.flatMap((tile, i) => {
                        const edge = tile.getBoundingClientRect().right;
                        return [...tile.querySelectorAll('.mx-score__team')]
                            .filter((row) => row.getBoundingClientRect().right > edge + 0.5)
                            .map((row) => `card ${i + 1}: "${row.textContent.trim()}" ends past the card`);
                    }),
                );
                assert.deepEqual(outside, [], `${name} at ${width}px`);
                const truncated = await page.$$eval('.mx-score__team > span', (names) =>
                    names.some((n) => n.scrollWidth > n.clientWidth),
                );
                assert(truncated, `${name} at ${width}px: the long name should end in an ellipsis`);
            }
        }
        console.log('Score cards: scores stay inside the card with long team names, 3 layouts at 320/390/430px.');
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e.message || e);
    process.exit(1);
});
