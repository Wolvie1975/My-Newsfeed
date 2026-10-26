const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    const browser = await chromium.launch();
    try {
        for (const javaScriptEnabled of [true, false]) {
            const context = await browser.newContext({ javaScriptEnabled });
            const page = await context.newPage();
            const errors = [];
            page.on('pageerror', (e) => errors.push(e.message));
            for (const width of [360, 390, 1440]) {
                await page.setViewportSize({ width, height: 950 });
                await page.goto(base + '/');
                const card = page.locator('[data-home-section="watch"]');
                await card.waitFor();
                assert.equal(await page.locator('.personal-links a[href="watch"]').count(), 0);
                const box = await card.boundingBox();
                const sports = await page.locator('[data-home-section="sports"]').boundingBox();
                assert(box.y + box.height <= sports.y + 1);
                // Broadsheet (October 25): Where to watch comes first — the masthead's left ear on desktop,
                // the first card on phones — above the lead story.
                const lead = await page.locator('[data-home-section="lead"]').boundingBox();
                assert(box.y + box.height <= lead.y + 1);
                if (width >= 744) assert(box.x < lead.x);
                assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
                const input = page.getByLabel('Movie or TV title', { exact: true });
                assert.equal(await input.getAttribute('maxlength'), '120');
                assert(await input.evaluate((n) => n.required));
                await input.fill('Law & Order');
                await card.getByRole('button', { name: 'Search', exact: true }).click();
                await page.waitForURL((u) => u.pathname === '/watch' && u.searchParams.get('q') === 'Law & Order');
                assert.equal(await page.locator('#watch-query').inputValue(), 'Law & Order');
                assert(await page.getByRole('heading', { name: 'Where to watch', exact: true }).isVisible());
            }
            assert.deepEqual(errors, []);
            await context.close();
        }
        console.log(
            'Homepage Where to watch: first, above the lead and Today & Tonight, removed inline link, 360/390/1440px, required field, encoded title navigation to /watch, retained query, JS/no-JS passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
