// The folded phone fronts for Home, Sports and Scoreboard (docs/mobile-refresh-handoff.md, release 2) at 320, 390 and
// 430px in light and dark under three colour schemes: the ribbon, dark block, timeline and folds; no sideways scroll,
// readable text, 44px targets (64px fold summaries), no repeated ids, at most one fold open. Folds open one at a time
// with JavaScript off; the Scoreboard's date stepper and Sport select work and a refresh keeps the open fold; a Home
// section hidden in Settings hides its fold; Sports' Section select navigates; 1024/1280/1440px don't change.
//
// With profiles switched on (the live site) it needs ADMIN_PASSWORD: it adds one throwaway profile, opens it, sets the
// scheme, theme and hidden Home section on that profile, and deletes it at the end.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { audit } = require('./contrast-audit.cjs');

const base = process.env.BASE_URL || 'http://localhost:5295';
const adminBase = process.env.ADMIN_BASE_URL || base;
const shots = process.env.SCREENSHOT_DIR;
const password = process.env.ADMIN_PASSWORD;
const pages = [
    { path: '/', title: /^Good (morning|afternoon|evening)$/, desktop: '.bs-home' },
    { path: '/sports', title: /^Sports$/, desktop: '.spf' },
    { path: '/scoreboard', title: /^Scoreboard$/, desktop: '.sb-groups, [data-scoreboard] .scoreboard-dates' },
];
const PERSONAL = 'mynewsfeed-personal-v1';

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
    const look = (scheme, theme) => ({
        'mynewsfeed-color-scheme': scheme,
        theme,
        [PERSONAL]: JSON.stringify({ order: [], hidden: [] }),
    });
    try {
        if (profiles) {
            admin = await (await browser.newContext({ viewport: { width: 1440, height: 1000 } })).newPage();
            await admin.goto(adminBase + '/login?returnUrl=%2Fadmin%2Fprofiles');
            await admin.locator('#password').fill(password);
            await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
            await admin.waitForURL('**/admin/profiles');
            await admin.waitForLoadState('networkidle');
            const name = 'zz Fronts ' + Date.now().toString(36);
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

        // Layout, contrast and targets at phone widths.
        for (const scheme of ['kansas', 'monochrome', 'cardinals'])
            for (const theme of ['light', 'dark'])
                for (const width of [320, 390, 430]) {
                    const context = await contextFor(width, look(scheme, theme));
                    const page = await context.newPage();
                    page.on('pageerror', (e) => errors.push(e.message));
                    let first = true;
                    for (const { path, title } of pages) {
                        // Profile settings only need saving once per scheme and theme.
                        await visit(page, path, first ? look(scheme, theme) : {});
                        first = false;
                        await page.waitForTimeout(300);
                        const label = `${path} ${scheme} ${theme} ${width}px`;
                        const phone = page.locator('.mx-phone');
                        assert(await phone.isVisible(), label + ' phone front shows');
                        assert.match(await page.locator('.mx-ribbon h1').innerText(), title, label + ' title');
                        const r = await page.evaluate(() => {
                            const visible = (n) => n.offsetParent !== null;
                            const ids = [...document.querySelectorAll('[id]')].map((n) => n.id);
                            const field = document.querySelector('.mx-ribbon select, .mx-ribbon input');
                            return {
                                theme: document.documentElement.getAttribute('data-theme'),
                                over: document.documentElement.scrollWidth - innerWidth,
                                fieldRight: field.getBoundingClientRect().right - innerWidth,
                                fieldFont: getComputedStyle(field).fontSize,
                                dupes: ids.filter((id, i) => ids.indexOf(id) !== i),
                                summaries: [...document.querySelectorAll('.mx-fold > summary')]
                                    .filter(visible)
                                    .filter((n) => n.getBoundingClientRect().height < 64).length,
                                small: [
                                    ...document.querySelectorAll(
                                        '.mx-phone a, .mx-phone button:not([hidden]), .mx-phone select, .mx-phone input',
                                    ),
                                ]
                                    .filter(visible)
                                    .filter((n) => !n.closest('.mx-block__title, .mx-top__title'))
                                    .filter((n) => n.getBoundingClientRect().height < 44)
                                    .map((n) => n.className || n.tagName),
                                open: [...document.querySelectorAll('.mx-fold')].filter((d) => d.open).length,
                                folds: document.querySelectorAll('.mx-fold').length,
                                desktopHidden: [...document.querySelectorAll('.mx-desktop *')].every(
                                    (n) => n.getClientRects().length === 0,
                                ),
                            };
                        });
                        assert.equal(r.theme, theme, label + ' theme');
                        assert.equal(r.over, 0, label + ' scrolls sideways');
                        assert(r.fieldRight <= 0, label + ' ribbon field past the right edge');
                        assert.equal(r.fieldFont, '16px', label + ' ribbon field font (iOS zoom)');
                        assert.deepEqual(r.dupes, [], label + ' repeated ids');
                        assert.equal(r.summaries, 0, label + ' fold summaries under 64px');
                        assert.deepEqual(r.small, [], label + ' touch targets under 44px');
                        assert(r.open <= 1, label + ' more than one fold open');
                        if (r.folds) assert.equal(r.open, 1, label + ' a fold starts open');
                        assert(r.desktopHidden, label + ' desktop copy hidden');
                        assert.deepEqual(await page.evaluate(audit), [], label + ' contrast');
                        if (shots && scheme === 'kansas')
                            await page.screenshot({
                                path: `${shots}phone-${path === '/' ? 'home' : path.slice(1)}-${width}-${theme}.png`,
                                fullPage: true,
                            });
                    }
                    await context.close();
                }

        // Folds with JavaScript off: native <details name>, one open at a time; the selects have a Go button.
        {
            const context = await contextFor(390, look('kansas', 'light'), false);
            const page = await context.newPage();
            for (const { path } of pages) {
                await page.goto(base + path);
                const folds = page.locator('.mx-fold');
                const n = await folds.count();
                if (n < 2) {
                    console.log(`Note: fewer than two folds on ${path}; exclusive opening not checked.`);
                    continue;
                }
                const shut = (await folds.evaluateAll((d) => d.map((x) => x.open))).indexOf(false);
                await folds.nth(shut).locator('summary').click();
                assert.deepEqual(
                    await folds.evaluateAll((d) => d.map((x) => x.open)),
                    [...Array(n)].map((_, i) => i === shut),
                    path + ' opening a fold closes the others',
                );
            }
            await context.close();
        }

        // Scoreboard: the date stepper, the Sport select, and a refresh that keeps the open fold.
        {
            const context = await contextFor(390, look('kansas', 'light'));
            const page = await context.newPage();
            page.on('pageerror', (e) => errors.push(e.message));
            await visit(page, '/scoreboard');
            await Promise.all([
                page.waitForURL(/day=\d{4}-\d{2}-\d{2}/),
                page.locator('.mx-stepper a[aria-label^="Next day"]').click(),
            ]);
            await page.waitForLoadState('networkidle');
            await page.locator('.mx-stepper__day', { hasText: /^Tomorrow · / }).waitFor({ timeout: 10000 }); // next day
            await Promise.all([page.waitForURL(/day=/), page.getByRole('link', { name: 'Back to today' }).click()]);
            await page.waitForLoadState('networkidle');
            await page.locator('.mx-stepper__day', { hasText: /^Today · / }).waitFor({ timeout: 10000 }); // back to today
            const league = page.locator('#scoreboard-show-m option[value^="c:"]').first();
            if (await league.count()) {
                const value = await league.getAttribute('value');
                await Promise.all([
                    page.waitForURL(/show=c%3A|show=c:/),
                    page.selectOption('#scoreboard-show-m', value),
                ]);
                await page.waitForLoadState('networkidle');
                assert.equal(await page.locator('#scoreboard-show-m').inputValue(), value, 'sport kept');
                await page.goto(base + '/scoreboard', { waitUntil: 'networkidle' });
            }
            const folds = page.locator('[data-scoreboard] .mx-fold');
            if ((await folds.count()) >= 2) {
                const shut = (await folds.evaluateAll((d) => d.map((x) => x.open))).indexOf(false);
                const key = await folds.nth(shut).getAttribute('data-fold-key');
                await folds.nth(shut).locator('summary').click();
                await page.evaluate(() => document.activeElement?.blur());
                await Promise.all([
                    page.waitForResponse(
                        (r) => r.url().includes('/scoreboard/fragment') && r.url().includes('phone=true'),
                    ),
                    page.evaluate(() => window.dispatchEvent(new Event('online'))),
                ]);
                await page.waitForTimeout(500);
                assert.equal(
                    await page.locator(`[data-scoreboard] .mx-fold[data-fold-key="${key}"]`).evaluate((d) => d.open),
                    true,
                    'the fold the reader opened stays open after a refresh',
                );
                assert.equal(
                    await page.locator('[data-scoreboard] .mx-fold[open]').count(),
                    1,
                    'one fold open after refresh',
                );
                assert(await page.locator('.mx-phone .mx-ribbon').isVisible(), 'phone front survives the refresh');
            } else console.log('Note: fewer than two leagues today; refresh keeping the open fold not checked.');
            await context.close();
        }

        // Sports: the Section select navigates. Home: a section hidden in Settings hides its fold.
        {
            const context = await contextFor(390, {
                ...look('kansas', 'light'),
                [PERSONAL]: JSON.stringify({ order: [], hidden: ['videos'] }),
            });
            const page = await context.newPage();
            page.on('pageerror', (e) => errors.push(e.message));
            await visit(page, '/sports');
            await Promise.all([
                page.waitForURL('**/standings**'),
                page.selectOption('#sports-section-m', '/standings'),
            ]);
            await visit(page, '/', { [PERSONAL]: JSON.stringify({ order: [], hidden: ['videos'] }) });
            const videos = page.locator('.mx-phone [data-home-section="videos"]');
            if (await videos.count()) assert(!(await videos.isVisible()), 'hidden Videos section hides its fold');
            else console.log('Note: no videos here; the hidden-section fold was not checked.');
            await visit(page, '/', { [PERSONAL]: JSON.stringify({ order: [], hidden: [] }) });
            await context.close();
        }

        // iPad and desktop: unchanged.
        for (const width of [1024, 1280, 1440]) {
            const context = await contextFor(width);
            const page = await context.newPage();
            for (const { path, desktop } of pages) {
                await page.goto(base + path, { waitUntil: 'networkidle' });
                assert(!(await page.locator('.mx-phone').isVisible()), `${path} ${width}px phone front hidden`);
                assert(await page.locator(desktop).first().isVisible(), `${path} ${width}px desktop shows`);
                assert(
                    await page.locator('.page-heading, .bs-masthead').first().isVisible(),
                    `${path} ${width}px heading`,
                );
            }
            await context.close();
        }
        assert.deepEqual(errors, []);
        console.log(
            'Home, Sports and Scoreboard phone fronts at 320/390/430px (kansas, monochrome, cardinals; light/dark), folds without JavaScript, the date stepper, Sport and Section selects, a refresh keeping the open fold, a hidden Home section, and 1024/1280/1440px passed.',
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
