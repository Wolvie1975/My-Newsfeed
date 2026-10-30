// The News section (docs/news-section-handoff.md): the News front at phone widths (320/390/430 × three schemes × light
// and dark) and at 1024/1280/1440, Kansas City under News, the News tabs, the 6-link navigation and the phone menu, and
// the permanent redirects from the old Headlines and Local addresses. Every story row on the front has Share and Save.
//
// With profiles switched on (the live site) it needs ADMIN_PASSWORD: it adds one throwaway profile, opens it, sets the
// scheme and theme on that profile, and deletes it at the end.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { audit } = require('./contrast-audit.cjs');

const base = process.env.BASE_URL || 'http://localhost:5295';
const adminBase = process.env.ADMIN_BASE_URL || base;
const shots = process.env.SCREENSHOT_DIR;
const password = process.env.ADMIN_PASSWORD;

(async () => {
    // The redirects run before the profile gate, so they're checked first and without a profile.
    for (const [from, to] of [
        ['/local', '/news/kansas-city'],
        ['/local?section=events', '/news/kansas-city?section=events'],
        ['/news?category=all&source=3', '/news/all?category=all&source=3'],
        ['/news?q=election', '/news/all?q=election'],
    ]) {
        const response = await fetch(base + from, { redirect: 'manual' });
        assert.equal(response.status, 301, from);
        assert.equal(
            new URL(response.headers.get('location'), base).pathname +
                new URL(response.headers.get('location'), base).search,
            to,
            from,
        );
    }
    const profiles = (await fetch(base + '/editions', { headers: { Accept: 'text/html' } })).status !== 404;
    if (profiles && !password) {
        console.log(
            'Redirects passed. SKIP the rest: profiles are on here; set ADMIN_PASSWORD so the check can add a throwaway profile.',
        );
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
                    localStorage.setItem('mynewsfeed-color-scheme', s);
                    localStorage.setItem('theme', t);
                } catch {}
            },
            [scheme, theme],
        );
        return context;
    };
    // With profiles on, the scheme and theme are the open profile's settings, so set them there and reload.
    const visit = async (page, path, scheme, theme) => {
        await page.goto(base + path, { waitUntil: 'networkidle' });
        if (!profiles) return;
        await page.evaluate(
            ([s, t]) => {
                window.myNewspaperPrefs.setItem('mynewsfeed-color-scheme', s);
                window.myNewspaperPrefs.setItem('theme', t);
            },
            [scheme, theme],
        );
        await page.waitForTimeout(900);
        await page.reload({ waitUntil: 'networkidle' });
    };
    // Layout facts shared by every width: sideways scroll, repeated ids and touch targets under 44px.
    const layout = (page, scope) =>
        page.evaluate((scope) => {
            const visible = (n) => n.offsetParent !== null || getComputedStyle(n).position === 'fixed';
            const ids = [...document.querySelectorAll('[id]')].map((n) => n.id);
            return {
                over: document.documentElement.scrollWidth - innerWidth,
                dupes: ids.filter((id, i) => ids.indexOf(id) !== i),
                small: [...document.querySelectorAll(`${scope} a, ${scope} button:not([hidden]), ${scope} select`)]
                    .filter(visible)
                    // Links inside a title or a line of text are as tall as the line; their row is the target.
                    .filter(
                        (n) =>
                            !n.closest(
                                '.mx-block__title, .mx-top__title, .ef-headlines__title, .ef-lead__title, .nf-pictures__title, .ef-note, .spf-agate, .mx-ribbon__kicker',
                            ),
                    )
                    .filter((n) => n.getBoundingClientRect().height < 43.9)
                    .map((n) => n.className || n.tagName),
            };
        }, scope);
    try {
        if (profiles) {
            admin = await (await browser.newContext({ viewport: { width: 1440, height: 1000 } })).newPage();
            await admin.goto(adminBase + '/login?returnUrl=%2Fadmin%2Fprofiles');
            await admin.locator('#password').fill(password);
            await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
            await admin.waitForURL('**/admin/profiles');
            await admin.waitForLoadState('networkidle');
            const name = 'zz News ' + Date.now().toString(36);
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
            await p.goto(base + '/news');
            await p.locator(`form[action="profiles/${id}/open"]`).getByRole('button').click();
            await p.keyboard.type('3691');
            await p.waitForURL((u) => u.pathname === '/news');
            storageState = await context.storageState();
            await context.close();
        }

        // Phones: the folded News front, and Kansas City under News.
        for (const scheme of ['kansas', 'monochrome', 'cardinals'])
            for (const theme of ['light', 'dark'])
                for (const width of [320, 390, 430]) {
                    const context = await contextFor(width, { scheme, theme });
                    const page = await context.newPage();
                    page.on('pageerror', (e) => errors.push(e.message));
                    const label = `${scheme} ${theme} ${width}px`;
                    await visit(page, '/news', scheme, theme);
                    assert(await page.locator('.mx-phone').first().isVisible(), label + ' phone front shows');
                    assert.equal(await page.locator('.mx-ribbon h1').innerText(), 'News');
                    assert.equal(
                        await page.locator('#news-section-m').inputValue(),
                        '/news',
                        label + ' News home selected',
                    );
                    assert.equal(
                        await page.locator('#news-section-m').evaluate((s) => getComputedStyle(s).fontSize),
                        '16px',
                    );
                    const r = await layout(page, '.mx-phone');
                    assert.equal(r.over, 0, label + ' scrolls sideways');
                    assert.deepEqual(r.dupes, [], label + ' repeated ids');
                    assert.deepEqual(r.small, [], label + ' touch targets under 44px');
                    // Briefing tiles sit side by side inside the screen.
                    const tiles = await page
                        .locator('.nf-brief--phone .nf-tile')
                        .evaluateAll((ns) => ns.map((n) => n.getBoundingClientRect()).map((b) => [b.left, b.right]));
                    assert(tiles.length >= 1, label + ' briefing tiles');
                    // A new profile (or no profile) is on its first visit: no Since last visit tile yet.
                    assert.equal(
                        await page.locator('.nf-tile', { hasText: 'Since last visit' }).count(),
                        0,
                        label + ' no Since tile on a first visit',
                    );
                    assert(
                        tiles.every(([l, rt]) => l >= 0 && rt <= width),
                        label + ' tiles inside the screen',
                    );
                    // Every top headline has Save with Share under it.
                    for (const row of await page.locator('.mx-phone .mx-top li').all()) {
                        const save = await row.locator('.save').boundingBox();
                        const share = await row.locator('.share-button:not([hidden])').boundingBox();
                        assert(
                            save && share && share.y >= save.y + save.height - 1,
                            label + ' Save and Share on each top headline',
                        );
                    }
                    assert.deepEqual(
                        await page
                            .locator('.mx-phone .mx-fold')
                            .evaluateAll((d) => d.map((x) => x.open))
                            .then((o) => o.slice(0, 1)),
                        [true],
                        label + ' first fold open',
                    );
                    assert.deepEqual(await page.evaluate(audit), [], label + ' contrast');
                    if (shots && scheme === 'kansas' && width !== 430)
                        await page.screenshot({ path: `${shots}news-front-${width}-${theme}.png`, fullPage: true });
                    if (scheme === 'kansas') {
                        await visit(page, '/news/kansas-city', scheme, theme);
                        assert.equal(
                            await page.locator('#news-section-m').inputValue(),
                            '/news/kansas-city',
                            label + ' Kansas City selected',
                        );
                        assert.match(
                            await page.locator('.mx-ribbon__kicker').innerText(),
                            /^news · closer to home$/i,
                            label + ' Kansas City kicker',
                        );
                        const k = await layout(page, '.mx-phone');
                        assert.equal(k.over, 0, label + ' Kansas City scrolls sideways');
                        assert.deepEqual(k.dupes, [], label + ' Kansas City repeated ids');
                        assert.deepEqual(await page.evaluate(audit), [], label + ' Kansas City contrast');
                    }
                    await context.close();
                }

        // Share opens for a top headline; the phone menu's News group; the ribbon select without JavaScript.
        {
            const context = await contextFor(390);
            const page = await context.newPage();
            page.on('pageerror', (e) => errors.push(e.message));
            await page.goto(base + '/news', { waitUntil: 'networkidle' });
            const first = page.locator('.mx-phone .mx-top li').first();
            if (await first.count()) {
                const title = (await first.locator('.mx-top__title').innerText()).trim();
                await first.locator('.share-button').click();
                const dialog = page.getByRole('dialog');
                await dialog.waitFor();
                assert.equal((await dialog.locator('.article-share__title').innerText()).trim(), title);
                await page.keyboard.press('Escape');
                await dialog.waitFor({ state: 'detached' });
            }
            await page.locator('.mobile-nav > summary').click();
            assert.deepEqual(
                await page.locator('[aria-labelledby="mobile-news-heading"] .mobile-subnav a').allInnerTexts(),
                ['News home', 'All headlines', 'Kansas City'],
            );
            await context.close();
            const plain = await contextFor(390, { js: false });
            const q = await plain.newPage();
            await q.goto(base + '/news');
            await q.selectOption('#news-section-m', '/news/all');
            await q.locator('.mx-ribbon button[type=submit]').click();
            await q.waitForURL((u) => u.pathname === '/news/all');
            await plain.close();
        }

        // iPad and desktop: the News front's columns, tabs and navigation.
        for (const width of [1024, 1280, 1440])
            for (const theme of ['light', 'dark']) {
                const context = await contextFor(width, { theme });
                const page = await context.newPage();
                page.on('pageerror', (e) => errors.push(e.message));
                const label = `${theme} ${width}px`;
                await visit(page, '/news', 'kansas', theme);
                assert(!(await page.locator('.mx-phone').first().isVisible()), label + ' phone front hidden');
                const nav = page.locator('.site-nav--inline a');
                assert.deepEqual(await nav.allInnerTexts(), [
                    'Home',
                    'News',
                    'Sports',
                    'Entertainment',
                    'Where to watch',
                    'YouTube',
                ]);
                assert.equal(await nav.filter({ hasText: 'News' }).getAttribute('aria-current'), 'page');
                assert.equal(
                    await page.locator('.section-tabs:visible a[aria-current="page"]').innerText(),
                    'News home',
                );
                for (const module of ['.nf-brief', '.nf-day', '.ef-lead', '.nf-desks'])
                    assert(await page.locator(module).first().isVisible(), `${label} ${module}`);
                // Every headline row on the front has Share and Save.
                const rows = await page
                    .locator('.nf .ef-headlines li')
                    .evaluateAll((ns) =>
                        ns.map((n) => [
                            !!n.querySelector('[data-share-open]:not([hidden])'),
                            !!n.querySelector('.save'),
                        ]),
                    );
                assert(
                    rows.length > 0 && rows.every(([share, save]) => share && save),
                    label + ' Share and Save on every row',
                );
                const r = await layout(page, 'main');
                assert.equal(r.over, 0, label + ' scrolls sideways');
                assert.deepEqual(r.dupes, [], label + ' repeated ids');
                assert.deepEqual(await page.evaluate(audit), [], label + ' contrast');
                if (shots && width !== 1024)
                    await page.screenshot({ path: `${shots}news-front-${width}-${theme}.png`, fullPage: true });
                // Under News, the section link is marked as the current section rather than the page.
                await page.goto(base + '/news/kansas-city', { waitUntil: 'networkidle' });
                assert.equal(await nav.filter({ hasText: 'News' }).getAttribute('aria-current'), 'true');
                assert.equal(
                    await page.locator('.section-tabs:visible a[aria-current="page"]').innerText(),
                    'Kansas City',
                );
                await page.locator('.lc-jump a', { hasText: 'Local news' }).click();
                await page.waitForFunction(() => location.hash === '#local-news');
                await page.waitForFunction(
                    () => document.querySelector('#local-news').getBoundingClientRect().top < innerHeight / 2,
                    null,
                    { timeout: 5000 },
                );
                assert.equal((await layout(page, 'main')).over, 0, label + ' Kansas City scrolls sideways');
                await context.close();
            }
        assert.deepEqual(errors, []);
        console.log(
            'News section: 301s from /local and old /news lists; the News front at 320/390/430 (kansas, monochrome, cardinals; light/dark) with briefing tiles, Save and Share on every top headline, folds, Share dialog, phone menu and no-JS Section select; 1024/1280/1440 light/dark columns, tabs, 6-link nav, Share and Save on every row; Kansas City under News with its tabs and jump links passed.',
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
