// Shared set-up for the browser checks (*.browser.cjs). Every check requires it; browser-lib.test.cjs enforces that.
//
//   const lib = require('./lib.cjs');
//   lib.base, lib.adminBase, lib.adminPassword     where the sites are (BASE_URL, ADMIN_BASE_URL, ADMIN_PASSWORD)
//   lib.shotDir                                     SCREENSHOT_DIR (created, ends in /) or null: screenshots on request
//   lib.screenshots('name')                         SCREENSHOT_DIR or test-results/browser/<name>/, created
//   lib.PHONE_WIDTHS, lib.WIDTHS, lib.THEMES        320/390/430; plus 768 and 1440; light and dark
//   lib.widths('320,390,1440') / lib.themes()       the defaults, unless WIDTHS / THEMES override them
//   lib.profilesEnabled()                           whether the site shows the profile picker (/editions)
//   lib.signInAdmin(browser, { returnPath })        an Admin page, signed in (default: on Admin · Profiles)
//   lib.addProfile(admin, 'Label', { pin })         adds `zz Label <time>` through Admin; remembered for teardown
//   lib.openProfile(browser, profile, { path })     opens it from the picker; returns the storage state (one device)
//   lib.openProfileIn(page, profile, path)          the same on a page you keep using
//   lib.adoptProfile(admin, name)                   registers a zz profile made elsewhere (setup flow) for teardown
//   lib.deleteProfiles()                            deletes every profile added above through Admin (call in finally)
//   lib.run(async (browser) => { … }, launchOpts)   launch, run, report, and always delete the profiles and close
//
// Clean-up (AGENTS.md, "Browser tests clean up their own data"): profiles go through Admin's delete. As a safety net,
// when the check exits, tools/sql/cleanup-zz.sh removes `zz ` profiles and the unassigned devices created since the
// check started, in CLEANUP_DB (WebScraper or WebScraper_Test; `none` to skip). Without CLEANUP_DB it is WebScraper
// only when BASE_URL is the live site (.env PUBLIC_BASE_URL); otherwise the check says what to run. Under run.cjs the
// runner does this once for the whole suite instead.
const { chromium, request } = require('playwright');
const { execFileSync } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '../..');
const started = new Date().toISOString().slice(0, 19);

