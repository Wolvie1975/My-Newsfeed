// Your topics (docs/news-section-handoff.md, phase 5): following a phrase with the live preview and its three verdicts,
// Except, the topic page (New and Earlier, Share and Save, Mark as read, Edit, Stop following), the News front's box
// and phone fold, another profile getting 404, and the form without JavaScript; 320/390/430 and 1440 light and dark.
//
// Topics belong to a profile, so this needs profiles on and ADMIN_PASSWORD: it adds two throwaway profiles and deletes
// them (and with them their topics) at the end.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { audit } = require('./contrast-audit.cjs');

const base = process.env.BASE_URL || 'http://localhost:5295';
const adminBase = process.env.ADMIN_BASE_URL || base;
const shots = process.env.SCREENSHOT_DIR;
const password = process.env.ADMIN_PASSWORD;

(async () => {
    const profiles = (await fetch(base + '/editions', { headers: { Accept: 'text/html' } })).status !== 404;
    if (!profiles) {
        // With profiles off, topics don't exist.
        assert.equal((await fetch(base + '/news/topics/follow')).status, 404);
        console.log(
            'Profiles are off here: /news/topics answers 404. SKIP the rest (needs profiles on and ADMIN_PASSWORD).',
        );
        return;
    }
    if (!password) {
        console.log('SKIP: set ADMIN_PASSWORD so the check can add throwaway profiles.');
        return;
    }
    const browser = await chromium.launch();
    const errors = [];
    const ids = [];
    let admin = null;
    const tag = Date.now().toString(36);
    // Adds a profile in Admin and opens it on a new browser; returns that browser's saved state.
    const openProfile = async (label) => {
        const name = `zz ${label} ${tag}`;
        await admin.goto(adminBase + '/admin/profiles', { waitUntil: 'networkidle' });
        await admin.getByRole('button', { name: 'Add profile', exact: true }).first().click();
        await admin.locator('#add-name').fill(name);
        await admin.locator('#add-pin').fill('3691');
        await admin.locator('#add-confirm').fill('3691');
        await admin.locator('form[aria-labelledby="add-title"] button[type="submit"]').click();
        await admin.getByText(`Added ${name}.`).waitFor();
        const id = Number(
            (await admin.locator('.adm-table a.adm-row-title', { hasText: name }).getAttribute('href')).match(
                /(\d+)$/,
            )[1],
        );
        ids.push(id);
        const context = await browser.newContext({ viewport: { width: 390, height: 900 } });
        const p = await context.newPage();
        await p.goto(base + '/news');
        await p.locator(`form[action="profiles/${id}/open"]`).getByRole('button').click();
        await p.keyboard.type('3691');
        await p.waitForURL((u) => u.pathname === '/news');
        const state = await context.storageState();
        await context.close();
        return state;
    };
    const layout = (page) =>
        page.evaluate(() => {
            const visible = (n) => n.offsetParent !== null || getComputedStyle(n).position === 'fixed';
            const all = [...document.querySelectorAll('[id]')].map((n) => n.id);
            return {
                over: document.documentElement.scrollWidth - innerWidth,
                dupes: all.filter((id, i) => all.indexOf(id) !== i),
                small: [
                    ...document.querySelectorAll('main :is(a, button:not([hidden]), select, input:not([type=hidden]))'),
                ]
                    .filter(visible)
                    .filter((n) => !n.closest('.ef-headlines__title, .tp-help, .ef-note, .mx-ribbon__kicker'))
                    .filter((n) => n.getBoundingClientRect().height < 43.9)
                    .map((n) => n.className || n.tagName),
            };
        });
    try {
        admin = await (await browser.newContext({ viewport: { width: 1440, height: 1000 } })).newPage();
        await admin.goto(adminBase + '/login?returnUrl=%2Fadmin%2Fprofiles');
        await admin.locator('#password').fill(password);
        await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
        await admin.waitForURL('**/admin/profiles');
        const mine = await openProfile('Topics');
        const theirs = await openProfile('Other');

        // Follow with the live preview, on a phone.
        const context = await browser.newContext({ viewport: { width: 390, height: 900 }, storageState: mine });
        const p = await context.newPage();
        p.on('pageerror', (e) => errors.push(e.message));
        await p.goto(base + '/news', { waitUntil: 'networkidle' });
        await p.locator('.mx-phone a.mx-more', { hasText: 'Follow a topic' }).click();
        await p.waitForURL((u) => u.pathname === '/news/topics/follow');
        const preview = p.locator('[data-topic-preview]');
        assert(await preview.isHidden(), 'no preview before typing');
        // A nonsense word: no matches yet.
        await p.locator('#tp-terms').fill('zzqxv' + tag);
        await p.locator('.tp-verdict--none').waitFor();
        assert.match(await p.locator('[data-topic-verdict] b').innerText(), /No matches yet/);
        // A very common word: very broad, still followable.
        await p.locator('#tp-terms').fill('the');
        await p.locator('.tp-verdict--broad').waitFor();
        assert(await p.locator('[data-topic-submit]').isEnabled());
        // A normal phrase: the chart, the latest matches with the phrase marked, and the button names it.
        await Promise.all([
            p.waitForResponse((r) => r.url().includes('/me/topics/preview') && r.url().includes('Kansas+City')),
            p.locator('#tp-terms').fill('Kansas City'),
        ]);
        await p.waitForTimeout(200);
        assert.equal(await p.locator('[data-topic-chart] .tp-chart__day').count(), 7);
        assert.match(await p.locator('[data-topic-chart]').getAttribute('aria-label'), /^Matches per day, past week: /);
        assert.equal(await p.locator('[data-topic-submit]').innerText(), 'Follow “Kansas City”');
        // A match can be in the summary only; a title that names the phrase has it marked.
        const marks = await p
            .locator('[data-topic-latest] li b')
            .evaluateAll((ns) =>
                ns.map((n) => [
                    /kansas\s+city/i.test(n.textContent),
                    [...n.querySelectorAll('mark')].map((m) => m.textContent),
                ]),
            );
        assert(
            marks.every(([named, marked]) => !named || marked.some((m) => /^kansas\s+city$/i.test(m))),
            'phrase marked in titles',
        );
        // Except hides matches and says how many.
        await Promise.all([
            p.waitForResponse((r) => r.url().includes('/me/topics/preview') && r.url().includes('except=Chiefs')),
            p.locator('#tp-except').fill('Chiefs'),
        ]);
        await p.waitForTimeout(200);
        const hidden = await p.locator('[data-topic-hidden]').innerText();
        if (hidden) assert.match(hidden, /hidden by Except: Chiefs$/);
        for (const theme of ['light', 'dark'])
            for (const width of [320, 390, 430, 1440]) {
                await p.setViewportSize({ width, height: 900 });
                await p.emulateMedia({ colorScheme: theme });
                await p.evaluate((t) => document.documentElement.setAttribute('data-theme', t), theme);
                const r = await layout(p);
                assert.equal(r.over, 0, `follow ${width} ${theme} scrolls sideways`);
                assert.deepEqual(r.dupes, [], `follow ${width} ${theme} repeated ids`);
                assert.deepEqual(r.small, [], `follow ${width} ${theme} small targets`);
                assert.deepEqual(await p.evaluate(audit), [], `follow ${width} ${theme} contrast`);
                if (shots && (width === 390 || width === 1440))
                    await p.screenshot({ path: `${shots}topic-follow-${width}-${theme}.png`, fullPage: true });
            }
        await p.setViewportSize({ width: 390, height: 900 });
        await p.locator('[data-topic-submit]').click();
        await p.waitForURL(/\/news\/topics\/\d+\?done=topic-followed/);
        const topicUrl = new URL(p.url());
        const topicPath = topicUrl.pathname;
        assert.equal(await p.locator('.mx-ribbon h1').innerText(), 'Kansas City');
        assert.match(await p.locator('.tp-since__sub').innerText(), /except “Chiefs” · all sections$/);
        // Every row has Share and Save; a title that names the phrase has it marked.
        const rows = await p.locator('.tp-rows li').evaluateAll((ns) =>
            ns.map((n) => {
                const title = n.querySelector('.ef-headlines__title');
                return [
                    !!n.querySelector('[data-share-open]:not([hidden])'),
                    !!n.querySelector('.save'),
                    !/kansas\s+city/i.test(title.textContent) || !!title.querySelector('mark'),
                ];
            }),
        );
        assert(
            rows.every(([share, save, mark]) => share && save && mark),
            'Share, Save and the phrase marked on every row',
        );
        for (const theme of ['light', 'dark'])
            for (const width of [320, 390, 430, 1440]) {
                await p.setViewportSize({ width, height: 900 });
                await p.evaluate((t) => document.documentElement.setAttribute('data-theme', t), theme);
                const r = await layout(p);
                assert.equal(r.over, 0, `topic ${width} ${theme} scrolls sideways`);
                assert.deepEqual(r.dupes, [], `topic ${width} ${theme} repeated ids`);
                assert.deepEqual(r.small, [], `topic ${width} ${theme} small targets`);
                assert.deepEqual(await p.evaluate(audit), [], `topic ${width} ${theme} contrast`);
                if (shots && (width === 390 || width === 1440))
                    await p.screenshot({ path: `${shots}topic-page-${width}-${theme}.png`, fullPage: true });
            }
        await p.setViewportSize({ width: 390, height: 900 });
        // Opening the page doesn't mark anything read; the button does.
        await p.getByRole('button', { name: 'Mark as read' }).click();
        await p.waitForURL(/done=topic-read/);
        assert.equal(await p.locator('.tp-since__count b').innerText(), '0');

        // The News front lists it: a fold on phones, a box on desktop.
        await p.goto(base + '/news', { waitUntil: 'networkidle' });
        assert(
            await p
                .locator('.mx-phone .mx-fold', { hasText: 'Your topics' })
                .locator('a', { hasText: 'Kansas City' })
                .count(),
        );
        await p.setViewportSize({ width: 1440, height: 900 });
        assert(await p.locator('.nf-topics a', { hasText: 'Kansas City' }).isVisible());
        await p.setViewportSize({ width: 390, height: 900 });

        // Another profile can't open it.
        const other = await browser.newContext({ storageState: theirs });
        assert.equal((await (await other.newPage()).goto(base + topicPath)).status(), 404);
        await other.close();

        // Edit: the form comes back filled in; saving keeps it.
        await p.goto(base + topicPath);
        await p.getByRole('link', { name: 'Edit topic' }).click();
        await p.waitForURL(/\/news\/topics\/follow\?edit=\d+/);
        assert.equal(await p.locator('#tp-terms').inputValue(), 'Kansas City');
        assert.equal(await p.locator('#tp-except').inputValue(), 'Chiefs');
        await p.locator('#tp-hub').selectOption('headlines');
        await p.locator('[data-topic-submit]').click();
        await p.waitForURL(/done=topic-saved/);
        assert.match(await p.locator('.tp-since__sub').innerText(), /News only$/);

        // Without JavaScript the form still follows a topic.
        const plain = await browser.newContext({
            javaScriptEnabled: false,
            viewport: { width: 320, height: 900 },
            storageState: mine,
        });
        const q = await plain.newPage();
        await q.goto(base + '/news/topics/follow');
        await q.locator('#tp-terms').fill('Royals');
        await q.locator('[data-topic-submit]').click();
        await q.waitForURL(/\/news\/topics\/\d+\?done=topic-followed/);
        assert.equal((await q.locator('.mx-ribbon h1').innerText()).trim(), 'Royals');
        // A word too short comes back with the reason.
        await q.goto(base + '/news/topics/follow');
        await q.locator('#tp-terms').fill('x');
        await q.locator('[data-topic-submit]').click();
        await q.waitForURL(/error=topic-invalid/);
        assert(await q.locator('.tp-error').isVisible());
        await plain.close();

        // Stop following.
        await p.goto(base + topicPath);
        await p.getByRole('button', { name: 'Stop following' }).click();
        await p.waitForURL((u) => u.pathname === '/news');
        assert.equal((await p.goto(base + topicPath)).status(), 404);
        await context.close();

        assert.deepEqual(errors, []);
        console.log(
            'Your topics: preview verdicts (none, broad, good), chart, marked matches, Except count; follow, topic page (Share/Save/marks, Mark as read), front box and phone fold, edit, another profile 404, no-JS follow and error, stop following; 320/390/430/1440 light/dark overflow, contrast, ids, 44px targets passed.',
        );
    } finally {
        for (const id of ids) {
            try {
                await admin.goto(adminBase + '/admin/profiles/' + id);
                await admin.waitForLoadState('networkidle');
                await admin.getByRole('button', { name: 'Delete profile…' }).click();
                await admin.getByRole('button', { name: 'Delete profile', exact: true }).click();
                await admin.waitForURL('**/admin/profiles?deleted=*');
            } catch (e) {
                console.error(
                    `Could not delete the throwaway profile ${id}; remove it in Admin · Profiles. ${e.message}`,
                );
            }
        }
        await browser.close();
    }
})().catch((e) => {
    console.error(e.message || e);
    process.exit(1);
});
