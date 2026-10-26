const assert = require('node:assert/strict'),
    { chromium } = require('playwright');
const base = process.env.BASE_URL || 'http://localhost:5295',
    root = process.env.SCREENSHOT_DIR || 'test-results/';
(async () => {
    const b = await chromium.launch();
    try {
        const p = await b.newPage(),
            errors = [];
        p.on('pageerror', (e) => errors.push(e.message));
        for (const route of ['sports', 'entertainment'])
            for (const width of [320, 390, 430, 768, 1440])
                for (const theme of ['light', 'dark']) {
                    await p.setViewportSize({ width, height: 950 });
                    await p.emulateMedia({ colorScheme: theme });
                    assert.equal((await p.goto(base + '/' + route)).status(), 200);
                    assert.equal(await p.locator('h1').innerText(), route === 'sports' ? 'Sports' : 'Entertainment');
                    assert(
                        await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                        route + ' overflow ' + width,
                    );
                    assert(!(await p.locator('#blazor-error-ui').isVisible()));
                    // Sports keeps its hub; Entertainment is the concept front page (EntertainmentFront).
                    const cards = p.locator(route === 'sports' ? '.paper-feed .article' : '.ef-lead, .ef-headlines li');
                    assert((await cards.count()) > 0 && (await cards.count()) <= 8);
                    assert.equal(await p.locator('#category,#source,[data-load-more]').count(), 0);
                    assert(await p.locator(route === 'sports' ? '.front-agenda' : '.ef-screen').isVisible());
                    // Section navigation: underlined tabs from 768px, a dropdown on phones.
                    if (width < 768) {
                        assert.equal(await p.locator('#' + route + '-section').inputValue(), '/' + route);
                        assert((await p.locator('#' + route + '-section').boundingBox()).height >= 44);
                        assert(!(await p.locator('.section-tabs').isVisible()));
                    } else {
                        const current = p.locator('.section-tabs a[aria-current="page"]');
                        assert.equal(await current.getAttribute('href'), route);
                        assert((await current.boundingBox()).height >= 44);
                        assert(!(await p.locator('#' + route + '-section').isVisible()));
                    }
                    assert.equal(await p.locator('.hub-links').count(), 0);
                    if (route === 'sports') assert(await p.locator('[data-scoreboard]').count());
                    else assert(await p.locator('.ef-box').isVisible());
                    assert(
                        await p
                            .locator('.hub-all-news,.front-agenda a,.ef-head a,.ef-more')
                            .evaluateAll((ns) =>
                                ns.every((n) => !n.offsetParent || n.getBoundingClientRect().height >= 44),
                            ),
                    );
                    if (width < 700 && (await p.locator('.paper-support').count()))
                        assert(
                            (await p.locator('.news-front__agenda').evaluate((n) => n.getBoundingClientRect().top)) <
                                (await p.locator('.paper-support').evaluate((n) => n.getBoundingClientRect().top)),
                        );
                    if (width <= 430) {
                        await p.locator('.mobile-nav > summary').click();
                        const link = p
                            .locator('[aria-labelledby="mobile-' + route + '-heading"] .mobile-subnav a')
                            .first();
                        assert.equal(await link.getAttribute('href'), route);
                        assert.equal(await link.getAttribute('aria-current'), 'page');
                        await p.locator('.mobile-nav > summary').click();
                    } else if (width === 1440) {
                        // Desktop: the hub is a plain masthead link; its pages are reached from the section tabs.
                        assert.equal(
                            await p.locator('.site-nav--inline a[href="' + route + '"]').getAttribute('aria-current'),
                            'page',
                        );
                        await p.locator('.section-tabs a[href="' + route + '/news"]').waitFor({ state: 'visible' });
                    }
                    if (width <= 430 || width === 1440)
                        await p.screenshot({ path: root + route + '-' + width + '-' + theme + '.png', fullPage: true });
                }
        for (const route of ['sports', 'entertainment']) {
            await p.goto(base + '/' + route);
            await p.locator(route === 'sports' ? '.hub-all-news' : '.ef-main > .ef-more').click();
            await p.waitForURL('**/' + route + '/news');
            await p.locator('#hub-category').waitFor();
            await p.locator('#source').waitFor();
            assert(await p.locator('#source').count());
            assert.equal(await p.locator('.front-agenda,[data-scoreboard]').count(), 0);
            await p
                .getByRole('link', { name: '← ' + (route === 'sports' ? 'Sports' : 'Entertainment') + ' overview' })
                .click();
            await p.waitForURL('**/' + route);
            await p.reload();
            assert.equal(await p.locator('h1').innerText(), route === 'sports' ? 'Sports' : 'Entertainment');
        }
        for (const [hub, target] of [
            ['sports', 'scoreboard'],
            ['entertainment', 'movies'],
        ]) {
            await p.goto(base + '/' + hub);
            await p.locator('.section-tabs a[href="' + target + '"]').click();
            await p.waitForURL('**/' + target);
            assert.equal(new URL(p.url()).pathname, '/' + target);
        }
        await p.goto(base + '/sports');
        const share = p.locator('.paper-feed [data-share-open]').first();
        await share.click();
        await p.getByRole('dialog').waitFor();
        await p.keyboard.press('Escape');
        await p.getByRole('dialog').waitFor({ state: 'detached' });
        assert(await share.evaluate((n) => document.activeElement === n));
        const plain = await b.newPage({ javaScriptEnabled: false, viewport: { width: 390, height: 950 } });
        for (const route of ['sports', 'entertainment']) {
            await plain.goto(base + '/news');
            await plain.locator('.mobile-nav > summary').click();
            await plain
                .locator('[aria-labelledby="mobile-' + route + '-heading"] .mobile-subnav a')
                .first()
                .click();
            await plain.waitForURL('**/' + route);
            assert(await plain.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
            await plain.selectOption('#' + route + '-section', '/' + route + '/news');
            await plain.locator('.section-select button').click();
            await plain.waitForURL('**/' + route + '/news');
        }
        assert.deepEqual(errors, []);
        console.log(
            '20 hub width/theme views passed; bounded stories, scores/releases/agendas/box office, mobile order, 44px links, desktop tabs and phone dropdowns, hub/article navigation, reload, share focus and no-JS links passed.',
        );
    } finally {
        await b.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
