// Admin · Profiles, Manage profile and Base edition (profiles handoff stage A1) at phone, iPad and desktop widths in light
// and dark: no sideways scroll, readable text, 44px controls, add-profile and Reset PIN checks, the base edition's save
// bar, and Delete. It adds one throwaway profile and deletes it again; it never saves household settings or the base
// edition, so it's safe against a shared database.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { audit } = require('./contrast-audit.cjs');

const base = process.env.ADMIN_BASE_URL || process.env.BASE_URL || 'http://localhost:5297';
const password = process.env.ADMIN_PASSWORD;
const shots = process.env.SCREENSHOT_DIR;
if (!password) {
    console.log('SKIP: signs in to admin; set ADMIN_PASSWORD to run it.');
    process.exit(0);
}
const widths = (process.env.WIDTHS || '320,390,430,744,820,1024,1440').split(',').map(Number);
const name = 'zz browser ' + Date.now().toString(36);

(async () => {
    const browser = await chromium.launch();
    let manage = null;
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    const p = await context.newPage();
    const errors = [];
    p.on('pageerror', (e) => errors.push(e.message));
    const open = async (path) => {
        await p.goto(base + path);
        await p.waitForLoadState('networkidle');
        await p.waitForTimeout(400);
    };
    try {
        await open('/login?returnUrl=%2Fadmin%2Fprofiles');
        await p.locator('#password').fill(password);
        await p.getByRole('button', { name: 'Sign in', exact: true }).click();
        await p.waitForURL('**/admin/profiles');
        await p.waitForLoadState('networkidle');
        assert.equal(
            await p
                .locator('.adm-nav__link.active')
                .innerText()
                .then((t) => t.trim()),
            'Profiles',
            'menu entry',
        );

        // Add a profile: mismatched PINs are refused, then it's added with the chosen color.
        await p.getByRole('button', { name: 'Add profile', exact: true }).first().click();
        await p.locator('#add-name').fill(name);
        await p.locator('.adm-swatch', { hasText: 'Teal' }).click();
        await p.locator('#add-pin').fill('1357');
        await p.locator('#add-confirm').fill('1358');
        await p.locator('form[aria-labelledby="add-title"] button[type="submit"]').click();
        await p.getByRole('alert').filter({ hasText: 'don’t match' }).waitFor();
        await p.locator('#add-pin').fill('12a4');
        assert.equal(await p.locator('#add-pin').inputValue(), '124', 'PIN fields keep digits only');
        await p.locator('#add-pin').fill('1357');
        await p.locator('#add-confirm').fill('1357');
        await p.locator('form[aria-labelledby="add-title"] button[type="submit"]').click();
        await p.getByText(`Added ${name}.`).waitFor();
        const link = p.locator('.adm-table a.adm-row-title', { hasText: name });
        manage = '/' + (await link.getAttribute('href')).replace(/^\//, '');
        const row = p.locator('.adm-table tr', { has: p.locator('a.adm-row-title', { hasText: name }) });
        assert.equal((await row.locator('.adm-pill').innerText()).trim(), 'Active');
        assert.equal((await row.locator('td').nth(2).innerText()).trim(), 'Set', 'PIN set');
        assert.equal(
            await row.locator('.adm-avatar').evaluate((n) => getComputedStyle(n).backgroundColor),
            'rgb(15, 118, 110)',
            'chosen color',
        );

        // Manage profile: Reset PIN checks both entries; Save stays off until something changes.
        await open(manage);
        assert.equal((await p.locator('.adm-head h1').innerText()).trim(), name);
        assert(await p.getByRole('button', { name: 'Save changes' }).isDisabled(), 'save disabled while clean');
        await p.locator('.adm-swatch', { hasText: 'Violet' }).click();
        await p.waitForTimeout(300);
        assert(
            (await p.locator('.adm-savebar__state').first().innerText()).includes('color'),
            'save bar names the change',
        );
        await open(manage);
        await p.locator('#reset-pin').fill('2468');
        await p.locator('#reset-confirm').fill('2469');
        await p.getByRole('button', { name: 'Set new PIN' }).click();
        await p.getByRole('alert').filter({ hasText: 'don’t match' }).waitFor();
        await p.locator('#reset-confirm').fill('2468');
        await p.getByRole('button', { name: 'Set new PIN' }).click();
        await p.getByText('PIN is reset').waitFor();
        const body = await p.locator('main').innerText();
        for (const hidden of ['Open as', 'Copy settings', 'Remove PIN', 'Last read', 'Saved items'])
            assert(!body.includes(hidden), `Manage profile never shows "${hidden}"`);

        // Base edition: a change shows in the save bar and can be taken back; nothing is saved.
        await open('/admin/profiles/base');
        const save = p.getByRole('button', { name: 'Save base edition' });
        assert(await save.isDisabled(), 'base edition clean on load');
        const sw = p.locator('[aria-labelledby="home-opinion"]');
        await sw.click();
        await p.waitForTimeout(300);
        assert(await save.isEnabled(), 'base edition dirty after a change');
        await sw.click();
        await p.waitForTimeout(300);
        assert(await save.isDisabled(), 'base edition clean again');
        // Services list only what's chosen plus search matches; skipped where the catalog has no key.
        const find = p.locator('#service-find');
        if (await find.count()) {
            const before = await p.locator('.adm-field').filter({ has: find }).locator('.adm-choice').count();
            await find.fill('net');
            await p.waitForTimeout(400);
            const after = await p.locator('.adm-field').filter({ has: find }).locator('.adm-choice').count();
            assert(after > before && after <= before + 24, `service search lists matches (${before} → ${after})`);
            await find.fill('');
            await p.waitForTimeout(400);
        } else console.log('Note: no service catalog here; service search not checked.');

        const pages = ['/admin/profiles', manage, '/admin/profiles/base'];
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
                        // Visible new controls smaller than 44px in either direction.
                        small: [
                            ...document.querySelectorAll(
                                '.adm-order .btn, .adm-swatch, .adm-choice, .adm-group > summary, .adm-switch, .adm-menu > summary',
                            ),
                        ]
                            .filter((n) => n.offsetParent !== null)
                            .map((n) => {
                                const b = n.getBoundingClientRect();
                                // The switch's tap area is its ::before, taller than the track it draws.
                                const tap = n.matches('.adm-switch')
                                    ? parseFloat(getComputedStyle(n, '::before').height)
                                    : b.height;
                                return { width: b.width, height: Math.max(b.height, tap || 0) };
                            })
                            .filter((b) => b.height < 44 || b.width < 44).length,
                    }));
                    assert.equal(r.over, 0, label + ' scrolls sideways');
                    assert(r.h1, label + ' page heading');
                    assert.equal(r.small, 0, label + ' touch targets under 44px');
                    if (width === 390 || width === 1440) {
                        assert.deepEqual(await p.evaluate(audit), [], label + ' contrast');
                        if (shots) {
                            const file = path.replace(/^\/admin\//, '').replace(/[^a-z0-9]+/g, '-');
                            await p.screenshot({ path: `${shots}admin-${file}-${width}-${theme}.png`, fullPage: true });
                        }
                    }
                }
                assert.deepEqual(errors, [], `${theme} ${width}px script errors`);
            }

        // Phone: the list is cards, and the ⋯ menu opens.
        await p.setViewportSize({ width: 390, height: 900 });
        await open('/admin/profiles');
        const card = p.locator('.adm-cards .adm-cardrow', { hasText: name });
        assert(await card.isVisible(), 'phone card');
        await card.locator('.adm-menu > summary').click();
        assert(await card.getByRole('button', { name: 'Sign out all devices' }).isVisible(), 'phone ⋯ menu');

        // Delete from Manage profile, through the confirm panel.
        await p.setViewportSize({ width: 1440, height: 1000 });
        await open(manage);
        await p.getByRole('button', { name: 'Delete profile…' }).click();
        await p.getByRole('button', { name: 'Delete profile', exact: true }).click();
        await p.waitForURL('**/admin/profiles?deleted=*');
        await p.getByText(`Deleted ${name}.`).waitFor();
        manage = null;
        assert.equal(await p.locator('.adm-table a.adm-row-title', { hasText: name }).count(), 0, 'gone from the list');
        assert.deepEqual(errors, []);
        console.log(
            `Admin profiles: list, Manage profile and Base edition at ${widths.join('/')}px light/dark, add, PIN checks, save bars, phone menu and delete passed.`,
        );
    } finally {
        // Leave nothing behind if a check failed halfway.
        if (manage) {
            try {
                await open(manage);
                await p.getByRole('button', { name: 'Delete profile…' }).click();
                await p.getByRole('button', { name: 'Delete profile', exact: true }).click();
                await p.waitForURL('**/admin/profiles?deleted=*');
                console.error(`Cleaned up ${name}.`);
            } catch (e) {
                console.error(`Could not delete ${name}; remove it in Admin · Profiles. ${e.message}`);
            }
        }
        await browser.close();
    }
})().catch((e) => {
    console.error(e.message || e);
    process.exit(1);
});
