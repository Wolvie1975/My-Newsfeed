// College daily feature: site-local dates, empty-day state, all sports and retained weekly schedule. Temporary profiles are removed.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');

const base = process.env.BASE_URL || 'http://localhost:5295';
const adminBase = process.env.ADMIN_BASE_URL || base;
const shots = process.env.SCREENSHOT_DIR;
const password = process.env.ADMIN_PASSWORD;

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
            const name = 'zz CollegeToday ' + Date.now().toString(36);
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
            console.log('Temporary college profile', id);
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
        const zone = process.env.DISPLAY_TIMEZONE || 'America/Chicago';
        const today = new Intl.DateTimeFormat('en-CA', { timeZone: zone }).format(new Date());
        const longToday = new Intl.DateTimeFormat('en-US', {
            timeZone: zone,
            weekday: 'long',
            month: 'long',
            day: 'numeric',
            year: 'numeric',
        }).format(new Date());
        const sports = ['volleyball', 'football', 'mens-basketball', 'soccer'];
        for (const theme of ['light', 'dark']) {
            const setup = await contextFor(390, { theme }),
                settings = await setup.newPage();
            await visit(settings, '/college?sport=volleyball', 'ribbon', theme);
            await setup.close();
            for (const js of [true, false]) {
                const context = await contextFor(390, { theme, js }),
                    p = await context.newPage();
                for (const width of [320, 390, 430, 820, 1024, 1440]) {
                    await p.setViewportSize({ width, height: 1100 });
                    for (const sport of width === 390 ? sports : ['volleyball']) {
                        await p.goto(base + '/college?sport=' + sport, { waitUntil: 'networkidle' });
                        const hasToday = (await p.locator('.cl-main .group__bar').allTextContents()).some(
                            (text) => text.trim() === longToday,
                        );
                        const hero = p.locator('[data-college-featured-day]');
                        assert.equal(
                            await hero.count(),
                            hasToday ? 1 : 0,
                            'highlight exists exactly when today has games',
                        );
                        if (hasToday) {
                            assert.equal(
                                await hero.getAttribute('data-college-featured-day'),
                                today,
                                'featured game is today in site zone',
                            );
                            assert((await hero.textContent()).includes('Today’s featured game'));
                        } else {
                            assert(
                                (await p.locator('[data-college-no-games-today]').textContent()).includes(
                                    'No games today',
                                ),
                            );
                        }
                        assert.equal(await p.locator('[aria-label="Game of the week"]').count(), 0);
                        assert(
                            await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                            'no overflow',
                        );
                        assert(await p.locator('.cl-main').count(), 'weekly desktop schedule retained');
                        const select = p.locator('[data-phone-sports="college"] select').last();
                        if (width < 1024) {
                            assert(await select.isVisible(), 'sport filter visible on phones/tablets');
                            const box = await select.boundingBox();
                            assert(box.height >= 44, '44px sport selector');
                        }
                        if (shots)
                            await p.screenshot({
                                path: `${shots}/${theme}-${width}-${sport}-${js ? 'js' : 'nojs'}.png`,
                            });
                        console.log(
                            'PASS college today',
                            width,
                            theme,
                            sport,
                            js ? 'JS' : 'no JS',
                            hasToday ? 'today highlight' : 'no games today',
                        );
                    }
                }
                await context.close();
            }
        }
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
