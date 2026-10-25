const { chromium } = require('playwright');
const assert = require('node:assert/strict');
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage();
        const errors = [];
        page.on('pageerror', (e) => errors.push(e.message));
        const article = { url: 'https://example.com/article', title: 'Legacy article', source: 'Publisher' };
        const video = {
            url: 'https://www.youtube.com/watch?v=test',
            title: 'Saved video',
            source: 'Channel',
            kind: 'video',
        };
        for (const items of [[video, article], [article], [video], []]) {
            await page.route('**/api/saved', (r) =>
                r.fulfill({
                    contentType: 'application/json',
                    body: JSON.stringify({ articles: items, token: 'test' }),
                }),
            );
            await page.goto((process.env.BASE_URL || 'http://localhost:5295') + '/saved');
            await page.locator('[data-saved-section="article"]').waitFor();
            await page.waitForFunction(() =>
                document.querySelector('[data-saved-list]').getAttribute('data-rendered')?.startsWith('[true,'),
            );
            assert.equal(
                await page.locator('[data-saved-section="article"] article').count(),
                items.includes(article) ? 1 : 0,
            );
            assert.equal(
                await page.locator('[data-saved-section="video"] article').count(),
                items.includes(video) ? 1 : 0,
            );
            assert.equal(await page.getByRole('heading', { name: 'Articles', exact: true }).count(), 1);
            assert.equal(await page.getByRole('heading', { name: 'Videos', exact: true }).count(), 1);
            if (!items.includes(article))
                assert(await page.getByText('No saved articles yet.', { exact: true }).isVisible());
            if (!items.includes(video))
                assert(await page.getByText('No videos saved for later yet.', { exact: true }).isVisible());
            for (const width of [390, 768, 1440]) {
                await page.setViewportSize({ width, height: 900 });
                assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
            }
            await page.unroute('**/api/saved');
        }
        assert.deepEqual(errors, []);
        console.log(
            'Separate Articles/Videos sections: mixed, article-only, video-only, empty, legacy defaults and responsive layouts passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
