// The folded phone Headlines front (docs/mobile-refresh-handoff.md, release 1) at 320, 390 and 430px in light and dark
// under three colour schemes: ribbon, dark lead block, top stories, timeline and category folds; no sideways scroll,
// readable text, 44px targets (64px fold summaries), no repeated ids. Folds open one at a time with JavaScript off, the
// News Section select opens a category, "More stories" and a search keep the old layout, and 1024/1280/1440px don't change.
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
            await admin.goto(adminBase + '/login?returnUrl=%2Fadmin%2Fprofiles');
            await admin.locator('#password').fill(password);
            await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
            await admin.waitForURL('**/admin/profiles');
            await admin.waitForLoadState('networkidle');
            const name = 'zz Phone ' + Date.now().toString(36);
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
            await p.goto(base + '/news/all');
            await p.locator(`form[action="profiles/${id}/open"]`).getByRole('button').click();
            await p.keyboard.type('3691');
            await p.waitForURL('**/news/all');
            storageState = await context.storageState();
            await context.close();
        }
        // Layout, contrast and targets at phone widths.
        for (const scheme of ['kansas', 'monochrome', 'cardinals'])
            for (const theme of ['light', 'dark'])
                for (const width of [320, 390, 430]) {
                    const context = await contextFor(width, { scheme, theme });
                    const page = await context.newPage();
                    page.on('pageerror', (e) => errors.push(e.message));
                    await visit(page, '/news/all', scheme, theme);
                    await page.waitForTimeout(300);
                    const label = `${scheme} ${theme} ${width}px`;
                    const phone = page.locator('.mx-phone');
                    if (!(await phone.count())) {
                        console.log('SKIP: no headlines yet, so there is no phone front to check.');
                        return;
                    }
                    assert(await phone.isVisible(), label + ' phone front shows');
                    assert(!(await page.locator('.paper-feed').isVisible()), label + ' desktop feed hidden');
                    assert.equal(await page.locator('.mx-ribbon h1').innerText(), 'All headlines');
                    const r = await page.evaluate(() => {
                        const visible = (n) => n.offsetParent !== null || getComputedStyle(n).position === 'fixed';
                        const ids = [...document.querySelectorAll('[id]')].map((n) => n.id);
                        const select = document.querySelector('#news-section-m').getBoundingClientRect();
                        return {
                            theme: document.documentElement.getAttribute('data-theme'),
                            over: document.documentElement.scrollWidth - innerWidth,
                            selectRight: select.right - innerWidth,
                            selectFont: getComputedStyle(document.querySelector('#news-section-m')).fontSize,
                            dupes: ids.filter((id, i) => ids.indexOf(id) !== i),
                            summaries: [...document.querySelectorAll('.mx-fold > summary')]
                                .filter(visible)
                                .filter((n) => n.getBoundingClientRect().height < 64).length,
                            small: [
                                ...document.querySelectorAll(
                                    '.mx-phone a, .mx-phone button:not([hidden]), .mx-phone select, .mx-top .save',
                                ),
                            ]
                                .filter(visible)
                                // Links inside a title or a line of text are as tall as the line; their row is the target.
                                .filter((n) => !n.closest('.mx-block__title, .mx-top__title'))
                                .filter((n) => n.getBoundingClientRect().height < 44)
                                .map((n) => n.className || n.tagName),
                            open: [...document.querySelectorAll('.mx-fold')].map((d) => d.open),
                            darkBlock: getComputedStyle(document.querySelector('.mx-block')).backgroundColor,
                            pageBg: getComputedStyle(document.body).backgroundColor,
                        };
                    });
                    assert.equal(r.theme, theme, label + ' theme');
                    assert.equal(r.over, 0, label + ' scrolls sideways');
                    assert(r.selectRight <= 0, label + ' select past the right edge');
                    assert.equal(r.selectFont, '16px', label + ' select font (iOS zoom)');
                    assert.deepEqual(r.dupes, [], label + ' repeated ids');
                    assert.equal(r.summaries, 0, label + ' fold summaries under 64px');
                    assert.deepEqual(r.small, [], label + ' touch targets under 44px');
                    if (r.open.length)
                        assert.deepEqual(
                            r.open,
                            r.open.map((_, i) => i === 0),
                            label + ' first fold open',
                        );
                    assert.notEqual(r.darkBlock, r.pageBg, label + ' dark block separates from the page');
                    // Every top story has Save with Share under it, both inside the row.
                    for (const row of await page.locator('.mx-phone .mx-top li').all()) {
                        const [rowBox, save, share] = await Promise.all([
                            row.boundingBox(),
                            row.locator('.save').boundingBox(),
                            row.locator('.share-button:not([hidden])').boundingBox(),
                        ]);
                        assert(save && share, label + ' Save and Share on each top story');
                        assert(share.y >= save.y + save.height - 1, label + ' Share under Save');
                        assert(
                            share.x + share.width <= width && share.y + share.height <= rowBox.y + rowBox.height + 1,
                            label + ' Share inside its row',
                        );
                    }
                    if (width === 390 && scheme === 'kansas') {
                        const first = page.locator('.mx-phone .mx-top li').first();
                        const title = (await first.locator('.mx-top__title').innerText()).trim();
                        await first.locator('.share-button').click();
                        const dialog = page.getByRole('dialog');
                        await dialog.waitFor();
                        assert.equal(
                            (await dialog.locator('.article-share__title').innerText()).trim(),
                            title,
                            label + ' Share names the story',
                        );
                        await page.keyboard.press('Escape');
                        await dialog.waitFor({ state: 'detached' });
                    }
                    const bad = await page.evaluate(audit);
                    assert.deepEqual(bad, [], label + ' contrast');
                    if (shots && (width === 390 || width === 320) && scheme === 'kansas')
                        await page.screenshot({
                            path: `${shots}headlines-phone-${width}-${theme}.png`,
                            fullPage: true,
                        });
                    await context.close();
                }

        // Folds with JavaScript off: native <details name>, one open at a time; the select has a Go button.
        {
            const context = await contextFor(390, { js: false });
            const page = await context.newPage();
            await page.goto(base + '/news/all');
            const folds = page.locator('.mx-fold');
            const n = await folds.count();
            assert(await page.locator('.mx-ribbon noscript, .mx-ribbon button[type="submit"]').count(), 'Go button');
            if (n >= 2) {
                await folds.nth(1).locator('summary').click();
                assert.deepEqual(
                    await folds.evaluateAll((d) => d.map((x) => x.open)),
                    [...Array(n)].map((_, i) => i === 1),
                    'opening the second fold closes the first',
                );
                await folds.nth(1).locator('summary').focus();
                await page.keyboard.press('Enter');
                assert.equal(await folds.nth(1).evaluate((d) => d.open), false, 'Enter closes it');
                await page.keyboard.press(' ');
                assert.equal(await folds.nth(1).evaluate((d) => d.open), true, 'Space opens it');
            } else console.log('Note: fewer than two category folds here; exclusive opening not checked.');
            await context.close();
        }

        // The News Section select opens a category; "More stories" and a search keep the old layout.
        {
            const context = await contextFor(390);
            const page = await context.newPage();
            page.on('pageerror', (e) => errors.push(e.message));
            await visit(page, '/news/all', 'kansas', 'light');
            const option = page.locator('#news-section-m option:not([selected])[value*="category="]');
            if (await option.count()) {
                const name = (await option.first().innerText()).trim();
                await Promise.all([
                    page.waitForURL(/category=\d+/),
                    page.selectOption('#news-section-m', await option.first().getAttribute('value')),
                ]);
                await page.waitForLoadState('networkidle');
                assert.equal(await page.locator('.mx-ribbon h1').innerText(), name, 'title follows the category');
                assert.equal(
                    await page
                        .locator('.mx-fold__name', { hasText: new RegExp(`^${name.replace(/ News$/, '')}$`) })
                        .count(),
                    0,
                    'the open category has no fold of its own',
                );
                await page.goto(base + '/news/all', { waitUntil: 'networkidle' });
            }
            const more = page.locator('.mx-timeline .mx-more');
            if (await more.count()) {
                await Promise.all([page.waitForURL(/after=/), more.click()]);
                // Enhanced navigation changes the address before it swaps the page in.
                await page.locator('.mx-phone').waitFor({ state: 'detached' });
                assert(await page.locator('.paper-feed').isVisible(), 'More stories: the list shows');
            }
            await page.goto(base + '/news/all?q=the', { waitUntil: 'networkidle' });
            assert.equal(await page.locator('.mx-phone').count(), 0, 'search keeps the old layout');
            await context.close();
        }

        // iPad and desktop: unchanged.
        for (const width of [1024, 1280, 1440]) {
            const context = await contextFor(width);
            const page = await context.newPage();
            await page.goto(base + '/news/all', { waitUntil: 'networkidle' });
            assert(!(await page.locator('.mx-phone').isVisible()), `${width}px phone front hidden`);
            assert(await page.locator('.paper-feed').isVisible(), `${width}px feed shows`);
            assert(await page.locator('.section-tabs:visible').first().isVisible(), `${width}px News tabs show`);
            await context.close();
        }
        assert.deepEqual(errors, []);
        console.log(
            'Headlines phone front at 320/390/430px (kansas, monochrome, cardinals; light/dark), folds without JavaScript, category select, fallbacks and 1024/1280/1440px passed.',
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
