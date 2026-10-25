const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
const samples = [
    ['baseball/mlb/401930515', ['1st inning', '9th inning']],
    ['football/nfl/401872988', ['Q1', 'Q4', 'OT']],
    ['football/college-football/401856832', ['Q1', 'Q4']],
    ['soccer/usa.1/761889', ['1st half', '2nd half']],
    ['soccer/usa.nwsl/401854026', ['1st half', '2nd half']],
    ['soccer/eng.2/401880218', ['1st half', '2nd half']],
    ['hockey/nhl/401892525', ['1st period', '3rd period', 'OT', 'SO']],
];
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage();
        const errors = [];
        page.on('pageerror', (e) => errors.push(e.message));
        for (const [path, labels] of samples) {
            const response = await page.goto(base + '/box-score/' + path);
            assert.equal(response.status(), 200, path);
            const text = await page.locator('[data-box-content]').textContent();
            for (const label of labels) assert(text.includes(label), path + ' ' + label);
        }
        for (const theme of ['light', 'dark'])
            for (const width of [390, 1440]) {
                await page.setViewportSize({ width, height: 900 });
                await page.evaluate((t) => localStorage.setItem('theme', t), theme);
                await page.goto(base + '/box-score/hockey/nhl/401892525');
                await page.locator('summary').filter({ hasText: 'Scoring by period' }).click();
                assert(await page.getByRole('rowheader', { name: 'SO', exact: true }).count());
                assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
            }
        assert.equal((await page.request.get(base + '/box-score/hockey/nhl/999999999')).status(), 404);
        assert.deepEqual(errors, []);
        console.log(
            'Seven repaired period-label routes and four NHL width/theme layouts passed, including OT/SO and unavailable fallback.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
