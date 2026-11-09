// The standard post-deploy check against the live sites, and the live part of mobile verification (AGENTS.md):
//
//   node tools/live-check.cjs                       everything below
//   WIDTHS=390,1440 THEMES=light node tools/live-check.cjs      a quicker pass
//   PAGES=/,/news node tools/live-check.cjs         only these public pages
//
// 1. /healthz on both sites.
// 2. Admin sign-in over HTTPS, with a Secure, HttpOnly session cookie; adds a temporary `zz live …` profile.
// 3. The public pages below at 320/390/430/1440px in light and dark, opened as that profile: no sideways overflow, no
//    script errors, no Blazor error banner, the right theme, and no fall back to the picker.
// 4. Every admin list route, plus the first edit page each one links to, at 390 and 1440px.
// 5. Teardown, also when a check fails: deletes the profile through Admin, then tools/sql/cleanup-zz.sh removes any
//    `zz ` leftovers and the unassigned devices created since the recorded start, and checks the household.
//
// Addresses and the admin password come from the environment or .env (PUBLIC_BASE_URL, ADMIN_BASE_URL,
// ADMIN_PASSWORD); nothing secret is printed. When the tailnet name doesn't resolve here, it is mapped to the
// Tailscale container's address. Results and screenshots go to test-results/live/ (summary.md for release history).
const { chromium } = require('playwright');
const { execFileSync } = require('node:child_process');
const dns = require('node:dns').promises;
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const outDir = path.join(root, 'test-results/live');
const shotDir = path.join(outDir, 'screens');

const publicPages = [
    '/',
    '/news',
    '/news/kansas-city',
    '/sports',
    '/scoreboard',
    '/college',
    '/college/rankings',
    '/standings',
    '/events',
    '/championship/results',
    '/mls/results',
    '/f1',
    '/entertainment',
    '/movies',
    '/movies/digital',
    '/box-office',
    '/tv',
    '/watch',
    '/tech',
    '/videos',
    '/saved',
    '/settings',
];
const adminRoutes = [
    '/admin',
    '/admin/categories',
    '/admin/pages',
    '/admin/profiles',
    '/admin/profiles/base',
    '/admin/requests',
    '/admin/results',
    '/admin/results/new',
    '/admin/schedule-coverage',
    '/admin/sources',
    '/admin/sources/new',
    '/admin/sports-event-types',
    '/admin/sports-event-types/new',
    '/admin/sports-events',
    '/admin/tv-series',
    '/admin/tv-series/new',
    '/admin/youtube-feeds',
    '/admin/youtube-feeds/new',
    '/admin/youtube-videos',
];
// Failures already known and recorded elsewhere: reported as known, not as failures.
const known = {
    '/admin/youtube-videos/36379/edit@390':
        'long description overflows at 390px (known since November 4; predates step 4)',
};

const list = (value, fallback) => (value ? value.split(',').map((s) => s.trim()) : fallback);
const widths = list(process.env.WIDTHS, ['320', '390', '430', '1440']).map(Number);
const themes = list(process.env.THEMES, ['light', 'dark']);
const pages = list(process.env.PAGES, publicPages);

