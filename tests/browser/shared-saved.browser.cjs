const assert = require('node:assert/strict');
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const base = process.env.BASE_URL || 'http://127.0.0.1:5297';
(async () => {
    const browser = await chromium.launch({ args: process.env.PWA_HOST_IP ? [`--host-resolver-rules=MAP ${new URL(base).hostname} ${process.env.PWA_HOST_IP}`] : [] });
    const id = require('node:crypto').randomUUID();
    const urls = ['legacy', 'new', 'blocked', 'retry'].map(name => `https://example.com/mydailyfeed-test/${id}/${name}`);
    let page;
    const errors = [];
    async function ready(p) { await p.waitForFunction(() => [...document.querySelectorAll('[data-save]')].every(b => !b.disabled)); }
    async function refresh(p) { await p.evaluate(() => window.dispatchEvent(new Event('focus'))); }
    function card(p, url) { return p.locator('[data-saved-list] article').filter({ has: p.locator(`a[href="${url}"]`) }); }
    async function inject(p, url) {
        await p.evaluate(url => {
            const b = document.createElement('button'); b.dataset.save = ''; b.dataset.url = url;
            b.dataset.title = 'Shared Saved test'; b.dataset.summary = '<img src=x onerror=alert(1)>';
            document.body.appendChild(b);
        }, url);
        const button = p.locator(`button[data-url="${url}"]`).last();
        await button.waitFor(); await p.waitForFunction(url => !document.querySelector(`button[data-url="${url}"]`).disabled, url);
        await button.click();
    }
    try {
        const emptyContext = await browser.newContext();
        const emptyPage = await emptyContext.newPage();
        await emptyPage.route('**/api/saved', route => route.fulfill({ contentType: 'application/json', body: '{"articles":[],"token":"empty-test"}' }));
        await emptyPage.goto(base + '/saved');
        await emptyPage.getByRole('heading', { name: 'Your Saved list is empty' }).waitFor();
        assert.equal(await emptyPage.locator('[data-reading-filter], [data-reading-count], [data-reading-toolbar]').count(), 0);
        await emptyContext.close();
        const first = await browser.newContext(), second = await browser.newContext();
        await first.addInitScript(({ id, url }) => {
            if (!localStorage.getItem('shared-test-seeded')) {
                localStorage.setItem('saved-articles', JSON.stringify([{ url, title: 'Imported legacy bookmark', read: true, savedAt: '2026-01-01T12:00:00Z' }]));
                Object.defineProperty(crypto, 'randomUUID', { value: undefined });
                localStorage.setItem('shared-test-seeded', '1');
            }
        }, { id, url: urls[0] });
        page = await first.newPage(); const other = await second.newPage();
        for (const p of [page, other]) p.on('pageerror', e => errors.push(e.message));
        await page.goto(base + '/saved');
        await card(page, urls[0]).waitFor();
        await other.goto(base + '/saved'); await card(other, urls[0]).waitFor();
        // Save in one device; the other discovers it on the actual periodic refresh.
        await inject(page, urls[1]); await card(page, urls[1]).waitFor();
        await card(other, urls[1]).waitFor({ timeout: 25000 });
        assert.equal(await card(page, urls[1]).locator('summary img, .summary img').count(), 0);
        await ready(other); await card(other, urls[0]).locator('[data-save]').click();
        await card(other, urls[0]).waitFor({ state: 'detached' });
        await refresh(page); await card(page, urls[0]).waitFor({ state: 'detached' });
        // Replayed import receipt cannot resurrect a bookmark another device removed.
        await page.evaluate(() => { const receipt = JSON.parse(localStorage.getItem('mydailyfeed-saved-import-v1')); receipt.done = false; localStorage.setItem('mydailyfeed-saved-import-v1', JSON.stringify(receipt)); });
        await page.reload(); await page.waitForFunction(() => JSON.parse(localStorage.getItem('mydailyfeed-saved-import-v1')).done);
        assert.equal(await card(page, urls[0]).count(), 0);
        const statuses = await page.evaluate(async () => {
            const data = await (await fetch('/api/saved')).json();
            const post = (value, token) => fetch('/api/saved/add', { method: 'POST', headers: { 'Content-Type': 'application/json', ...(token ? { 'X-Saved-CSRF': token } : {}) }, body: JSON.stringify(value) }).then(r => r.status);
            return [await post({ url: 'https://example.com/csrf', title: 'blocked' }), await post({ url: 'javascript:alert(1)', title: 'unsafe' }, data.token)];
        });
        assert.deepEqual(statuses, [400, 400]);
        const blocked = await browser.newContext();
        await blocked.addInitScript(() => { Storage.prototype.setItem = function () { throw new Error('blocked'); }; });
        const noStorage = await blocked.newPage(); noStorage.on('pageerror', e => errors.push(e.message));
        await noStorage.goto(base + '/saved'); await card(noStorage, urls[1]).waitFor();
        await inject(noStorage, urls[2]); await card(noStorage, urls[2]).waitFor();
        await refresh(page); await card(page, urls[2]).waitFor();
        await page.route('**/api/saved/add', route => route.fulfill({ status: 503, contentType: 'application/json', body: '{"error":"Test connection unavailable"}' }));
        await inject(page, urls[3]);
        await page.getByText('Test connection unavailable', { exact: true }).waitFor();
        assert.equal(await card(page, urls[3]).count(), 0);
        await page.unroute('**/api/saved/add');
        await page.locator(`button[data-url="${urls[3]}"]`).last().click();
        await card(page, urls[3]).waitFor();
        await first.setOffline(true); await refresh(page);
        await page.waitForFunction(() => [...document.querySelectorAll('[data-save]')].every(b => b.disabled));
        assert(await card(page, urls[1]).isVisible());
        await first.setOffline(false); await refresh(page); await ready(page);
        for (const width of [390, 1440]) {
            await page.setViewportSize({ width, height: 900 });
            assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
        }
        assert.deepEqual(errors, []);
        console.log('Shared Saved: independent-device import/save/remove, periodic sync, replay protection, CSRF, unsafe links, blocked storage, failure/retry, offline recovery and responsive checks passed.');
    } finally {
        if (page) {
            await page.context().setOffline(false);
            await page.unroute('**/api/saved/add');
            await page.evaluate(async urls => {
                const data = await (await fetch('/api/saved')).json();
                for (const url of urls) {
                    const response = await fetch('/api/saved/remove', { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-Saved-CSRF': data.token }, body: JSON.stringify({ url }) });
                    if (!response.ok) throw new Error('Test bookmark cleanup failed');
                }
            }, urls);
        }
        await browser.close();
    }
})().catch(error => { console.error(error); process.exit(1); });
