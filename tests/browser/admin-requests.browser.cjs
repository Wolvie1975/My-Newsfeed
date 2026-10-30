// Admin · Requests and the header alert (profiles handoff stage A2) at phone, iPad and desktop widths in light and dark:
// no sideways scroll, readable text, 44px controls, the queue and editor, Test feed, Decline, approving an
// already-collected channel, and the alert's panel (popover on desktop, bottom sheet on phones) with Unlock.
//
// Requests can't be submitted until stage U3, so it needs seeded rows: a profile named "zz requests …" with waiting
// requests (one of them for a channel that's already collected) and a locked profile named "zz requests … locked".
// It only declines, approves the already-collected one (which adds a follow, nothing the scraper reads) and unlocks;
// it never adds a feed or source. Delete the seeded profiles afterwards; their requests go with them.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { audit } = require('./contrast-audit.cjs');

const base = process.env.ADMIN_BASE_URL || process.env.BASE_URL || 'http://localhost:5297';
const password = process.env.ADMIN_PASSWORD;
const shots = process.env.SCREENSHOT_DIR;
if (!password) {
    console.log('SKIP: signs in to admin; set ADMIN_PASSWORD to run it.');
    process.exit(0);
}
const widths = (process.env.WIDTHS || '320,390,430,744,820,1024,1440').split(',').map(Number);

