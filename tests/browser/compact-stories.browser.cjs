const fs = require('node:fs'),
    assert = require('node:assert/strict'),
    { chromium } = require('playwright');
const base = process.env.BASE_URL || 'http://localhost:5295',
    root = process.env.SCREENSHOT_DIR || 'test-results/';
(async () => {
    const b = await chromium.launch();
    try {
        const p = await b.newPage(),
            errors = [];
        p.on('pageerror', (e) => errors.push(e.message));
        if (process.env.PREVIEW_CSS)
            await p.route('**/public*.css', (r) =>
                r.fulfill({ contentType: 'text/css', body: fs.readFileSync(process.env.PREVIEW_CSS, 'utf8') }),
            );
        // Sports and Entertainment have their own front pages (section-hubs); these routes use the compact story rows.
        for (const route of ['news', 'sports/news', 'entertainment/news'])
            for (const width of [320, 390, 430, 768, 1440])
                for (const theme of ['light', 'dark']) {
                    await p.setViewportSize({ width, height: 950 });
                    await p.emulateMedia({ colorScheme: theme });
                    assert.equal((await p.goto(base + '/' + route)).status(), 200);
                    assert(
                        await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                        route + ' overflow ' + width,
                    );
                    const row = p
                        .locator('.paper-rows .article,.paper-support .article,.paper-feed > .article')
                        .first();
                    await row.waitFor();
                    if (width < 700) {
                        assert(
                            await p
                                .locator('.paper-feed .article:not(.article--lead) .thumb')
                                .evaluateAll((ns) => ns.every((n) => getComputedStyle(n).display === 'none')),
                            'mobile headline images hidden ' + route,
                        );
                        assert(
                            await row.evaluate((n) => {
                                const a = n.getBoundingClientRect(),
                                    h = n.querySelector('.title').getBoundingClientRect(),
                                    d = n.querySelector('time,.publication-unavailable').getBoundingClientRect();
                                return (
                                    getComputedStyle(n.querySelector('.thumb')).display === 'none' &&
                                    Math.abs(h.left - a.left) < 2 &&
                                    Math.abs(d.left - a.left) < 2
                                );
                            }),
                            'full width metadata ' + route,
                        );
                        assert(
                            await row
                                .locator('.share-button,.save')
                                .evaluateAll((ns) => ns.every((n) => n.getBoundingClientRect().height >= 44)),
                        );
                        if (width === 390 && theme === 'light')
                            await row.screenshot({ path: root + route.replaceAll('/', '-') + '-row.png' });
                    }
                    if (width >= 700) {
                        // Tablets: large pictures above the headline. Desktop (concept board): a 96px thumbnail beside it.
                        const card = p.locator('.paper-rows .article').first();
                        if (await card.count())
                            assert(
                                await card.evaluate((n, wide) => {
                                    const a = n.getBoundingClientRect(),
                                        t = n.querySelector('.thumb').getBoundingClientRect();
                                    return wide
                                        ? [96, 72].includes(Math.round(t.width)) // 72px posters on Entertainment headlines
                                        : Math.abs(t.width - a.width) < 1 && t.height === 200;
                                }, width >= 1024),
                                'large More stories image ' + route,
                            );
                    }
                    if (route === 'news' && (width <= 430 || width === 1440))
                        await p.screenshot({ path: root + 'news-' + width + '-' + theme + '.png' });
                }
        await p.setViewportSize({ width: 320, height: 950 });
        await p.goto(base + '/news');
        const row = p.locator('.paper-rows .article').first();
        await row.locator('.src__name').evaluate((n) => (n.textContent = 'CBS News'));
        assert(
            await row.evaluate(
                (n) =>
                    Math.abs(
                        n.querySelector('.article-actions').getBoundingClientRect().top -
                            n.querySelector('.article-publisher').getBoundingClientRect().top,
                    ) < 2,
            ),
            'short source same footer row',
        );
        await row
            .locator('.src__name')
            .evaluate((n) => (n.textContent = 'The Hollywood Reporter — Film and Television Coverage'));
        assert(
            await row.evaluate(
                (n) =>
                    n.querySelector('.article-actions').getBoundingClientRect().top >=
                    n.querySelector('.article-publisher').getBoundingClientRect().bottom,
            ),
            'long source wraps actions',
        );
        assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
        await row.screenshot({ path: root + 'long-publisher.png' });
        const share = row.locator('[data-share-open]');
        await share.click();
        await p.getByRole('dialog').waitFor();
        await p.keyboard.press('Escape');
        await p.getByRole('dialog').waitFor({ state: 'detached' });
        assert(await share.evaluate((n) => document.activeElement === n));
        const more = p.locator('[data-load-more]');
        if (await more.count()) {
            const before = await p.locator('.feed .article').count();
            await more.click();
            await p.waitForFunction((n) => document.querySelectorAll('.feed .article').length > n, before);
            const last = p.locator('.feed > .article').last();
            assert(
                await last.evaluate(
                    (n) =>
                        getComputedStyle(n.querySelector('.thumb')).display === 'none' &&
                        Math.abs(
                            n.querySelector('.src').getBoundingClientRect().left - n.getBoundingClientRect().left,
                        ) < 2,
                ),
            );
        }
        assert.deepEqual(errors, []);
        console.log(
            '50 route/width/theme checks passed; mobile More stories images hidden, full-width headlines/dates, 44px actions, short/long publisher footers, screenshots, share/focus and loaded-story layout passed.',
        );
    } finally {
        await b.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