function setting(name) {
    if (process.env[name]) return process.env[name];
    const file = path.join(root, '.env');
    const line =
        fs.existsSync(file) &&
        fs
            .readFileSync(file, 'utf8')
            .split('\n')
            .find((l) => l.startsWith(name + '='));
    return line ? line.slice(name.length + 1).replace(/^['"]|['"]$/g, '') : '';
}

async function hostRules(urls) {
    const rules = [];
    for (const host of new Set(urls.map((u) => new URL(u).hostname))) {
        try {
            await dns.lookup(host);
        } catch {
            const ip = execFileSync('docker', ['exec', 'tailscale', 'tailscale', 'ip', '-4'])
                .toString()
                .trim()
                .split('\n')[0];
            rules.push(`MAP ${host} ${ip}`);
        }
    }
    return rules;
}

function sql(script, args) {
    return execFileSync(path.join(root, 'tools/sql', script), args, { cwd: root }).toString();
}

async function main() {
    const publicBase = setting('PUBLIC_BASE_URL').replace(/\/$/, '');
    const adminBase = setting('ADMIN_BASE_URL').replace(/\/$/, '');
    const password = setting('ADMIN_PASSWORD');
    if (!publicBase || !adminBase || !password)
        throw new Error('Set PUBLIC_BASE_URL, ADMIN_BASE_URL and ADMIN_PASSWORD');

    fs.rmSync(outDir, { recursive: true, force: true });
    fs.mkdirSync(shotDir, { recursive: true });
    // UTC, to the second, recorded before anything touches the site; the clean-up removes unassigned devices since.
    const start = new Date().toISOString().slice(0, 19);
    const results = [];
    const pass = (area, item) => results.push({ area, item, ok: true });
    const fail = (area, item, why) => {
        const k = known[item];
        results.push({ area, item, ok: !!k, known: k, why });
        console.log(`  ${k ? 'known' : 'FAIL '} ${area} ${item}: ${why}`);
    };

    const rules = await hostRules([publicBase, adminBase]);
    const browser = await chromium.launch({ args: rules.length ? [`--host-resolver-rules=${rules.join(',')}`] : [] });
    let admin = null;
    let manage = null;
    let profileName = null;
    let cleanup;
    try {
        // 1. Health.
        const probe = await (await browser.newContext()).newPage();
        for (const [label, base] of [
            ['public', publicBase],
            ['admin', adminBase],
        ]) {
            const r = await probe.goto(base + '/healthz');
            const body = (await probe.textContent('body')) || '';
            if (r.status() === 200 && body.includes('Healthy')) pass('health', label);
            else fail('health', label, `${r.status()} ${body.slice(0, 60)}`);
        }
        await probe.context().close();

        // 2. Admin sign-in and a temporary profile.
        const adminContext = await browser.newContext({
            viewport: { width: 1440, height: 1000 },
            timezoneId: 'America/Chicago',
        });
        admin = await adminContext.newPage();
        const adminErrors = [];
        admin.on('pageerror', (e) => adminErrors.push(e.message));
        await admin.goto(adminBase + '/login?returnUrl=%2Fadmin%2Fprofiles');
        await admin.locator('#password').fill(password);
        await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
        await admin.waitForURL('**/admin/profiles');
        await admin.waitForLoadState('networkidle');
        const session = (await adminContext.cookies()).find((c) => c.name === 'myNewspaper.Admin.Session');
        if (session?.secure && session.httpOnly) pass('admin', 'sign-in, Secure HttpOnly session cookie');
        else
            fail(
                'admin',
                'sign-in',
                `session cookie ${session ? `secure=${session.secure} httpOnly=${session.httpOnly}` : 'missing'}`,
            );

        profileName = 'zz live ' + Date.now().toString(36);
        const pin = String(1000 + Math.floor(Math.random() * 9000));
        for (let attempt = 0; attempt < 12 && !(await admin.locator('#add-name').isVisible()); attempt++) {
            await admin.getByRole('button', { name: 'Add profile', exact: true }).first().click();
            try {
                await admin.locator('#add-name').waitFor({ state: 'visible', timeout: 1500 });
            } catch {
                if (attempt === 11) throw new Error('Admin Add profile did not become interactive');
            }
        }
        await admin.locator('#add-name').fill(profileName);
        await admin.locator('#add-pin').fill(pin);
        await admin.locator('#add-confirm').fill(pin);
        await admin.locator('form[aria-labelledby="add-title"] button[type="submit"]').click();
        await admin.getByText(`Added ${profileName}.`).waitFor();
        const href = await admin.locator('.adm-table a.adm-row-title', { hasText: profileName }).getAttribute('href');
        manage = '/' + href.replace(/^\//, '');
        const id = Number(href.match(/(\d+)$/)[1]);

        // 3. Public pages as that profile, one browser context (one device) throughout.
        const context = await browser.newContext({
            viewport: { width: 390, height: 900 },
            timezoneId: 'America/Chicago',
        });
        const p = await context.newPage();
        let errors = [];
        p.on('pageerror', (e) => errors.push(e.message));
        await p.goto(publicBase + '/');
        await p.locator(`form[action="profiles/${id}/open"]`).getByRole('button').click();
        await p.keyboard.type(pin);
        await p.waitForURL((u) => !u.pathname.startsWith('/editions'));
        const saved = p.waitForResponse(
            (r) => new URL(r.url()).pathname === '/api/me/settings' && r.request().method() === 'PUT',
        );
        await p.evaluate(() => window.myNewspaperPrefs.setItem('theme', 'system'));
        await (await saved).finished();

        console.log(`public: ${pages.length} pages × ${widths.join('/')}px × ${themes.join('/')}`);
        for (const route of pages) {
            for (const theme of themes) {
                await p.emulateMedia({ colorScheme: theme });
                for (const width of widths) {
                    const item = `${route}@${width}/${theme}`;
                    errors = [];
                    await p.setViewportSize({ width, height: width < 800 ? 900 : 1000 });
                    const r = await p.goto(publicBase + route);
                    await p.waitForLoadState('networkidle');
                    const problems = [];
                    if (!r || r.status() >= 400) problems.push(`HTTP ${r ? r.status() : 'none'}`);
                    if (new URL(p.url()).pathname.startsWith('/editions')) problems.push('fell back to the picker');
                    const overflow = await p.evaluate(() => document.documentElement.scrollWidth - innerWidth);
                    if (overflow > 1) problems.push(`overflows ${overflow}px`);
                    if (await p.locator('#blazor-error-ui').isVisible()) problems.push('error banner');
                    // Clipping inside a sideways-scrolling strip doesn't overflow the page, so check score cards
                    // directly (November 5, 2026: long team names pushed scores past the card's edge).
                    const clipped = await p.$$eval(
                        '.mx-score',
                        (tiles) =>
                            tiles.filter((t) =>
                                [...t.querySelectorAll('.mx-score__team')].some(
                                    (r) => r.getBoundingClientRect().right > t.getBoundingClientRect().right + 0.5,
                                ),
                            ).length,
                    );
                    if (clipped) problems.push(`${clipped} score card(s) with the score past the card's edge`);
                    const actual = await p.locator('html').getAttribute('data-theme');
                    if (actual !== theme) problems.push(`theme ${actual}`);
                    if (errors.length) problems.push(`script error: ${errors[0].slice(0, 80)}`);
                    if (problems.length) fail('public', item, problems.join('; '));
                    else pass('public', item);
                    if (width === 390 || (width === 1440 && theme === 'light')) {
                        const file = `public${route === '/' ? '-home' : route.replace(/\//g, '-')}-${width}-${theme}.png`;
                        await p.screenshot({ path: path.join(shotDir, file), fullPage: width === 390 });
                    }
                }
            }
        }
        await context.close();

        // 4. Admin routes, plus the first edit page each list links to.
        const routes = [...adminRoutes];
        console.log(`admin: ${adminRoutes.length} routes plus edit pages × 390/1440px`);
        for (let i = 0; i < routes.length; i++) {
            const route = routes[i];
            for (const width of [390, 1440]) {
                adminErrors.length = 0;
                await admin.setViewportSize({ width, height: width < 800 ? 900 : 1000 });
                const r = await admin.goto(adminBase + route);
                await admin.waitForLoadState('networkidle');
                await admin.waitForTimeout(300);
                const problems = [];
                if (!r || r.status() >= 400) problems.push(`HTTP ${r ? r.status() : 'none'}`);
                if (new URL(admin.url()).pathname.startsWith('/login')) problems.push('signed out');
                const overflow = await admin.evaluate(() => document.documentElement.scrollWidth - innerWidth);
                if (overflow > 1) problems.push(`overflows ${overflow}px`);
                if (await admin.locator('#blazor-error-ui').isVisible()) problems.push('error banner');
                if (adminErrors.length) problems.push(`script error: ${adminErrors[0].slice(0, 80)}`);
                if (problems.length) fail('admin', `${route}@${width}`, problems.join('; '));
                else pass('admin', `${route}@${width}`);
            }
            if (adminRoutes.includes(route)) {
                const edit = await admin
                    .locator('main a[href*="/edit"], main a[href*="admin/profiles/"]')
                    .evaluateAll((ns) => ns.map((n) => n.getAttribute('href')).find((h) => /\d/.test(h)));
                const link = edit && '/' + edit.replace(/^\//, '').split('#')[0];
                if (link && link !== manage && !routes.includes(link)) routes.push(link);
            }
        }
    } catch (e) {
        // A step that can't continue (sign-in refused, a page that never loads) still gets teardown and a summary.
        fail('run', 'aborted', e.message.split('\n')[0]);
    } finally {
        // 5. Teardown: Admin's delete first (removes the profile's devices, follows and requests), then the SQL net.
        if (admin && manage) {
            try {
                await admin.setViewportSize({ width: 1440, height: 1000 });
                await admin.goto(adminBase + manage);
                await admin.waitForLoadState('networkidle');
                // The confirm panel only opens once the page is interactive; retry the first click until it does.
                const confirm = admin.getByRole('button', { name: 'Delete profile', exact: true });
                for (let attempt = 0; attempt < 12 && !(await confirm.isVisible()); attempt++) {
                    await admin.getByRole('button', { name: 'Delete profile…' }).click();
                    try {
                        await confirm.waitFor({ state: 'visible', timeout: 1500 });
                    } catch {}
                }
                await confirm.click();
                await admin.waitForURL('**/admin/profiles?deleted=*');
                pass('cleanup', `deleted ${profileName} through Admin`);
            } catch (e) {
                fail('cleanup', `delete ${profileName} through Admin`, e.message.split('\n')[0]);
            }
        }
        await browser.close();
        try {
            cleanup = sql('cleanup-zz.sh', [start, '--apply']);
            pass('cleanup', 'zz leftovers and unassigned devices since start removed; household check passed');
        } catch (e) {
            cleanup = (e.stdout || '').toString() + (e.stderr || '').toString();
            fail('cleanup', 'cleanup-zz.sh / household check', cleanup.trim().split('\n').pop());
        }
    }

    const failures = results.filter((r) => !r.ok);
    const knownHits = results.filter((r) => r.known);
    const count = (area) => {
        const all = results.filter((r) => r.area === area);
        return `${all.filter((r) => r.ok).length}/${all.length}`;
    };
    const summary = [
        `# Live check — ${new Date().toISOString().slice(0, 16).replace('T', ' ')} UTC`,
        '',
        `Start (UTC): ${start}. Public: ${publicBase}. Admin: ${adminBase}.`,
        '',
        `- Health: ${count('health')}`,
        `- Admin sign-in and pages (${adminRoutes.length} routes + edit pages, 390/1440px): ${count('admin')}`,
        `- Public pages (${pages.length} × ${widths.join('/')}px × ${themes.join('/')}): ${count('public')}`,
        `- Clean-up: ${count('cleanup')}`,
        '',
        failures.length ? '## Failures' : 'No failures.',
        ...failures.map((f) => `- ${f.area} ${f.item}: ${f.why}`),
        ...(knownHits.length ? ['', '## Known', ...knownHits.map((k) => `- ${k.item}: ${k.why} (${k.known})`)] : []),
        '',
        '## Household after clean-up',
        '',
        '```',
        cleanup.trim(),
        '```',
        '',
        `Screenshots: test-results/live/screens/ (390px light and dark, 1440px light).`,
    ].join('\n');
    fs.writeFileSync(path.join(outDir, 'summary.md'), summary + '\n');
    fs.writeFileSync(path.join(outDir, 'results.json'), JSON.stringify(results, null, 2));
    console.log('\n' + summary);
    return failures.length ? 1 : 0;
}

main().then(
    (code) => process.exit(code),
    (e) => {
        console.error(e.message || e);
        process.exit(1);
    },
);
