// Proves a refactor changes nothing visible: runs two builds side by side against WebScraper_Test and compares the
// rendered HTML of the public site and every admin page (coding-standards.md, Releases).
//
//   node tools/compare-builds.cjs <old-ref> [<new-ref>]     new defaults to the working tree, uncommitted changes included
//   node tools/compare-builds.cjs HEAD~3 --public-only      skip Admin
//   MAX_PAGES=400 node tools/compare-builds.cjs main        public pages to crawl (default 300)
//
// Steps: a git worktree for each ref (the working tree is used as is), `dotnet publish` of Web and Admin, four
// servers on 127.0.0.1 (public on 5401/5402 with profiles and background jobs off; admin on 5411/5412 over HTTPS with
// a throwaway certificate, because its session cookie is Secure), then a crawl. Public pages: the seed list plus
// same-site links found on them, fetched from both builds back to back. Admin: every list route plus the first edit
// page linked from each, compared as main.innerHTML after the interactive render settles. Volatile markup
// (antiforgery tokens, Blazor comments and state, _bl_/b- attributes, ports, countdowns) is normalised first.
//
// Writes each differing page's two normalised versions to test-results/compare/ and exits 1 if any page differs.
// Servers, worktrees and temporary files are removed in teardown, also on failure or Ctrl+C.
const { spawn, execFileSync } = require('node:child_process');
const crypto = require('node:crypto');
const fs = require('node:fs');
const https = require('node:https');
const os = require('node:os');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const outDir = path.join(root, 'test-results/compare');
const maxPages = Number(process.env.MAX_PAGES) || 300;

