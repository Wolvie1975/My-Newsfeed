// TV series Delete (October 27, 2026): the row ⋯ menu, its confirm panel (Stop following instead, Cancel, Delete) at
// phone and desktop widths in light and dark. It stops and deletes only TV_FIXTURE_TITLE, a throwaway followed series
// added beforehand with an unused TVmaze ID; real series are only opened and cancelled, never changed.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { audit } = require('./contrast-audit.cjs');

const base = process.env.ADMIN_BASE_URL || process.env.BASE_URL || 'http://localhost:5297';
const password = process.env.ADMIN_PASSWORD;
const title = process.env.TV_FIXTURE_TITLE;
if (!password || !title) {
    console.log('SKIP: set ADMIN_PASSWORD and TV_FIXTURE_TITLE (a throwaway followed series) to run it.');
    process.exit(0);
}
const widths = (process.env.WIDTHS || '320,390,430,744,1024,1440').split(',').map(Number);
const shots = process.env.SHOTS;

(async () => {
    const browser = await chromium.launch();
    try {
        const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
        const p = await context.newPage();
        const errors = [];
        p.on('pageerror', (e) => errors.push(e.message));
        const open = async () => {
            await p.goto(base + '/admin/tv-series');
            await p.waitForLoadState('networkidle');
            await p.waitForTimeout(500);
        };
        const row = () => p.locator('tr', { hasText: title });
        const menu = () => row().locator('details.adm-menu');

        await p.goto(base + '/admin');
        await p.locator('#password').fill(password);
        await p.getByRole('button', { name: 'Sign in', exact: true }).click();
        await p.waitForURL('**/admin');

        for (const theme of ['light', 'dark']) {
            for (const width of widths) {
                const label = `${theme} ${width}px`;
                await p.setViewportSize({ width, height: 900 });
                await p.evaluate((t) => localStorage.setItem('theme', t), theme);
                await open();
                assert.equal(await row().count(), 1, `${label}: fixture row listed`);
                await menu().locator('summary').scrollIntoViewIfNeeded();
                const box = await menu().locator('summary').boundingBox();
                assert(
                    box && box.width >= 44 && box.height >= 44,
                    `${label}: ⋯ target is 44px (${box?.width}×${box?.height})`,
                );
                await menu().locator('summary').click();
                const item = row().getByRole('button', { name: 'Delete…' });
                assert((await item.boundingBox())?.height >= 44, `${label}: Delete… item is 44px tall`);
                await item.click();
                const confirm = p.locator('.adm-confirm');
                await confirm.waitFor();
                assert.match(await confirm.locator('h2').innerText(), new RegExp(`Delete ${title}`));
                for (const name of ['Stop following instead', 'Delete series', 'Cancel']) {
                    const b = await confirm.getByRole('button', { name }).boundingBox();
                    assert(b && b.height >= 44, `${label}: ${name} is 44px tall`);
                }
                assert.equal(
                    await p.evaluate(() => document.documentElement.scrollWidth - innerWidth),
                    0,
                    `${label}: no sideways scroll`,
                );
                if (width === 390 || width === 1440)
                    assert.deepEqual(await p.evaluate(audit), [], `${label}: contrast`);
                if (shots) await p.screenshot({ path: `${shots}/tv-delete-${width}-${theme}.png`, fullPage: false });
                await confirm.getByRole('button', { name: 'Cancel' }).click();
                await confirm.waitFor({ state: 'detached', timeout: 5000 });
                assert.equal(await row().count(), 1, `${label}: Cancel keeps the series`);
                assert.deepEqual(errors, [], `${label}: script errors`);
            }
        }

        // Stop following instead: the switch goes off and the series stays listed.
        await p.setViewportSize({ width: 390, height: 900 });
        await open();
        await menu().locator('summary').click();
        await row().getByRole('button', { name: 'Delete…' }).click();
        await p.getByRole('button', { name: 'Stop following instead' }).click();
        await p.getByText(`Stopped following ${title}. Its history was kept.`).waitFor();
        assert.equal(await row().getByRole('switch').getAttribute('aria-checked'), 'false', 'stopped');
        assert.equal(await p.locator('.adm-confirm').count(), 0, 'panel closed after stopping');

        // A stopped series offers only Delete; deleting removes the row.
        await menu().locator('summary').click();
        await row().getByRole('button', { name: 'Delete…' }).click();
        assert.equal(
            await p.getByRole('button', { name: 'Stop following instead' }).count(),
            0,
            'no Stop for a stopped series',
        );
        await p.getByRole('button', { name: 'Delete series' }).click();
        await p.getByText(`Deleted ${title}.`).waitFor();
        assert.equal(await row().count(), 0, 'row removed');
        await open();
        assert.equal(await row().count(), 0, 'still gone after reload');
        assert.deepEqual(errors, [], 'script errors');
        console.log(`PASS: TV series delete at ${widths.join('/')}px light and dark`);
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
