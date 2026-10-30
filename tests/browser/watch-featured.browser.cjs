const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    const browser = await chromium.launch();
    try {
        for (const width of [320, 390, 430, 1440])
            for (const theme of ['light', 'dark']) {
                const page = await browser.newPage({ viewport: { width, height: 1000 }, colorScheme: theme });
                const errors = [];
                page.on('pageerror', (e) => errors.push(e.message));
                await page.goto(base + '/watch?services=8,9,337');
                const rows = await page.locator('.mx-desktop [data-watch-row]').evaluateAll((ns) =>
                    ns.map((n) => ({
                        h: n.querySelector('h2').textContent,
                        c: n.querySelectorAll('.watch-shelf > li').length,
                        kinds: [...n.querySelectorAll('[data-watch-title]')].map(
                            (a) => a.getAttribute('href').split('/')[1],
                        ),
                    })),
                );
                assert.deepEqual(
                    rows.map((r) => r.h),
                    [
                        'Popular on your services',
                        'Trending movies this week',
                        'Trending TV this week',
                        'New movies you can watch',
                    ],
                );
                assert(
                    rows.every((r) => r.c > 0 && r.c <= 12),
                    JSON.stringify(rows.map((r) => r.c)),
                );
                assert(new Set(rows[0].kinds).size === 2, 'services row mixes movies and TV');
                assert(
                    rows[1].kinds.every((k) => k === 'movie') &&
                        rows[2].kinds.every((k) => k === 'tv') &&
                        rows[3].kinds.every((k) => k === 'movie'),
                );
                assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'overflow');
                assert.equal(await page.locator('#watch-sort').count(), 0);
                const arrow = page.locator('.mx-desktop [data-watch-row]').first().locator('[data-watch-scroll="1"]');
                assert.equal(await arrow.isVisible(), width >= 768);
                if (width >= 768) {
                    await arrow.click();
                    await page.waitForFunction(
                        () => document.querySelector('.mx-desktop .watch-shelf').scrollLeft > 50,
                    );
                    assert(
                        await page
                            .locator('.mx-desktop [data-watch-row]')
                            .first()
                            .locator('[data-watch-scroll="-1"]')
                            .isEnabled(),
                    );
                }
                if (width < 1024) {
                    const rail = page.locator('.mx-watch-rail');
                    assert((await rail.locator('li').count()) > 0);
                    await rail.evaluate((e) => (e.scrollLeft = 100));
                    assert((await rail.evaluate((e) => e.scrollLeft)) > 0);
                    await page.locator('#watch-tv-m-heading').locator('..').getByRole('link').click();
                } else await page.locator('#watch-row-tv-heading').locator('..').getByRole('link').click();
                await page.waitForURL(
                    (u) => u.searchParams.get('kind') === 'tv' && u.searchParams.get('sort') === 'popular',
                );
                await page.locator('.watch-grid .watch-card').first().waitFor();
                if (width < 1024) await page.locator('#watch-browse-m').selectOption('featured');
                else await page.getByRole('link', { name: 'Featured', exact: true }).click();
                await page
                    .locator(width < 1024 ? '.mx-watch-rail' : '.mx-desktop [data-watch-row]')
                    .first()
                    .waitFor();
                assert.deepEqual(errors, []);
                await page.close();
            }
        const plain = await browser.newPage({ javaScriptEnabled: false, viewport: { width: 390, height: 900 } });
        await plain.goto(base + '/watch');
        assert.equal(await plain.locator('#watch-row-services').count(), 0);
        assert.equal(await plain.locator('.mx-desktop [data-watch-row]').count(), 3);
        assert.equal(await plain.locator('[data-watch-scroll]').first().isVisible(), false);
        console.log(
            'Featured rows: order, 12-title limit, mixed services row, phone/desktop light/dark, arrows on wider screens, See all into the grid, Featured toggle and no-JavaScript rows passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
