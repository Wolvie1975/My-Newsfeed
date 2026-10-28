// The edition picker and the gate (profiles handoff stage U1), with profiles switched on, at phone, iPad and desktop
// widths in light and dark: every page sends a device without a profile to /editions, one column per profile with
// counts only, the PIN pad (a bottom sheet on phones, a dialog from 744px), wrong PIN and lockout, the no-JavaScript
// form, the header's edition chip and the phone menu's Switch edition, a script's fetch sent to the picker when the
// session's gate pass is gone, and signing the device out.
//
// It needs Profiles:Enabled=true on the site and the admin password: it adds two throwaway profiles in Admin ·
// Profiles and deletes them again at the end, even if a check fails.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { audit } = require('./contrast-audit.cjs');

const base = process.env.BASE_URL || 'http://localhost:5295';
const password = process.env.ADMIN_PASSWORD;
const shots = process.env.SCREENSHOT_DIR;
if (!password) {
    console.log('SKIP: adds profiles in admin; set ADMIN_PASSWORD to run it.');
    process.exit(0);
}
const widths = (process.env.WIDTHS || '320,390,430,744,820,1024,1440').split(',').map(Number);
const tag = Date.now().toString(36);
const people = [
    { name: 'zz Ann ' + tag, color: 'Teal', pin: '1357' },
    { name: 'zz Bo ' + tag, color: 'Violet', pin: '2468' },
];

