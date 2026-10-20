const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://127.0.0.1:5296';
(async () => {
    const browser = await chromium.launch({ args: process.env.PWA_HOST_IP ? [`--host-resolver-rules=MAP ${new URL(base).hostname} ${process.env.PWA_HOST_IP}`] : [] });
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
        assert.equal(await page.evaluate(async () => (await fetch('/calendar/week.ics')).status), 404);
        await page.goto(base + '/settings');
        await page.uncheck('[data-home-visible="videos"]');
        await page.locator('[data-home-move="news:-1"]').click();
        await page.goto(base + '/');
        assert(await page.locator('[data-home-section="videos"]').isHidden());
        assert(await page.locator('[data-home-grid]').evaluate(e => e.classList.contains('is-customized')));
        await page.goto(base + '/news');
        assert.equal(await page.locator('[data-news-read], [data-news-mute], .reading-actions, .reading-preferences').count(), 0);
        await page.goto(base + '/settings');
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
        // Saving still works without read/mute buttons.
        await page.goto(base + '/news?category=all');
        const first = page.locator('[data-news-url]').first(), save = first.locator('[data-save]');
        await page.waitForFunction(() => !document.querySelector('[data-news-url] [data-save]').disabled);
        const wasSaved = await save.getAttribute('aria-pressed') === 'true';
        const savedUrl = await save.getAttribute('data-url');
        if (!wasSaved) await save.click();
        await page.waitForFunction(url => document.querySelector(`[data-save][data-url="${url}"]`).getAttribute('aria-pressed') === 'true', savedUrl);
        await page.goto(base + '/saved');
        assert(await page.locator('[data-saved-list] [data-save]').count() > 0);
        assert.equal(await page.locator('[data-mark-read], [data-news-read], [data-news-mute]').count(), 0);
        if (!wasSaved) {
            await page.locator('[data-saved-list] [data-save]').evaluateAll((buttons, url) => buttons.find(b => b.dataset.url === url).click(), savedUrl);
            await page.waitForFunction(url => !document.querySelector(`[data-saved-list] [data-save][data-url="${url}"]`), savedUrl);
        }
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
