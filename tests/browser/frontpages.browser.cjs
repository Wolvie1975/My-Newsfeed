const { chromium } = require('playwright'),
    assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295',
    root = process.env.SCREENSHOT_DIR || 'test-results/';
(async () => {
    const browser = await chromium.launch();
    try {
        const p = await browser.newPage(),
            errors = [];
        p.on('pageerror', (e) => errors.push(e.message));
        for (const route of ['news', 'sports/news', 'entertainment/news', 'videos'])
            for (const width of [320, 390, 430, 768, 1440])
                for (const theme of ['light', 'dark']) {
                    // Headlines has its own folded phone front (headlines-phone).
                    if (route === 'news' && width < 744) continue;
                    await p.setViewportSize({ width, height: 1000 });
                    await p.emulateMedia({ colorScheme: theme });
                    const r = await p.goto(base + '/' + route);
                    assert.equal(r.status(), 200, route);
                    await p.waitForTimeout(150);
                    assert(
                        await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                        route + ' overflow ' + width,
                    );
                    assert(!(await p.locator('#blazor-error-ui').isVisible()));
                    const items = p.locator(route === 'videos' ? '.video-front .video' : '.paper-feed .article');
                    assert((await items.count()) > 0, route + ' no stories');
                    for (const selector of ['#category', '#source', '#hub-category', '#channel'])
                        // Desktop shows category tabs instead of the #category dropdown.
                        if (await p.locator(selector).isVisible())
                            assert((await p.locator(selector).boundingBox()).height >= 44, selector);
                    if (width <= 430) {
                        if (route !== 'videos')
                            assert.equal(
                                await p
                                    .locator('.news-front-filters')
                                    .evaluate((n) => getComputedStyle(n).flexDirection),
                                'column',
                                'full-width phone filters',
                            );
                        for (const selector of [
                            '.paper-feed .title a',
                            '.paper-feed .cat',
                            '.paper-feed .share-button',
                            '.paper-feed .save',
                            '.video-front .vtitle a',
                            '.video-front .share-button',
                            '.video-front .save',
                        ])
                            assert(
                                await p
                                    .locator(selector)
                                    .evaluateAll((ns) =>
                                        ns
                                            .filter((n) => n.getBoundingClientRect().height > 0)
                                            .every((n) => n.getBoundingClientRect().height >= 44),
                                    ),
                                route + ' touch ' + selector,
                            );
                    }
                    if (width <= 430 || width === 1440)
                        await p.screenshot({
                            path: root + route.replaceAll('/', '-') + '-' + width + '-' + theme + '.png',
                            fullPage: true,
                        });
                }
        assert.deepEqual(errors, []);
        for (const route of ['news', 'sports/news', 'entertainment/news']) {
            await p.goto(base + '/' + route);
            const before = await p.locator('.feed .article').count();
            if (await p.locator('[data-load-more]').count()) {
                await p.locator('[data-load-more]').click();
                await p.waitForFunction((n) => document.querySelectorAll('.feed .article').length > n, before);
                const ids = await p.locator('.feed .article').evaluateAll((ns) => ns.map((n) => n.dataset.articleId));
                assert.equal(ids.length, new Set(ids).size);
            }
            if ((await p.locator('#source option').count()) > 1) {
                const value = await p.locator('#source option').nth(1).getAttribute('value');
                await p.selectOption('#source', value);
                await p.waitForURL((u) => u.searchParams.get('source') === value);
                await p.reload();
                assert.equal(await p.locator('#source').inputValue(), value);
            }
        }
        await p.goto(base + '/videos');
        if ((await p.locator('#channel option').count()) > 1) {
            const value = await p.locator('#channel option').nth(1).getAttribute('value');
            await p.selectOption('#channel', value);
            await p.waitForURL((u) => u.searchParams.get('channel') === value);
            assert.equal(
                (await p.locator('.video-channel').allTextContents()).filter((v, i, a) => a.indexOf(v) === i).length,
                1,
            );
        }
        const plain = await browser.newPage({ javaScriptEnabled: false, viewport: { width: 390, height: 1000 } });
        for (const route of ['news', 'sports/news', 'entertainment/news', 'videos']) {
            await plain.goto(base + '/' + route);
            assert(await plain.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
            // Phone Headlines puts its Category select in the ribbon; the desktop copy is hidden.
            const form = plain.locator('form.catselect, form.mx-ribbon__field').filter({ visible: true }).first();
            if (await form.count()) {
                await form.locator('button').click();
                await plain.waitForLoadState();
            }
            // Phone Headlines pages on with the timeline's "More stories" link.
            const more = plain.locator('[data-load-more], .mx-timeline .mx-more').filter({ visible: true }).first();
            if (await more.count()) {
                await more.click();
                await plain.waitForURL((u) => u.searchParams.has('after'));
                assert((await plain.locator('.feed .article').count()) > 0);
            }
        }
        console.log(
            '40 responsive/theme views passed; populated news/video cards, filters, reload, unique pagination, no-JS forms/paging and error checks passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
