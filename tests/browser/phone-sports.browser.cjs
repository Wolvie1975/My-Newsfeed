// Release 3: folded Top 25, Standings, College Sports and F1; profiles-on runs create and delete a temporary profile.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { audit } = require('./contrast-audit.cjs');

const base = process.env.BASE_URL || 'http://localhost:5295';
const shots = process.env.SCREENSHOT_DIR;
const password = process.env.ADMIN_PASSWORD;
const pages = [
    { path: '/rankings', title: /^Top 25$/, desktop: '.rk-cols' },
    { path: '/standings', title: /^Standings$/, desktop: '.standings-view' },
    { path: '/college', title: /^College Sports$/, desktop: '.cl-cols' },
    { path: '/f1', title: /^F1$/, desktop: '.f1-cols' },
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
        'mynewsfeed-sports-teams-v1': JSON.stringify(['kansas', 'byu']),
    });
    try {
        if (profiles) {
            admin = await (await browser.newContext({ viewport: { width: 1440, height: 1000 } })).newPage();
            await admin.goto(base + '/login?returnUrl=%2Fadmin%2Fprofiles');
            await admin.locator('#password').fill(password);
            await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
            await admin.waitForURL('**/admin/profiles');
            await admin.waitForLoadState('networkidle');
            const name = 'zz Sports ' + Date.now().toString(36);
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

        // Folded pages: three phone widths, three schemes, both themes.
        for (const scheme of ['kansas', 'monochrome', 'cardinals'])
            for (const theme of ['light', 'dark'])
                for (const width of [320, 390, 430]) {
                    const context = await contextFor(width, look(scheme, theme));
                    const page = await context.newPage();
                    page.on('pageerror', (e) => errors.push(e.message));
                    for (const { path, title } of pages) {
                        await visit(page, path, look(scheme, theme));
                        const phone = page.locator('[data-phone-sports]');
                        assert.equal(await phone.count(), 1);
                        assert(await phone.isVisible());
                        assert.match(await phone.locator('.mx-ribbon h1').innerText(), title);
                        const r = await page.evaluate(() => {
                            const root = document.querySelector('[data-phone-sports]');
                            const visible = (n) =>
                                n.getBoundingClientRect().width > 0 && n.getBoundingClientRect().height > 0;
                            const ids = [...document.querySelectorAll('[id]')].map((n) => n.id);
                            const folds = [...root.querySelectorAll('.mx-fold:not([hidden])')];
                            const controls = [...root.querySelectorAll('a,button,select,summary')].filter(visible);
                            return {
                                over: document.documentElement.scrollWidth - innerWidth,
                                duplicates: ids.filter((id, i) => ids.indexOf(id) !== i),
                                small: controls
                                    .filter((n) => n.getBoundingClientRect().height < 43.9)
                                    .map((n) => [
                                        n.tagName,
                                        n.textContent.trim().slice(0, 60),
                                        n.getBoundingClientRect().height,
                                    ]),
                                summaries: folds.filter(
                                    (n) => n.querySelector('summary').getBoundingClientRect().height < 63.9,
                                ).length,
                                folds: folds.length,
                                open: folds.filter((n) => n.open).length,
                                select: getComputedStyle(root.querySelector('.mx-ribbon select')).fontSize,
                            };
                        });
                        const label = `${path} ${width}/${scheme}/${theme}`;
                        assert(r.over <= 0, label + ' overflow ' + r.over);
                        assert.deepEqual(r.duplicates, [], label + ' duplicate ids');
                        assert.deepEqual(r.small, [], label + ' touch targets');
                        assert.equal(r.summaries, 0, label + ' fold summaries');
                        if (r.folds) assert.equal(r.open, 1, label + ' one fold starts open');
                        assert.equal(r.select, '16px', label + ' select text');
                        assert.deepEqual(await page.evaluate(audit), [], label + ' contrast');
                        if (shots && scheme === 'kansas')
                            await page.screenshot({
                                path: `${shots}phone-${path.slice(1)}-${width}-${theme}.png`,
                                fullPage: true,
                            });
                    }
                    await context.close();
                }
        // Native folds and all four ribbon selects work without JavaScript.
        {
            const context = await contextFor(390, {}, false);
            const page = await context.newPage();
            for (const { path } of pages) {
                await page.goto(base + path);
                const folds = page.locator('.mx-fold:not([hidden])');
                if ((await folds.count()) > 1) {
                    const closed = (await folds.evaluateAll((ns) => ns.map((n) => n.open))).indexOf(false);
                    const summary = folds.nth(closed).locator('summary');
                    await summary.focus();
                    await page.keyboard.press('Enter');
                    assert.equal(await folds.locator('xpath=self::*[@open]').count(), 1, path + ' exclusive Enter');
                    assert(await folds.nth(closed).evaluate((n) => n.open));
                    await page.keyboard.press('Space');
                    assert(!(await folds.nth(closed).evaluate((n) => n.open)), path + ' Space closes');
                    await summary.click();
                    assert(await folds.nth(closed).evaluate((n) => n.open));
                }
                if (path === '/f1') {
                    const weekend = page.locator('.mx-phone .mx-block .mx-weekend');
                    if (await weekend.count()) {
                        await weekend.locator('summary').click();
                        assert(await weekend.evaluate((n) => n.open), 'no-JS weekend timetable opens');
                        assert(await weekend.locator('li').count(), 'collected weekend sessions shown');
                    }
                }
                const form = page.locator('.mx-ribbon form');
                const select = form.locator('select');
                const value = await select.locator('option').evaluateAll((ns) => ns.find((n) => !n.selected)?.value);
                if (value) {
                    await select.selectOption(value);
                    await form.locator('button[type=submit]').click();
                    await page.waitForLoadState('networkidle');
                    assert.equal(await page.locator('.mx-ribbon select').inputValue(), value, path + ' no-JS select');
                }
            }
            await context.close();
        }
        // JS selects, My Teams and persisted choices, deep links into existing views.
        {
            const context = await contextFor(390, look('kansas', 'light'));
            const page = await context.newPage();
            for (const { path } of pages) {
                await visit(page, path, look('kansas', 'light'));
                const select = page.locator('.mx-ribbon select');
                const value = await select.locator('option').evaluateAll((ns) => ns.find((n) => !n.selected)?.value);
                if (value) {
                    await select.selectOption(value);
                    await page.waitForLoadState('networkidle');
                    await page.waitForTimeout(500);
                    assert.equal(await page.locator('.mx-ribbon select').inputValue(), value, path + ' JS select');
                    await page.reload({ waitUntil: 'networkidle' });
                    assert.equal(
                        await page.locator('.mx-ribbon select').inputValue(),
                        value,
                        path + ' persisted selection',
                    );
                }
            }
            await visit(page, '/college', look('kansas', 'light'));
            const selectedGames = page.locator('[data-phone-team-only]:not([hidden])');
            if (await selectedGames.count())
                assert(
                    (await selectedGames.allTextContents()).every((t) => /Kansas|BYU/.test(t)),
                    'only chosen teams in Your teams',
                );
            await page.goto(base + '/f1?calendar=all', { waitUntil: 'networkidle' });
            assert((await page.locator('.mx-phone .mx-fold[open]').count()) <= 1);
            await context.close();
        }
        // Retained tablet/desktop layouts, no repeated ids or sideways scroll.
        for (const width of [744, 1024, 1440]) {
            const context = await contextFor(width);
            const page = await context.newPage();
            for (const { path, desktop } of pages) {
                await page.goto(base + path, { waitUntil: 'networkidle' });
                assert(!(await page.locator('[data-phone-sports]').isVisible()));
                assert(await page.locator(desktop).first().isVisible());
                assert(
                    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                    path + ' desktop overflow',
                );
            }
            await context.close();
        }
        assert.deepEqual(errors, []);
        console.log(
            'Top 25, Standings, College and F1: 320/390/430px, three schemes, light/dark; contrast, overflow, 44px targets, exclusive folds/keyboard/no-JS selects, JS navigation and persistence, My Teams, full F1 calendar and retained 744/1024/1440px layouts passed.',
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
