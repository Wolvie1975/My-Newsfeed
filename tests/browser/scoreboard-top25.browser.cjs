const assert = require('node:assert/strict');
const { chromium } = require('playwright');
const base = process.env.BASE_URL || 'http://localhost:5295';
const adminBase = process.env.ADMIN_BASE_URL || base;
const password = process.env.ADMIN_PASSWORD;

(async () => {
    const profiles = (await fetch(base + '/editions', { headers: { Accept: 'text/html' } })).status !== 404;
    if (profiles && !password) {
        console.log('SKIP: profiles are on here; set ADMIN_PASSWORD so the check can add a throwaway profile.');
        return;
    }

    const browser = await chromium.launch();
    let admin = null;
    let id = null;
    let storageState = process.env.STORAGE_STATE;

    try {
        if (profiles && !storageState) {
            admin = await (await browser.newContext({ viewport: { width: 1440, height: 1000 } })).newPage();
            await admin.goto(adminBase + '/login?returnUrl=%2Fadmin%2Fprofiles');
            await admin.locator('#password').fill(password);
            await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
            await admin.waitForURL('**/admin/profiles');
            await admin.waitForLoadState('networkidle');

            const name = 'zz Top25 ' + Date.now().toString(36);
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
            await p.goto(base + '/scoreboard');
            await p.locator(`form[action="profiles/${id}/open"]`).getByRole('button').click();
            await p.keyboard.type('3691');
            await p.waitForURL('**/scoreboard');
            storageState = await context.storageState();
            await context.close();
        }

        const errors = [];

        // Desktop test
        {
            const context = await browser.newContext({
                viewport: { width: 1280, height: 900 },
                storageState,
            });
            const page = await context.newPage();
            page.on('pageerror', (e) => errors.push(e.message));

            await page.goto(base + '/scoreboard', { waitUntil: 'networkidle' });
            const showSelect = page.locator('#scoreboard-show');
            const top25Option = showSelect.locator('option[value="top25"]');
            assert.equal(await top25Option.count(), 1, 'Top 25 option should exist in desktop dropdown');
            assert.equal((await top25Option.textContent()).trim(), 'Top 25');

            // Select Top 25
            await page.goto(base + '/scoreboard?show=top25', { waitUntil: 'networkidle' });
            assert.equal(await showSelect.inputValue(), 'top25');

            // The upcoming ranked games this week section should be rendered
            const top25Week = page.locator('.sb-top25-week');
            assert.equal(await top25Week.count(), 1, 'sb-top25-week section should be present');
            assert.match(await top25Week.locator('.sb-top25-week__title').textContent(), /Upcoming ranked games this week/i);

            // Check if any rank badge is rendered
            const ranks = page.locator('.sb-rank');
            const rankCount = await ranks.count();
            if (rankCount > 0) {
                const firstRank = await ranks.first().textContent();
                assert.match(firstRank, /^#[1-9][0-9]?$/, 'Rank badge should format as #1-25');
            }
            await context.close();
        }

        // Mobile test
        {
            const context = await browser.newContext({
                viewport: { width: 390, height: 844 },
                isMobile: true,
                storageState,
            });
            const page = await context.newPage();
            page.on('pageerror', (e) => errors.push(e.message));

            await page.goto(base + '/scoreboard?show=top25', { waitUntil: 'networkidle' });
            const mobileSelect = page.locator('#scoreboard-show-m');
            assert.equal(await mobileSelect.count(), 1, 'Mobile sport dropdown should exist');
            assert.equal(await mobileSelect.inputValue(), 'top25');

            await context.close();
        }

        assert.deepEqual(errors, []);
        console.log('Scoreboard Top 25 browser test passed.');
    } finally {
        if (id && admin) {
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
