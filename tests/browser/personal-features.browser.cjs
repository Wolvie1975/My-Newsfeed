const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://127.0.0.1:5296';
(async () => {
    const browser = await chromium.launch();
    try {
        const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
        const page = await context.newPage(), errors = [];
        page.on('pageerror', e => errors.push(e.message));
        // Removed controls must not survive in any of their old entry points.
        for (const path of ['/', '/videos', '/movies', '/movies/digital', '/tv', '/settings']) {
            assert.equal((await page.goto(base + path)).status(), 200);
            assert.equal(await page.locator('a[href^="week"], a[href^="search"], a[href^="watchlist"], [data-watch-add], [data-episode-status]').count(), 0);
        }
        assert.equal((await page.goto(base + '/watchlist')).status(), 404);
        assert.equal((await page.goto(base + '/search?q=Kansas')).status(), 404);
        assert.equal((await page.goto(base + '/week')).status(), 404);
        assert.equal((await page.request.get(base + '/calendar/week.ics')).status(), 404);
        await page.goto(base + '/settings');
        await page.uncheck('[data-home-visible="videos"]');
        await page.locator('[data-home-move="news:-1"]').click();
        await page.goto(base + '/');
        assert(await page.locator('[data-home-section="videos"]').isHidden());
        assert(await page.locator('[data-home-grid]').evaluate(e => e.classList.contains('is-customized')));
        await page.goto(base + '/news');
        await page.locator('[data-news-read]').first().click();
        await page.check('[data-news-hide-read]');
        assert(await page.locator('[data-news-url]').first().isHidden());
        await page.locator('[data-news-mute]:visible').first().click();
        await page.goto(base + '/settings');
        await page.locator('[data-news-unmute]').first().click();
        await page.selectOption('[data-news-category]', 'all');
        await page.goto(base + '/news');
        await page.waitForURL('**/news?category=all');
        // Homepage choices still synchronize between tabs.
        const other = await context.newPage();
        await other.goto(base + '/');
        await page.goto(base + '/settings');
        await page.check('[data-home-visible="videos"]');
        await other.waitForFunction(() => !document.querySelector('[data-home-section="videos"]').hidden);
        await other.close();
        // Feed and Saved keep the same read state.
        await page.goto(base + '/news?category=all');
        await page.uncheck('[data-news-hide-read]');
        const first = page.locator('[data-news-url]').first(), save = first.locator('[data-save]');
        if (await save.getAttribute('aria-pressed') !== 'true') await save.click();
        const mark = first.locator('[data-news-read]');
        if (await mark.getAttribute('aria-pressed') !== 'true') await mark.click();
        await page.goto(base + '/saved');
        assert.equal(await page.locator('[data-mark-read]').first().getAttribute('aria-pressed'), 'true');
        await page.locator('[data-mark-read]').first().click();
        await page.goto(base + '/news?category=all');
        assert.equal(await page.locator('[data-news-read]').first().getAttribute('aria-pressed'), 'false');
        const noJs = await browser.newContext({ javaScriptEnabled: false }), plain = await noJs.newPage();
        for (const path of ['/local', '/tv']) assert.equal((await plain.goto(base + path)).status(), 200);
        await noJs.close();
        const blocked = await browser.newContext();
        await blocked.addInitScript(() => { Storage.prototype.setItem = function () { throw Error('blocked'); }; });
        const blockedPage = await blocked.newPage();
        await blockedPage.goto(base + '/settings');
        assert((await blockedPage.locator('[data-personal-status]').innerText()).includes('unavailable'));
        await blocked.close();
        assert.deepEqual(errors, []);
        console.log('My Week/Search/Watchlist removal, homepage, reading, cross-tab, blocked-storage and no-JavaScript checks passed.');
    } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exit(1); });
