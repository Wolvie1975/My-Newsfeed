const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage();
        const errors = [];
        page.on('pageerror', (e) => errors.push(e.message));
        await page.goto(base + '/');
        assert.equal(await page.locator('[data-home-section="f1"]').count(), 0);
        const card = page.locator('[data-home-section="rankings"]');
        await card.waitFor();
        const options = await card
            .locator('select option')
            .evaluateAll((ns) => ns.map((n) => ({ value: n.value, text: n.textContent })));
        assert(options.length, 'Production ranking sources expected');
        for (const option of options) {
            await card.locator('select').selectOption(option.value);
            await page.waitForURL((u) => u.searchParams.get('rankingSourceId') === option.value);
            assert.equal(await card.locator('select').inputValue(), option.value);
            assert.equal(await card.locator('.d-more').getAttribute('href'), 'rankings?sourceId=' + option.value);
            const ranks = await card.locator('tbody tr td:first-child').allTextContents();
            assert(ranks.every((r) => +r >= 1 && +r <= 25));
            if (!ranks.length) assert((await card.innerText()).includes('Rankings not available yet'));
            else {
                assert.deepEqual(await card.locator('thead th').allTextContents(), [
                    'Rank',
                    'School',
                    'Record',
                    'Previous',
                ]);
                assert.equal(await card.locator('tbody .big12-badge').count(), 0);
                assert((await card.locator('tbody img').count()) > 0);
                assert(
                    await card
                        .locator('tbody img')
                        .evaluateAll((ns) => ns.every((n) => n.width === 22 && n.height === 22)),
                );
                const highlighted = card.locator('tr.ranking-big12');
                if (await highlighted.count()) {
                    assert(
                        await highlighted
                            .first()
                            .locator('td')
                            .first()
                            .evaluate((n) => getComputedStyle(n).boxShadow !== 'none'),
                    );
                }
                const scroll = await card.locator('.d-rankings__table').boundingBox();
                if (page.viewportSize().width < 744) assert(scroll.height <= 441);
                else
                    assert(
                        await card.locator('.d-rankings__table').evaluate((n) => n.scrollHeight <= n.clientHeight + 1),
                    );
            }
        }
        for (const width of [390, 768, 1024, 1440, 1920]) {
            await page.setViewportSize({ width, height: 900 });
            await page.waitForTimeout(250);
            assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
            const table = card.locator('.d-rankings__table');
            if (await table.count()) {
                if (width >= 744) {
                    assert(await table.evaluate((n) => n.scrollHeight <= n.clientHeight + 1));
                    const last = await card.locator('tbody tr').last().boundingBox();
                    const area = await table.boundingBox();
                    assert(last.y + last.height <= area.y + area.height + 1);
                } else assert((await table.boundingBox()).height <= 441);
            }
        }
        await page.locator('select').focus();
        assert.deepEqual(errors, []);
        await card.screenshot({ path: process.env.TOP25_SCREENSHOT || 'test-results/home-top25.png' });
        const plain = await browser.newContext({ javaScriptEnabled: false, viewport: { width: 390, height: 900 } });
        const p = await plain.newPage();
        await p.goto(base + '/');
        await p.locator('#home-ranking-source').selectOption(options.at(-1).value);
        await p.locator('[data-home-section="rankings"]').getByRole('button', { name: 'Apply' }).click();
        assert.equal(new URL(p.url()).searchParams.get('rankingSourceId'), options.at(-1).value);
        assert.equal(await p.locator('#home-ranking-source').inputValue(), options.at(-1).value);
        await plain.close();
        assert.equal((await page.request.get(base + '/healthz')).status(), 200);
        console.log(
            'Homepage Top 25: available source selection, matching full-page link, ranks 1–25, expanded desktop/scrolling mobile table, no standalone F1, responsive widths, no-JS Apply, no JS errors, health 200 passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
