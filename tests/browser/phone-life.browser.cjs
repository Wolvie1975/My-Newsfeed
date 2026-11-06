// Release 3: folded Standings, College Sports and F1; profiles-on runs create and delete a temporary profile.
const { chromium } = require('playwright');
const lib = require('./lib.cjs');
const assert = require('node:assert/strict');
const { audit } = require('./contrast-audit.cjs');

const base = lib.base;
const shots = lib.shotDir;
const password = lib.adminPassword;
const PERSONAL = 'mynewsfeed-personal-v1';

(async () => {
    const profiles = (await fetch(base + '/editions', { headers: { Accept: 'text/html' } })).status !== 404;
    if (profiles && !password) {
        console.log('SKIP: profiles are on here; set ADMIN_PASSWORD so the check can add a throwaway profile.');
        return;
    }
    const browser = await chromium.launch();
    const errors = [];
    let admin, storageState;
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
        return context;
    };
    // With profiles on, settings belong to the open profile, so set them there and reload.
    const visit = async (page, path, settings = {}) => {
        await page.goto(base + path, { waitUntil: 'networkidle' });
        if (!profiles || !Object.keys(settings).length) return;
        const saved = page.waitForResponse(
            (response) =>
                new URL(response.url()).pathname === '/api/me/settings' && response.request().method() === 'PUT',
        );
        await page.evaluate((s) => {
            for (const [k, v] of Object.entries(s)) window.myNewspaperPrefs.setItem(k, v);
        }, settings);
        const response = await saved;
        assert.equal(response.status(), 200, 'Profile settings save completes before reload');
        await page.reload({ waitUntil: 'networkidle' });
    };
    const look = (scheme, theme) => ({
        'mynewsfeed-color-scheme': scheme,
        theme,
        [PERSONAL]: JSON.stringify({ order: [], hidden: [] }),
        'mynewsfeed-sports-teams-v1': JSON.stringify(['kansas', 'byu']),
    });
    try {
        if (profiles) {
            admin = await lib.signInAdmin(browser);
            const profile = await lib.addProfile(admin, 'Life', { pin: '3691' });
            storageState = await lib.openProfile(browser, profile, { path: '/sports', waitUntil: 'domcontentloaded' });
        }

        const forecast = {
            location: 'Kansas City',
            updated: new Date().toISOString(),
            stale: false,
            periods: Array.from({ length: 7 }, (_, i) => ({
                name: ['Today', 'Tonight', 'Thursday', 'Thursday night', 'Friday', 'Friday night', 'Saturday'][i],
                temperature: 80 - i * 4,
                unit: 'F',
                summary: i % 2 ? 'Mostly clear' : 'Sunny',
                wind: '10 mph S',
                isDaytime: i % 2 === 0,
            })),
        };
        const prepare = async (context) => {
            await context.route('**/local/weather*', (route) => route.fulfill({ json: forecast }));
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
        };
        for (const scheme of ['kansas', 'monochrome', 'cardinals'])
            for (const theme of lib.THEMES)
                for (const width of lib.PHONE_WIDTHS) {
                    const ctx = await contextFor(width, look(scheme, theme));
                    await prepare(ctx);
                    const p = await ctx.newPage();
                    p.on('pageerror', (e) => errors.push(e.message));
                    for (const path of ['/watch?services=8,9,337', '/news/kansas-city', '/videos']) {
                        await visit(p, path, look(scheme, theme));
                        await p.waitForTimeout(250);
                        assert(await p.locator('.mx-ribbon').filter({ visible: true }).isVisible(), path + ' ribbon');
                        assert(
                            await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                            `${path} ${width} overflow`,
                        );
                        const ids = await p.locator('[id]').evaluateAll((ns) => ns.map((n) => n.id));
                        assert.equal(ids.length, new Set(ids).size, path + ' ids');
                        assert.deepEqual(await p.evaluate(audit), [], `${path} ${scheme} ${theme} ${width} contrast`);
                        const folds = p.locator('[data-phone-life] details.mx-fold');
                        assert.equal(await folds.locator(':scope[open]').count(), 1);
                        for (const e of await folds.locator('summary').all())
                            assert((await e.boundingBox()).height >= 64, '64px summary');
                        if ((await folds.count()) > 1) {
                            await folds.nth(1).locator('summary').click();
                            assert.equal(await folds.locator(':scope[open]').count(), 1);
                            await folds.first().locator('summary').focus();
                            await p.keyboard.press('Enter');
                            assert(await folds.first().evaluate((e) => e.open));
                        }
                        for (const target of await p
                            .locator(
                                '[data-phone-life] a, [data-phone-life] button, .mx-ribbon input, .mx-ribbon select',
                            )
                            .filter({ visible: true })
                            .all()) {
                            const box = await target.boundingBox();
                            assert(box.height >= 43.9, path + ' 44px target: ' + (await target.textContent()));
                        }
                        if (shots && scheme === 'kansas') {
                            await p.evaluate(() => scrollTo(0, 0));
                            await p.screenshot({
                                path: `${shots}/life-${path.split('?')[0].slice(1)}-${width}-${theme}.png`,
                                fullPage: true,
                            });
                        }
                        if (path.startsWith('/watch')) {
                            const rail = p.locator('.mx-watch-rail');
                            assert((await rail.locator('li').count()) > 0);
                            await rail.evaluate((e) => (e.scrollLeft = 100));
                            assert((await rail.evaluate((e) => e.scrollLeft)) > 0);
                            await rail.locator('[data-watch-title]').first().click();
                            const d = p.getByRole('dialog');
                            await d.waitFor();
                            await d.locator('[data-watch-content]').waitFor();
                            await p.keyboard.press('Escape');
                            await d.waitFor({ state: 'detached' });
                        } else if (path === '/news/kansas-city') {
                            assert.equal(
                                (await p.locator('[data-weather-feature]').innerText()).replace(/\s+/g, ' '),
                                'TODAY 80°F Sunny Wind 10 mph S',
                            );
                            assert.equal(await p.locator('[data-weather-tiles] article').count(), 3);
                            assert.equal(await p.locator('[data-phone-forecast] article').count(), 7);
                            assert.equal(await p.locator('[data-weather-locate]').count(), 1);
                            assert.equal(await p.locator('#local-news-m .mx-top li').count(), 3);
                            assert.equal(
                                await p.locator('#local-news-m').getByText('error', { exact: true }).count(),
                                0,
                            );
                        } else {
                            const save = p.locator('.mx-video-feature [data-save]');
                            await save.click();
                            await p.waitForFunction(
                                () =>
                                    document
                                        .querySelector('.mx-video-feature [data-save]')
                                        .getAttribute('aria-pressed') === 'true',
                            );
                            await save.click();
                            const share = p.locator('.mx-video-feature [data-share-open]');
                            await share.click();
                            await p.getByRole('dialog').waitFor();
                            await p.keyboard.press('Escape');
                            await p.getByRole('dialog').waitFor({ state: 'detached' });
                            assert(await share.evaluate((e) => e === document.activeElement));
                            assert((await p.locator('#video-channel-m').inputValue()).startsWith('/videos'));
                        }
                    }
                    await ctx.close();
                }
        // Native fallback, keyboard folds and all three selectors without JavaScript.
        for (const width of lib.PHONE_WIDTHS)
            for (const theme of ['light']) {
                const ctx = await contextFor(width, look('kansas', theme), false),
                    p = await ctx.newPage();
                for (const path of ['/watch?services=8', '/news/kansas-city', '/videos']) {
                    await p.goto(base + path);
                    const folds = p.locator('[data-phone-life] details.mx-fold');
                    if ((await folds.count()) > 1) {
                        await folds.nth(1).locator('summary').click();
                        assert.equal(await folds.locator(':scope[open]').count(), 1);
                    }
                    assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
                }
                await p.locator('#video-channel-m').selectOption({ index: 1 });
                await p.locator('.mx-ribbon button[type=submit]').click();
                assert(new URL(p.url()).searchParams.has('category'));
                // The events view (the ribbon's Events choice before Kansas City moved under News) still opens its fold.
                await p.goto(base + '/news/kansas-city?section=events#local-more-m');
                assert(
                    await p
                        .getByText('Things to do', { exact: true })
                        .locator('xpath=ancestor::details')
                        .evaluate((e) => e.open),
                );
                await p.goto(base + '/watch?services=8');
                await p.locator('#watch-browse-m').selectOption('movie');
                await p.locator('.mx-ribbon__field button').click();
                assert.equal(new URL(p.url()).searchParams.get('services'), '8');
                assert.equal(
                    await p.locator('.mx-ribbon #entertainment-section-m').inputValue(),
                    '/watch',
                    'Watch Section dropdown',
                );
                assert(await p.locator('.watch-grid').isVisible());
                await ctx.close();
            }
        // Real filtering, complete lists and reload persistence.
        {
            const ctx = await contextFor(390);
            await prepare(ctx);
            const p = await ctx.newPage();
            await p.goto(base + '/videos?q=MacBook');
            const option = p.locator('#video-channel-m option').filter({ hasText: 'Category: Tech' });
            await p.locator('#video-channel-m').selectOption(await option.getAttribute('value'));
            await p.waitForURL('**/videos?**category=tech**');
            assert.equal(new URL(p.url()).searchParams.get('q'), 'MacBook');
            await p.reload();
            assert((await p.locator('#video-channel-m').inputValue()).includes('category=tech'));
            await p.goto(base + '/videos?category=tech&view=all');
            assert(await p.getByRole('heading', { name: 'All matching videos' }).isVisible());
            assert((await p.locator('[data-phone-life] .mx-video-row').count()) > 3);
            await p.goto(base + '/news/kansas-city?view=news#local-news-m');
            assert((await p.locator('#local-news-m .mx-top li').count()) > 3);
            await p.goto(base + '/videos?q=zzNoMatchingVideoForPhoneTest');
            assert(await p.getByText('No videos match this view.').isVisible());
            await p.goto(base + '/watch?services=8,9');
            await p.locator('#watch-query-m').fill('Arrival');
            await p.locator('.mx-ribbon__search button').click();
            await p.waitForURL('**/watch?**q=Arrival**');
            assert.equal(new URL(p.url()).searchParams.get('services'), '8,9');
            assert(await p.locator('.watch-results').isVisible());
            await p.locator('#watch-sort').selectOption('title');
            await p.waitForURL('**sort=title**');
            await p.reload();
            assert.equal(await p.locator('#watch-sort').inputValue(), 'title');
            await ctx.close();
        }
        for (const width of [1024, 1280, 1440])
            for (const theme of lib.THEMES) {
                const ctx = await contextFor(width, look('kansas', theme));
                await prepare(ctx);
                const p = await ctx.newPage();
                for (const path of ['/watch?services=8', '/news/kansas-city', '/videos']) {
                    await visit(p, path, look('kansas', theme));
                    assert(!(await p.locator('[data-phone-life]').isVisible()));
                    assert.equal(await p.locator('.mx-ribbon').filter({ visible: true }).count(), 0);
                    assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
                    assert.deepEqual(await p.evaluate(audit), []);
                    if (path.startsWith('/watch')) {
                        const grid = await p.locator('.watch-page').boundingBox();
                        const rows = await p.locator('.mx-desktop .watch-rows').boundingBox();
                        assert(Math.abs(rows.width - grid.width) < 2, 'desktop Watch rows span the full grid');
                    }
                    if (path === '/news/kansas-city') {
                        assert(!(await p.locator('[data-weather-tiles]').isVisible()));
                        assert(await p.locator('.weather-grid').isVisible());
                    }
                    if (shots && width === 1440)
                        await p.screenshot({
                            path: `${shots}/life-desktop-${path.split('?')[0].slice(1)}-${theme}.png`,
                        });
                }
                await ctx.close();
            }
        assert.deepEqual(errors, []);
        console.log(
            'Release 5: phone widths 320/390/430, three schemes, light/dark; contrast, overflow, unique IDs, native/keyboard/no-JS folds and selectors, weather focal/tiles/full forecast, save/share, watch modal/search/sort, category/query/reload, expanded lists, empty videos, retained 1024/1280/1440 layouts passed.',
        );
    } finally {
        await lib.deleteProfiles();
        await browser.close();
    }
})().catch((e) => {
    console.error(e.message || e);
    process.exit(1);
});
