// Settings saved to the profile (profiles handoff stage U2), with profiles switched on: a browser's old settings are
// imported once when a profile first opens there and then removed, the profile's theme and color scheme are on <html>
// before first paint, a change on one device shows on another, a second profile on the same device starts clean, and
// only the household admin's profile is offered "Admin sign in". Settings at phone, iPad and desktop widths in light
// and dark: no sideways scroll, readable text.
//
// It needs Profiles:Enabled=true and the admin password: it adds two throwaway profiles, marks one as the household
// admin, and deletes both at the end, even if a check fails; then it marks the profile that was the admin before.
const { chromium } = require('playwright');
const lib = require('./lib.cjs');
const assert = require('node:assert/strict');
const { audit } = require('./contrast-audit.cjs');

const base = lib.base;
const adminBase = lib.adminBase;
const password = lib.adminPassword;
const shots = lib.shotDir;
if (!password) {
    console.log('SKIP: adds profiles in admin; set ADMIN_PASSWORD to run it.');
    process.exit(0);
}
const widths = lib.widths('320,390,430,744,820,1024,1440');
const people = [
    { label: 'Cy', pin: '1470' },
    { label: 'Di', pin: '2580' },
];
const legacy = {
    theme: 'dark',
    'mynewsfeed-color-scheme': 'kansas',
    'mynewsfeed-personal-v1': JSON.stringify({ hidden: ['opinion'], teamsOnly: false }),
    'mynewsfeed-sports-teams-v1': JSON.stringify(['kansas']),
    'mynewsfeed-watch-services-v1': JSON.stringify([8]),
    'mynewsfeed-settings-order-v1': JSON.stringify(['teams', 'services', 'colors', 'appearance']),
    'mydailyfeed-scoreboard-teams-v1': JSON.stringify(['kansas']),
};

