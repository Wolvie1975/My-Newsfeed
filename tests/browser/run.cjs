// Runs every *.browser.cjs check against one running site and reports a summary.
//
//   BASE_URL=http://localhost:5295 npm run test:browser            all checks
//   BASE_URL=... npm run test:browser -- watch scoreboard            checks whose file name contains a filter
//   JOBS=2 BASE_URL=... npm run test:browser                         run checks in parallel (default 1; heavy checks time out when crowded)
//
// Each check is a standalone Node script that exits non-zero on failure; output is kept in test-results/browser/.
const { spawn } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');

const here = __dirname;
const outDir = path.resolve(here, '../../test-results/browser');
const filters = process.argv.slice(2);
const jobs = Math.max(1, Number(process.env.JOBS) || 1);
const base = process.env.BASE_URL || 'http://localhost:5295';

const files = fs
    .readdirSync(here)
    .filter((f) => f.endsWith('.browser.cjs'))
    .filter((f) => filters.length === 0 || filters.some((x) => f.includes(x)))
    .sort();

fs.mkdirSync(outDir, { recursive: true });

function run(file) {
    return new Promise((resolve) => {
        const started = Date.now();
        const log = fs.createWriteStream(path.join(outDir, file.replace('.browser.cjs', '.log')));
        const child = spawn(process.execPath, [path.join(here, file)], {
            env: { ...process.env, BASE_URL: base, SCREENSHOT_DIR: process.env.SCREENSHOT_DIR || outDir + '/' },
            stdio: ['ignore', 'pipe', 'pipe'],
        });
        let tail = '';
        const keep = (chunk) => {
            log.write(chunk);
            tail = (tail + chunk).slice(-2000);
        };
        child.stdout.on('data', keep);
        child.stderr.on('data', keep);
        child.on('close', (code) => {
            log.end();
            const skipped = code === 0 && tail.startsWith('SKIP');
            resolve({ file, code, skipped, seconds: (Date.now() - started) / 1000, tail });
        });
    });
}

(async () => {
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
    process.exitCode = failed.length ? 1 : 0;
})();