function envFile(name) {
    try {
        const line = fs
            .readFileSync(path.join(root, '.env'), 'utf8')
            .split('\n')
            .find((l) => l.startsWith(name + '='));
        return line ? line.slice(name.length + 1).replace(/^['"]|['"]$/g, '') : '';
    } catch {
        return '';
    }
}

const trim = (url) => url.replace(/\/$/, '');
const base = trim(process.env.BASE_URL || 'http://localhost:5295');
const adminBase = trim(process.env.ADMIN_BASE_URL || 'http://localhost:5297');
const adminPassword = process.env.ADMIN_PASSWORD || '';

const withSlash = (dir) => (dir.endsWith('/') ? dir : dir + '/');
const shotDir = process.env.SCREENSHOT_DIR ? withSlash(process.env.SCREENSHOT_DIR) : null;
if (shotDir) fs.mkdirSync(shotDir, { recursive: true });

function screenshots(name) {
    const dir = shotDir || path.join(root, 'test-results/browser', name) + '/';
    fs.mkdirSync(dir, { recursive: true });
    return dir;
}

const PHONE_WIDTHS = Object.freeze([320, 390, 430]);
const TABLET_WIDTH = 768;
const DESKTOP_WIDTH = 1440;
const WIDTHS = Object.freeze([...PHONE_WIDTHS, TABLET_WIDTH, DESKTOP_WIDTH]);
const THEMES = Object.freeze(['light', 'dark']);

const list = (value) => (Array.isArray(value) ? value : String(value).split(',')).map((s) => String(s).trim());
const widths = (defaults = WIDTHS) => list(process.env.WIDTHS || defaults).map(Number);
const themes = (defaults = THEMES) => list(process.env.THEMES || defaults);

async function profilesEnabled() {
    return (await fetch(base + '/editions', { headers: { Accept: 'text/html' } })).status !== 404;
}

// Admin's buttons render before its interactive circuit is ready; click until the expected control appears.
async function clickUntil(page, button, target, what) {
    for (let attempt = 0; attempt < 12 && !(await target.isVisible()); attempt++) {
        await button.click();
        try {
            await target.waitFor({ state: 'visible', timeout: 1500 });
        } catch {
            if (attempt === 11) throw new Error(`Admin ${what} did not become interactive`);
        }
    }
}

async function signInAdmin(
    browser,
    { returnPath = '/admin/profiles', viewport = { width: 1440, height: 1000 }, navigationTimeout } = {},
) {
    if (!adminPassword) throw new Error('ADMIN_PASSWORD is required to sign in to Admin');
    const page = await (await browser.newContext({ viewport })).newPage();
    if (navigationTimeout) page.setDefaultNavigationTimeout(navigationTimeout);
    await page.goto(adminBase + '/login?returnUrl=' + encodeURIComponent(returnPath));
    await page.locator('#password').fill(adminPassword);
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    await page.waitForURL('**' + returnPath);
    await page.waitForLoadState('networkidle');
    return page;
}

const created = [];

async function addProfile(admin, label, { pin = '3691', color } = {}) {
    const name = `zz ${label} ${Date.now().toString(36)}`;
    if (new URL(admin.url()).pathname !== '/admin/profiles') {
        await admin.goto(adminBase + '/admin/profiles');
        await admin.waitForLoadState('networkidle');
    }
    await clickUntil(
        admin,
        admin.getByRole('button', { name: 'Add profile', exact: true }).first(),
        admin.locator('#add-name'),
        'Add profile',
    );
    await admin.locator('#add-name').fill(name);
    if (color) await admin.locator('.adm-swatch', { hasText: color }).click();
    await admin.locator('#add-pin').fill(pin);
    await admin.locator('#add-confirm').fill(pin);
    await admin.locator('form[aria-labelledby="add-title"] button[type="submit"]').click();
    await admin.getByText(`Added ${name}.`).waitFor();
    const href = await admin.locator('.adm-table a.adm-row-title', { hasText: name }).getAttribute('href');
    const profile = { id: Number(href.match(/(\d+)$/)[1]), name, pin, manage: '/' + href.replace(/^\//, ''), admin };
    created.push(profile);
    return profile;
}

// Registers a `zz ` profile made some other way (the public setup flow) so deleteProfiles removes it too.
async function adoptProfile(admin, name) {
    if (!name.startsWith('zz ')) throw new Error(`Refusing to adopt "${name}": temporary profiles are named zz …`);
    await admin.goto(adminBase + '/admin/profiles');
    await admin.waitForLoadState('networkidle');
    const href = await admin.locator('.adm-table a.adm-row-title', { hasText: name }).getAttribute('href');
    const profile = { id: Number(href.match(/(\d+)$/)[1]), name, manage: '/' + href.replace(/^\//, ''), admin };
    created.push(profile);
    return profile;
}

async function openProfileIn(page, profile, route = '/', { waitUntil } = {}) {
    await page.goto(base + route, waitUntil ? { waitUntil } : undefined);
    await page.locator(`form[action="profiles/${profile.id}/open"]`).getByRole('button').click();
    await page.keyboard.type(profile.pin);
    await page.waitForURL((u) => u.pathname + u.search === route);
}

async function openProfile(browser, profile, { path: route = '/', waitUntil, viewport, settings } = {}) {
    const context = await browser.newContext({ viewport: viewport || { width: 390, height: 900 } });
    try {
        const page = await context.newPage();
        await openProfileIn(page, profile, route, { waitUntil });
        if (settings) await saveSettings(page, settings);
        return await context.storageState();
    } finally {
        await context.close();
    }
}

// Saves settings to the open profile (myNewspaperPrefs) and waits for the server to store them.
async function saveSettings(page, settings) {
    for (const [key, value] of Object.entries(settings)) {
        const saved = page.waitForResponse(
            (r) => new URL(r.url()).pathname === '/api/me/settings' && r.request().method() === 'PUT',
        );
        await page.evaluate(([k, v]) => window.myNewspaperPrefs.setItem(k, v), [key, value]);
        await (await saved).finished();
    }
}

/**
 * Sets a reader preference through the page's own window.myNewspaperPrefs, which works in both modes: with profiles
 * on it saves to the open profile (this waits for the server to store it), otherwise it writes localStorage.
 */
async function setPreference(page, key, value) {
    const profile = await page.evaluate(() => !!window.myNewspaperPrefs?.profile);
    const saved = profile
        ? page.waitForResponse(
              (r) => new URL(r.url()).pathname === '/api/me/settings' && r.request().method() === 'PUT',
          )
        : null;
    await page.evaluate(([k, v]) => window.myNewspaperPrefs.setItem(k, v), [key, value]);
    if (saved) await (await saved).finished();
}

/** Reads a reader preference the same way (the profile's value with profiles on, localStorage otherwise). */
function getPreference(page, key) {
    return page.evaluate((k) => window.myNewspaperPrefs.getItem(k), key);
}

// The reader follows the device's light or dark setting, so checks steer it with emulateMedia/colorScheme as they
// did before profiles; a profile's own saved theme would otherwise win.
const readerSettings = { theme: 'system' };

async function deleteProfile(profile) {
    if (!/^zz /.test(profile.name)) throw new Error(`Refusing to delete "${profile.name}": not a zz profile`);
    const admin = profile.admin;
    await admin.setViewportSize({ width: 1440, height: 1000 });
    await admin.goto(adminBase + profile.manage);
    await admin.waitForLoadState('networkidle');
    // The page's own name, not the one we were given: never delete a household profile by a wrong id. A bare
    // "Manage profile" means Admin has no such profile: already deleted.
    if ((await admin.title()).trim() === 'Manage profile') return forget(profile);
    if (!/Manage zz /.test(await admin.title()))
        throw new Error(
            `Refusing to delete profile ${profile.id}: Admin calls it "${await admin.title()}", not a zz profile`,
        );
    const confirm = admin.getByRole('button', { name: 'Delete profile', exact: true });
    await clickUntil(admin, admin.getByRole('button', { name: 'Delete profile…' }), confirm, 'Delete profile');
    await confirm.click();
    await admin.waitForURL('**/admin/profiles?deleted=*');
    forget(profile);
}

function forget(profile) {
    const index = created.indexOf(profile);
    if (index >= 0) created.splice(index, 1);
}

// Deletes every profile addProfile created, newest first. A failure is reported and fails the check; the zz
// clean-up on exit still removes what's left.
async function deleteProfiles() {
    for (const profile of [...created].reverse()) {
        try {
            await deleteProfile(profile);
        } catch (e) {
            process.exitCode = 1;
            console.error(
                `Could not delete ${profile.name} (${profile.id}) through Admin: ${e.message.split('\n')[0]}`,
            );
        }
    }
}

// A reader session: with profiles on, every page sends a browser without a profile to the edition picker, so checks
// that read pages as a visitor use a context signed in as one throwaway `zz Reader` profile. Under run.cjs the runner
// creates it once and passes its storage state in READER_STATE; a check run on its own creates its own and deletes it
// through Admin when it exits (`node lib.cjs --delete-profile <id>`, synchronously, however the check exits).
let readerState;
const readerProfiles = [];

// Runs when a check exits, before the zz clean-up: deletes the reader profile through Admin.
function deleteReaderProfiles() {
    for (const profile of readerProfiles) {
        try {
            execFileSync(process.execPath, [__filename, '--delete-profile', String(profile.id)], { stdio: 'inherit' });
        } catch {
            console.error(
                `Could not delete ${profile.name} (${profile.id}) through Admin; the zz clean-up removes it.`,
            );
            if (!process.exitCode) process.exitCode = 1;
        }
    }
}
async function readerStorageState(browser) {
    if (process.env.READER_STATE) {
        // The run's shared reader: put its settings back to the defaults first, whatever the last check changed.
        if (readerState === undefined) {
            await resetReaderSettings(process.env.READER_STATE);
            readerState = process.env.READER_STATE;
        }
        return readerState;
    }
    if (readerState !== undefined) return readerState;
    if (!(await profilesEnabled())) return (readerState = null);
    const admin = await signInAdmin(browser);
    const profile = await addProfile(admin, 'Reader');
    created.splice(created.indexOf(profile), 1); // not the check's own profile: deleted on exit, after its browser closes
    readerState = await openProfile(browser, profile, { path: '/', settings: readerSettings });
    await admin.context().close();
    readerProfiles.push(profile);
    return readerState;
}

// Every saved setting removed (PUT /api/me/settings is a merge-patch where null removes a key), then readerSettings.
async function resetReaderSettings(storageState) {
    const api = await request.newContext({ baseURL: base, storageState });
    try {
        const response = await api.get('/api/me', { headers: { Accept: 'application/json' } });
        if (!response.ok()) throw new Error(`Reader profile: GET /api/me answered ${response.status()}`);
        const me = await response.json();
        const patch = {
            ...Object.fromEntries(Object.keys(me.settings || {}).map((k) => [k, null])),
            ...readerSettings,
        };
        const saved = await api.put('/api/me/settings', {
            headers: { 'X-Saved-CSRF': me.token, 'Content-Type': 'application/json' },
            data: patch,
        });
        if (!saved.ok()) throw new Error(`Reader profile: resetting settings answered ${saved.status()}`);
    } finally {
        await api.dispose();
    }
}

/** A browser context that reads the site as a visitor would: signed in as the reader profile when profiles are on. */
async function readerContext(browser, options = {}) {
    const storageState = options.storageState ?? (await readerStorageState(browser)) ?? undefined;
    return browser.newContext({ ...options, storageState });
}

/** A page in its own reader context (closing the browser closes both). */
async function readerPage(browser, options = {}) {
    return (await readerContext(browser, options)).newPage();
}

// Creates the reader profile for a whole run (run.cjs), writing its storage state to `file`; returns its id.
async function createReaderProfile(file) {
    const browser = await chromium.launch();
    try {
        if (!(await profilesEnabled())) return null;
        const admin = await signInAdmin(browser);
        const profile = await addProfile(admin, 'Reader');
        created.length = 0;
        fs.writeFileSync(
            file,
            JSON.stringify(await openProfile(browser, profile, { path: '/', settings: readerSettings })),
        );
        return profile.id;
    } finally {
        await browser.close();
    }
}

// Deletes a profile through Admin by id (used for the reader profile, after the browser that made it is gone).
async function deleteProfileById(id) {
    const browser = await chromium.launch();
    try {
        const admin = await signInAdmin(browser);
        await deleteProfile({ id, name: 'zz Reader', manage: '/admin/profiles/' + id, admin });
    } finally {
        await browser.close();
    }
}

async function run(main, launchOptions) {
    const browser = await chromium.launch(launchOptions);
    try {
        await main(browser);
    } catch (e) {
        process.exitCode = 1;
        console.error(e);
    } finally {
        await deleteProfiles();
        await browser.close();
    }
}

// The database behind `site`, for the zz clean-up: CLEANUP_DB, else WebScraper for the live site, else unknown.
function cleanupDatabase(site = base) {
    if (process.env.CLEANUP_DB) return process.env.CLEANUP_DB === 'none' ? null : process.env.CLEANUP_DB;
    const live = envFile('PUBLIC_BASE_URL');
    return live && trim(live) === site ? 'WebScraper' : undefined;
}

// Runs tools/sql/cleanup-zz.sh for a run that started at `start` (UTC). Returns { ok, ran, text }: ok is false when it
// failed or couldn't run because the database is unknown.
function cleanupZz(start, site = base) {
    const db = cleanupDatabase(site);
    if (db === null) return { ok: true, ran: false, text: 'zz clean-up skipped (CLEANUP_DB=none).' };
    if (db === undefined)
        return {
            ok: false,
            ran: false,
            text:
                `zz clean-up NOT run: set CLEANUP_DB to the database behind ${site} (WebScraper or WebScraper_Test), ` +
                `or run: tools/sql/cleanup-zz.sh ${start} --db <database> --apply`,
        };
    try {
        const out = execFileSync(path.join(root, 'tools/sql/cleanup-zz.sh'), [start, '--db', db, '--apply'], {
            cwd: root,
            stdio: ['ignore', 'pipe', 'pipe'],
        });
        return { ok: true, ran: true, text: `zz clean-up in ${db} since ${start}:\n${out.toString().trim()}` };
    } catch (e) {
        const text = ((e.stdout || '').toString() + (e.stderr || '').toString()).trim() || e.message;
        return { ok: false, ran: true, text: `zz clean-up in ${db} FAILED:\n${text}` };
    }
}

// A check run on its own cleans up when it exits, however it exits; under run.cjs (BROWSER_SUITE_START) the runner
// does it once at the end. Either way an unknown database is a warning; a clean-up that ran and failed fails the run.
if (require.main === module && process.argv[2] === '--delete-profile') {
    deleteProfileById(Number(process.argv[3])).then(
        () => process.exit(0),
        (e) => {
            console.error(e.message);
            process.exit(1);
        },
    );
} else if (require.main && require.main.filename.endsWith('.browser.cjs') && !process.env.BROWSER_SUITE_START) {
    process.on('exit', () => {
        deleteReaderProfiles();
        const result = cleanupZz(started);
        (result.ok ? console.log : console.error)(result.text);
        if (result.ran && !result.ok && !process.exitCode) process.exitCode = 1;
    });
}

module.exports = {
    base,
    adminBase,
    adminPassword,
    shotDir,
    screenshots,
    PHONE_WIDTHS,
    TABLET_WIDTH,
    DESKTOP_WIDTH,
    WIDTHS,
    THEMES,
    widths,
    themes,
    profilesEnabled,
    signInAdmin,
    addProfile,
    openProfile,
    openProfileIn,
    adoptProfile,
    deleteProfiles,
    readerContext,
    readerPage,
    setPreference,
    getPreference,
    createReaderProfile,
    deleteProfileById,
    run,
    cleanupDatabase,
    cleanupZz,
    started,
};
