// Release 4: folded Entertainment, Movies, TV & Streaming, Digital releases and Box office; profiles-on runs create and
// delete a temporary profile.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { audit } = require('./contrast-audit.cjs');

const base = process.env.BASE_URL || 'http://localhost:5295';
const shots = process.env.SCREENSHOT_DIR;
const password = process.env.ADMIN_PASSWORD;

const routes = [
    '/entertainment',
    '/movies',
    '/tv',
    '/movies/digital',
    '/box-office',
    '/box-office/weekend-preview',
    '/box-office/movie-forecasts',
];

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
    const shown = (p) => p.locator('.mx-phone details.mx-fold').filter({ visible: true });
    try {
        if (profiles) {
            admin = await (await browser.newContext({ viewport: { width: 1440, height: 1000 } })).newPage();
            await admin.goto(base + '/login?returnUrl=%2Fadmin%2Fprofiles');
            await admin.locator('#password').fill(password);
            await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
            await admin.waitForURL('**/admin/profiles');
            await admin.waitForLoadState('networkidle');
            const name = 'zz Screens ' + Date.now().toString(36);
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

        for (const scheme of ['kansas', 'monochrome', 'cardinals'])
            for (const theme of ['light', 'dark'])
                for (const width of [320, 390, 430]) {
                    const ctx = await contextFor(width, look(scheme, theme));
                    const p = await ctx.newPage();
                    p.on('pageerror', (e) => errors.push(e.message));
                    for (const path of routes) {
                        await visit(p, path, look(scheme, theme));
                        await p.waitForTimeout(250);
                        const ribbon = p.locator('.mx-ribbon').filter({ visible: true });
                        assert.equal(await ribbon.count(), 1, path + ' one ribbon');
                        assert.equal(await p.locator('.page-heading').filter({ visible: true }).count(), 0, path);
                        assert(
                            await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                            `${path} ${width} overflow`,
                        );
                        const ids = await p.locator('[id]').evaluateAll((ns) => ns.map((n) => n.id));
                        assert.equal(ids.length, new Set(ids).size, path + ' ids');
                        assert.deepEqual(await p.evaluate(audit), [], `${path} ${scheme} ${theme} ${width} contrast`);
                        // No ribbon title word is split across lines.
                        const split = await ribbon.locator('h1').evaluate((h) => {
                            const text = h.firstChild,
                                range = document.createRange();
                            return [...text.textContent.matchAll(/\S+/g)]
                                .filter((m) => {
                                    range.setStart(text, m.index);
                                    range.setEnd(text, m.index + m[0].length);
                                    return new Set([...range.getClientRects()].map((r) => Math.round(r.top))).size > 1;
                                })
                                .map((m) => m[0]);
                        });
                        assert.deepEqual(split, [], `${path} ${width} title word split`);
                        // Folds: the first starts open, one at a time, 64px summaries.
                        const folds = shown(p);
                        if (await folds.count()) {
                            assert.equal(await folds.locator(':scope[open]').count(), 1, path + ' one open fold');
                            assert(await folds.first().evaluate((e) => e.open), path + ' first fold open');
                            for (const s of await folds.locator('summary').all())
                                assert((await s.boundingBox()).height >= 64, path + ' 64px summary');
                            if ((await folds.count()) > 1) {
                                await folds.nth(1).locator('summary').click();
                                assert.equal(await folds.locator(':scope[open]').count(), 1, path + ' exclusive');
                                await folds.first().locator('summary').focus();
                                await p.keyboard.press('Enter');
                                assert(await folds.first().evaluate((e) => e.open), path + ' keyboard');
                            }
                        }
                        for (const target of await p
                            .locator('.mx-phone a, .mx-phone button, .mx-ribbon select')
                            .filter({ visible: true })
                            .all()) {
                            const box = await target.boundingBox();
                            assert(box.height >= 43.9, path + ' 44px target: ' + (await target.textContent()));
                        }
                        if (shots && scheme === 'kansas')
                            await p.screenshot({
                                path: `${shots}/screens-${path.slice(1).replaceAll('/', '-')}-${width}-${theme}.png`,
                                fullPage: true,
                            });
                    }
                    await ctx.close();
                }

        // Page specifics and the ribbon selects, at 390px.
        {
            const ctx = await contextFor(390);
            const p = await ctx.newPage();
            p.on('pageerror', (e) => errors.push(e.message));
            await p.goto(base + '/entertainment');
            const lead = p.locator('.mx-phone .mx-block').first();
            if (await lead.locator('[data-save]').count()) {
                await lead.locator('[data-save]').click();
                await p.waitForFunction(
                    () =>
                        document.querySelector('.mx-phone .mx-block [data-save]').getAttribute('aria-pressed') ===
                        'true',
                );
                await lead.locator('[data-share-open]').click();
                await p.getByRole('dialog').waitFor();
                await p.keyboard.press('Escape');
                await p.getByRole('dialog').waitFor({ state: 'detached' });
            }
            assert((await p.locator('.mx-phone .mx-top li').count()) <= 3, 'Entertainment top list has at most three');
            await p.locator('#entertainment-section-m').selectOption('/movies');
            await p.waitForURL((u) => u.pathname === '/movies');
            // Each Entertainment page's ribbon keeps the Section dropdown above its own select.
            for (const [path, current] of [['/tv', '/tv'], ['/movies/digital', '/movies/digital'], ['/box-office/weekend-preview', '/box-office'], ['/movies', '/movies']]) {
                await p.goto(base + path, { waitUntil: 'networkidle' });
                assert.equal(await p.locator('.mx-ribbon #entertainment-section-m').inputValue(), current, path + ' Section dropdown');
            }
            // Movies: seven days, today marked, each day's count in words; the week's releases are listed.
            const days = p.locator('.mx-days a');
            assert.equal(await days.count(), 7);
            assert.equal(await p.locator('.mx-days a[aria-current="date"]').count(), 1);
            for (const day of await days.all())
                assert.match(await day.locator('.sr-only').textContent(), /^(no releases|\d+ releases?)$/);
            const dotted = await p.locator('.mx-days a.mx-days__has').count();
            if (dotted) assert((await p.locator('#movies-week-m .mx-timeline li').count()) > 0);
            await p.locator('#movie-view-m').selectOption('theatrical');
            await p.waitForURL((u) => u.searchParams.get('type') === 'theatrical');
            assert.equal(await p.locator('#movie-view-m').inputValue(), 'theatrical');
            await p.locator('#movie-view-m').selectOption('digital');
            await p.waitForURL((u) => u.pathname === '/movies/digital');
            // A title search keeps the full layout on phones.
            await p.goto(base + '/movies?q=the');
            assert.equal(await p.locator('.mx-ribbon').filter({ visible: true }).count(), 0, 'search keeps layout');
            assert(await p.locator('.mv-week').isVisible());
            // Digital releases: Source select.
            await p.goto(base + '/movies/digital');
            const sources = await p.locator('#digital-source-m option').evaluateAll((ns) => ns.map((n) => n.value));
            if (sources.length > 1) {
                await p.locator('#digital-source-m').selectOption(sources[1]);
                await p.waitForURL((u) => u.searchParams.get('source') === sources[1]);
                assert.equal(await p.locator('#digital-source-m').inputValue(), sources[1]);
            }
            assert.equal(await p.locator('.mx-phone .mx-block').count(), 1, 'Digital has one dark block');
            // TV: Week select moves a week on; the attribution stays on phones.
            await p.goto(base + '/tv');
            assert(await p.locator('.mx-phone').getByRole('link', { name: 'TVmaze' }).isVisible());
            await p.locator('#tv-week-m').selectOption({ label: 'Next week' });
            await p.waitForURL((u) => u.searchParams.has('from'));
            assert.equal(
                await p.locator('#tv-week-m option:checked').textContent(),
                'Next week',
                'TV week kept after reload',
            );
            // Box office: No. 1, at most five chart rows with words for the change, View select.
            await p.goto(base + '/box-office');
            const rows = p.locator('.mx-chart li');
            if (await rows.count()) {
                assert((await rows.count()) <= 5);
                assert.equal((await p.locator('.mx-phone .mx-number').textContent()).trim(), '1');
                for (const change of await p.locator('.mx-chart__change').allTextContents())
                    assert.match(change, /from last weekend|Opening weekend|Change unavailable/);
            }
            await p.locator('#box-office-view-m').selectOption('/box-office/weekend-preview');
            await p.waitForURL((u) => u.pathname === '/box-office/weekend-preview');
            assert.equal(await p.locator('#box-office-view-m').inputValue(), '/box-office/weekend-preview');
            await ctx.close();
        }

        // Without JavaScript: folds still open and close, and each ribbon select has a Go button.
        for (const width of [320, 390, 430]) {
            const ctx = await contextFor(width, look('kansas', 'light'), false),
                p = await ctx.newPage();
            for (const path of routes) {
                await p.goto(base + path);
                assert(await p.locator('.mx-ribbon').isVisible(), path);
                assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth), path);
                const folds = shown(p);
                if ((await folds.count()) > 1) {
                    await folds.nth(1).locator('summary').click();
                    assert(await folds.nth(1).evaluate((e) => e.open), path + ' no-JS fold');
                }
            }
            // The Go button of the form holding that select (the ribbon also carries the Section dropdown).
            const go = (p, id) => p.locator(`.mx-ribbon form:has(#${id}) button[type=submit]`).click();
            await p.goto(base + '/entertainment');
            await p.locator('#entertainment-section-m').selectOption('/tv');
            await go(p, 'entertainment-section-m');
            assert.equal(new URL(p.url()).pathname, '/tv');
            await p.locator('#tv-week-m').selectOption({ label: 'In two weeks' });
            await go(p, 'tv-week-m');
            assert(new URL(p.url()).searchParams.has('from'));
            await p.goto(base + '/movies');
            await p.locator('#movie-view-m').selectOption('subscription');
            await go(p, 'movie-view-m');
            assert.equal(new URL(p.url()).searchParams.get('type'), 'subscription');
            await p.goto(base + '/box-office');
            await p.locator('#box-office-view-m').selectOption('/box-office/movie-forecasts');
            await go(p, 'box-office-view-m');
            assert.equal(new URL(p.url()).pathname, '/box-office/movie-forecasts');
            await ctx.close();
        }

        // Tablet and desktop keep their layouts.
        for (const width of [744, 1024, 1440])
            for (const theme of ['light', 'dark']) {
                const ctx = await contextFor(width, look('kansas', theme));
                const p = await ctx.newPage();
                for (const path of routes) {
                    await visit(p, path, look('kansas', theme));
                    assert.equal(await p.locator('.mx-ribbon').filter({ visible: true }).count(), 0, path);
                    assert.equal(await p.locator('.mx-phone').filter({ visible: true }).count(), 0, path);
                    assert(await p.locator('.page-heading').first().isVisible(), path + ' heading');
                    assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth), path);
                    assert.deepEqual(await p.evaluate(audit), [], `${path} ${theme} ${width} contrast`);
                    if (shots && width === 1440)
                        await p.screenshot({
                            path: `${shots}/screens-desktop-${path.slice(1).replaceAll('/', '-')}-${theme}.png`,
                        });
                }
                await ctx.close();
            }
        assert.deepEqual(errors, []);
        console.log(
            'Release 4: Entertainment, Movies, TV & Streaming, Digital releases and Box office (three views) at 320/390/430, three schemes, light/dark; contrast, overflow, unique IDs, unsplit titles, 44px targets, 64px summaries, first fold open, exclusive and keyboard folds, lead save/share, week strip, ribbon selects with and without JavaScript, search keeps layout, retained 744/1024/1440 layouts passed.',
        );
    } finally {
        if (id) {
            try {
                await admin.goto(base + '/admin/profiles/' + id);
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
