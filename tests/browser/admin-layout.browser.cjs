// Admin redesign (October 26, 2026; docs/admin-redesign-handoff.md): every admin page in the new shell at phone, iPad
// and desktop widths in light and dark, with readable text, a working phone menu, menu counts that agree with the status
// page, Sources filters in the address and the editor's save bar. Read-only: it never saves or deletes anything.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { audit } = require('./contrast-audit.cjs');

const base = process.env.BASE_URL || 'http://localhost:5295';
const password = process.env.ADMIN_PASSWORD;
if (!password) {
    console.log('SKIP: signs in to admin; set ADMIN_PASSWORD to run it.');
    process.exit(0);
}

const lists = [
    '/admin',
    '/admin/schedule-coverage',
    '/admin/sources',
    '/admin/categories',
    '/admin/pages',
    '/admin/youtube-feeds',
    '/admin/youtube-videos',
    '/admin/sports-event-types',
    '/admin/sports-events',
    '/admin/results',
    '/admin/tv-series',
];
const extras = [
    '/admin/sources/new',
    '/admin/youtube-feeds/new',
    '/admin/results/new',
    '/admin/tv-series/new',
    '/admin/profiles',
    '/admin/profiles/base',
    '/admin/requests',
];
const widths = (process.env.WIDTHS || '320,390,430,744,820,1024,1440').split(',').map(Number);

(async () => {
    const browser = await chromium.launch();
    try {
        const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
        const p = await context.newPage();
        const errors = [];
        p.on('pageerror', (e) => errors.push(e.message));
        const open = async (path) => {
            await p.goto(base + path);
            await p.waitForLoadState('networkidle');
            await p.waitForTimeout(500);
        };

        // Signed out: the sign-in card, without the admin menu.
        await open('/admin');
        assert(p.url().includes('/login'), 'admin needs sign-in');
        assert.equal(await p.locator('.adm-side').count(), 0, 'no admin menu when signed out');
        assert.deepEqual(await p.evaluate(audit), [], 'sign-in contrast');
        await p.locator('#password').fill(password);
        await p.getByRole('button', { name: 'Sign in', exact: true }).click();
        await p.waitForURL('**/admin');

        // One editor per list, from that list's first edit link.
        const editors = [];
        for (const path of lists.slice(2)) {
            await open(path);
            const href = await p
                .locator('a[href*="/edit"]')
                .first()
                .getAttribute('href')
                .catch(() => null);
            if (href) editors.push('/' + href.replace(/^\//, ''));
        }
        const pages = [...lists, ...extras, ...editors];

        for (const theme of ['light', 'dark'])
            for (const width of widths) {
                await p.setViewportSize({ width, height: 900 });
                await p.evaluate((t) => localStorage.setItem('theme', t), theme);
                for (const path of pages) {
                    await open(path);
                    const label = `${theme} ${width}px ${path}`;
                    const r = await p.evaluate(() => ({
                        over: document.documentElement.scrollWidth - innerWidth,
                        h1: !!document.querySelector('.adm-head h1'),
                        side: getComputedStyle(document.querySelector('.adm-side')).display !== 'none',
                    }));
                    assert.equal(r.over, 0, label + ' scrolls sideways');
                    assert(r.h1, label + ' page heading');
                    assert.equal(r.side, width > 900, label + ' sidebar only on wide screens');
                    // Text contrast once per theme, at one phone and one desktop width.
                    if (width === 390 || width === 1440) {
                        const bad = await p.evaluate(audit);
                        assert.deepEqual(bad, [], label + ' contrast');
                    }
                }
                assert.deepEqual(errors, [], `${theme} ${width}px script errors`);
            }

        // Phone menu: the CSS-only toggle shows and hides the sections; the current page is highlighted.
        await p.setViewportSize({ width: 390, height: 900 });
        await open('/admin/sources');
        await p.locator('label[for="adm-menu-toggle"]').click();
        assert(await p.locator('.adm-side').isVisible(), 'menu opens');
        assert.equal(await p.locator('.adm-nav__link.active').innerText(), 'Sources');
        await p.locator('label[for="adm-menu-toggle"]').click();
        assert(await p.locator('.adm-side').isHidden(), 'menu closes');

        // Menu counts agree with the status page: problems (enabled) plus one when the scraper is stale.
        await p.setViewportSize({ width: 1440, height: 1000 });
        await p.evaluate(() => localStorage.setItem('theme', 'light'));
        await open('/admin');
        const navTotal = Number(
            (await p
                .locator('.adm-nav__link[href="admin"] .adm-nav__count')
                .innerText()
                .catch(() => '0')) || 0,
        );
        const problems = await p.locator('#attention .adm-issue .adm-pill--bad').count();
        const stale = await p.locator('.adm-health__icon--bad').count();
        assert.equal(navTotal, problems + stale, 'menu count matches the status page');

        // Sources: filters go into the address, so Back from an editor keeps them.
        await open('/admin/sources');
        await p.locator('#source-search').fill('news');
        await p.locator('#source-search').press('Enter');
        await p.waitForURL(/q=news/);
        await p.getByRole('button', { name: /^Enabled/ }).click();
        await p.waitForURL(/status=enabled/);
        assert(/q=news/.test(p.url()), 'search kept with the status filter');
        const first = p.locator('.adm-table a.adm-row-title').first();
        if (await first.count()) {
            await first.click();
            await p.waitForURL(/\/edit$/);
            await p.waitForLoadState('networkidle');
            // Editor: Save stays off until something changes, and the save bar names the change.
            const save = p.getByRole('button', { name: 'Save changes', exact: true });
            assert(await save.isDisabled(), 'save disabled while clean');
            await p.locator('#label').fill('zz changed');
            await p.locator('#label').blur();
            await p.waitForTimeout(300);
            assert((await p.locator('.adm-savebar__state').innerText()).includes('name'), 'save bar names the change');
            assert(await save.isEnabled(), 'save enabled once dirty');
            await p.goBack();
            await p.waitForURL(/status=enabled/);
            assert(/q=news/.test(p.url()), 'Back keeps the filters');
        }
        assert.deepEqual(errors, []);
        console.log(
            `Admin layout: ${pages.length} pages at ${widths.join('/')}px light/dark, sign-in, phone menu, contrast, menu counts, Sources filters and editor save bar passed.`,
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e.message || e);
    process.exit(1);
});
