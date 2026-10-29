// Settings disclosures; uses and removes a temporary profile. Requires ADMIN_PASSWORD.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');

const base = process.env.BASE_URL || 'http://localhost:5295';
const adminBase = process.env.ADMIN_BASE_URL || base;
const shots = process.env.SCREENSHOT_DIR;
const password = process.env.ADMIN_PASSWORD;

(async () => {
    const profiles = (await fetch(base + '/editions', { headers: { Accept: 'text/html' } })).status !== 404;
    if (profiles && !password) {
        console.log('SKIP: profiles are on here; set ADMIN_PASSWORD so the check can add a throwaway profile.');
        return;
    }
    const browser = await chromium.launch();
    let admin = null,
        id = null,
        storageState;
    const look = (scheme, theme) => ({ 'mynewsfeed-color-scheme': scheme, theme });
    const contextFor = async (width, settings = {}, js = true) => {
        const context = await browser.newContext({
            viewport: { width, height: 900 },
            javaScriptEnabled: js,
            storageState,
        });
        if (!profiles)
            await context.addInitScript((s) => {
                try {
                    for (const [k, v] of Object.entries(s)) localStorage.setItem(k, v);
                } catch {}
            }, settings);
        // Third-party posters are left out: the poster hosts throttle a run's hundreds of fresh fetches (and without
        // JavaScript Chrome ignores loading="lazy"), which stalls the load event. Pages fall back to no poster.
        await context.route('**/*', (route) =>
            route.request().resourceType() === 'image' && !route.request().url().startsWith(base)
                ? route.abort()
                : route.fallback(),
        );
        // A private Saved list, so the check never touches the real one.
        let saved = [];
        await context.route('**/api/saved**', (route) => {
            const req = route.request(),
                path = new URL(req.url()).pathname;
            if (req.method() === 'POST') {
                const item = req.postDataJSON();
                if (path.endsWith('/add')) saved.push(item);
                if (path.endsWith('/remove')) saved = saved.filter((a) => a.url !== item.url);
            }
            return route.fulfill({ json: { token: 'test-only', articles: saved } });
        });
        return context;
    };
    // With profiles on, settings belong to the open profile, so set them there and reload.
    const visit = async (page, path, settings = {}) => {
        await page.goto(base + path, { waitUntil: 'networkidle' });
        if (!profiles || !Object.keys(settings).length) return;
        await page.evaluate((s) => {
            for (const [k, v] of Object.entries(s)) window.myNewspaperPrefs.setItem(k, v);
        }, settings);
        await page.waitForTimeout(900);
        await page.reload({ waitUntil: 'networkidle' });
    };
    try {
        if (profiles) {
            admin = await (await browser.newContext({ viewport: { width: 1440, height: 1000 } })).newPage();
            await admin.goto(adminBase + '/login?returnUrl=%2Fadmin%2Fprofiles');
            await admin.locator('#password').fill(password);
            await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
            await admin.waitForURL('**/admin/profiles');
            await admin.waitForLoadState('networkidle');
            const name = 'zz Panels ' + Date.now().toString(36);
            await admin.getByRole('button', { name: 'Add profile', exact: true }).first().click();
            await admin.locator('#add-name').fill(name);
            await admin.locator('#add-pin').fill('3691');
            await admin.locator('#add-confirm').fill('3691');
            await admin.locator('form[aria-labelledby="add-title"] button[type="submit"]').click();
            await admin.getByText(`Added ${name}.`).waitFor();
            id = Number(
                (await admin.locator('.adm-table a.adm-row-title', { hasText: name }).getAttribute('href')).match(
                    /(\d+)$/,
                )[1],
            );
            const context = await browser.newContext({ viewport: { width: 390, height: 900 } });
            const p = await context.newPage();
            await p.goto(base + '/sports');
            await p.locator(`form[action="profiles/${id}/open"]`).getByRole('button').click();
            await p.keyboard.type('3691');
            await p.waitForURL('**/sports');
            storageState = await context.storageState();
            await context.close();
        }

        for (const width of [320, 390, 430, 744, 1440])
            for (const theme of ['light', 'dark']) {
                const ctx = await contextFor(width, look('kansas', theme)),
                    p = await ctx.newPage();
                await visit(p, '/settings', look('kansas', theme));
                const panels = p.locator('details.settings-panel');
                assert.equal(await panels.count(), 9, 'all Settings sections collapse');
                assert.equal(await p.locator('details.settings-panel[open]').count(), 0, 'start collapsed');
                for (let i = 0; i < (await panels.count()); i++) {
                    const panel = panels.nth(i),
                        summary = panel.locator(':scope > summary');
                    assert((await summary.boundingBox()).height >= 44, '44px summary');
                    await summary.click();
                    assert(await panel.evaluate((n) => n.open));
                    await summary.focus();
                    await p.keyboard.press('Enter');
                    assert(!(await panel.evaluate((n) => n.open)), 'keyboard closes');
                    await p.keyboard.press('Space');
                    assert(await panel.evaluate((n) => n.open), 'keyboard opens');
                    await summary.click();
                }
                if (width < 500) {
                    await p.locator('#settings-jump-m').selectOption('/settings#color-scheme-heading');
                } else await p.locator('.set-index a[href="settings#color-scheme-heading"]').click();
                const colors = p.locator('[data-settings-section="colors"]');
                await p.evaluate(
                    () => new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve))),
                );
                assert(await colors.evaluate((n) => n.open), 'jump opens target');
                assert((await colors.locator('summary').boundingBox()).y >= 64, 'jump heading clears sticky header');
                await colors.locator('[data-color-scheme][value="monochrome"]').check();
                await p.waitForTimeout(900);
                await p.reload({ waitUntil: 'networkidle' });
                assert(await colors.evaluate((n) => n.open), 'deep link opens after reload');
                assert(await colors.locator('[data-color-scheme][value="monochrome"]').isChecked(), 'saved scheme');
                await colors.locator('summary').click();
                if (width < 500) {
                    await p.locator('#settings-jump-m').selectOption('/settings#appearance-heading');
                    await p.locator('#settings-jump-m').selectOption('/settings#color-scheme-heading');
                } else await p.locator('.set-index a[href="settings#color-scheme-heading"]').click();
                assert(await colors.evaluate((n) => n.open), 'revisit target opens');
                const arrange = p.locator('.settings-arrange');
                await arrange.locator('summary').click();
                await p.getByRole('button', { name: 'Move up Color scheme', exact: true }).click();
                assert.equal(
                    await p
                        .locator('[data-settings-sections] > [data-settings-section]')
                        .first()
                        .getAttribute('data-settings-section'),
                    'colors',
                    'reorder preserves panels',
                );
                assert(await colors.evaluate((n) => n.open));
                await p.getByRole('button', { name: 'Restore default Settings order', exact: true }).click();
                await arrange.locator('summary').click();
                await p.evaluate(() => {
                    location.hash = '';
                });
                await colors.locator('summary').click();
                assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'no overflow');
                if (shots) await p.screenshot({ path: shots + '/' + width + '-' + theme + '-closed.png' });
                const profile = p.locator('#profile');
                await profile.locator('summary').click();
                await profile.locator('.set-color').last().click();
                await profile.locator('form[action="me/profile"] button').click();
                await p.waitForLoadState('networkidle');
                assert(await p.locator('#profile').evaluate((n) => n.open), 'save reveals feedback');
                assert(
                    (await p.locator('#profile > summary').boundingBox()).y >= 64,
                    'saved form heading clears sticky header',
                );
                assert(await p.locator('#profile .set-colors input').last().isChecked());
                if (shots) await p.screenshot({ path: shots + '/' + width + '-' + theme + '-open.png' });
                await ctx.close();
            }
        const ctx = await contextFor(390, {}, false),
            p = await ctx.newPage();
        await p.goto(base + '/settings', { waitUntil: 'domcontentloaded' });
        await p.locator('#profile > summary').click();
        assert(await p.locator('#profile').evaluate((n) => n.open));
        await p.locator('#profile .set-color').last().click();
        await p.locator('#profile form[action="me/profile"] button').click();
        await p.waitForLoadState('domcontentloaded');
        assert(await p.locator('#profile').evaluate((n) => n.open), 'native fragment reveals saved form without JS');
        await p.locator('#my-services > summary').click();
        await p.locator('#my-services input[name="service"]').first().check();
        await p.getByRole('button', { name: 'Save services', exact: true }).click();
        await p.waitForLoadState('domcontentloaded');
        assert(await p.locator('#my-services').evaluate((n) => n.open), 'services result stays visible without JS');
        for (const [code, section] of [
            ['profile-saved', 'profile'],
            ['show-followed', 'shows'],
            ['channel-on', 'channels'],
            ['request-sent', 'requests'],
        ]) {
            await p.goto(base + '/settings?done=' + code + '#' + section, { waitUntil: 'domcontentloaded' });
            assert(await p.locator('#' + section).evaluate((n) => n.open), code + ' opens its section');
            assert(await p.locator('#' + section + ' .set-message').isVisible());
        }
        await ctx.close();
        console.log(
            'PASS 320/390/430/744/1440 light/dark: nine collapsible panels, pointer/Enter/Space, jump/deep links, reorder, save/reload feedback, no-JS form and overflow.',
        );
    } finally {
        if (id) {
            try {
                await admin.goto(adminBase + '/admin/profiles/' + id);
                await admin.waitForLoadState('networkidle');
                await admin.getByRole('button', { name: 'Delete profile…' }).click();
                await admin.getByRole('button', { name: 'Delete profile', exact: true }).click();
                await admin.waitForURL('**/admin/profiles?deleted=*');
            } catch (e) {
                console.error(
                    `Could not delete the throwaway profile ${id}; remove it in Admin · Profiles. ${e.message}`,
                );
            }
        }
        await browser.close();
    }
})().catch((e) => {
    console.error(e.message || e);
    process.exit(1);
});
