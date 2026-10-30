// Header scheme picker: responsive native dialog, shared Settings, persistence and retry. Temporary profiles are deleted in finally.
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
    const errors = [];
    let admin = null,
        id = null,
        storageState;
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
        await response.finished();
        await page.reload({ waitUntil: 'networkidle' });
    };
    try {
        if (profiles) {
            admin = await (await browser.newContext({ viewport: { width: 1440, height: 1000 } })).newPage();
            admin.setDefaultNavigationTimeout(90000);
            await admin.goto(adminBase + '/login?returnUrl=%2Fadmin%2Fprofiles');
            await admin.locator('#password').fill(password);
            await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
            await admin.waitForURL('**/admin/profiles');
            await admin.waitForLoadState('networkidle');
            const name = 'zz Schemes ' + Date.now().toString(36);
            // Admin can finish its interactive circuit after the server-rendered button appears.
            for (let attempt = 0; attempt < 12 && !(await admin.locator('#add-name').isVisible()); attempt++) {
                await admin.getByRole('button', { name: 'Add profile', exact: true }).first().click();
                try {
                    await admin.locator('#add-name').waitFor({ state: 'visible', timeout: 1500 });
                } catch {
                    if (attempt === 11) throw new Error('Admin Add profile did not become interactive');
                }
            }
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
            console.log('Temporary scheme profile', id);
            const context = await browser.newContext({ viewport: { width: 390, height: 900 } });
            const p = await context.newPage();
            await p.goto(base + '/news/all');
            await p.locator(`form[action="profiles/${id}/open"]`).getByRole('button').click();
            await p.keyboard.type('3691');
            await p.waitForURL('**/news/all');
            storageState = await context.storageState();
            await context.close();
        }
        if (shots) require('node:fs').mkdirSync(shots, { recursive: true });
        const setScheme = async (page, value) => {
            if ((await page.locator('html').getAttribute('data-color-scheme')) === value) return;
            const pending = profiles
                ? page.waitForResponse(
                      (r) => new URL(r.url()).pathname === '/api/me/settings' && r.request().method() === 'PUT',
                  )
                : null;
            await page.locator('[data-scheme-dialog] input[value="' + value + '"]').check();
            await page.waitForFunction((v) => document.documentElement.dataset.colorScheme === v, value);
            if (pending) {
                const response = await pending;
                assert.equal(response.status(), 200);
                await response.finished();
            }
            await page.waitForFunction(
                () =>
                    document.querySelector('[data-scheme-dialog] [data-scheme-status]').textContent ===
                    'Color scheme saved.',
            );
        };
        for (const theme of (process.env.SCHEME_PICKER_THEMES || 'light,dark').split(',')) {
            const context = await contextFor(1440, { theme });
            const page = await context.newPage();
            page.on('pageerror', (e) => errors.push(e.message));
            await visit(page, '/saved', 'kansas', theme);
            const trigger = page.locator('[data-scheme-open]'),
                dialog = page.locator('[data-scheme-dialog]');
            for (const width of [320, 390, 430, 820, 1024, 1280, 1440]) {
                await page.setViewportSize({ width, height: 900 });
                assert(await trigger.isVisible());
                const button = await trigger.boundingBox();
                assert(button.width >= 44 && button.height >= 44);
                assert(
                    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                    'Header overflow ' + width,
                );
                const url = page.url();
                await trigger.click();
                assert.equal(page.url(), url, 'Opens without navigation');
                assert(await dialog.isVisible());
                assert.equal(await dialog.locator('input[data-color-scheme]').count(), 16);
                const box = await dialog.boundingBox();
                assert(
                    box.x >= 0 && box.y >= 0 && box.x + box.width <= width + 1 && box.y + box.height <= 901,
                    'Dialog fits viewport',
                );
                assert(
                    await dialog.evaluate((n) => n.scrollWidth <= n.clientWidth + 1),
                    'No horizontal dialog scrolling',
                );
                const value =
                    (await page.locator('html').getAttribute('data-color-scheme')) === 'kansas'
                        ? 'cardinals'
                        : 'kansas';
                await setScheme(page, value);
                assert.equal(await page.locator('html').getAttribute('data-theme'), theme, 'Light/dark preserved');
                for (let i = 0; i < 6; i++) {
                    await page.keyboard.press('Tab');
                    assert(
                        await page.evaluate(() => !!document.activeElement.closest('[data-scheme-dialog]')),
                        'Focus stays in modal',
                    );
                }
                for (let i = 0; i < 3; i++) {
                    await page.keyboard.press('Shift+Tab');
                    assert(
                        await page.evaluate(() => !!document.activeElement.closest('[data-scheme-dialog]')),
                        'Reverse focus stays in modal',
                    );
                }
                assert.equal(
                    await page.evaluate(() => getComputedStyle(document.body).overflow),
                    'hidden',
                    'Background scrolling locked',
                );
                await dialog.evaluate((n) => (n.scrollTop = 0));
                if (shots) {
                    await dialog.screenshot({ path: `${shots}/picker-${width}-${theme}.png` });
                    await page.locator('.site-header').screenshot({ path: `${shots}/header-${width}-${theme}.png` });
                }
                await page.keyboard.press('Escape');
                assert(!(await dialog.isVisible()));
                assert(await trigger.evaluate((n) => n === document.activeElement), 'Focus restored');
                await trigger.click();
                assert(await dialog.locator(`input[value="${value}"]`).isChecked());
                await dialog.getByRole('button', { name: 'Done', exact: true }).click();
                assert(!(await dialog.isVisible()));
                await trigger.click();
                await page.mouse.click(2, 2);
                assert(!(await dialog.isVisible()), 'Backdrop dismisses');
                console.log('PASS picker', width, theme);
            }
            await page.setViewportSize({ width: 1440, height: 900 });
            await trigger.click();
            const values = await dialog.locator('input[data-color-scheme]').evaluateAll((ns) => ns.map((n) => n.value));
            for (const value of values) {
                await setScheme(page, value);
                assert.equal(await page.locator('html').getAttribute('data-theme'), theme);
            }
            await page.keyboard.press('Escape');
            await page.reload({ waitUntil: 'networkidle' });
            assert.equal(
                await page.locator('html').getAttribute('data-color-scheme'),
                values.at(-1),
                'Scheme persists refresh',
            );
            await page.goto(base + '/settings', { waitUntil: 'networkidle' });
            const colors = page.locator('[data-settings-section="colors"]');
            if (!(await colors.evaluate((n) => n.open))) await colors.locator('summary').click();
            await trigger.click();
            await setScheme(page, 'coastal');
            await page.keyboard.press('Escape');
            assert(
                await page.locator('input[name="color-scheme"][value="coastal"]').isChecked(),
                'Settings stays in sync',
            );
            const pending = profiles
                ? page.waitForResponse(
                      (r) => new URL(r.url()).pathname === '/api/me/settings' && r.request().method() === 'PUT',
                  )
                : null;
            await page.locator('input[name="color-scheme"][value="olive"]').check();
            if (pending) {
                const response = await pending;
                assert.equal(response.status(), 200);
                await response.finished();
            }
            await trigger.click();
            assert(await dialog.locator('input[value="olive"]').isChecked(), 'Picker follows Settings');
            // Failed persistence must leave the preview usable and offer an actual retry.
            if (profiles) {
                await page.route('**/api/me/settings', (route) =>
                    route.fulfill({ status: 503, contentType: 'application/json', body: '{}' }),
                );
                await dialog.locator('input[value="city"]').check();
                await page.waitForFunction(() => !document.querySelector('[data-scheme-retry]').hidden);
                assert((await dialog.locator('[data-scheme-status]').innerText()).includes('could not be saved'));
                await page.unroute('**/api/me/settings');
                const saved = page.waitForResponse(
                    (r) => new URL(r.url()).pathname === '/api/me/settings' && r.request().method() === 'PUT',
                );
                await dialog.locator('[data-scheme-retry]').click();
                const response = await saved;
                assert.equal(response.status(), 200);
                await response.finished();
            } else {
                await page.evaluate(() => {
                    window.schemePickerOriginalSet = window.myNewspaperPrefs.setItem;
                    window.myNewspaperPrefs.setItem = () => {
                        throw Error('fixture storage failure');
                    };
                });
                await dialog.locator('input[value="city"]').check();
                assert(await dialog.locator('[data-scheme-retry]').isVisible());
                await page.evaluate(() => (window.myNewspaperPrefs.setItem = window.schemePickerOriginalSet));
                await dialog.locator('[data-scheme-retry]').click();
            }
            await page.waitForFunction(
                () =>
                    document.querySelector('[data-scheme-dialog] [data-scheme-status]').textContent ===
                    'Color scheme saved.',
            );
            await dialog.getByRole('button', { name: 'Done', exact: true }).click();
            await page.locator('.saved-link').click();
            await page.waitForURL('**/saved');
            await page.waitForLoadState('networkidle');
            await page.locator('.saved-link[aria-current="page"]').waitFor();
            assert.equal(
                await page.locator('html').getAttribute('data-color-scheme'),
                'city',
                'Scheme survives enhanced navigation',
            );
            await trigger.click();
            assert(await dialog.locator('input[value="city"]').isChecked());
            await page.keyboard.press('Escape');
            // Five actions including an admin notification still fit on narrow screens.
            await page
                .locator('.saved-link')
                .evaluate((n) =>
                    n.insertAdjacentHTML(
                        'afterend',
                        '<a class="requests-link" href="requests"><span class="requests-link__count">12</span><span>▣</span></a>',
                    ),
                );
            for (const width of [320, 390, 430]) {
                await page.setViewportSize({ width, height: 900 });
                const rects = await page
                    .locator(
                        '.brand,.header-clock,.header-actions>.saved-link,.header-actions>.theme-toggle,.header-actions>.scheme-toggle,.header-actions>.requests-link,.mobile-nav>summary',
                    )
                    .evaluateAll((ns) => ns.map((n) => n.getBoundingClientRect().toJSON()));
                for (let i = 0; i < rects.length; i++)
                    for (const r of rects.slice(i + 1)) {
                        const a = rects[i];
                        assert(
                            a.right <= r.left + 1 ||
                                r.right <= a.left + 1 ||
                                a.bottom <= r.top + 1 ||
                                r.bottom <= a.top + 1,
                            'Admin header overlap',
                        );
                    }
                await page.locator('.mobile-nav>summary').click();
                // Wait for the disclosure and responsive header layout to settle before measuring.
                try {
                    await page.waitForFunction(
                        () => {
                            const rect = document.querySelector('.mobile-nav__links').getBoundingClientRect();
                            return Math.abs(rect.bottom - innerHeight) < 3;
                        },
                        null,
                        { timeout: 5000 },
                    );
                } catch (error) {
                    console.error(
                        'Menu state',
                        width,
                        theme,
                        await page.evaluate(() => ({
                            open: document.querySelector('.mobile-nav').open,
                            header: document.querySelector('.site-header').getBoundingClientRect().toJSON(),
                            menu: document.querySelector('.mobile-nav__links').getBoundingClientRect().toJSON(),
                            saved: document.querySelector('.saved-link').getAttribute('aria-current'),
                            requests: document.querySelectorAll('.requests-link').length,
                        })),
                    );
                    if (shots) await page.screenshot({ path: `${shots}/menu-failure-${width}-${theme}.png` });
                    throw error;
                }
                const menu = await page.locator('.mobile-nav__links').boundingBox();
                assert(
                    Math.abs(menu.y + menu.height - 900) < 3,
                    `Menu geometry ${width} ${theme}: ${JSON.stringify(menu)}; header ${JSON.stringify(await page.locator('.site-header').boundingBox())}`,
                );
                await page.keyboard.press('Escape');
                if (shots)
                    await page.locator('.site-header').screenshot({ path: `${shots}/admin-${width}-${theme}.png` });
            }
            await context.close();
            console.log('PASS all schemes, persistence, Settings sync, save retry and admin layout', theme);
        }
        const plain = await contextFor(390, { js: false }),
            page = await plain.newPage();
        await page.goto(base + '/saved', { waitUntil: 'networkidle' });
        await page.locator('[data-scheme-open]').click();
        await page.waitForURL((u) => u.pathname === '/settings' && u.hash === '#color-scheme-heading');
        assert(!(await page.locator('[data-scheme-dialog]').isVisible()));
        await plain.close();
        assert.deepEqual(errors, []);
        console.log(
            'Color scheme picker responsive, keyboard, persistence, retry, Settings and no-JS fallback passed.',
        );
    } finally {
        if (id) {
            try {
                await admin.goto(adminBase + '/admin/profiles/' + id);
                await admin.waitForLoadState('networkidle');
                const confirm = admin.getByRole('button', { name: 'Delete profile', exact: true });
                for (let attempt = 0; attempt < 12 && !(await confirm.isVisible()); attempt++) {
                    await admin.getByRole('button', { name: 'Delete profile…' }).click();
                    try {
                        await confirm.waitFor({ state: 'visible', timeout: 1500 });
                    } catch {
                        // Retry while the admin circuit becomes interactive.
                    }
                }
                await confirm.click();
                await admin.waitForURL('**/admin/profiles?deleted=*');
            } catch (e) {
                process.exitCode = 1;
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
