// The public and admin sites must be separate origins with independent PWA identity and authentication.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = process.env.BASE_URL || 'http://localhost:5295';
const admin = process.env.ADMIN_BASE_URL || 'http://localhost:5297';
const password = process.env.ADMIN_PASSWORD;
if (!password) throw new Error('Set ADMIN_PASSWORD for the separate admin preview.');
assert.notEqual(new URL(base).origin, new URL(admin).origin);
(async () => {
    const browser = await chromium.launch();
    const shots = process.env.SCREENSHOT_DIR || 'test-results/browser/admin-split/';
    fs.mkdirSync(shots, { recursive: true });
    try {
        const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
        const page = await context.newPage();
        const errors = [];
        page.on('pageerror', (e) => errors.push(e.message));
        for (const path of ['/admin', '/login', '/admin/api/profiles', '/admin.css', '/admin-alert.js']) {
            const response = await context.request.get(base + path, { maxRedirects: 0 });
            assert([401, 404].includes(response.status()), 'Public site must not serve ' + path);
        }
        for (const origin of [base, admin]) {
            const manifest = await (await context.request.get(origin + '/manifest.webmanifest')).json();
            assert.equal(manifest.id, origin === admin ? '/mynewspaper-admin' : '/');
            assert.equal(manifest.name, origin === admin ? 'myNewspaper Admin' : 'myNewspaper');
            assert.equal(manifest.start_url, origin === admin ? '/admin' : '/');
            for (const icon of manifest.icons)
                assert.equal((await context.request.get(origin + icon.src)).status(), 200);
            await page.goto(origin + '/offline.html');
            await page.evaluate(() => localStorage.setItem('pwa-isolation', location.origin));
            await page.goto(origin + '/');
            await page.evaluate(() => navigator.serviceWorker.ready);
            await page.waitForFunction(() => !!navigator.serviceWorker.controller);
            assert.equal(await page.evaluate(() => localStorage.getItem('pwa-isolation')), new URL(origin).origin);
            const keys = await page.evaluate(() => caches.keys());
            assert(keys.some((k) => k.startsWith(origin === admin ? 'mynewspaper-admin-pwa-' : 'mynewsfeed-pwa-')));
            assert(!keys.some((k) => k.startsWith(origin === admin ? 'mynewsfeed-pwa-' : 'mynewspaper-admin-pwa-')));
        }
        await page.goto(admin + '/login?returnUrl=%2Fadmin%2Fsources');
        for (const width of [320, 390, 430]) {
            await page.setViewportSize({ width, height: 950 });
            assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
            await page.screenshot({ path: `${shots}admin-login-${width}.png` });
        }
        await page.locator('#password').fill(password);
        await page.getByRole('button', { name: 'Sign in', exact: true }).click();
        await page.waitForURL('**/admin/sources');
        await page.locator('.adm-head h1').waitFor();
        const session = (await context.cookies(admin)).find((c) => c.name === 'myNewspaper.Admin.Session');
        assert(session?.httpOnly, 'Admin session is distinct and HTTP-only');
        assert.equal((await context.request.get(admin + '/admin/api/profiles')).status(), 200);
        assert.equal((await context.request.get(base + '/admin/api/profiles')).status(), 404);
        for (const width of [320, 390, 430, 744, 1440]) {
            for (const theme of ['light', 'dark']) {
                await page.setViewportSize({ width, height: 1100 });
                await page.evaluate((t) => localStorage.setItem('theme', t), theme);
                for (const path of ['/admin/sources', '/admin/profiles', '/admin/requests']) {
                    await page.goto(admin + path, { waitUntil: 'networkidle' });
                    assert.equal(await page.locator('html').getAttribute('data-theme'), theme);
                    assert(
                        await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                        `${width} ${theme} ${path}`,
                    );
                    assert(!(await page.locator('#blazor-error-ui').isVisible()));
                    await page.screenshot({ path: `${shots}split-${path.split('/').pop()}-${width}-${theme}.png` });
                }
                if (width < 900) {
                    await page.locator('label[for="adm-menu-toggle"]').click();
                    assert(await page.locator('.adm-side').isVisible());
                }
                const alert = page.locator('[data-admin-alert-toggle]');
                const loaded = page.waitForResponse(
                    (response) => new URL(response.url()).pathname === '/admin/api/requests/summary',
                );
                await alert.click();
                const summary = await loaded;
                assert.equal(summary.status(), 200, 'Requests panel loads from the admin API');
                const data = await summary.json();
                assert(Array.isArray(data.locked) && Array.isArray(data.waiting));
                await page.waitForFunction(
                    () => document.querySelector('[data-admin-alert-status]').textContent !== 'Loading…',
                );
                const panel = page.locator('[data-admin-alert-panel]');
                await panel.waitFor();
                const bounds = await panel.boundingBox();
                assert(
                    bounds &&
                        bounds.x >= 0 &&
                        bounds.y >= 0 &&
                        bounds.x + bounds.width <= width + 1 &&
                        bounds.y + bounds.height <= 1101,
                    'Requests panel outside viewport',
                );
                await page.screenshot({ path: `${shots}requests-panel-${width}-${theme}.png` });
                await page.keyboard.press('Escape');
                assert(!(await panel.isVisible()));
                if (width < 900) {
                    await page.locator('label[for="adm-menu-toggle"]').click();
                    assert(!(await page.locator('.adm-side').isVisible()));
                }
            }
        }
        await page.goto(admin + '/admin');
        assert.equal(
            await page.getByRole('link', { name: 'View site', exact: true }).first().getAttribute('href'),
            new URL(base).origin.replace('127.0.0.1', 'localhost') + '/',
        );
        await page.evaluate(async () => {
            const cache = await caches.open('mynewspaper-admin-pwa-v1');
            const urls = (await cache.keys()).map((r) => new URL(r.url).pathname);
            if (urls.some((path) => path.startsWith('/admin') || path === '/login'))
                throw new Error('Admin data was cached');
        });
        await context.setOffline(true);
        await page.goto(admin + '/admin');
        assert(await page.getByRole('heading', { name: 'Admin is offline' }).isVisible());
        await context.setOffline(false);
        await page.goto(admin + '/admin');
        await page.locator('.adm-head h1').waitFor();
        assert.deepEqual(errors, []);
        console.log(
            'Separate PWA identity, storage/cache isolation, route isolation, login, responsive admin, themes, menu and offline recovery passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