const publicSeeds = [
    '/',
    '/news',
    '/news/all',
    '/news/kansas-city',
    '/sports',
    '/sports/news',
    '/sports/teams',
    '/scoreboard',
    '/college',
    '/college/rankings',
    '/college/results',
    '/college/standings',
    '/standings',
    '/events',
    '/championship/results',
    '/mls/results',
    '/f1',
    '/f1/results',
    '/f1/standings',
    '/entertainment',
    '/entertainment/news',
    '/entertainment/tv-film',
    '/movies',
    '/movies/digital',
    '/box-office',
    '/box-office/movie-forecasts',
    '/box-office/weekend-preview',
    '/tv',
    '/watch',
    '/videos',
    '/tech',
    '/tech/news',
    '/tech/linux',
    '/tech/linux/releases',
    '/tech/technology',
    '/tech/video-games',
    '/tech/week',
    '/not-found',
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
// Links not worth following: endpoints, files, auth and pages that change state.
const skipLink =
    /^\/(api|feed\/more|healthz|login|logout|editions|requests|saved|settings|profiles|_|css|js|img|icons|lib)\b|\.(png|jpe?g|svg|ico|webp|css|js|json|xml|txt|webmanifest)$/i;

// Static assets carry a content fingerprint (broadsheet.zjkjitnphg.css). A changed asset is listed separately: its
// effect is visual, which this HTML comparison can't judge.
const fingerprint = /([\w-]+)\.([a-z0-9]{10})\.(css|js|mjs)\b/g;
const assets = (html) => new Set([...html.matchAll(fingerprint)].map((m) => m[0]));

function normalise(html, port) {
    return html
        .replace(fingerprint, '$1.HASH.$3')
        .replace(/CfDJ8[\w-]+/g, 'CfDJ8…')
        .replace(/<!--Blazor[\s\S]*?-->/g, '')
        .replace(/\s_bl_[0-9a-f-]{8,}(="[^"]*")?/g, '')
        .replace(/\sb-[a-z0-9]{10}(="")?/g, '')
        .replace(/\sdata-(lead|rotation)-refresh-after="[^"]*"/g, '')
        .replaceAll(`127.0.0.1:${port}`, 'HOST')
        .replaceAll(`localhost:${port}`, 'HOST');
}

const run = (cmd, args, opts = {}) => execFileSync(cmd, args, { cwd: root, stdio: 'pipe', ...opts }).toString();

function connectionString() {
    let conn = process.env.ConnectionStrings__DefaultConnection;
    if (!conn) {
        const line = fs
            .readFileSync(path.join(root, '.env'), 'utf8')
            .split('\n')
            .find((l) => l.startsWith('SQL_CONNECTION='));
        if (!line) throw new Error('Set ConnectionStrings__DefaultConnection or SQL_CONNECTION in .env');
        conn = line.slice('SQL_CONNECTION='.length).replace(/^['"]|['"]$/g, '');
    }
    // Always the test copy, never the live database.
    const parts = conn.split(';').filter((p) => p && !/^\s*(database|initial catalog)\s*=/i.test(p));
    return [...parts, 'Database=WebScraper_Test'].join(';');
}

// Status of GET url; the local admin servers use a throwaway certificate, accepted for this request only.
function status(url) {
    if (url.startsWith('http:')) return fetch(url).then((r) => r.status);
    return new Promise((resolve, reject) =>
        https.get(url, { rejectUnauthorized: false }, (r) => resolve(r.resume().statusCode)).on('error', reject),
    );
}

async function waitHealthy(url, child) {
    for (let i = 0; i < 120; i++) {
        if (child.exitCode !== null) throw new Error(`${url} exited early (${child.exitCode}); see ${child.logFile}`);
        try {
            if ((await status(url + '/healthz')) === 200) return;
        } catch {}
        await new Promise((r) => setTimeout(r, 1000));
    }
    throw new Error(`${url} did not become healthy; see ${child.logFile}`);
}

function startServer(dll, env, logFile) {
    const log = fs.openSync(logFile, 'w');
    const child = spawn('dotnet', [dll], {
        cwd: path.dirname(dll),
        env: { ...process.env, ...env },
        stdio: ['ignore', log, log],
    });
    child.logFile = logFile;
    return child;
}

function firstDifference(a, b) {
    const la = a.replace(/>\s*</g, '>\n<').split('\n');
    const lb = b.replace(/>\s*</g, '>\n<').split('\n');
    let i = 0;
    while (i < la.length && i < lb.length && la[i] === lb[i]) i++;
    return { line: i + 1, old: (la[i] || '').slice(0, 200), new: (lb[i] || '').slice(0, 200) };
}

async function main() {
    const args = process.argv.slice(2);
    const publicOnly = args.includes('--public-only');
    const refs = args.filter((a) => !a.startsWith('--'));
    if (refs.length < 1 || refs.length > 2) {
        console.error('usage: node tools/compare-builds.cjs <old-ref> [<new-ref>] [--public-only]');
        process.exit(2);
    }
    const temp = fs.mkdtempSync(path.join(os.tmpdir(), 'compare-builds-'));
    const servers = [];
    const worktrees = [];
    let browser = null;
    const cleanup = () => {
        for (const s of servers) if (s.exitCode === null) s.kill('SIGTERM');
        for (const w of worktrees) {
            try {
                run('git', ['worktree', 'remove', '--force', w]);
            } catch {}
        }
        fs.rmSync(temp, { recursive: true, force: true });
    };
    process.on('SIGINT', () => {
        cleanup();
        process.exit(130);
    });

    try {
        fs.rmSync(outDir, { recursive: true, force: true });
        fs.mkdirSync(outDir, { recursive: true });

        // Trees: a worktree per ref; the working tree when the new ref is omitted.
        const trees = refs.map((ref, i) => {
            const sha = run('git', ['rev-parse', '--short', ref + '^{commit}']).trim();
            const dir = path.join(temp, i === 0 ? 'old' : 'new');
            run('git', ['worktree', 'add', '--detach', dir, sha]);
            worktrees.push(dir);
            return { label: `${ref} (${sha})`, dir };
        });
        if (trees.length === 1) trees.push({ label: 'working tree', dir: root });
        console.log(`Comparing ${trees[0].label} with ${trees[1].label}`);

        for (const [i, tree] of trees.entries()) {
            const side = i === 0 ? 'old' : 'new';
            for (const app of publicOnly ? ['Web'] : ['Web', 'Admin']) {
                process.stdout.write(`publish ${side} ${app}… `);
                run(
                    'dotnet',
                    [
                        'publish',
                        `src/myNewspaper.${app}`,
                        '-c',
                        'Release',
                        '-o',
                        path.join(temp, `${side}-${app}`),
                        '-nologo',
                    ],
                    { cwd: tree.dir, maxBuffer: 64 * 1024 * 1024 },
                );
                console.log('done');
            }
        }

        const conn = connectionString();
        const common = { ConnectionStrings__DefaultConnection: conn, ASPNETCORE_ENVIRONMENT: 'Production' };
        const ports = { old: 5401, new: 5402, oldAdmin: 5411, newAdmin: 5412 };
        for (const side of ['old', 'new']) {
            servers.push(
                startServer(
                    path.join(temp, `${side}-Web`, 'myNewspaper.Web.dll'),
                    {
                        ...common,
                        BackgroundJobs__Enabled: 'false',
                        Profiles__Enabled: 'false',
                        DataProtection__KeysPath: path.join(temp, `${side}-web-keys`),
                        ASPNETCORE_URLS: `http://127.0.0.1:${ports[side]}`,
                    },
                    path.join(outDir, `${side}-web.log`),
                ),
            );
        }

        // Admin needs HTTPS: a throwaway certificate and a local-only password.
        let adminPassword = null;
        if (!publicOnly) {
            const key = path.join(temp, 'cert.key');
            const crt = path.join(temp, 'cert.crt');
            const pfx = path.join(temp, 'cert.pfx');
            const pfxPassword = crypto.randomBytes(12).toString('hex');
            run('openssl', [
                'req',
                '-x509',
                '-newkey',
                'rsa:2048',
                '-nodes',
                '-days',
                '1',
                '-subj',
                '/CN=127.0.0.1',
                '-addext',
                'subjectAltName=IP:127.0.0.1',
                '-keyout',
                key,
                '-out',
                crt,
            ]);
            run(
                'openssl',
                ['pkcs12', '-export', '-inkey', key, '-in', crt, '-out', pfx, '-passout', 'env:PFX_PASSWORD'],
                {
                    env: { ...process.env, PFX_PASSWORD: pfxPassword },
                },
            );
            adminPassword = crypto.randomBytes(18).toString('base64url');
            for (const side of ['old', 'new']) {
                servers.push(
                    startServer(
                        path.join(temp, `${side}-Admin`, 'myNewspaper.Admin.dll'),
                        {
                            ...common,
                            Admin__Password: adminPassword,
                            Profiles__Enabled: 'true',
                            DataProtection__KeysPath: path.join(temp, `${side}-admin-keys`),
                            ASPNETCORE_URLS: `https://127.0.0.1:${ports[side + 'Admin']}`,
                            ASPNETCORE_Kestrel__Certificates__Default__Path: pfx,
                            ASPNETCORE_Kestrel__Certificates__Default__Password: pfxPassword,
                        },
                        path.join(outDir, `${side}-admin.log`),
                    ),
                );
            }
        }

        await Promise.all([
            waitHealthy(`http://127.0.0.1:${ports.old}`, servers[0]),
            waitHealthy(`http://127.0.0.1:${ports.new}`, servers[1]),
            ...(publicOnly
                ? []
                : [
                      waitHealthy(`https://127.0.0.1:${ports.oldAdmin}`, servers[2]),
                      waitHealthy(`https://127.0.0.1:${ports.newAdmin}`, servers[3]),
                  ]),
        ]);
        console.log('servers healthy');

        const differences = [];
        const changedAssets = new Set();
        const record = (kind, route, oldHtml, newHtml) => {
            const slug = (kind + route).replace(/[^a-z0-9]+/gi, '-').slice(0, 120);
            fs.writeFileSync(path.join(outDir, `${slug}.old.html`), oldHtml);
            fs.writeFileSync(path.join(outDir, `${slug}.new.html`), newHtml);
            differences.push({ kind, route, ...firstDifference(oldHtml, newHtml) });
        };

        // Public: crawl from the seeds on the old build, compare each page with the new one.
        const queue = [...publicSeeds];
        const seen = new Set(queue);
        let compared = 0;
        const get = async (port, route) => {
            const r = await fetch(`http://127.0.0.1:${port}${route}`, {
                headers: { Accept: 'text/html' },
                redirect: 'manual',
            });
            return { status: r.status, location: r.headers.get('location') || '', html: await r.text() };
        };
        while (queue.length > 0 && compared < maxPages) {
            const route = queue.shift();
            const [a, b] = await Promise.all([get(ports.old, route), get(ports.new, route)]);
            compared++;
            const oldAssets = assets(a.html);
            for (const asset of assets(b.html))
                if (!oldAssets.has(asset)) changedAssets.add(asset.replace(fingerprint, '$1.$3'));
            const oldHtml =
                `${a.status} ${a.location.replaceAll(String(ports.old), 'PORT')}\n` + normalise(a.html, ports.old);
            const newHtml =
                `${b.status} ${b.location.replaceAll(String(ports.new), 'PORT')}\n` + normalise(b.html, ports.new);
            if (oldHtml !== newHtml) record('public', route, oldHtml, newHtml);
            for (const m of a.html.matchAll(/href="([^"#]*)"/g)) {
                let link = m[1].replace(/&amp;/g, '&');
                if (/^https?:/i.test(link)) continue;
                if (!link.startsWith('/')) link = '/' + link;
                if (!skipLink.test(link) && !seen.has(link)) {
                    seen.add(link);
                    queue.push(link);
                }
            }
        }
        console.log(`public: ${compared} pages compared (${seen.size} found, cap ${maxPages})`);

        // Admin: sign in to both, then every route plus the first edit page each list links to.
        let adminCompared = 0;
        if (!publicOnly) {
            const { chromium } = require('playwright');
            browser = await chromium.launch();
            const pages = {};
            for (const side of ['old', 'new']) {
                const context = await browser.newContext({
                    ignoreHTTPSErrors: true,
                    viewport: { width: 1440, height: 1000 },
                });
                const p = await context.newPage();
                const base = `https://127.0.0.1:${ports[side + 'Admin']}`;
                await p.goto(base + '/login?returnUrl=%2Fadmin');
                await p.locator('#password').fill(adminPassword);
                await p.getByRole('button', { name: 'Sign in', exact: true }).click();
                await p.waitForURL('**/admin');
                pages[side] = { p, base, port: ports[side + 'Admin'] };
            }
            const render = async ({ p, base, port }, route) => {
                await p.goto(base + route);
                await p.waitForLoadState('networkidle');
                await p.waitForTimeout(500);
                const main = p.locator('main');
                return normalise((await main.count()) ? await main.first().innerHTML() : await p.content(), port);
            };
            const routes = [...adminRoutes];
            for (let i = 0; i < routes.length; i++) {
                const route = routes[i];
                const [oldHtml, newHtml] = [await render(pages.old, route), await render(pages.new, route)];
                adminCompared++;
                if (oldHtml !== newHtml) record('admin', route, oldHtml, newHtml);
                if (adminRoutes.includes(route)) {
                    const edit = await pages.old.p
                        .locator(
                            'main a[href*="/edit"], main a[href^="admin/profiles/"], main a[href^="/admin/profiles/"]',
                        )
                        .evaluateAll((ns) => ns.map((n) => n.getAttribute('href')).find((h) => /\d/.test(h)));
                    if (edit) {
                        const link = '/' + edit.replace(/^\//, '');
                        if (!routes.includes(link)) routes.push(link);
                    }
                }
            }
            console.log(`admin: ${adminCompared} pages compared`);
        }

        console.log('');
        if (changedAssets.size > 0) {
            console.log(`note: changed assets (check them visually): ${[...changedAssets].sort().join(', ')}`);
        }
        if (differences.length === 0) {
            console.log(`compare-builds: identical (${compared} public, ${adminCompared} admin pages)`);
            return 0;
        }
        console.log(
            `compare-builds: ${differences.length} page(s) differ; normalised copies in ${path.relative(root, outDir)}/`,
        );
        for (const d of differences.slice(0, 20)) {
            console.log(`  ${d.kind} ${d.route}  line ${d.line}\n    old: ${d.old}\n    new: ${d.new}`);
        }
        return 1;
    } finally {
        if (browser) await browser.close();
        cleanup();
    }
}

main().then(
    (code) => process.exit(code),
    (e) => {
        console.error(e.message || e);
        process.exit(1);
    },
);
