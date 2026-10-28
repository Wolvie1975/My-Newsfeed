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
        // Sports and Entertainment have their own front pages (section-hubs); these routes use the newspaper feed (rows from 744px, picture cards on phones).
        for (const route of ['news', 'sports/news', 'entertainment/news'])
            for (const width of [320, 390, 430, 768, 1440])
                for (const theme of ['light', 'dark']) {
                    // Headlines has its own folded phone front (headlines-phone); the cards stay on the other two.
                    if (route === 'news' && width < 744) continue;
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
                    if (width < 744) {
                        // Phones (October 26, 2026): every story is a picture card, as on F1 News.
                        assert(
                            await p
                                .locator('.paper-feed .article .thumb')
                                .evaluateAll((ns) => ns.every((n) => getComputedStyle(n).display !== 'none')),
                            'mobile headline pictures shown ' + route,
                        );
                        assert(
                            await row.evaluate((n) => {
                                const a = n.getBoundingClientRect(),
                                    t = n.querySelector('.thumb').getBoundingClientRect();
                                return Math.abs(t.width - a.width) < 3 && t.top - a.top < 2;
                            }),
                            'full width picture on top ' + route,
                        );
                        assert(
                            await row
                                .locator('.share-button,.save')
                                .evaluateAll((ns) => ns.every((n) => n.getBoundingClientRect().height >= 44)),
                        );
                        if (width === 390 && theme === 'light')
                            await row.screenshot({ path: root + route.replaceAll('/', '-') + '-row.png' });
                    }
                    if (width >= 744) {
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
                                }, width >= 744),
                                'large More stories image ' + route,
                            );
                    }
                    if (route === 'news' && (width <= 430 || width === 1440))
                        await p.screenshot({ path: root + 'news-' + width + '-' + theme + '.png' });
                }
        await p.setViewportSize({ width: 320, height: 950 });
        await p.goto(base + '/sports/news');
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
        // Picture cards (as on F1 News): a long name wraps in its own column and the actions stay beside it.
        assert(
            await row.evaluate((n) => {
                const name = n.querySelector('.src__name').getBoundingClientRect();
                return [...n.querySelectorAll('.share-button,.save')].every(
                    (b) => b.getBoundingClientRect().left >= name.right - 1,
                );
            }),
            'long source clear of the actions',
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
            // Stories added by Load more are the same picture cards on phones.
            assert(
                await last.evaluate((n) => {
                    const t = n.querySelector('.thumb');
                    return (
                        getComputedStyle(t).display !== 'none' &&
                        Math.abs(t.getBoundingClientRect().width - n.getBoundingClientRect().width) < 3
                    );
                }),
                'loaded story is a picture card',
            );
        }
        assert.deepEqual(errors, []);
        console.log(
            '50 route/width/theme checks passed; phone picture cards (as on F1 News), 44px actions, short/long publisher footers, screenshots, share/focus and loaded-story layout passed.',
        );
    } finally {
        await b.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
