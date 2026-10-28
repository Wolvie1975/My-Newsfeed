const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { audit } = require('./contrast-audit.cjs');

const base = process.env.BASE_URL || 'http://localhost:5295';
const shots = process.env.SCREENSHOT_DIR;
const password = process.env.ADMIN_PASSWORD;
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
        'mynewsfeed-sports-teams-v1': JSON.stringify(['kansas city current', 'sporting kansas city', 'wrexham afc']),
    });
    try {
        if (profiles) {
            admin = await (await browser.newContext({ viewport: { width: 1440, height: 1000 } })).newPage();
            await admin.goto(base + '/login?returnUrl=%2Fadmin%2Fprofiles');
            await admin.locator('#password').fill(password);
            await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
            await admin.waitForURL('**/admin/profiles');
            await admin.waitForLoadState('networkidle');
            const name = 'zz Points ' + Date.now().toString(36);
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

        const expected = async (page) =>
            page.locator('.mx-desktop table.standings-table').evaluateAll((tables) => {
                const rows = [];
                for (const table of tables) {
                    const heads = [...table.querySelectorAll('thead th')].map((n) => n.textContent.trim());
                    const indices = ['W', 'D', 'L'].map((h) => heads.indexOf(h));
                    const points = heads.findIndex((h) => ['P', 'PTS', 'Points'].includes(h));
                    if (points < 0 || indices.some((i) => i < 0)) continue;
                    for (const row of table.querySelectorAll('tbody tr')) {
                        const cells = [...row.children].map((n) => n.textContent.trim());
                        rows.push({
                            name: row.querySelector('.standing-team-name')?.textContent.trim(),
                            points: cells[points],
                            record: indices.map((i, j) => ['W', 'D', 'L'][j] + ' ' + cells[i]).join(' · '),
                        });
                    }
                }
                return rows;
            });
        const verify = async (page) => {
            const rows = await expected(page);
            assert(rows.length, 'published soccer table expected');
            assert.equal(
                await page.locator('.mx-phone .mx-number').innerText(),
                rows[0].points,
                'leader highlights points',
            );
            assert.equal(await page.locator('.mx-phone .mx-block__note').innerText(), rows[0].record, 'leader W-D-L');
            assert.equal(await page.locator('.mx-phone .mx-sport-list .mx-head > span').first().innerText(), 'PTS');
            const actual = await page.locator('.mx-phone .mx-standing-list li').evaluateAll((ns) =>
                ns.map((n) => ({
                    name: n.querySelector(':scope > span > a,:scope > span > b').textContent.trim(),
                    points: n.querySelector(':scope > strong').textContent.trim(),
                    record: n.querySelector('small').textContent.trim(),
                })),
            );
            assert.equal(actual.length, rows.length, 'all published teams retained');
            for (const row of actual)
                assert.deepEqual(
                    row,
                    rows.find((r) => r.name === row.name),
                    'points and W-D-L for ' + row.name,
                );
            const tiles = page.locator('.mx-phone [data-phone-team-only]:not([hidden])');
            for (let i = 0; i < (await tiles.count()); i++) {
                const tile = tiles.nth(i),
                    name = (await tile.locator('.mx-tile__label').innerText()).split('·')[0].trim().toLowerCase();
                const row = rows.find((r) => r.name.toLowerCase() === name);
                assert(row, 'personal team in source table');
                assert.equal(await tile.locator('.mx-tile__value').innerText(), row.points + ' pts');
                assert.equal(await tile.locator('.mx-tile__sub').first().innerText(), row.record);
            }
            assert(
                await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                'no sideways overflow',
            );
            assert.deepEqual(await page.evaluate(audit), [], 'contrast');
            const small = await page
                .locator('.mx-phone a,.mx-phone button,.mx-phone select,.mx-phone summary')
                .evaluateAll((ns) =>
                    ns
                        .filter((n) => n.getBoundingClientRect().width > 0 && n.getBoundingClientRect().height < 43.9)
                        .map((n) => n.textContent.trim()),
                );
            assert.deepEqual(small, [], '44px touch targets');
        };
        let nwsl;
        {
            const c = await contextFor(390, look('kansas', 'light')),
                p = await c.newPage();
            await visit(p, '/standings', look('kansas', 'light'));
            const options = await p
                .locator('#standings-source-m option')
                .evaluateAll((ns) => ns.map((n) => ({ id: n.value, name: n.textContent.trim() })));
            nwsl = options.find((s) => s.name === 'NWSL');
            assert(nwsl, 'NWSL source');
            for (const source of options.filter(
                (s) =>
                    s.name === 'NWSL' ||
                    s.name === 'MLS' ||
                    s.name === 'EFL Championship' ||
                    s.name === 'Premier League',
            )) {
                await visit(p, '/standings?sourceId=' + source.id, look('kansas', 'light'));
                await verify(p);
                console.log('PASS soccer data', source.name);
            }
            await c.close();
        }
        for (const theme of ['light', 'dark'])
            for (const width of [320, 390, 430]) {
                const c = await contextFor(width, look('kansas', theme)),
                    p = await c.newPage();
                p.on('pageerror', (e) => errors.push(e.message));
                await visit(p, '/standings?sourceId=' + nwsl.id, look('kansas', theme));
                await verify(p);
                const folds = p.locator('.mx-fold:not([hidden])');
                if ((await folds.count()) > 1) {
                    await folds.nth(1).locator('summary').click();
                    assert.equal(await folds.evaluateAll((ns) => ns.filter((n) => n.open).length), 1);
                }
                await p.reload({ waitUntil: 'networkidle' });
                await verify(p);
                await p.evaluate(() => scrollTo(0, 0));
                await p.waitForTimeout(100);
                if (shots)
                    await p.screenshot({
                        path: shots + 'soccer-points-' + width + '-' + theme + '.png',
                        fullPage: true,
                    });
                console.log('PASS NWSL points/W-D-L, personal tile, fold and reload', width, theme);
                await c.close();
            }
        {
            const c = await contextFor(390, {}, false),
                p = await c.newPage();
            await p.goto(base + '/standings?sourceId=' + nwsl.id);
            await verify(p);
            console.log('PASS no-JS soccer data');
            await c.close();
        }
        for (const width of [744, 1440]) {
            const c = await contextFor(width),
                p = await c.newPage();
            await p.goto(base + '/standings?sourceId=' + nwsl.id);
            assert(await p.locator('.standings-view').isVisible());
            assert.equal(
                await p.locator('.mx-desktop .standings-table thead th').nth(1).innerText(),
                'GP',
                'desktop column order retained',
            );
            assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
            await c.close();
        }
        assert.deepEqual(errors, []);
        console.log('Soccer points/W-D-L phone summaries and retained desktop table passed.');
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
