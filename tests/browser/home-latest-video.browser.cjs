// Profile-scoped latest home video, empty state, mobile folds and save actions. Temporary profiles are removed.
const { chromium } = require('playwright');
const lib = require('./lib.cjs');
const assert = require('node:assert/strict');

const base = lib.base;
const shots = lib.shotDir;
const password = lib.adminPassword;

(async () => {
    const profiles =
        !!password || (await fetch(base + '/editions', { headers: { Accept: 'text/html' } })).status !== 404;
    if (profiles && !password) {
        console.log('SKIP: profiles are on here; set ADMIN_PASSWORD so the check can add a throwaway profile.');
        return;
    }
    const browser = await chromium.launch({
        args: process.env.HOST_RESOLVER_RULES ? ['--host-resolver-rules=' + process.env.HOST_RESOLVER_RULES] : [],
    });
    let admin, id, storageState;
    const contextFor = async (width, { scheme = 'kansas', theme = 'light', js = true } = {}) => {
        const context = await browser.newContext({
            viewport: { width, height: 900 },
            javaScriptEnabled: js,
            storageState,
        });
        await context.addInitScript(
            ([s, t]) => {
                try {
                    if (!localStorage.getItem('mynewsfeed-color-scheme'))
                        localStorage.setItem('mynewsfeed-color-scheme', s);
                    if (!localStorage.getItem('theme')) localStorage.setItem('theme', t);
                } catch {}
            },
            [scheme, theme],
        );
        context.setDefaultNavigationTimeout(90000);
        return context;
    };
    // With profiles on, the scheme and theme are the open profile's settings, so set them there and reload.
    const visit = async (page, path, scheme, theme) => {
        await page.goto(base + path, { waitUntil: 'networkidle' });
        if (!profiles) return;
        const saved = page.waitForResponse(
            (response) =>
                new URL(response.url()).pathname === '/api/me/settings' && response.request().method() === 'PUT',
        );
        await page.evaluate(
            ([s, t]) => {
                window.myNewspaperPrefs.setItem('mynewsfeed-color-scheme', s);
                window.myNewspaperPrefs.setItem('theme', t);
            },
            [scheme, theme],
        );
        const response = await saved;
        assert.equal(response.status(), 200, 'Profile theme and scheme save succeeds before reload');
        await page.reload({ waitUntil: 'networkidle' });
    };
    try {
        if (profiles) {
            admin = await lib.signInAdmin(browser, { navigationTimeout: 90000 });
            const profile = await lib.addProfile(admin, 'HomeVideo', { pin: '3691' });
            id = profile.id;
            console.log('Temporary video profile', id);
            storageState = await lib.openProfile(browser, profile, { path: '/news/all' });
        }
        if (shots) require('node:fs').mkdirSync(shots, { recursive: true });
        const setupContext = await contextFor(1440),
            setup = await setupContext.newPage();
        if (profiles) {
            await setup.goto(base + '/settings#channels', { waitUntil: 'networkidle' });
            const toggle = async (on) => {
                await setup.locator('details').evaluateAll((nodes) => nodes.forEach((n) => (n.open = true)));
                const button = setup
                    .locator(`#channels [role="switch"][aria-checked="${on ? 'true' : 'false'}"]`)
                    .first();
                if (!(await button.count())) return false;
                const response = setup.waitForResponse(
                    (r) => r.request().method() === 'POST' && r.url().includes('/me/channels/'),
                );
                await button.click();
                await response;
                await setup.waitForLoadState('networkidle');
                return true;
            };
            // This is a disposable profile. Never change another reader's follows.
            while (await toggle(true)) {
                // Keep clearing only this disposable profile’s inherited follows.
            }
            await setup.goto(base + '/', { waitUntil: 'networkidle' });
            assert.equal(await setup.locator('[data-home-latest-video] [data-latest-video-id]').count(), 0);
            assert(await setup.locator('[data-home-latest-video] a[href="settings#channels"]').count());
            await setup.goto(base + '/settings#channels', { waitUntil: 'networkidle' });
            assert(await toggle(false), 'at least one household channel is available');
            await toggle(false);
        }
        await setup.close();
        await setupContext.close();
        const referenceContext = await contextFor(1440),
            reference = await referenceContext.newPage();
        await reference.goto(base + '/', { waitUntil: 'networkidle' });
        const ref = reference.locator('[data-home-latest-video] [data-latest-video-id]').first();
        assert(await ref.count(), 'followed channels have a collected video');
        const expected = await ref.getAttribute('data-latest-video-id');
        await referenceContext.close();
        for (const theme of lib.THEMES) {
            const settingsContext = await contextFor(1440, { theme }),
                settings = await settingsContext.newPage();
            await visit(settings, '/', 'ribbon', theme);
            await settingsContext.close();
            for (const js of [true, false]) {
                const context = await contextFor(390, { theme, js }),
                    p = await context.newPage();
                for (const width of [320, 390, 430, 820, 1024, 1440]) {
                    await p.setViewportSize({ width, height: 1000 });
                    await p.goto(base + '/', { waitUntil: 'networkidle' });
                    const card = p.locator('[data-home-latest-video]').filter({ visible: true }).first();
                    if (!(await card.count())) {
                        const fold = p.locator('details[data-home-section="opinion"]').filter({ visible: true });
                        await fold.locator('summary').click();
                    }
                    await card.scrollIntoViewIfNeeded();
                    assert.equal(
                        await card.locator('[data-latest-video-id]').getAttribute('data-latest-video-id'),
                        expected,
                    );
                    assert.equal(await p.locator('[data-home-opinion]').count(), 0);
                    const link = card.locator('.home-latest-video__title a');
                    assert((await link.getAttribute('href')).startsWith('https://'));
                    assert.equal(await link.getAttribute('target'), '_blank');
                    assert(await card.locator('time').textContent());
                    assert(await card.locator('.home-latest-video__channel').textContent());
                    assert(
                        await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                        'no horizontal overflow',
                    );
                    if (js) {
                        const save = card.locator('[data-save]');
                        const state = await save.getAttribute('aria-pressed');
                        await save.click();
                        await p.waitForFunction(({ e, state }) => e.getAttribute('aria-pressed') !== state, {
                            e: await save.elementHandle(),
                            state,
                        });
                        await save.click();
                        for (const selector of ['[data-save]', '[data-share-open]']) {
                            const box = await card.locator(selector).boundingBox();
                            assert(box.width >= 44 && box.height >= 44, 'touch target');
                        }
                        const shareBox = await card.locator('[data-share-open]').boundingBox();
                        const saveBox = await card.locator('[data-save]').boundingBox();
                        assert(Math.abs(saveBox.x - (shareBox.x + shareBox.width) - 8) < 1, '8px action gap');
                        assert(Math.abs(saveBox.y - shareBox.y) < 1, 'aligned action buttons');
                    }
                    if (shots) await card.screenshot({ path: `${shots}/${js ? 'js' : 'nojs'}-${theme}-${width}.png` });
                    console.log('PASS latest video', width, theme, js ? 'JS' : 'no JS');
                }
                await context.close();
            }
        }
    } finally {
        await lib.deleteProfiles();
        await browser.close();
    }
})().catch((e) => {
    console.error(e.message || e);
    process.exit(1);
});
