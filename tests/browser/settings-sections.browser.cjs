// Settings' profile sections (profiles handoff stage U3), with profiles switched on, at phone, iPad and desktop widths in
// light and dark: Your profile (name and color, Change PIN with a wrong and then the right current PIN, this device's
// picker), TV shows (search, Follow, Unfollow, and Track this show following for the open profile), household channel
// switches, and a request that shows under Your requests. No sideways scroll, readable text, 44px controls.
//
// It needs Profiles:Enabled=true and the admin password. It adds one throwaway profile and deletes it at the end (its
// follows and request go with it). The show it follows is one the household already tracks, so the scraper's list
// doesn't change.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { audit } = require('./contrast-audit.cjs');

const base = process.env.BASE_URL || 'http://localhost:5295';
const password = process.env.ADMIN_PASSWORD;
const shots = process.env.SCREENSHOT_DIR;
if (!password) {
    console.log('SKIP: adds a profile in admin; set ADMIN_PASSWORD to run it.');
    process.exit(0);
}
const widths = (process.env.WIDTHS || '320,390,430,744,820,1024,1440').split(',').map(Number);
const tag = Date.now().toString(36);
const person = { name: 'zz Ed ' + tag, pin: '3691' };

(async () => {
    if ((await fetch(base + '/editions', { headers: { Accept: 'text/html' } })).status === 404) {
        console.log('SKIP: profiles are switched off on this site (Profiles:Enabled=false).');
        return;
    }
    const browser = await chromium.launch();
    const admin = await (await browser.newContext({ viewport: { width: 1440, height: 1000 } })).newPage();
    const errors = [];
    const settle = async (page) => {
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(300);
    };
    let id = null;
    try {
        await admin.goto(base + '/login?returnUrl=%2Fadmin%2Ftv-series');
        await admin.locator('#password').fill(password);
        await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
        await admin.waitForURL('**/admin/tv-series');
        await settle(admin);
        // A show the household already tracks, from the admin's list.
        const switches = admin.locator('table [role="switch"][aria-checked="true"]');
        assert((await switches.count()) > 0, 'a tracked show to follow');
        const show = (await switches.first().getAttribute('aria-label')).replace(/^Following: /, '');

        await admin.goto(base + '/admin/profiles');
        await settle(admin);
        await admin.getByRole('button', { name: 'Add profile', exact: true }).first().click();
        await admin.locator('#add-name').fill(person.name);
        await admin.locator('#add-pin').fill(person.pin);
        await admin.locator('#add-confirm').fill(person.pin);
        await admin.locator('form[aria-labelledby="add-title"] button[type="submit"]').click();
        await admin.getByText(`Added ${person.name}.`).waitFor();
        id = Number(
            (await admin.locator('.adm-table a.adm-row-title', { hasText: person.name }).getAttribute('href')).match(
                /(\d+)$/,
            )[1],
        );

        const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
        const p = await context.newPage();
        p.on('pageerror', (e) => errors.push(e.message));
        const open = async (pin) => {
            await p.goto(base + '/settings');
            await settle(p);
            await p.locator(`form[action="profiles/${id}/open"]`).getByRole('button').click();
            await p.keyboard.type(pin);
            await p.waitForURL('**/settings');
            await settle(p);
        };
        await open(person.pin);
        assert(await p.locator('#profile-heading').isVisible(), 'Your profile section');
        assert.equal(await p.locator('#profile-name').inputValue(), person.name);

        // Color, then this device's picker.
        await p.locator('.set-color', { hasText: 'Violet' }).click();
        await p.getByRole('button', { name: 'Save name and color' }).click();
        await p.getByText('Name and color saved.').waitFor();
        assert(await p.locator('.set-color', { hasText: 'Violet' }).locator('input').isChecked());
        await p.locator('input[name="pickerMode"][value="daily"]').check();
        await p.getByRole('button', { name: 'Save for this device' }).click();
        await p.getByText('Saved for this device.').waitFor();
        assert(await p.locator('input[name="pickerMode"][value="daily"]').isChecked());

        // Change PIN: a wrong current PIN says how many tries are left; the right one changes it.
        const pin = async (current, next) => {
            await p.locator('#pin-current').fill(current);
            await p.locator('#pin-new').fill(next);
            await p.locator('#pin-confirm').fill(next);
            await p.getByRole('button', { name: 'Change PIN' }).click();
            await settle(p);
        };
        await pin('0000', '4826');
        await p.getByText('That isn’t your current PIN. 4 tries left').waitFor();
        await pin(person.pin, '4826');
        await p.getByText('PIN changed.').waitFor();

        // TV: follow a tracked show from Settings, see it listed, unfollow it.
        await p.locator('#tv-query').fill(show);
        await p.locator('.tv-search button[type="submit"]').click();
        await settle(p);
        const follow = p.getByRole('button', { name: `Follow ${show}`, exact: true }).first();
        await follow.waitFor({ timeout: 20000 });
        await follow.click();
        await p.getByText('Following. Episodes appear on your TV calendar').waitFor();
        const row = p.locator('.set-rows[aria-label="Shows you follow"] li', { hasText: show });
        assert(await row.isVisible(), 'followed show listed');
        await row.getByRole('button', { name: `Unfollow ${show}` }).click();
        await p.getByText('Unfollowed.').waitFor();
        assert.equal(await row.count(), 0, 'unfollowed');
        // Track this show follows for the open profile too.
        await p.goto(base + '/tv/track?q=' + encodeURIComponent(show));
        await settle(p);
        await p
            .getByRole('button', { name: `Follow ${show}`, exact: true })
            .first()
            .click();
        await p.getByText('Following.').waitFor();
        await p.goto(base + '/settings');
        await settle(p);
        assert(await row.isVisible(), 'Track this show followed it for this profile');
        await row.getByRole('button', { name: `Unfollow ${show}` }).click();
        await p.getByText('Unfollowed.').waitFor();

        // Household channels: switch the first one off and back on.
        const channel = p.locator('.set-rows[aria-label="Household channels"] [role="switch"]').first();
        if (await channel.count()) {
            await p.locator('summary', { hasText: 'All household channels' }).click();
            const name = await channel.getAttribute('aria-label');
            assert.equal(await channel.getAttribute('aria-checked'), 'true', 'household channels start on');
            await channel.click();
            await p.getByText('Channel off.').waitFor();
            const again = p.locator(`.set-rows[aria-label="Household channels"] [role="switch"][aria-label="${name}"]`);
            assert.equal(await again.getAttribute('aria-checked'), 'false');
            await again.click();
            await p.getByText('Channel on.').waitFor();
        } else console.log('Note: no household channels here; switches not checked.');

        // A request shows under Your requests as Waiting.
        await p.locator('input[name="kind"][value="news"]').check();
        await p.locator('#request-name').fill('zz request ' + tag);
        await p.getByRole('button', { name: 'Send request' }).click();
        await p.getByText('Sent. The admin will look at it').waitFor();
        const mine = p.locator('.set-rows[aria-label="Your requests"] li', { hasText: 'zz request ' + tag });
        assert.equal((await mine.locator('.set-status').innerText()).trim(), 'Waiting');

        // Layout at every width and theme.
        for (const theme of ['light', 'dark'])
            for (const width of widths) {
                await p.setViewportSize({ width, height: 900 });
                await p.goto(base + '/settings');
                await settle(p);
                await p.locator(`input[data-theme-mode][value="${theme}"]`).check();
                await p.waitForTimeout(700);
                for (const path of [
                    '/settings?tvq=' + encodeURIComponent(show),
                    '/tv/track?q=' + encodeURIComponent(show),
                ]) {
                    await p.goto(base + path);
                    await settle(p);
                    const label = `${theme} ${width}px ${path.split('?')[0]}`;
                    const r = await p.evaluate(() => ({
                        theme: document.documentElement.getAttribute('data-theme'),
                        over: document.documentElement.scrollWidth - innerWidth,
                        small: [
                            ...document.querySelectorAll(
                                '.set-main .nf-btn, .set-switch, .set-chip, .set-color, .set-input, .tv-results .nf-btn, .set-link, .settings-scheme',
                            ),
                        ]
                            .filter((n) => n.offsetParent !== null)
                            .filter((n) => n.getBoundingClientRect().height < 44).length,
                    }));
                    assert.equal(r.theme, theme, label + ' theme');
                    assert.equal(r.over, 0, label + ' scrolls sideways');
                    assert.equal(r.small, 0, label + ' touch targets under 44px');
                    if (width === 390 || width === 1440) {
                        assert.deepEqual(await p.evaluate(audit), [], label + ' contrast');
                        if (shots) {
                            const file = path.startsWith('/settings') ? 'settings' : 'track';
                            await p.screenshot({ path: `${shots}${file}-${width}-${theme}.png`, fullPage: true });
                        }
                    }
                }
            }
        assert.deepEqual(errors, []);
        await context.close();
        console.log(
            `Settings sections at ${widths.join('/')}px light/dark: profile, picker, Change PIN, follow and unfollow, Track this show, channel switches and a request passed.`,
        );
    } finally {
        if (id) {
            try {
                await admin.goto(base + '/admin/profiles/' + id);
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
