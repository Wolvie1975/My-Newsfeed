// Story cards on the headline feeds (News.razor's card layout, .hc-feed): every route/width/theme shows cards with no
// overflow; phone actions are 44px touch targets; short and long publisher names fit the card footer; share returns
// focus; Load more adds more cards. Card images and the top arrangement are headline-top's.
const lib = require('./lib.cjs');
const fs = require('node:fs'),
    assert = require('node:assert/strict'),
    { chromium } = require('playwright');
const base = lib.base,
    root = lib.screenshots('compact-stories');
const CARD = '.hc-feed article.hc-card';
(async () => {
    const b = await chromium.launch();
    try {
        const p = await lib.readerPage(b),
            errors = [];
        p.on('pageerror', (e) => errors.push(e.message));
        if (process.env.PREVIEW_CSS)
            await p.route('**/public*.css', (r) =>
                r.fulfill({ contentType: 'text/css', body: fs.readFileSync(process.env.PREVIEW_CSS, 'utf8') }),
            );
        let checked = 0;
        for (const route of ['news/all', 'sports/news', 'entertainment/news'])
            for (const width of [320, 390, 430, 768, 1440])
                for (const theme of lib.THEMES) {
                    // All headlines has its own folded phone front (headlines-phone); the cards stay on the other two.
                    if (route === 'news/all' && width < 744) continue;
                    await p.setViewportSize({ width, height: 950 });
                    await p.emulateMedia({ colorScheme: theme });
                    assert.equal((await p.goto(base + '/' + route)).status(), 200);
                    const label = `${route} ${width} ${theme}`;
                    assert(
                        await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                        label + ' overflow',
                    );
                    const card = p.locator(CARD + ':visible').first();
                    await card.waitFor();
                    if (width < 744)
                        assert(
                            await card
                                .locator('.share-button,.save')
                                .evaluateAll(
                                    (ns) => ns.length && ns.every((n) => n.getBoundingClientRect().height >= 44),
                                ),
                            label + ' 44px actions',
                        );
                    if (width === 390 && theme === 'light')
                        await card.screenshot({ path: root + route.replaceAll('/', '-') + '-card.png' });
                    checked++;
                }
        await p.setViewportSize({ width: 320, height: 950 });
        await p.emulateMedia({ colorScheme: 'light' });
        await p.goto(base + '/sports/news');
        const card = p.locator(CARD + ':visible').first();
        // The actions have their own row under the publisher; a short or long name stays inside the card above them.
        const footerFits = () =>
            card.evaluate((n) => {
                const box = (s) => n.querySelector(s).getBoundingClientRect();
                const c = n.getBoundingClientRect(),
                    pub = box('.article-publisher'),
                    name = box('.src__name'),
                    actions = box('.article-actions');
                return actions.top >= pub.bottom - 1 && name.left >= c.left - 1 && name.right <= c.right + 1;
            });
        for (const name of ['CBS News', 'The Hollywood Reporter — Film and Television Coverage']) {
            await card.locator('.src__name').evaluate((n, v) => (n.textContent = v), name);
            assert(await footerFits(), 'publisher footer fits: ' + name);
        }
        assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'long source overflow');
        await card.screenshot({ path: root + 'long-publisher.png' });
        const share = card.locator('[data-share-open]');
        await share.click();
        await p.getByRole('dialog').waitFor();
        await p.keyboard.press('Escape');
        await p.getByRole('dialog').waitFor({ state: 'detached' });
        assert(await share.evaluate((n) => document.activeElement === n), 'share returns focus');
        const more = p.locator('[data-load-more]:visible');
        let loaded = false;
        if (await more.count()) {
            const count = () => p.locator(CARD).count();
            const before = await count();
            await more.first().click();
            await p.waitForFunction(([sel, n]) => document.querySelectorAll(sel).length > n, [CARD, before]);
            // Stories added by Load more are cards too, with no overflow.
            assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'loaded overflow');
            loaded = true;
        }
        assert.deepEqual(errors, []);
        console.log(
            `${checked} route/width/theme checks passed; 44px phone actions, short/long publisher footers, share/focus` +
                (loaded ? ' and Load more passed.' : ' passed (no Load more on this data).'),
        );
    } finally {
        await b.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
