// YouTube channel categories (October 26, 2026): tabs come from the scraper's YoutubeCategories, every tab has videos,
// the tabs together hold exactly what All holds (Other catches uncategorized channels), each tab shows only its own
// videos and channels, phones choose with a dropdown, and a one-video carousel has no controls.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
const total = async (p) => Number((await p.locator('.filters--videos .count').innerText()).match(/(\d+) videos?/)[1]);

(async () => {
    const browser = await chromium.launch();
    try {
        const p = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
        const errors = [];
        p.on('pageerror', (e) => errors.push(e.message));
        await p.goto(base + '/videos');
        const tabs = await p
            .locator('.yt-cats a')
            .evaluateAll((ns) => ns.map((n) => ({ name: n.textContent.trim(), href: n.getAttribute('href') })));
        assert.equal(tabs[0].name, 'All');
        assert(tabs.length > 1, 'category tabs shown');
        const all = await total(p);
        let sum = 0;
        for (const tab of tabs.slice(1)) {
            await p.goto(base + '/' + tab.href);
            assert.equal(await p.locator('.yt-cats a[aria-current="page"]').innerText(), tab.name);
            const count = await total(p);
            assert(count > 0, tab.name + ' is not empty');
            sum += count;
            assert((await p.locator('.filters--videos .count').innerText()).startsWith(tab.name));
            // Inside a tab the kicker is the channel alone; the channel list holds only this tab's channels.
            assert.equal(await p.locator('.yt-kicker', { hasText: tab.name + ' · ' }).count(), 0);
            const channels = await p.locator('#channel option').count();
            if (channels > 1) {
                await p.selectOption('#channel', { index: 1 });
                await p.waitForURL(/channel=/);
                assert(new URL(p.url()).searchParams.get('category'), 'channel choice keeps the category');
                assert((await total(p)) <= count);
            }
            await p.goto(base + '/' + tab.href);
            const slides = await p.locator('[data-yt-slide]').count();
            assert.equal(await p.locator('[data-yt-controls]').count(), slides > 1 ? 1 : 0, tab.name + ' controls');
        }
        assert.equal(sum, all, 'the tabs together hold every video in All');
        // All: the category leads the channel name.
        await p.goto(base + '/videos');
        assert(await p.locator('.yt-kicker').first().innerText());
        // An unknown category falls back to All; an old channel link from another category is dropped, not emptied.
        await p.goto(base + '/videos?category=no-such-thing');
        assert.equal(await total(p), all);
        await p.setViewportSize({ width: 390, height: 900 });
        await p.goto(base + '/videos');
        assert(await p.locator('.yt-cats').isHidden());
        assert(await p.locator('select#category').isVisible());
        const slug = new URL(base + '/' + tabs[1].href).searchParams.get('category');
        await p.selectOption('select#category', slug);
        await p.waitForURL('**/videos?**category=' + slug + '**');
        for (const width of [320, 390, 430, 744, 1440]) {
            await p.setViewportSize({ width, height: 900 });
            await p.goto(base + '/' + tabs[1].href);
            assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'overflow ' + width);
            assert(
                await p
                    .locator('.yt-cats a, select#category')
                    .evaluateAll((ns) =>
                        ns
                            .filter((n) => n.getClientRects().length)
                            .every((n) => n.getBoundingClientRect().height >= 44),
                    ),
                '44px category controls ' + width,
            );
        }
        assert.deepEqual(errors, []);
        console.log(
            `YouTube categories: ${tabs.length - 1} tabs (${tabs
                .slice(1)
                .map((t) => t.name)
                .join(
                    ', ',
                )}) add up to All (${all}); per-tab channels, kickers, carousel controls, phone dropdown and 320–1440px passed.`,
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e.message || e);
    process.exit(1);
});