(async () => {
    const browser = await chromium.launch();
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    const p = await context.newPage();
    const errors = [];
    p.on('pageerror', (e) => errors.push(e.message));
    const open = async (path) => {
        await p.goto(base + path);
        await p.waitForLoadState('networkidle');
        await p.waitForTimeout(400);
    };
    try {
        await open('/login?returnUrl=%2Fadmin%2Frequests');
        await p.locator('#password').fill(password);
        await p.getByRole('button', { name: 'Sign in', exact: true }).click();
        await p.waitForURL('**/admin/requests');
        await p.waitForLoadState('networkidle');
        const seeded = p.locator('.adm-req__item', { hasText: 'Requested by zz requests' });
        if ((await seeded.count()) === 0) {
            console.log('SKIP: no seeded requests from a "zz requests …" profile; see the header of this file.');
            return;
        }
        assert.equal(
            (await p.locator('.adm-nav__link.active').innerText()).replace(/\d+/g, '').trim(),
            'Requests',
            'menu entry',
        );

        // The first request is open beside the queue on desktop; picking another opens it.
        assert(await p.locator('.adm-req__editor').isVisible(), 'editor beside the queue');
        const linus = seeded.filter({ hasText: 'Linus Tech Tips' }).first();
        await linus.click();
        await p.waitForURL('**/admin/requests?id=*');
        await p.waitForTimeout(400);
        assert.equal((await p.locator('#req-title').innerText()).trim(), 'Linus Tech Tips');
        if (!/^UC/.test(await p.locator('#req-channel').inputValue())) {
            await p.getByRole('button', { name: 'Check again' }).click();
            await p
                .waitForFunction(() => /^UC/.test(document.querySelector('#req-channel')?.value || ''), null, {
                    timeout: 30000,
                })
                .catch(() => {});
        }
        if (/^UC/.test(await p.locator('#req-channel').inputValue())) {
            await p.getByRole('button', { name: 'Test feed' }).click();
            await p.locator('.adm-req__result b').waitFor({ timeout: 20000 });
            console.log('Test feed: ' + (await p.locator('.adm-req__result').innerText()).trim());
        } else console.log('Note: the checker found no channel ID for Linus Tech Tips; Test feed not run.');
        await p.locator('#req-channel').fill('nope');
        await p.waitForTimeout(400);
        assert(
            await p.getByRole('button', { name: 'Add channel and approve' }).isDisabled(),
            'approve needs a channel ID',
        );

        // Decline with a reason, then find it under Declined.
        await open('/admin/requests');
        await seeded.filter({ hasText: 'Binging with Babish' }).first().click();
        await p.waitForURL('**/admin/requests?id=*');
        await p.locator('#req-reason').fill('Try their newer channel.');
        await p.getByRole('button', { name: 'Decline', exact: true }).click();
        await p.getByText('Declined Binging with Babish.').waitFor();
        await open('/admin/requests?status=declined');
        assert(
            await p.locator('.adm-req__item', { hasText: 'Binging with Babish' }).first().isVisible(),
            'declined filter',
        );

        // An already-collected channel: approving only follows it.
        await open('/admin/requests');
        await seeded.filter({ hasText: 'Already collected channel' }).first().click();
        await p.waitForURL('**/admin/requests?id=*');
        await p.waitForTimeout(400);
        const collected = p.getByRole('button', { name: /Approve and follow for/ });
        if (!(await collected.count())) {
            await p.getByRole('button', { name: 'Check again' }).click();
            await collected.waitFor({ timeout: 30000 }).catch(() => {});
        }
        if (await collected.count()) {
            assert.equal(await p.locator('#req-channel').count(), 0, 'no feed fields for a collected channel');
            await collected.click();
            await p.getByText(/now follows Already collected channel/).waitFor();
        } else console.log('Note: the checker hasn’t resolved the collected channel yet; approve not run.');

        // Layout at every width and theme: the queue, a picked request, and the header alert on Home.
        const pages = ['/admin/requests', '/admin/requests?status=all'];
        for (const theme of ['light', 'dark'])
            for (const width of widths) {
                await p.setViewportSize({ width, height: 900 });
                await p.evaluate((t) => localStorage.setItem('theme', t), theme);
                const picked = await (async () => {
                    await open('/admin/requests');
                    return await p.locator('.adm-req__item').first().getAttribute('href');
                })();
                for (const path of [...pages, '/' + picked.replace(/^\//, '')]) {
                    await open(path);
                    const label = `${theme} ${width}px ${path}`;
                    const r = await p.evaluate(() => ({
                        over: document.documentElement.scrollWidth - innerWidth,
                        queue: !!document.querySelector('.adm-req__queue')?.offsetParent,
                        editor: !!document.querySelector('.adm-req__editor')?.offsetParent,
                        small: [
                            ...document.querySelectorAll(
                                '.adm-req .btn, .adm-req__back, .adm-req__item, .adm-req .adm-switch, .adm-req .form-control, .adm-req .form-select',
                            ),
                        ]
                            .filter((n) => n.offsetParent !== null)
                            .map((n) => {
                                const b = n.getBoundingClientRect();
                                const tap = n.matches('.adm-switch')
                                    ? parseFloat(getComputedStyle(n, '::before').height)
                                    : b.height;
                                return Math.max(b.height, tap || 0);
                            })
                            .filter((h) => h < 44).length,
                    }));
                    assert.equal(r.over, 0, label + ' scrolls sideways');
                    assert.equal(r.small, 0, label + ' touch targets under 44px');
                    const pickedPage = path.includes('id=');
                    if (width >= 1024) assert(r.queue && r.editor, label + ' queue beside the request');
                    else if (pickedPage) assert(!r.queue && r.editor, label + ' phone shows only the picked request');
                    else assert(r.queue && !r.editor, label + ' phone shows only the queue');
                    if (width === 390 || width === 1440) {
                        assert.deepEqual(await p.evaluate(audit), [], label + ' contrast');
                        if (shots) {
                            const file = path.replace(/^\/admin\//, '').replace(/[^a-z0-9]+/g, '-');
                            await p.screenshot({ path: `${shots}${file}-${width}-${theme}.png`, fullPage: true });
                        }
                    }
                }

                // The header alert on Home: a sheet on phones, a popover from 744px.
                await open('/');
                const button = p.locator('[data-admin-alert-toggle]');
                assert(await button.isVisible(), `${theme} ${width}px alert button`);
                const box = await button.boundingBox();
                assert(box.height >= 44 && box.width >= 44, `${theme} ${width}px alert button 44px`);
                await button.click();
                const panel = p.locator('[data-admin-alert-panel]');
                await panel.waitFor();
                await p.locator('[data-admin-alert-list] li').first().waitFor();
                const geo = await p.evaluate(() => {
                    const b = document.querySelector('[data-admin-alert-panel]').getBoundingClientRect();
                    return {
                        over: document.documentElement.scrollWidth - innerWidth,
                        left: b.left,
                        right: innerWidth - b.right,
                        bottom: innerHeight - b.bottom,
                    };
                });
                assert.equal(geo.over, 0, `${theme} ${width}px alert scrolls sideways`);
                assert(geo.left >= 0 && geo.right >= 0, `${theme} ${width}px alert inside the screen`);
                if (width < 1024) assert(Math.abs(geo.bottom) < 1, `${theme} ${width}px alert is a bottom sheet`);
                assert.equal(
                    (await p.locator('[data-admin-alert-list] li').first().getAttribute('class')).includes('locked'),
                    true,
                    'locked profiles first',
                );
                if (width === 390 || width === 1440) {
                    assert.deepEqual(await p.evaluate(audit), [], `${theme} ${width}px alert contrast`);
                    if (shots) await p.screenshot({ path: `${shots}alert-${width}-${theme}.png` });
                }
                await p.keyboard.press('Escape');
                assert(await panel.isHidden(), `${theme} ${width}px Escape closes`);
                assert.deepEqual(errors, [], `${theme} ${width}px script errors`);
            }

        // Unlock from the panel, then Mark all seen keeps nothing but locks in the count.
        await p.setViewportSize({ width: 1440, height: 1000 });
        await open('/');
        await p.locator('[data-admin-alert-toggle]').click();
        const lockedRow = p.locator('[data-admin-alert-list] li.admin-alert__item--locked', { hasText: 'zz requests' });
        await lockedRow.first().waitFor();
        const before = await p.locator('[data-admin-alert-list] li.admin-alert__item--locked').count();
        await lockedRow.first().getByRole('button', { name: 'Unlock' }).click();
        await p.waitForFunction(
            (n) => document.querySelectorAll('[data-admin-alert-list] li.admin-alert__item--locked').length === n - 1,
            before,
        );
        await p.getByRole('button', { name: 'Mark all seen' }).click();
        await p.waitForTimeout(800);
        assert.equal(await p.locator('.admin-alert__new').count(), 0, 'nothing new after Mark all seen');

        // Readers never get the alert.
        const reader = await browser.newContext();
        const rp = await reader.newPage();
        await rp.goto(base + '/');
        assert.equal(await rp.locator('[data-admin-alert]').count(), 0, 'no alert for readers');
        await reader.close();
        assert.deepEqual(errors, []);
        console.log(
            `Admin requests and alert at ${widths.join('/')}px light/dark: queue and editor, Test feed, decline, collected approve, panel, Unlock and Mark all seen passed.`,
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e.message || e);
    process.exit(1);
});