(async () => {
    if ((await fetch(base + '/editions', { headers: { Accept: 'text/html' } })).status === 404) {
        console.log('SKIP: profiles are switched off on this site (Profiles:Enabled=false).');
        return;
    }
    const browser = await chromium.launch();
    const admin = await lib.signInAdmin(browser);
    const errors = [];
    const settle = async (page) => {
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(400);
    };
    const added = [];
    let previousAdmin = null;
    try {
        await settle(admin);
        for (const person of people) added.push(await lib.addProfile(admin, person.label, person));
        const [cy, di] = added;
        // Note which profile is the household admin now, so it can be put back at the end.
        for (const href of await admin
            .locator('.adm-table a.adm-row-title')
            .evaluateAll((links) => links.map((l) => l.getAttribute('href')))) {
            await admin.goto(adminBase + '/' + href.replace(/^\//, ''));
            await settle(admin);
            if ((await admin.locator('[aria-labelledby="admin-l"]').getAttribute('aria-checked')) === 'true') {
                previousAdmin = href;
                break;
            }
        }
        // Cy is the household admin.
        await admin.goto(adminBase + '/admin/profiles/' + cy.id);
        await settle(admin);
        await admin.locator('[aria-labelledby="admin-l"]').click();
        await admin.getByText('now show the Admin sign in link').waitFor();

        const device = async () => {
            const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
            const page = await context.newPage();
            page.on('pageerror', (e) => errors.push(e.message));
            return { context, page };
        };
        const open = async (page, person, path = '/settings') => {
            await page.goto(base + path);
            await settle(page);
            await page.locator(`form[action="profiles/${person.id}/open"]`).getByRole('button').click();
            await page.keyboard.type(person.pin);
            await page.waitForURL((u) => !u.pathname.startsWith('/editions'));
            await settle(page);
        };
        const me = (page) => page.evaluate(async () => (await (await fetch('/api/me')).json()).settings);

        // Device 1 has old browser settings. Opening Cy imports them once, then they're gone from the browser.
        const one = await device();
        await one.page.goto(base + '/editions');
        await one.page.evaluate((values) => {
            Object.entries(values).forEach(([k, v]) => localStorage.setItem(k, v));
        }, legacy);
        await open(one.page, cy);
        await one.page.waitForFunction(
            () => document.documentElement.getAttribute('data-theme-mode') === 'dark',
            null,
            {
                timeout: 10000,
            },
        );
        const imported = await me(one.page);
        assert.equal(imported.theme, 'dark', 'imported theme');
        assert.equal(imported.colorScheme, 'kansas', 'imported scheme');
        assert.deepEqual(imported.teams, ['kansas'], 'imported teams');
        assert.deepEqual(imported.services, [8], 'imported services');
        assert.deepEqual(imported.home.hidden, ['opinion'], 'imported home sections');
        assert.deepEqual(imported.scoreboardTeams, ['kansas'], 'imported scoreboard stars');
        const left = await one.page.evaluate(
            (keys) => keys.filter((k) => localStorage.getItem(k) !== null),
            Object.keys(legacy),
        );
        assert.deepEqual(left, [], 'old keys removed after the import');
        assert.equal(
            await one.page.evaluate(() => document.documentElement.getAttribute('data-color-scheme')),
            'kansas',
        );
        assert.equal(
            await one.page.locator('.adm-link', { hasText: 'Admin sign in' }).count(),
            1,
            'admin profile gets the link',
        );

        // The theme is in the HTML the server sends, so there's no flash before scripts run.
        const html = await one.page.evaluate(async () =>
            (await fetch('/settings', { headers: { Accept: 'text/html' } })).text(),
        );
        assert.match(
            html,
            /<html[^>]*data-theme-mode="dark"[^>]*data-color-scheme="kansas"/,
            'theme on <html> from the server',
        );
        assert(!/<\/script><b>/.test(html), 'page data is escaped');

        // Device 2: Cy's settings follow; a change there saves to the profile and shows back on device 1.
        const two = await device();
        await open(two.page, cy);
        assert.equal(await two.page.evaluate(() => document.documentElement.getAttribute('data-theme-mode')), 'dark');
        assert(
            await two.page.locator('input[data-sports-choice][value="kansas"]').first().isChecked(),
            'teams on device 2',
        );
        // The imported home sections apply on the home page: Opinion is hidden for Cy, on a device that never had it.
        await two.page.goto(base + '/');
        await settle(two.page);
        // Home renders the desktop and the phone front; the setting applies to every copy of a section.
        const hidden = async (key) => {
            const flags = await two.page
                .locator(`[data-home-section="${key}"]`)
                .evaluateAll((ns) => ns.map((n) => n.hidden));
            assert(flags.length > 0, `home section ${key} present`);
            return flags;
        };
        assert(
            (await hidden('opinion')).every((h) => h),
            'imported hidden section',
        );
        assert(
            (await hidden('news')).every((h) => !h),
            'news stays shown',
        );
        await two.page.goto(base + '/settings#appearance-heading');
        await settle(two.page);
        await two.page.locator('input[data-theme-mode][value="light"]').check();
        await two.page.waitForTimeout(1200);
        assert.equal((await me(two.page)).theme, 'light', 'theme change saved to the profile');
        await one.page.reload();
        await settle(one.page);
        assert.equal(
            await one.page.evaluate(() => document.documentElement.getAttribute('data-theme')),
            'light',
            'device 1 follows',
        );

        // A second profile on device 1 starts clean: the old keys went to Cy, not to whoever opens next.
        await open(one.page, di, '/editions');
        await one.page.goto(base + '/settings');
        await settle(one.page);
        const diSettings = await me(one.page);
        assert.equal(diSettings.teams, undefined, 'second profile has no imported teams');
        assert.equal(await one.page.evaluate(() => document.documentElement.getAttribute('data-theme-mode')), 'system');
        assert.equal(
            await one.page.locator('.adm-link', { hasText: 'Admin sign in' }).count(),
            0,
            'no link for others',
        );
        assert.equal(await one.page.getByRole('link', { name: 'Admin sign in' }).count(), 0, 'not in the menu either');
        assert(
            (await one.page.locator('[data-watch-services-status]').innerText()).includes(`${di.name}’s edition`),
            'Settings says where choices save',
        );

        // Settings at every width and theme, for Di.
        const p = one.page;
        for (const theme of lib.THEMES)
            for (const width of widths) {
                await p.setViewportSize({ width, height: 900 });
                if (!(await p.locator('[data-settings-section="appearance"]').evaluate((n) => n.open)))
                    await p.locator('[data-settings-section="appearance"] > summary').click();
                await p.locator('input[data-theme-mode][value="' + theme + '"]').check();
                await p.waitForTimeout(700);
                await p.reload();
                await settle(p);
                const label = `${theme} ${width}px /settings`;
                assert.equal(await p.evaluate(() => document.documentElement.getAttribute('data-theme')), theme, label);
                assert.equal(
                    await p.evaluate(() => document.documentElement.scrollWidth - innerWidth),
                    0,
                    label + ' scrolls sideways',
                );
                if (width === 390 || width === 1440) {
                    assert.deepEqual(await p.evaluate(audit), [], label + ' contrast');
                    if (shots) await p.screenshot({ path: `${shots}settings-${width}-${theme}.png`, fullPage: true });
                }
            }
        assert.equal((await me(p)).theme, 'dark', 'last choice saved');
        assert.deepEqual(errors, []);
        await one.context.close();
        await two.context.close();
        console.log(
            `Profile settings at ${widths.join('/')}px light/dark: import once, theme on <html>, two devices, clean second profile and the admin link passed.`,
        );
    } finally {
        await lib.deleteProfiles();
        // Deleting Cy cleared the household admin; mark the one that was marked before.
        if (previousAdmin) {
            try {
                await admin.goto(adminBase + '/' + previousAdmin.replace(/^\//, ''));
                await settle(admin);
                if ((await admin.locator('[aria-labelledby="admin-l"]').getAttribute('aria-checked')) !== 'true') {
                    await admin.locator('[aria-labelledby="admin-l"]').click();
                    await admin.getByText('now show the Admin sign in link').waitFor();
                }
            } catch (e) {
                console.error(`Could not mark the household admin again; do it on Manage profile. ${e.message}`);
            }
        }
        await browser.close();
    }
})().catch((e) => {
    console.error(e.message || e);
    process.exit(1);
});
