const assert = require('node:assert/strict'),
    { chromium } = require('playwright');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    const b = await chromium.launch();
    try {
        for (const js of [true, false]) {
            const p = await b.newPage({ javaScriptEnabled: js });
            const errors = [];
            p.on('pageerror', (e) => errors.push(e.message));
            for (const width of [320, 375, 390, 430, 767, 768, 1440])
                for (const theme of ['light', 'dark']) {
                    await p.setViewportSize({ width, height: 1000 });
                    await p.emulateMedia({ colorScheme: theme });
                    await p.goto(base + '/scoreboard');
                    if (js) assert.equal(await p.locator('html').getAttribute('data-theme'), theme);
                    assert(await p.getByRole('combobox', { name: 'Show', exact: true }).count());
                    assert.equal(await p.getByRole('button', { name: 'Apply', exact: true }).count(), js ? 0 : 1);
                    if (width < 768) {
                        const prev = await p.locator('.scoreboard-prev').boundingBox(),
                            next = await p.locator('.scoreboard-next').boundingBox(),
                            date = await p.locator('.scoreboard-date').boundingBox();
                        assert(Math.abs(prev.y - next.y) < 1);
                        assert(Math.abs(prev.width - next.width) < 1);
                        // One row as in the concept: ← previous day, the date, next day →.
                        assert(prev.x + prev.width <= date.x && date.x + date.width <= next.x);
                        assert(prev.height >= 44 && next.height >= 44);
                    }
                    assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
                    await p.goto(base + '/');
                    if (js) {
                        await p.evaluate(() =>
                            localStorage.setItem(
                                'mynewsfeed-personal-v1',
                                JSON.stringify({ order: ['watch', 'news', 'scoreboard', 'sports', 'tv'] }),
                            ),
                        );
                        await p.reload();
                    }
                    const today = await p.locator('[data-home-section="sports"]').boundingBox(),
                        scores = await p.locator('[data-home-section="scoreboard"]').boundingBox();
                    assert(today.y <= scores.y, 'Today & Tonight must precede Scoreboard');
                    assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
                }
            await p.goto(base + '/scoreboard');
            const select = p.getByRole('combobox', { name: 'Show', exact: true });
            const value = await select.locator('option').evaluateAll((ns) => ns.find((n) => n.value)?.value);
            await select.selectOption(value);
            if (!js) await p.getByRole('button', { name: 'Apply', exact: true }).click();
            await p.waitForURL((u) => u.searchParams.get('show') === value);
            assert.deepEqual(errors, []);
            await p.close();
        }
        console.log(
            'Show auto-submit/no-JS fallback, matched mobile date buttons, phone/tablet/desktop overflow and default/legacy Home sports order passed.',
        );
    } finally {
        await b.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