(async () => {
    if ((await fetch(base + '/editions', { headers: { Accept: 'text/html' } })).status === 404) {
        console.log('SKIP: profiles are switched off on this site (Profiles:Enabled=false).');
        return;
    }
    const browser = await chromium.launch();
    const adminContext = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    const admin = await adminContext.newPage();
    const errors = [];
    const watch = (page) => page.on('pageerror', (e) => errors.push(e.message));
    watch(admin);
    const settle = async (page) => {
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(300);
    };
    const added = [];
    try {
        await admin.goto(base + '/login?returnUrl=%2Fadmin%2Fprofiles');
        await admin.locator('#password').fill(password);
        await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
        await admin.waitForURL('**/admin/profiles');
        await settle(admin);
        for (const person of people) {
            await admin.getByRole('button', { name: 'Add profile', exact: true }).first().click();
            await admin.locator('#add-name').fill(person.name);
            await admin.locator('.adm-swatch', { hasText: person.color }).click();
            await admin.locator('#add-pin').fill(person.pin);
            await admin.locator('#add-confirm').fill(person.pin);
            await admin.locator('form[aria-labelledby="add-title"] button[type="submit"]').click();
            await admin.getByText(`Added ${person.name}.`).waitFor();
            const href = await admin
                .locator('.adm-table a.adm-row-title', { hasText: person.name })
                .getAttribute('href');
            added.push({ ...person, id: Number(href.match(/(\d+)$/)[1]) });
        }

        // A new device: every page goes to the picker, and coming back lands where it started.
        const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
        const p = await context.newPage();
        watch(p);
        await p.goto(base + '/tv');
        await settle(p);
        assert(
            p.url().includes('/editions?returnUrl=%2Ftv'),
            'a page without a profile goes to the picker: ' + p.url(),
        );
        for (const person of added) {
            const column = p.locator('.edition', { hasText: person.name + '’s edition' });
            assert(await column.isVisible(), person.name + ' column');
        }
        const body = await p.locator('main').innerText();
        assert(!/Read the standard edition|Guest/.test(body), 'no standard edition or guest');

        // Wrong PIN, then the right one.
        const [ann, bo] = added;
        const annForm = p.locator(`form[action="profiles/${ann.id}/open"]`);
        assert.equal(await annForm.locator('.edition__pin').isVisible(), false, 'the pad replaces the PIN field');
        await annForm.getByRole('button').click();
        const dialog = p.locator('[data-pin-dialog]');
        await dialog.waitFor();
        assert.equal((await p.locator('[data-pin-title]').innerText()).trim(), `Enter ${ann.name}’s PIN`);
        for (const d of '0000') await dialog.getByRole('button', { name: d, exact: true }).click();
        await p.locator('[data-pin-status]', { hasText: 'Wrong PIN · 4 tries left' }).waitFor();
        await p.keyboard.type(ann.pin);
        await p.waitForURL('**/tv');
        await settle(p);
        assert(await p.locator('.tv, main').first().isVisible(), 'opened the page it came from');

        // Five wrong PINs lock Bo; the column says so, and Bo stays locked for the picker.
        await p.goto(base + '/editions');
        await settle(p);
        await p.locator(`form[action="profiles/${bo.id}/open"]`).getByRole('button').click();
        for (let i = 0; i < 5; i++) {
            await p.keyboard.type('9999');
            await p.locator('[data-pin-status]', { hasText: i < 4 ? 'Wrong PIN' : 'Locked' }).waitFor();
            if (i < 4) await p.locator('[data-pin-status]', { hasText: `${4 - i} ` }).waitFor();
        }
        await p.goto(base + '/editions');
        await settle(p);
        assert(
            await p.locator('.edition', { hasText: bo.name }).locator('.edition__note--locked').isVisible(),
            'locked column',
        );

        // Layout at every width and theme, the picker and an opened page with the edition chip.
        for (const theme of ['light', 'dark'])
            for (const width of widths) {
                await p.setViewportSize({ width, height: 900 });
                await p.evaluate((t) => localStorage.setItem('theme', t), theme);
                for (const path of ['/editions', '/']) {
                    await p.goto(base + path);
                    await settle(p);
                    const label = `${theme} ${width}px ${path}`;
                    const r = await p.evaluate(() => ({
                        over: document.documentElement.scrollWidth - innerWidth,
                        small: [
                            ...document.querySelectorAll(
                                '.edition__open, .picker__link, .edition-chip, .mobile-nav summary, .saved-link',
                            ),
                        ]
                            .filter((n) => n.offsetParent !== null)
                            .filter((n) => n.getBoundingClientRect().height < 44).length,
                        chip: !!document.querySelector('.edition-chip')?.offsetParent,
                    }));
                    assert.equal(r.over, 0, label + ' scrolls sideways');
                    assert.equal(r.small, 0, label + ' touch targets under 44px');
                    if (path === '/') assert.equal(r.chip, width >= 744, label + ' edition chip from 744px');
                    if (width === 390 || width === 1440) {
                        assert.deepEqual(await p.evaluate(audit), [], label + ' contrast');
                        if (shots) {
                            const file = path === '/' ? 'home' : 'editions';
                            await p.screenshot({ path: `${shots}${file}-${width}-${theme}.png`, fullPage: true });
                        }
                    }
                }
                // The pad at this width: a bottom sheet on phones, a dialog in the middle from 744px.
                await p.goto(base + '/editions');
                await settle(p);
                await p.locator(`form[action="profiles/${ann.id}/open"]`).getByRole('button').click();
                await p.locator('[data-pin-dialog]').waitFor();
                const g = await p.evaluate(() => {
                    const b = document.querySelector('[data-pin-dialog]').getBoundingClientRect();
                    const keys = [...document.querySelectorAll('[data-pin-key]')].map((k) => k.getBoundingClientRect());
                    return {
                        bottom: innerHeight - b.bottom,
                        left: b.left,
                        right: innerWidth - b.right,
                        small: keys.filter((k) => k.height < 44 || k.width < 44).length,
                    };
                });
                const label = `${theme} ${width}px PIN pad`;
                assert(g.left >= 0 && g.right >= 0, label + ' inside the screen');
                assert.equal(g.small, 0, label + ' keys under 44px');
                if (width < 744) assert(Math.abs(g.bottom) < 1, label + ' is a bottom sheet');
                else assert(g.bottom > 20, label + ' is a dialog');
                if (width === 390 || width === 1440) {
                    assert.deepEqual(await p.evaluate(audit), [], label + ' contrast');
                    if (shots) await p.screenshot({ path: `${shots}pin-${width}-${theme}.png` });
                }
                await p.keyboard.press('Escape');
                assert.deepEqual(errors, [], label + ' script errors');
            }

        // Phones: Switch edition is in the menu.
        await p.setViewportSize({ width: 390, height: 900 });
        await p.goto(base + '/');
        await settle(p);
        await p.locator('.mobile-nav summary').click();
        assert(
            await p.getByRole('link', { name: `Switch edition (${ann.name}’s)` }).isVisible(),
            'menu Switch edition',
        );

        // A new browser session: the page's own scripts hit the gate and are sent to the picker.
        const gate = (await context.cookies()).find((c) => c.name === 'mnp.gate');
        assert(gate, 'gate cookie after opening');
        await context.clearCookies({ name: 'mnp.gate' });
        await p.evaluate(() => fetch('/api/saved'));
        await p.waitForURL('**/editions?returnUrl=*');

        // Without JavaScript the column posts the typed PIN.
        const plain = await browser.newContext({ javaScriptEnabled: false, viewport: { width: 390, height: 900 } });
        const np = await plain.newPage();
        await np.goto(base + '/saved');
        assert(np.url().includes('/editions'), 'no-JS picker');
        const npForm = np.locator(`form[action="profiles/${ann.id}/open"]`);
        await npForm.locator('.edition__pin').fill('1111');
        await npForm.getByRole('button').click();
        await np.locator('.edition', { hasText: ann.name }).getByText('Wrong PIN').waitFor();
        await np.locator(`form[action="profiles/${ann.id}/open"] .edition__pin`).fill(ann.pin);
        await np.locator(`form[action="profiles/${ann.id}/open"]`).getByRole('button').click();
        await np.waitForURL('**/saved');
        await plain.close();

        // Sign the device out from the picker.
        await p.goto(base + '/editions');
        await settle(p);
        await p.keyboard.press('Escape');
        await p.locator(`form[action="profiles/${ann.id}/open"]`).getByRole('button').click();
        await p.keyboard.type(ann.pin);
        await p.waitForURL((u) => !u.pathname.startsWith('/editions'));
        await p.goto(base + '/editions');
        await settle(p);
        await p.getByRole('button', { name: `Sign this device out of ${ann.name}’s edition` }).click();
        await p.waitForURL('**/editions');
        await p.goto(base + '/');
        assert(p.url().includes('/editions'), 'signed out goes to the picker');
        assert.deepEqual(errors, []);
        console.log(
            `Editions at ${widths.join('/')}px light/dark: gate, PIN pad, wrong PIN, lockout, no-JS form, chip, menu, script gate and sign-out passed.`,
        );
    } finally {
        for (const person of added) {
            try {
                await admin.goto(base + '/admin/profiles/' + person.id);
                await settle(admin);
                await admin.getByRole('button', { name: 'Delete profile…' }).click();
                await admin.getByRole('button', { name: 'Delete profile', exact: true }).click();
                await admin.waitForURL('**/admin/profiles?deleted=*');
            } catch (e) {
                console.error(`Could not delete ${person.name}; remove it in Admin · Profiles. ${e.message}`);
            }
        }
        await browser.close();
    }
})().catch((e) => {
    console.error(e.message || e);
    process.exit(1);
});
