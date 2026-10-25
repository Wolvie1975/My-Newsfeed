const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';

(async () => {
    const browser = await chromium.launch();
    try {
        const context = await browser.newContext();
        await context.addInitScript(() =>
            localStorage.setItem(
                'mynewsfeed-personal-v1',
                JSON.stringify({ order: ['videos', 'movies', 'tv', 'sports', 'news', 'watch'] }),
            ),
        );
        const page = await context.newPage();
        const errors = [];
        page.on('pageerror', (error) => errors.push(error.message));
        const geometry = () =>
            page.locator('[data-home-grid] > [data-home-section]:not([hidden])').evaluateAll((nodes) =>
                nodes.map((node) => {
                    const r = node.getBoundingClientRect();
                    return { key: node.dataset.homeSection, x: r.x, y: r.y, bottom: r.bottom, right: r.right };
                }),
            );
        async function check(width) {
            await page.setViewportSize({ width, height: 1000 });
            await page.waitForTimeout(350);
            const all = await geometry();
            // The rotating lead story always comes first and Where to watch directly after it.
            assert.deepEqual(
                all.slice(0, 2).map((card) => card.key),
                ['lead', 'watch'],
            );
            const [lead, ...cards] = all;
            if (width >= 1024) {
                assert.equal(await page.locator('[data-home-grid]').getAttribute('data-masonry'), '');
                // Full-width lead above the two cascading columns.
                assert(lead.right - lead.x > 1.8 * (cards[0].right - cards[0].x), 'lead should span both columns');
                assert(lead.bottom <= Math.min(cards[0].y, cards[1].y));
                assert(cards[0].x < cards[1].x);
                assert(Math.abs(cards[0].y - cards[1].y) < 2);
                const bottoms = [cards[0].bottom, cards[1].bottom];
                const rowGap = await page
                    .locator('[data-home-grid]')
                    .evaluate((node) => parseFloat(getComputedStyle(node).rowGap));
                const nextRow = cards
                    .slice(0, 2)
                    .map((card) => Math.ceil((card.bottom - card.y + rowGap) / (1 + rowGap)));
                for (const card of cards.slice(2)) {
                    // Placement uses rounded grid spans; natural bottoms can differ within a row.
                    const column = nextRow[0] <= nextRow[1] ? 0 : 1;
                    assert.equal(card.x, cards[column].x, `${card.key} should use the shorter column`);
                    const gap = card.y - bottoms[column];
                    assert(gap >= 23 && gap <= 50, `${card.key} gap: ${gap}`);
                    bottoms[column] = card.bottom;
                    nextRow[column] += Math.ceil((card.bottom - card.y + rowGap) / (1 + rowGap));
                }
            } else {
                assert.equal(await page.locator('[data-home-grid]').getAttribute('data-masonry'), null);
                for (let i = 1; i < all.length; i++) assert(all[i].y >= all[i - 1].bottom);
            }
            for (let i = 0; i < all.length; i++)
                for (const b of all.slice(i + 1)) {
                    const a = all[i];
                    assert(
                        a.right <= b.x + 1 || b.right <= a.x + 1 || a.bottom <= b.y + 1 || b.bottom <= a.y + 1,
                        'Cards overlap',
                    );
                }
            assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
        }
        await page.goto(base + '/');
        for (const width of [1440, 1920, 320, 390, 767, 768, 1023, 1024, 1440]) await check(width);
        await page.locator('[data-home-section="watch"]').evaluate((node) => {
            const extra = document.createElement('div');
            extra.id = 'height-probe';
            extra.style.height = '700px';
            node.append(extra);
        });
        await check(1440);
        await page.locator('#height-probe').evaluate((node) => node.remove());
        await page.locator('[data-home-section="sports"]').evaluate((node) => (node.hidden = true));
        await check(1440);
        await page.locator('[data-home-section="sports"]').evaluate((node) => (node.hidden = false));
        await check(1440);
        const plain = await browser.newContext({ javaScriptEnabled: false, viewport: { width: 1440, height: 1000 } });
        const staticPage = await plain.newPage();
        await staticPage.goto(base + '/');
        const firstTwo = () =>
            staticPage.locator('[data-home-section]').evaluateAll((nodes) =>
                nodes.slice(0, 2).map((node) => {
                    const r = node.getBoundingClientRect();
                    return { key: node.dataset.homeSection, top: r.top, bottom: r.bottom, width: r.width };
                }),
            );
        for (const width of [1440, 390]) {
            await staticPage.setViewportSize({ width, height: 1000 });
            const [lead, watch] = await firstTwo();
            assert.deepEqual([lead.key, watch.key], ['lead', 'watch']);
            assert(lead.bottom <= watch.top, `no-JS ${width}px: lead should sit above Watch`);
            if (width >= 1024) assert(lead.width > 1.8 * watch.width, 'no-JS desktop lead should span both columns');
        }
        await plain.close();
        assert.deepEqual(errors, []);
        console.log(
            'Cascading columns, full-width lead then Watch, mobile order, resizing, dynamic heights, hidden sections and no-JavaScript fallback passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((error) => {
    console.error(error);
    process.exit(1);
});
