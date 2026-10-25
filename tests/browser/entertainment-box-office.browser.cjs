const assert = require('node:assert/strict'),
    { chromium } = require('playwright');
const base = process.env.BASE_URL || 'http://localhost:5295',
    root = process.env.SCREENSHOT_DIR;
(async () => {
    const browser = await chromium.launch();
    try {
        for (const js of [true, false]) {
            const p = await browser.newPage({ javaScriptEnabled: js });
            const errors = [];
            p.on('pageerror', (e) => errors.push(e.message));
            for (const route of ['/box-office', '/box-office/weekend-preview', '/box-office/movie-forecasts'])
                for (const width of [320, 390, 430, 1440])
                    for (const theme of ['light', 'dark']) {
                        await p.setViewportSize({ width, height: 900 });
                        await p.emulateMedia({ colorScheme: theme });
                        await p.goto(base + route);
                        assert(!(await p.locator('#blazor-error-ui').isVisible()));
                        assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
                        if (js) assert.equal(await p.locator('html').getAttribute('data-theme'), theme);
                        if (width < 768) {
                            await p.locator('.mobile-nav > summary').click();
                            const entertainment = p.locator('[aria-labelledby="mobile-entertainment-heading"]');
                            const link = entertainment.locator('a[href="box-office"]');
                            await link.scrollIntoViewIfNeeded();
                            assert.equal(await link.getAttribute('aria-current'), 'page');
                            assert((await link.boundingBox()).height >= 44);
                            assert.equal(
                                await p.locator('[aria-labelledby="mobile-news-heading"] a[href="box-office"]').count(),
                                0,
                            );
                            if (root && js && route === '/box-office')
                                await p.screenshot({ path: root + '/box-office-menu-' + width + '-' + theme + '.png' });
                            await p.locator('.mobile-nav > summary').click();
                        } else {
                            const groups = p.locator('.desktop-nav-group');
                            const entertainment = groups.filter({
                                has: p.locator('summary', { hasText: 'Entertainment' }),
                            });
                            assert(
                                await entertainment
                                    .locator('summary')
                                    .evaluate((n) => n.classList.contains('is-current')),
                            );
                            assert.equal(
                                await entertainment.locator('a[href="box-office"][aria-current="page"]').count(),
                                1,
                            );
                            const news = groups.filter({ has: p.locator('summary', { hasText: 'News' }) });
                            assert.equal(await news.locator('a[href="box-office"]').count(), 0);
                            assert(
                                !(await news.locator('summary').evaluate((n) => n.classList.contains('is-current'))),
                            );
                        }
                    }
            await p.goto(base + '/news');
            assert.equal(await p.locator('main a[href="box-office"]').count(), 0);
            assert.deepEqual(errors, []);
            await p.close();
        }
        console.log(
            'Box Office moved to Entertainment on mobile/desktop, active states on all three routes, 44px links, Headlines shortcut removal, light/dark overflow and no-JS checks passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
