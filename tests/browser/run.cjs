// Runs every *.browser.cjs check against one running site and reports a summary.
//
//   BASE_URL=http://localhost:5295 npm run test:browser            all checks
//   BASE_URL=... npm run test:browser -- watch scoreboard            checks whose file name contains a filter
//   JOBS=2 BASE_URL=... npm run test:browser                         run checks in parallel (default 1; heavy checks time out when crowded)
//   CLEANUP_DB=WebScraper_Test BASE_URL=... npm run test:browser      the database behind the site, for the clean-up
//
// Each check is a standalone Node script that exits non-zero on failure; output is kept in test-results/browser/.
// Settings (BASE_URL, ADMIN_BASE_URL, ADMIN_PASSWORD, SCREENSHOT_DIR, WIDTHS, THEMES) are read by lib.cjs. At the end
// the runner removes `zz ` profiles and the unassigned devices created since it started (lib.cjs, cleanupZz) and
// fails the run only if that clean-up ran and failed (or the household check failed). Without CLEANUP_DB, away from the
// live site, it warns and prints the command to run instead.
const { spawn } = require('node:child_process');
const lib = require('./lib.cjs');
const fs = require('node:fs');
const path = require('node:path');

const here = __dirname;
const outDir = path.resolve(here, '../../test-results/browser');
const filters = process.argv.slice(2);
const jobs = Math.max(1, Number(process.env.JOBS) || 1);
const base = lib.base;
// UTC, to the second, before any check touches the site; checks see it and leave the clean-up to the runner.
const start = new Date().toISOString().slice(0, 19);

const files = fs
    .readdirSync(here)
    .filter((f) => f.endsWith('.browser.cjs'))
    .filter((f) => filters.length === 0 || filters.some((x) => f.includes(x)))
    .sort();

fs.mkdirSync(outDir, { recursive: true });

const checkTimeout = Number(process.env.CHECK_TIMEOUT) || 600;

function run(file) {
    return new Promise((resolve) => {
        const started = Date.now();
        const log = fs.createWriteStream(path.join(outDir, file.replace('.browser.cjs', '.log')));
        const child = spawn(process.execPath, [path.join(here, file)], {
            env: {
                ...process.env,
                BASE_URL: base,
                SCREENSHOT_DIR: process.env.SCREENSHOT_DIR || outDir + '/',
                BROWSER_SUITE_START: start,
                ...(readerId ? { READER_STATE: readerFile } : {}),
            },
            stdio: ['ignore', 'pipe', 'pipe'],
        });
        let tail = '';
        const keep = (chunk) => {
            log.write(chunk);
            tail = (tail + chunk).slice(-2000);
        };
        child.stdout.on('data', keep);
        child.stderr.on('data', keep);
        // A check that hangs fails after CHECK_TIMEOUT seconds (default 600) instead of stalling the run. Anything it
        // created is removed by the zz clean-up at the end of the run.
        let timedOut = false;
        const limit = setTimeout(() => {
            timedOut = true;
            keep(`\nTIMED OUT after ${checkTimeout} s\n`);
            child.kill('SIGTERM');
            setTimeout(() => child.kill('SIGKILL'), 20000).unref();
        }, checkTimeout * 1000);
        child.on('close', (code) => {
            clearTimeout(limit);
            if (timedOut && code === 0) code = 1;
            log.end();
            const skipped = code === 0 && /^SKIP\b/m.test(tail);
            resolve({ file, code, skipped, seconds: (Date.now() - started) / 1000, tail });
        });
    });
}

// With profiles on, one throwaway reader profile for the whole run (lib.readerContext); deleted through Admin at the end.
const readerFile = path.join(outDir, 'reader-state.json');
let readerId = null;

(async () => {
    fs.rmSync(readerFile, { force: true });
    if (lib.adminPassword && (await lib.profilesEnabled())) {
        readerId = await lib.createReaderProfile(readerFile);
        console.log(`Reader profile ${readerId} for checks that read pages as a visitor`);
    }
    console.log(`Running ${files.length} browser checks against ${base} (${jobs} at a time)`);
    const queue = [...files];
    const results = [];
    await Promise.all(
        Array.from({ length: jobs }, async () => {
            while (queue.length) {
                const result = await run(queue.shift());
                results.push(result);
                const status = result.skipped ? 'SKIP' : result.code === 0 ? 'PASS' : 'FAIL';
                console.log(`${status}  ${result.file}  (${result.seconds.toFixed(1)}s)`);
            }
        }),
    );
    const failed = results.filter((r) => r.code !== 0);
    for (const r of failed) console.log(`\n--- ${r.file} ---\n${r.tail.trim().split('\n').slice(-12).join('\n')}`);
    const skipped = results.filter((r) => r.skipped).length;
    console.log(
        `\n${results.length - failed.length - skipped} passed, ${failed.length} failed, ${skipped} skipped; logs in ${outDir}`,
    );
    if (readerId) {
        try {
            await lib.deleteProfileById(readerId);
        } catch (e) {
            console.log(`Could not delete reader profile ${readerId} through Admin: ${e.message.split('\n')[0]}`);
        }
        fs.rmSync(readerFile, { force: true });
    }
    const cleanup = lib.cleanupZz(start);
    console.log('\n' + cleanup.text);
    process.exitCode = failed.length || (cleanup.ran && !cleanup.ok) ? 1 : 0;
})();
