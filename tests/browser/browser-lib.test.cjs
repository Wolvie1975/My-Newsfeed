// Guard for the shared browser-check helper (docs/coding-standards.md, Tests; guardrails table): every *.browser.cjs
// uses tests/browser/lib.cjs for its addresses, admin password, screenshots and widths, and for temporary profiles,
// so the clean-up in lib.cjs covers every check. Runs with `npm run test:unit` (and so tools/verify.sh).
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const here = __dirname;
const checks = fs
    .readdirSync(here)
    .filter((f) => f.endsWith('.browser.cjs'))
    .sort()
    .map((f) => ({ file: f, text: fs.readFileSync(path.join(here, f), 'utf8') }));

// admin-profiles tests Admin's Add profile and Delete screens themselves; its teardown still goes through lib.cjs.
const drivesProfileScreens = new Set(['admin-profiles.browser.cjs']);

const offenders = (predicate) => checks.filter(predicate).map((c) => c.file);

test('there are browser checks to guard', () => {
    assert(checks.length > 50, `found only ${checks.length} *.browser.cjs files`);
});

test('every browser check requires lib.cjs', () => {
    assert.deepEqual(
        offenders((c) => !c.text.includes("require('./lib.cjs')")),
        [],
    );
});

test('no browser check reads its own address, password, screenshot or width settings', () => {
    const own = /process\.env\.(BASE_URL|ADMIN_BASE_URL|ADMIN_PASSWORD|SCREENSHOT_DIR|SHOTS|WIDTHS|THEMES)\b/;
    assert.deepEqual(
        offenders((c) => own.test(c.text)),
        [],
    );
});

test('no browser check hard-codes a site address', () => {
    const address = /https?:\/\/(localhost|127\.0\.0\.1|172\.\d+\.\d+\.\d+|[\w.-]+\.ts\.net)/;
    assert.deepEqual(
        offenders((c) => address.test(c.text)),
        [],
    );
});

test('temporary profiles are added and deleted through lib.cjs', () => {
    const own = /'Add profile'|Delete profile…|#add-name/;
    assert.deepEqual(
        offenders((c) => !drivesProfileScreens.has(c.file) && own.test(c.text)),
        [],
    );
    const unmanaged = /'zz [^']*' \+ Date\.now|`zz \$\{/;
    assert.deepEqual(
        offenders((c) => unmanaged.test(c.text) && !c.text.includes('lib.adoptProfile')),
        [],
        'a check that names a zz profile itself must hand it to lib.adoptProfile for teardown',
    );
});

test('a check that adds profiles deletes them in teardown', () => {
    assert.deepEqual(
        offenders(
            (c) =>
                /lib\.(addProfile|adoptProfile)\(/.test(c.text) &&
                !/lib\.deleteProfiles\(\)/.test(c.text) &&
                !/lib\.run\(/.test(c.text),
        ),
        [],
    );
});

test('the zz clean-up only assumes the live database for the live site', () => {
    const lib = require('./lib.cjs');
    const saved = process.env.CLEANUP_DB;
    try {
        delete process.env.CLEANUP_DB;
        assert.equal(lib.cleanupDatabase('http://localhost:5295'), undefined, 'a local site needs CLEANUP_DB');
        process.env.CLEANUP_DB = 'WebScraper_Test';
        assert.equal(lib.cleanupDatabase('http://localhost:5295'), 'WebScraper_Test');
        process.env.CLEANUP_DB = 'none';
        assert.equal(lib.cleanupDatabase('http://localhost:5295'), null);
        const result = lib.cleanupZz('2026-11-05T00:00:00', 'http://localhost:5295');
        assert.deepEqual([result.ok, result.ran], [true, false]);
        delete process.env.CLEANUP_DB;
        const unknown = lib.cleanupZz('2026-11-05T00:00:00', 'http://localhost:5295');
        assert.deepEqual([unknown.ok, unknown.ran], [false, false]);
        assert.match(unknown.text, /CLEANUP_DB/);
    } finally {
        if (saved === undefined) delete process.env.CLEANUP_DB;
        else process.env.CLEANUP_DB = saved;
    }
});
