const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    const browser = await chromium.launch();
    const context = await browser.newContext();
    const page = await context.newPage();
    let added = false,
        url;
    const errors = [];
    page.on('pageerror', (e) => errors.push(e.message));
    async function remove(p) {
        await p.evaluate(async (url) => {
            const d = await (await fetch('/api/saved')).json();
            const r = await fetch('/api/saved/remove', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'X-Saved-CSRF': d.token },
                body: JSON.stringify({ url }),
            });
            if (!r.ok) throw Error('cleanup failed');
        }, url);
    }
    try {
        await page.goto(base + '/videos');
        await page.waitForFunction(() =>
            [...document.querySelectorAll('[data-save][data-kind="video"]')].some((b) => !b.disabled),
        );
        const button = page.locator('[data-save][data-kind="video"][aria-pressed="false"]:visible').first();
        assert(await button.count(), 'Need an unsaved video');
        url = await button.getAttribute('data-url');
        const title = await button.getAttribute('data-title');
        const source = await button.getAttribute('data-source');
        await button.click();
        added = true;
        await page.locator(`[data-save][data-url="${url}"][aria-pressed="true"]`).waitFor();
        const second = await browser.newContext();
        const other = await second.newPage();
        await other.goto(base + '/saved');
        const card = other.locator('article').filter({ has: other.locator(`a[href="${url}"]`) });
        await card.waitFor();
        assert.equal(await card.locator('.cat').textContent(), 'YouTube');
        assert.equal(await card.locator('h3').innerText(), title);
        assert.equal(await card.locator('.src__name').innerText(), source);
        assert.equal(await card.locator('h3 a').getAttribute('target'), '_blank');
        assert(await card.locator('img').count());
        await other.reload();
        await card.waitFor();
        assert.equal(await card.locator('.cat').textContent(), 'YouTube');
        for (const width of [390, 768, 1440]) {
            await page.setViewportSize({ width, height: 900 });
            for (const route of ['/', '/videos', '/saved']) {
                await page.goto(base + route);
                await page.waitForTimeout(250);
                assert(
                    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                    route + ' overflow at ' + width,
                );
                if (route !== '/saved') assert(await page.locator('[data-save][data-kind="video"]').count());
            }
        }
        await card.locator('[data-save]').click();
        await card.waitFor({ state: 'detached' });
        added = false;
        await page.goto(base + '/videos');
        await page.locator(`[data-save][data-url="${url}"][aria-pressed="false"]`).waitFor();
        assert.deepEqual(errors, []);
        assert.equal((await page.request.get(base + '/healthz')).status(), 200);
        await second.close();
        console.log(
            'Watch later: real video save, channel/thumbnail/type, independent device, reload persistence, remove and pressed-state sync, Home/Videos/Saved at 390/768/1440px, no JS errors, health 200 passed.',
        );
    } finally {
        if (added) await remove(page);
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
