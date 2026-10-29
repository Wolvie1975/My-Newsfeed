// U4: new edition setup (/editions/new, profiles handoff 6.2) from the picker's "+ Start a new edition". Needs profiles
// on and ADMIN_PASSWORD; every profile it creates is deleted through Admin · Profiles at the end.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { audit } = require('./contrast-audit.cjs');

const base = process.env.BASE_URL || 'http://localhost:5295';
const adminBase = process.env.ADMIN_BASE_URL || base;
const shots = process.env.SCREENSHOT_DIR;
const password = process.env.ADMIN_PASSWORD;

(async () => {
    const profiles = (await fetch(base + '/editions', { headers: { Accept: 'text/html' } })).status !== 404;
    if (!profiles || !password) {
        console.log('SKIP: needs profiles on and ADMIN_PASSWORD (it creates and deletes throwaway profiles).');
        return;
    }
    const browser = await chromium.launch();
    const errors = [];
    const created = [];
    const tag = Date.now().toString(36);
    let admin = null;
    const me = (page) => page.evaluate(async () => (await fetch('/api/me')).json());
    // Third-party images (service logos, posters) are left out, as in the other phone checks.
    const contextFor = async (width, theme = 'light', js = true) => {
        const context = await browser.newContext({
            viewport: { width, height: 900 },
            colorScheme: theme,
            javaScriptEnabled: js,
        });
        await context.route('**/*', (route) =>
            route.request().resourceType() === 'image' && !route.request().url().startsWith(base)
                ? route.abort()
                : route.fallback(),
        );
        return context;
    };
    try {
        admin = await (await browser.newContext({ viewport: { width: 1440, height: 1000 } })).newPage();
        await admin.goto(adminBase + '/login?returnUrl=%2Fadmin%2Fprofiles');
        await admin.locator('#password').fill(password);
        await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
        await admin.waitForURL('**/admin/profiles');

        // Layout: 320/390/430 light and dark, and the retained wider widths.
        for (const width of [320, 390, 430, 768, 1440])
            for (const theme of ['light', 'dark']) {
                const ctx = await contextFor(width, theme);
                const p = await ctx.newPage();
                p.on('pageerror', (e) => errors.push(e.message));
                await p.goto(base + '/editions/new', { waitUntil: 'networkidle' });
                assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth), `overflow ${width}`);
                const ids = await p.locator('[id]').evaluateAll((ns) => ns.map((n) => n.id));
                assert.equal(ids.length, new Set(ids).size, 'unique ids');
                assert.deepEqual(await p.evaluate(audit), [], `contrast ${width} ${theme}`);
                assert.equal(await p.locator('[data-setup-step]:visible').count(), 1, 'one step at a time');
                // Each color's name fits inside its swatch.
                assert(
                    await p
                        .locator('.setup__swatch')
                        .evaluateAll((ns) => ns.every((n) => n.scrollWidth <= n.clientWidth + 1)),
                    `swatch text overflows at ${width}`,
                );
                for (const target of await p
                    .locator(
                        '.setup a, .setup button, .setup input:not([type=hidden]):not([type=radio]), .setup__swatch',
                    )
                    .filter({ visible: true })
                    .all())
                    assert(
                        (await target.boundingBox()).height >= 43.9,
                        `44px target at ${width}: ${await target.evaluate((n) => n.outerHTML.slice(0, 80))}`,
                    );
                if (shots && width <= 430)
                    await p.screenshot({ path: `${shots}/setup-${width}-${theme}.png`, fullPage: true });
                await ctx.close();
            }

        // The full flow on a phone, from the picker.
        const ctx = await contextFor(390);
        const p = await ctx.newPage();
        p.on('pageerror', (e) => errors.push(e.message));
        await p.goto(base + '/editions');
        await p.getByRole('link', { name: '+ Start a new edition' }).click();
        await p.waitForURL('**/editions/new');
        // profiles.js (deferred) turns the long form into steps.
        await p.locator('form.setup.is-stepped').waitFor();
        const next = p.locator('[data-setup-next]');
        assert(await next.isDisabled(), 'Continue waits for a name and PIN');
        const name = `zz Setup ${tag}`;
        await p.locator('#setup-name').fill(name);
        assert.equal(await p.locator('[data-setup-initial]').textContent(), 'Z');
        await p.locator('.setup__swatch', { hasText: 'Teal' }).click();
        assert.equal(await p.locator('form[data-setup]').evaluate((f) => f.style.getPropertyValue('--ed')), '#0F766E');
        await p.locator('#setup-pin').fill('2468');
        await p.locator('#setup-pin-confirm').fill('2469');
        assert.equal(await p.locator('[data-setup-pin-status]').textContent(), 'The two PINs don’t match.');
        assert(await next.isDisabled());
        await p.locator('#setup-pin-confirm').fill('2468');
        assert(await next.isEnabled());
        await next.click();
        // Step 2: search opens the groups; tick the first match.
        assert.equal(await p.locator('[data-setup-count]').textContent(), 'Step 2 of 4');
        assert(
            await p.locator('#setup-step-2').evaluate((n) => n === document.activeElement),
            'focus moves to the step',
        );
        let team = null;
        if (await p.locator('#setup-team-filter').count()) {
            await p.locator('#setup-team-filter').fill('kansas');
            const option = p.locator('[data-setup-step]:visible [data-setup-option]:visible').first();
            const box = option.locator('input');
            if (!(await box.isChecked())) await box.check();
            team = await box.getAttribute('value');
        }
        await p.locator('[data-setup-back]').click();
        assert.equal(await p.locator('#setup-name').inputValue(), name, 'Back keeps step 1');
        await next.click();
        await next.click();
        // Step 3: services (none locally without a catalog key).
        assert.equal(await p.locator('[data-setup-count]').textContent(), 'Step 3 of 4');
        let service = null;
        const services = p.locator('[data-setup-service]');
        if (await services.count()) {
            const first = services.first();
            if (!(await first.isChecked())) await first.check();
            service = Number(await first.getAttribute('value'));
        }
        await next.click();
        // Step 4: hide the third section and move the second to the top.
        assert(await next.isHidden());
        const submit = p.locator('[data-setup-submit]');
        assert.equal(await submit.textContent(), `Open ${name}’s edition`);
        const sections = p.locator('[data-setup-sections] li');
        const hiddenKey = await sections.nth(2).locator('input[name=show]').getAttribute('value');
        await sections.nth(2).locator('input[name=show]').uncheck();
        const movedKey = await sections.nth(1).locator('input[name=show]').getAttribute('value');
        await sections.nth(1).locator('[data-setup-up]').click();
        assert.equal(await sections.first().locator('input[name=show]').getAttribute('value'), movedKey);
        if (shots) await p.screenshot({ path: `${shots}/setup-step4-390.png`, fullPage: true });
        await submit.click();
        await p.waitForURL((u) => u.pathname === '/');
        const mine = await me(p);
        assert.equal(mine.name, name);
        assert.equal(mine.color.toUpperCase(), '#0F766E');
        if (team) assert(mine.settings.teams.includes(team), 'chosen team saved');
        if (service) assert(mine.settings.services.includes(service), 'chosen service saved');
        assert.equal(mine.settings.home.order[0], movedKey, 'moved section first');
        assert(mine.settings.home.hidden.includes(hiddenKey), 'unticked section hidden');
        created.push(name);

        // A taken name comes back on step 1, keeping what was chosen.
        await p.goto(base + '/editions/new');
        await p.locator('form.setup.is-stepped').waitFor();
        await p.locator('#setup-name').fill(name);
        await p.locator('#setup-pin').fill('1357');
        await p.locator('#setup-pin-confirm').fill('1357');
        for (let i = 0; i < 3; i++) await next.click();
        await p.locator('[data-setup-sections] li').first().locator('input[name=show]').uncheck();
        await submit.click();
        await p.locator('[data-setup-error]').waitFor();
        assert.match(await p.locator('[data-setup-error]').textContent(), /name/i);
        assert.equal(await p.locator('[data-setup-count]').textContent(), 'Step 1 of 4');
        assert.equal(await p.locator('#setup-name').inputValue(), name);
        assert(!(await p.locator('[data-setup-sections] li').first().locator('input[name=show]').isChecked()));
        await ctx.close();

        // Without JavaScript: one long form that still creates and opens the edition.
        const plain = await contextFor(320, 'light', false);
        const q = await plain.newPage();
        await q.goto(base + '/editions/new');
        assert.equal(await q.locator('[data-setup-step]:visible').count(), 4, 'all steps without JavaScript');
        const plainName = `zz Plain ${tag}`;
        await q.locator('#setup-name').fill(plainName);
        await q.locator('#setup-pin').fill('8642');
        await q.locator('#setup-pin-confirm').fill('8642');
        await q.locator('[data-setup-submit]').click();
        await q.waitForURL((u) => u.pathname === '/');
        created.push(plainName);
        // A refused name returns to the setup page with the reason.
        await q.goto(base + '/editions/new');
        await q.locator('#setup-name').fill(plainName);
        await q.locator('#setup-pin').fill('8642');
        await q.locator('#setup-pin-confirm').fill('8642');
        await q.locator('[data-setup-submit]').click();
        await q.waitForURL('**/editions/new?error=**');
        assert(await q.locator('.edition__note--bad').first().isVisible());
        await plain.close();

        assert.deepEqual(errors, []);
        console.log(
            'U4 setup: picker link, four steps (Continue gated by name and matching PIN, Back keeps choices, focus moves), team search, services, home order and hiding saved to the new profile, taken name returns to step 1 with choices kept, no-JS long form and error; 320–1440 light/dark overflow, contrast, unique IDs, 44px targets passed.',
        );
    } finally {
        // Delete every profile this check made (Admin · Profiles, which also removes their devices).
        for (const name of created) {
            try {
                await admin.goto(adminBase + '/admin/profiles');
                await admin.waitForLoadState('networkidle');
                const href = await admin.locator('.adm-table a.adm-row-title', { hasText: name }).getAttribute('href');
                await admin.goto(adminBase + '/admin/profiles/' + href.match(/(\d+)$/)[1]);
                await admin.waitForLoadState('networkidle');
                await admin.getByRole('button', { name: 'Delete profile…' }).click();
                await admin.getByRole('button', { name: 'Delete profile', exact: true }).click();
                await admin.waitForURL('**/admin/profiles?deleted=*');
            } catch (e) {
                console.error(
                    `Could not delete the throwaway profile "${name}"; remove it in Admin · Profiles. ${e.message}`,
                );
            }
        }
        await browser.close();
    }
})().catch((e) => {
    console.error(e.message || e);
    process.exit(1);
});
