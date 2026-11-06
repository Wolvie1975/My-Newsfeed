// Run against a local app: NODE_PATH=<playwright node_modules> node tests/browser/theme-mode.browser.cjs
// Optional NEWSFEED_TEST_URL overrides the local preview URL.
const { chromium } = require('playwright');
const lib = require('./lib.cjs');
const assert = require('node:assert/strict');
(async () => {
    const browser = await chromium.launch({ headless: true });
    try {
        const context = await lib.readerContext(browser, { timezoneId: 'Asia/Tokyo', colorScheme: 'dark' });
        const page = await context.newPage();
        const errors = [];
        page.on('pageerror', (error) => errors.push(error.message));
        await page.clock.install({ time: new Date('2026-06-21T17:00:00Z') });
        const base = lib.base;
        // The Appearance panel is a fold on Settings.
        const appearance = async (p) => {
            const panel = p.locator('details[data-settings-section="appearance"]');
            if (!(await panel.evaluate((n) => n.open))) {
                await panel.locator('summary').first().click();
                await p.clock.runFor(1000); // the fold's opening animation runs on the fake clock
            }
            assert(await panel.evaluate((n) => n.open), 'Appearance panel open');
        };
        await page.goto(base + '/settings');
        await appearance(page);
        // With profiles on the theme is saved to the profile 400 ms after a change; the fake clock has to run that timer.
        const profile = await page.evaluate(() => !!window.myNewspaperPrefs?.profile);
        const save = (p, action) =>
            profile
                ? lib.whileSaving(p, async () => {
                      await action();
                      await p.clock.runFor(500);
                  })
                : action();
        // Radios are picked with force: Playwright's stability wait needs animation frames, which the fake clock holds
        // once its time has been moved. The theme each pick produces is asserted after it.
        const mode = (value) => page.locator('input[data-theme-mode][value="' + value + '"]');
        const theme = () => page.locator('html').getAttribute('data-theme');
        assert.equal(await theme(), 'dark');
        assert.equal(await mode('system').isChecked(), true);
        await save(page, () => mode('sun').check({ force: true }));
        assert.equal(await theme(), 'light');
        assert.match(
            await page.locator('[data-theme-status]').textContent(),
            /Kansas City today: sunrise .*CDT · sunset .*CDT/,
        );
        assert.equal(await lib.getPreference(page, 'theme'), 'sun');
        await page.reload();
        await appearance(page);
        assert.equal(await theme(), 'light');
        assert.equal(await mode('sun').isChecked(), true);
        const sunset = await page.evaluate(() =>
            window.myNewspaperSolar.times(new Date(), window.myNewspaperSolar.location).sunset.toISOString(),
        );
        await page.clock.setSystemTime(new Date(new Date(sunset).getTime() + 1000));
        await page.clock.runFor(30000);
        assert.equal(await theme(), 'dark');
        await save(page, () => page.locator('.header-actions > [data-theme-toggle]').click());
        assert.equal(await theme(), 'light');
        assert.equal(await mode('light').isChecked(), true);
        await page.clock.runFor(60000);
        assert.equal(await theme(), 'light');
        await save(page, () => mode('sun').check({ force: true }));
        assert.equal(await theme(), 'dark');
        await page.locator('.site-nav--inline a[href="news"]').click();
        await page.waitForURL('**/news');
        assert.equal(await theme(), 'dark');
        await page.locator('.settings-link').click();
        await page.waitForURL('**/settings');
        await appearance(page);
        assert.equal(await mode('sun').isChecked(), true);
        await page.clock.setSystemTime(new Date('2026-06-22T17:00:00Z'));
        await page.evaluate(() => window.dispatchEvent(new Event('focus')));
        assert.equal(await theme(), 'light');
        await page.emulateMedia({ colorScheme: 'light' });
        await appearance(page);
        await save(page, () => mode('system').check({ force: true }));
        assert.equal(await theme(), 'light');
        await page.emulateMedia({ colorScheme: 'dark' });
        await page.waitForFunction(() => document.documentElement.dataset.theme === 'dark');
        await save(page, () => mode('sun').check({ force: true }));
        const other = await context.newPage();
        await other.clock.install({ time: new Date('2026-06-22T17:00:00Z') });
        await other.goto(base + '/settings');
        await appearance(other);
        await save(other, () => other.locator('input[data-theme-mode][value="dark"]').check({ force: true }));
        // Another tab's change: live through the storage event in localStorage; from the profile on the next load.
        if (profile) {
            await page.reload();
            await appearance(page);
        }
        await page.waitForFunction(() => document.querySelector('input[data-theme-mode][value="dark"]').checked);
        assert.equal(await theme(), 'dark');
        await page.setViewportSize({ width: 390, height: 844 });
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
        assert.deepEqual(errors, []);
        const blocked = await lib.readerContext(browser);
        await blocked.addInitScript(() => {
            Storage.prototype.getItem = Storage.prototype.setItem = () => {
                throw new Error('Blocked storage');
            };
        });
        const blockedPage = await blocked.newPage();
        await blockedPage.clock.install({ time: new Date('2026-06-22T17:00:00Z') });
        await blockedPage.goto(base + '/settings');
        await appearance(blockedPage);
        await blockedPage.locator('input[data-theme-mode][value="sun"]').check({ force: true });
        assert.equal(await blockedPage.locator('html').getAttribute('data-theme'), 'light');
        await blocked.close();
        await context.close();
        console.log(
            'PASS: automatic boundaries, persistence, manual override, navigation, focus, device mode, cross-tab, mobile and blocked storage',
        );
    } finally {
        await browser.close();
    }
})().catch((error) => {
    console.error(error);
    process.exitCode = 1;
});
