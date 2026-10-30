// Release 6: phone ribbons on both All headlines pages, Saved and Settings; profiles-on runs create and delete a
// temporary profile.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { audit } = require('./contrast-audit.cjs');

const base = process.env.BASE_URL || 'http://localhost:5295';
const adminBase = process.env.ADMIN_BASE_URL || base;
const shots = process.env.SCREENSHOT_DIR;
const password = process.env.ADMIN_PASSWORD;

const savedItems = [
    { url: 'https://example.com/a1', title: 'First saved article', source: 'Example News', kind: 'article' },
    { url: 'https://example.com/a2', title: 'Second saved article', source: 'Example Daily', kind: 'article' },
    { url: 'https://www.youtube.com/watch?v=test0001', title: 'A saved video', source: 'Example TV', kind: 'video' },
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
    // A private Saved list per context, so the check never touches the real one.
    const mockSaved = async (context, initial = savedItems) => {
        let saved = initial.map((a) => ({ ...a }));
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
    const ribbonTargets = async (p, path) => {
        for (const target of await p
            .locator('.mx-ribbon a, .mx-ribbon button, .mx-ribbon select, .mx-list-feature a, .mx-list-feature button')
            .filter({ visible: true })
            .all())
            assert((await target.boundingBox()).height >= 43.9, path + ' 44px target: ' + (await target.textContent()));
    };
    try {
        if (profiles) {
            admin = await (await browser.newContext({ viewport: { width: 1440, height: 1000 } })).newPage();
            await admin.goto(adminBase + '/login?returnUrl=%2Fadmin%2Fprofiles');
            await admin.locator('#password').fill(password);
            await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
            await admin.waitForURL('**/admin/profiles');
            await admin.waitForLoadState('networkidle');
            const name = 'zz Tools ' + Date.now().toString(36);
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

        const routes = ['/sports/news', '/entertainment/news', '/saved', '/settings'];
        for (const scheme of ['kansas', 'monochrome', 'cardinals'])
            for (const theme of ['light', 'dark'])
                for (const width of [320, 390, 430]) {
                    const ctx = await contextFor(width, look(scheme, theme));
                    await mockSaved(ctx);
                    const p = await ctx.newPage();
                    p.on('pageerror', (e) => errors.push(e.message));
                    for (const path of routes) {
                        await visit(p, path, look(scheme, theme));
                        if (path === '/saved') await p.locator('[data-saved-list] .article').first().waitFor();
                        await p.waitForTimeout(250);
                        assert(await p.locator('.mx-ribbon').filter({ visible: true }).isVisible(), path + ' ribbon');
                        assert.equal(await p.locator('.page-heading').filter({ visible: true }).count(), 0, path);
                        assert(
                            await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                            `${path} ${width} overflow`,
                        );
                        const ids = await p.locator('[id]').evaluateAll((ns) => ns.map((n) => n.id));
                        assert.equal(ids.length, new Set(ids).size, path + ' ids');
                        assert.deepEqual(await p.evaluate(audit), [], `${path} ${scheme} ${theme} ${width} contrast`);
                        await ribbonTargets(p, path);
                        // No title word is split across lines ("Entertainme / nt") and the title shows no focus ring.
                        const split = await p
                            .locator('.mx-ribbon h1')
                            .filter({ visible: true })
                            .evaluate((h) => {
                                const text = h.firstChild,
                                    range = document.createRange();
                                const words = [...text.textContent.matchAll(/\S+/g)];
                                return words
                                    .filter((m) => {
                                        range.setStart(text, m.index);
                                        range.setEnd(text, m.index + m[0].length);
                                        return (
                                            new Set([...range.getClientRects()].map((r) => Math.round(r.top))).size > 1
                                        );
                                    })
                                    .map((m) => m[0]);
                            });
                        assert.deepEqual(split, [], `${path} ${width} title word split`);
                        assert.equal(
                            await p
                                .locator('.mx-ribbon h1')
                                .filter({ visible: true })
                                .evaluate((h) => getComputedStyle(h).outlineStyle),
                            'none',
                            path + ' title focus ring',
                        );
                        if (path.endsWith('/news')) {
                            // One dark feature: the first article on the page, nothing else.
                            assert.equal(await p.locator('.mx-list-feature').count(), 1, path + ' one feature');
                            assert(
                                await p
                                    .locator('.paper-feed .article')
                                    .first()
                                    .evaluate((n) => n.classList.contains('mx-list-feature')),
                                path + ' feature is first',
                            );
                            assert.equal(await p.locator('select[id$="-section-m"]').inputValue(), path);
                        }
                        if (path === '/saved') {
                            assert.deepEqual(await p.locator('#saved-show-m option').allTextContents(), [
                                'All · 3',
                                'Articles · 2',
                                'Videos · 1',
                            ]);
                            assert.equal(await p.locator('.mx-list-feature').count(), 1);
                            assert.equal(await p.locator('.mx-list-feature .title').innerText(), 'First saved article');
                            assert(!(await p.locator('.sv-tabs, [data-saved-tab]').first().isVisible()));
                        }
                        if (path === '/settings') {
                            assert(!(await p.locator('.set-index').isVisible()), 'Settings index hidden on phones');
                            const options = await p
                                .locator('#settings-jump-m option:not([disabled])')
                                .evaluateAll((ns) => ns.map((n) => n.value));
                            assert.equal(options.includes('/settings#profile'), profiles, 'profile anchor');
                            // Every anchor the select offers exists on the page.
                            for (const value of options)
                                assert.equal(await p.locator(value.slice(value.indexOf('#'))).count(), 1, value);
                        }
                        if (shots && scheme === 'kansas') {
                            await p.evaluate(() => scrollTo(0, 0));
                            await p.screenshot({
                                path: `${shots}/tools-${path.slice(1).replace('/', '-')}-${width}-${theme}.png`,
                            });
                        }
                    }
                    await ctx.close();
                }

        // Interactions at 390px.
        {
            const ctx = await contextFor(390);
            await mockSaved(ctx);
            const p = await ctx.newPage();
            p.on('pageerror', (e) => errors.push(e.message));
            // Headlines: the Section select navigates; Load more adds no second feature.
            await p.goto(base + '/sports/news');
            await p.locator('#hub-section-m').selectOption('/sports');
            await p.waitForURL((u) => u.pathname === '/sports');
            await p.goto(base + '/entertainment/news');
            const more = p.locator('[data-load-more]');
            if (await more.count()) {
                const before = await p.locator('.paper-feed .article').count();
                await more.click();
                await p.waitForFunction((n) => document.querySelectorAll('.paper-feed .article').length > n, before);
                assert.equal(await p.locator('.mx-list-feature').count(), 1, 'Load more adds no feature');
            }
            const feature = p.locator('.mx-list-feature');
            await feature.locator('[data-share-open]').click();
            await p.getByRole('dialog').waitFor();
            await p.keyboard.press('Escape');
            await p.getByRole('dialog').waitFor({ state: 'detached' });
            // Saved: removing the featured item moves the feature to the next one and updates the counts.
            await p.goto(base + '/saved');
            await p.locator('.mx-list-feature').waitFor();
            await p.locator('.mx-list-feature [data-save]').click();
            await p.waitForFunction(
                () => document.querySelector('.mx-list-feature .title')?.textContent === 'Second saved article',
            );
            assert.equal(await p.locator('.mx-list-feature').count(), 1);
            assert.deepEqual(await p.locator('#saved-show-m option').allTextContents(), [
                'All · 2',
                'Articles · 1',
                'Videos · 1',
            ]);
            await p.locator('#saved-show-m').selectOption('video');
            await p.waitForURL('**/saved?list=video');
            await p.locator('.mx-list-feature').waitFor();
            assert.equal(await p.locator('.mx-list-feature .title').innerText(), 'A saved video');
            assert.equal(await p.locator('[data-saved-section="article"]').count(), 0);
            assert.equal(await p.locator('#saved-show-m').inputValue(), 'video');
            // Settings: Jump to scrolls to the section without leaving the page.
            await p.goto(base + '/settings');
            await p.locator('#settings-jump-m').selectOption('/settings#my-teams-heading');
            await p.waitForFunction(() => location.hash === '#my-teams-heading');
            assert.equal(new URL(p.url()).pathname, '/settings');
            // The page scrolls smoothly; wait for it to land under the ribbon's 80px scroll margin.
            await p.waitForFunction(() => {
                const top = document.querySelector('#my-teams-heading').getBoundingClientRect().top;
                return top >= 0 && top < 200;
            });
            await ctx.close();
        }

        // Saved, empty and unreachable.
        {
            const ctx = await contextFor(390);
            await mockSaved(ctx, []);
            const p = await ctx.newPage();
            await p.goto(base + '/saved');
            await p.locator('[data-saved-empty]').waitFor();
            assert.equal(await p.locator('.mx-list-feature').count(), 0);
            assert.deepEqual(await p.locator('#saved-show-m option').allTextContents(), [
                'All · 0',
                'Articles · 0',
                'Videos · 0',
            ]);
            await ctx.close();
            const down = await contextFor(390);
            await down.route('**/api/saved**', (route) => route.fulfill({ status: 503, body: 'Unavailable' }));
            const q = await down.newPage();
            await q.goto(base + '/saved');
            await q.waitForFunction(
                () => !document.querySelector('[data-saved-status]').textContent.includes('Connecting'),
                null,
                { timeout: 15000 },
            );
            assert(await q.locator('.mx-ribbon').isVisible());
            assert.equal(await q.locator('.mx-list-feature').count(), 0);
            assert(await q.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
            await down.close();
        }

        // Without JavaScript: each ribbon select has a Go button that loads the chosen view.
        for (const width of [320, 390, 430]) {
            const ctx = await contextFor(width, look('kansas', 'light'), false),
                p = await ctx.newPage();
            for (const path of routes) {
                await p.goto(base + path);
                assert(await p.locator('.mx-ribbon').isVisible(), path);
                assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth), path);
            }
            await p.goto(base + '/sports/news');
            await p.locator('#hub-section-m').selectOption('/sports');
            await p.locator('.mx-ribbon button[type=submit]').click();
            assert.equal(new URL(p.url()).pathname, '/sports');
            await p.goto(base + '/saved');
            await p.locator('#saved-show-m').selectOption('article');
            await p.locator('.mx-ribbon button[type=submit]').click();
            assert.equal(new URL(p.url()).searchParams.get('list'), 'article');
            assert(await p.locator('p.empty', { hasText: 'Saved items need JavaScript turned on.' }).isVisible());
            await p.goto(base + '/settings');
            await p.locator('#settings-jump-m').selectOption('/settings#my-services');
            await p.locator('.mx-ribbon button[type=submit]').click();
            await p.waitForURL('**/settings#my-services');
            await ctx.close();
        }

        // Tablet and desktop keep their layouts: no ribbon, no dark feature, the old headings and indexes.
        for (const width of [1024, 1280, 1440])
            for (const theme of ['light', 'dark']) {
                const ctx = await contextFor(width, look('kansas', theme));
                await mockSaved(ctx);
                const p = await ctx.newPage();
                for (const path of routes) {
                    await visit(p, path, look('kansas', theme));
                    if (path === '/saved') await p.locator('[data-saved-list] .article').first().waitFor();
                    assert.equal(await p.locator('.mx-ribbon').filter({ visible: true }).count(), 0, path);
                    assert(await p.locator('.page-heading').first().isVisible(), path + ' heading');
                    assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth), path);
                    assert.deepEqual(await p.evaluate(audit), [], `${path} ${theme} ${width} contrast`);
                    const feature = p.locator('.mx-list-feature');
                    if (await feature.count())
                        assert.equal(
                            await feature.evaluate((n) => getComputedStyle(n).borderRadius),
                            await p
                                .locator('.article:not(.mx-list-feature)')
                                .first()
                                .evaluate((n) => getComputedStyle(n).borderRadius),
                            path + ' no phone feature styling',
                        );
                    if (path === '/saved') assert(await p.locator('[data-saved-tab]').first().isVisible());
                    if (path === '/settings') assert(await p.locator('.set-index').isVisible());
                    if (shots && width === 1440)
                        await p.screenshot({
                            path: `${shots}/tools-desktop-${path.slice(1).replace('/', '-')}-${theme}.png`,
                        });
                }
                await ctx.close();
            }
        assert.deepEqual(errors, []);
        console.log(
            'Release 6: All headlines (Sports, Entertainment), Saved and Settings ribbons at 320/390/430, three schemes, light/dark; contrast, overflow, unique IDs, 44px targets, one first-article feature (also after Load more and removal), counted Show select, Jump to, empty/unreachable Saved, no-JS Go paths, retained 1024/1280/1440 layouts passed.',
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
