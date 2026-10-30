// Folded phone layouts are covered by phone-fronts.browser.cjs; these checks exercise the retained tablet/desktop controls.
// Homepage scoreboard box: the Sport dropdown lists today's leagues, filters in place without moving the page, survives
// the five-minute refresh and bookmarks, and works without JavaScript.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
const shots = process.env.SCREENSHOT_DIR || 'test-results/';
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
        const errors = [];
        page.on('pageerror', (e) => errors.push(e.message));

        // The Sports page shares the compact box but has no Sport filter, ignores the homepage's parameter, and stays
        // unfiltered after its own refresh.
        const sports = await browser.newPage({ viewport: { width: 820, height: 900 } });
        await sports.goto(base + '/sports?scoreboardShow=' + encodeURIComponent('c:hockey|nhl'));
        const sportsBox = sports.locator('[data-scoreboard]').first();
        await sportsBox.waitFor();
        assert.equal(
            await sports.locator('#home-scoreboard-show, .scoreboard-home-filter').count(),
            0,
            'No Sport filter on /sports',
        );
        assert.equal(await sportsBox.getAttribute('data-scoreboard-sport-filter'), null);
        const sportsLeagues = new Set(
            (await sportsBox.locator('.scoreboard-competition').allTextContents()).map((t) => t.trim()),
        );
        const sportsRefresh = sports.waitForResponse((r) => r.url().includes('/scoreboard/fragment'));
        await sports.evaluate(() => document.dispatchEvent(new Event('visibilitychange')));
        assert(
            !new URL((await sportsRefresh).url()).searchParams.has('sportFilter'),
            'Sports refresh asks for no filter',
        );
        await sports.waitForTimeout(500);
        assert.equal(await sports.locator('.scoreboard-home-filter').count(), 0, 'Refresh adds no filter on /sports');
        await sports.close();

        await page.goto(base + '/');
        const box = page.locator('section[data-home-section="scoreboard"]');
        await box.waitFor();
        const select = box.locator('#home-scoreboard-show');
        if (!(await select.count())) {
            console.log(
                `SKIP fewer than two leagues have games today, so the Sport dropdown is not shown (the /sports check passed; leagues there: ${[...sportsLeagues].join(', ') || 'none'}).`,
            );
            return;
        }
        const options = await select
            .locator('option')
            .evaluateAll((ns) => ns.map((n) => ({ value: n.value, text: n.textContent.trim() })));
        assert.equal(options[0].value, '', 'All sports comes first');
        const leagues = options.slice(1);
        assert(leagues.length >= 2);
        assert(
            leagues.every((o) => o.value.startsWith('c:')),
            'Only leagues, no conferences or teams',
        );

        // Let late content above the box finish first, and measure the box on screen, which is what a jump moves.
        await page.waitForLoadState('networkidle');
        await page.evaluate(() => (window.__scoreboardNoReload = true));
        const choose = async (value) => {
            const current = box.locator('#home-scoreboard-show');
            await current.scrollIntoViewIfNeeded();
            const before = await page.evaluate(() => scrollY);
            const topBefore = (await box.boundingBox()).y;
            await current.selectOption(value);
            await page.waitForFunction(
                (v) =>
                    document.querySelector('#home-scoreboard-show')?.value === v &&
                    !document.querySelector('[data-home-section="scoreboard"][aria-busy]'),
                value,
            );
            assert.equal(await page.evaluate(() => window.__scoreboardNoReload), true, 'Choosing a sport reloaded');
            assert(Math.abs((await page.evaluate(() => scrollY)) - before) <= 1, 'Choosing a sport moved the page');
            assert(Math.abs((await box.boundingBox()).y - topBefore) <= 1, 'Choosing a sport moved the box on screen');
        };
        const shown = () => box.locator('.scoreboard-competition').allTextContents();

        for (const league of leagues) {
            await choose(league.value);
            assert.equal(new URL(page.url()).searchParams.get('scoreboardShow'), league.value);
            const labels = await shown();
            assert(labels.length >= 1 && labels.length <= 6, `1 to 6 games for ${league.text}`);
            assert(
                labels.every((l) => l.trim() === league.text),
                `Only ${league.text} games`,
            );
            assert.deepEqual(
                await box.locator('#home-scoreboard-show option').evaluateAll((ns) => ns.map((n) => n.value)),
                options.map((o) => o.value),
                'The list stays complete after choosing a league',
            );
            assert.equal(await page.evaluate(() => document.activeElement?.id), 'home-scoreboard-show');
        }

        // The five-minute refresh keeps the chosen league (it skips while the dropdown has focus, so blur first).
        const kept = leagues[0];
        await choose(kept.value);
        await page.evaluate(() => document.activeElement?.blur());
        const refreshed = page.waitForResponse((r) => r.url().includes('/scoreboard/fragment'));
        await page.evaluate(() => document.dispatchEvent(new Event('visibilitychange')));
        await refreshed;
        await page.waitForTimeout(500);
        assert.equal(await box.locator('#home-scoreboard-show').inputValue(), kept.value, 'Refresh kept the sport');
        assert(
            (await shown()).every((l) => l.trim() === kept.text),
            'Refresh kept the filter',
        );

        // All sports clears the filter from the address.
        await choose('');
        assert.equal(new URL(page.url()).searchParams.has('scoreboardShow'), false);

        // Bookmarks render filtered on the server; unknown values fall back to All sports.
        await page.goto(base + '/?scoreboardShow=' + encodeURIComponent(kept.value));
        assert.equal(await box.locator('#home-scoreboard-show').inputValue(), kept.value);
        assert((await shown()).every((l) => l.trim() === kept.text));
        await page.goto(base + '/?scoreboardShow=c:not|a-league');
        assert.equal(await box.locator('#home-scoreboard-show').inputValue(), '');

        // Phones, light and dark: the dropdown fits, with no sideways scrolling.
        for (const scheme of ['light', 'dark'])
            for (const width of [1024, 1280]) {
                const context = await browser.newContext({ viewport: { width, height: 900 }, colorScheme: scheme });
                const p = await context.newPage();
                p.on('pageerror', (e) => errors.push(e.message));
                await p.goto(base + '/?scoreboardShow=' + encodeURIComponent(kept.value));
                const b = p.locator('section[data-home-section="scoreboard"]');
                await b.scrollIntoViewIfNeeded();
                const s = await b.locator('#home-scoreboard-show').boundingBox();
                const card = await b.boundingBox();
                assert(s.height >= 44, `44px dropdown at ${width}`);
                assert(
                    s.x >= card.x && s.x + s.width <= card.x + card.width + 1,
                    `Dropdown inside the box at ${width}`,
                );
                assert(
                    await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                    `No overflow at ${width}`,
                );
                await b.screenshot({ path: `${shots}home-scoreboard-sport-${width}-${scheme}.png` });
                await context.close();
            }

        // Without JavaScript, the dropdown and Apply load the filtered page.
        const plain = await browser.newContext({ javaScriptEnabled: false, viewport: { width: 820, height: 900 } });
        const p = await plain.newPage();
        await p.goto(base + '/');
        await p.locator('#home-scoreboard-show').selectOption(kept.value);
        await p.locator('section[data-home-section="scoreboard"]').getByRole('button', { name: 'Apply' }).click();
        assert.equal(new URL(p.url()).searchParams.get('scoreboardShow'), kept.value);
        assert.equal(await p.locator('#home-scoreboard-show').inputValue(), kept.value);
        await plain.close();

        assert.deepEqual(errors, []);
        console.log(
            `Homepage scoreboard sport filter: ${leagues.length} leagues today (${leagues.map((l) => l.text).join(', ')}), in-place switch (no reload, scroll kept), list stays complete, refresh keeps the sport, bookmarks and unknown values, phones light/dark, no-JS Apply, no JS errors passed.`,
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
