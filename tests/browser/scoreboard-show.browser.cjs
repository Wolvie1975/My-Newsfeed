const assert = require('node:assert/strict');
const { chromium } = require('playwright');
const lib = require('./lib.cjs');
const base = lib.base;
(async () => {
    const browser = await chromium.launch();
    try {
        for (const js of [true, false]) {
            const page = await lib.readerPage(browser, { javaScriptEnabled: js });
            await page.goto(base + '/scoreboard');
            assert.equal(await page.locator('.scoreboard-filter select').count(), 2);
            assert.equal(await page.locator('#scoreboard-follow,#scoreboard-competition').count(), 0);
            const show = page.getByRole('combobox', { name: 'Show', exact: true });
            const groups = await show.locator('optgroup').evaluateAll((ns) => ns.map((n) => n.label));
            assert.deepEqual(groups, ['Leagues', 'Conferences', 'Teams']);
            const values = await show.locator('option').evaluateAll((ns) => ns.map((n) => n.value));
            assert.equal(values.length, new Set(values).size);
            assert(!values.some((v) => v.includes('womens-college-basketball')));
            assert(!(await show.locator('option').allTextContents()).some((t) => /women[’']s.*basketball/i.test(t)));
            assert(values.includes('c:basketball|wnba'));
            assert(values.includes('c:basketball|mens-college-basketball'));
            const conference = await show.locator('optgroup[label="Conferences"] option').first().getAttribute('value');
            const team = await show.locator('optgroup[label="Teams"] option').first().getAttribute('value');
            for (const value of ['c:baseball|mlb', conference, team, '']) {
                await show.selectOption(value);
                if (!js) await page.getByRole('button', { name: 'Apply', exact: true }).click();
                await page.waitForURL((u) => (u.searchParams.get('show') || '') === value);
                assert(!new URL(page.url()).searchParams.has('competition'));
                assert(!new URL(page.url()).searchParams.has('follow'));
            }
            // Old team links override an incompatible league and canonicalize date links.
            await page.goto(base + '/scoreboard?competition=baseball%7Cmlb&follow=' + team.slice(2) + '&status=final');
            assert.equal(await show.inputValue(), team);
            const href = await page.locator('.scoreboard-prev').getAttribute('href');
            const dateUrl = new URL(href, base + '/');
            assert.equal(dateUrl.searchParams.get('show'), team);
            assert.equal(dateUrl.searchParams.get('status'), 'final');
            assert(!dateUrl.searchParams.has('competition') && !dateUrl.searchParams.has('follow'));

            // Group selection: Big 12 · All sports (g:Big 12)
            const confOptions = await show
                .locator('optgroup[label="Conferences"] option')
                .evaluateAll((ns) => ns.map((n) => ({ value: n.value, text: n.textContent.trim() })));
            if (confOptions.length >= 2) {
                assert.equal(confOptions[0].value, 'g:Big 12');
                assert.equal(confOptions[0].text, 'Big 12 · All sports');
                assert(confOptions.some((o) => o.value.startsWith('f:')));

                await page.goto(base + '/scoreboard?show=g%3ABig%2012');
                assert.equal(await show.inputValue(), 'g:Big 12');
                const prevHref = await page.locator('.scoreboard-prev').getAttribute('href');
                if (prevHref) {
                    const gDateUrl = new URL(prevHref, base + '/');
                    assert.equal(gDateUrl.searchParams.get('show'), 'g:Big 12');
                }

                // Phone view (390px)
                await page.setViewportSize({ width: 390, height: 844 });
                await page.goto(base + '/scoreboard?show=g%3ABig%2012');
                const mobileShow = page.locator('#scoreboard-show-m');
                assert.equal(await mobileShow.inputValue(), 'g:Big 12');
                const mobileConfFirst = await mobileShow
                    .locator('optgroup[label="Conferences"] option')
                    .first()
                    .evaluate((n) => ({ value: n.value, text: n.textContent.trim() }));
                assert.equal(mobileConfFirst.value, 'g:Big 12');
                assert.equal(mobileConfFirst.text, 'Big 12 · All sports');
                await page.setViewportSize({ width: 1440, height: 900 });
            }

            // /events schedule choice check
            await page.goto(base + '/events');
            const schedSelect = page.locator('#schedule');
            const schedOptions = await schedSelect
                .locator('option')
                .evaluateAll((ns) => ns.map((n) => ({ value: n.value, text: n.textContent.trim() })));
            const big12Choices = schedOptions.filter((o) => o.value.startsWith('big12|'));
            if (big12Choices.length > 0) {
                assert.equal(big12Choices[0].value, 'big12|');
                assert.equal(big12Choices[0].text, 'Big 12 · All sports');
                await schedSelect.selectOption('big12|');
                if (!js) await page.getByRole('button', { name: 'Apply', exact: true }).click();
                await page.waitForURL((u) => u.searchParams.get('schedule') === 'big12|');
                assert.equal(await schedSelect.inputValue(), 'big12|');
            }

            await page.close();
        }
        console.log(
            'Grouped Show options, unique league choices, league/conference/team/all submission, legacy conflict resolution and canonical date links passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
