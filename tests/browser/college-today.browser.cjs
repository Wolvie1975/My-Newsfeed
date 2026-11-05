// College daily feature: site-local dates, empty-day state, all sports and retained weekly schedule. Temporary profiles are removed.
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
        await response.finished();
        await page.reload({ waitUntil: 'networkidle' });
    };
    try {
        if (profiles) {
            admin = await lib.signInAdmin(browser, { navigationTimeout: 90000 });
            const profile = await lib.addProfile(admin, 'CollegeToday', { pin: '3691' });
            id = profile.id;
            console.log('Temporary college profile', id);
            storageState = await lib.openProfile(browser, profile, { path: '/news/all' });
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
        for (const theme of lib.THEMES) {
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
        await lib.deleteProfiles();
        await browser.close();
    }
})().catch((e) => {
    console.error(e.message || e);
    process.exit(1);
});
