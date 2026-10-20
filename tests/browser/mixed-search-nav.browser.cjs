const assert=require('node:assert/strict');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const base=process.env.BASE_URL||'http://127.0.0.1:5298';
(async()=>{const browser=await chromium.launch({args:process.env.PWA_HOST_IP?[`--host-resolver-rules=MAP ${new URL(base).hostname} ${process.env.PWA_HOST_IP}`]:[]});try{
const page=await browser.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
await page.goto(base+'/watch?kind=tv');assert.equal(await page.locator('nav[aria-label="Content type"]').count(),1);
await page.locator('#watch-query').fill('Fargo');await page.locator('.watch-search button').click();await page.waitForURL('**/watch?**q=Fargo**');
assert.equal(await page.locator('nav[aria-label="Content type"]').count(),0);
const links=await page.locator('[data-watch-title]').evaluateAll(nodes=>nodes.map(n=>n.getAttribute('href')));
assert(links.some(h=>h.startsWith('watch/movie/')));assert(links.some(h=>h.startsWith('watch/tv/')));
await page.selectOption('#watch-sort','title');await page.locator('.watch-sort button').click();await page.waitForURL('**sort=title**');
assert.equal(await page.locator('nav[aria-label="Content type"]').count(),0);
const movie=page.locator('[data-watch-title][href^="watch/movie/"]').first();await movie.click();await page.locator('dialog[open] .watch-detail').waitFor();await page.keyboard.press('Escape');
await page.getByRole('link',{name:'Clear search',exact:true}).click();await page.locator('nav[aria-label="Content type"]').waitFor();assert.equal(await page.locator('nav[aria-label="Content type"] a[aria-current="page"]').innerText(),'TV shows');
for(const theme of ['light','dark'])for(const width of [390,768]){
await page.setViewportSize({width,height:900});await page.goto(base+'/events');await page.evaluate(theme=>localStorage.setItem('theme',theme),theme);await page.reload();
await page.locator('.mobile-nav > summary').click();
for(const [name,href] of [['YouTube','videos'],['Saved articles','saved']]){
const link=page.locator('.mobile-nav__links').getByRole('link',{name,exact:true});assert.equal(await link.count(),1);assert.equal(await link.getAttribute('href'),href);assert(await link.evaluate(n=>n.parentElement.classList.contains('mobile-nav__links')));
}
assert.equal(await page.locator('.mobile-nav__links .nav-group-label').filter({hasText:/^(YouTube|Your links)$/}).count(),0);
for(const name of ['Sports','Entertainment']){const group=page.getByRole('group',{name,exact:true});assert(await group.isVisible());assert(await group.locator('.mobile-subnav > a').count()>2);assert.equal(await group.evaluate(n=>getComputedStyle(n).borderTopStyle),'solid');}
assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
if(process.env.SCREENSHOT_DIR)await page.screenshot({path:`${process.env.SCREENSHOT_DIR}/nav-${theme}-${width}.png`});
await page.getByRole('group',{name:'Entertainment',exact:true}).getByRole('link',{name:'Movies',exact:true}).click();await page.waitForURL('**/movies');assert.equal(await page.locator('.mobile-nav').getAttribute('open'),null);
}
await page.setViewportSize({width:1440,height:900});await page.goto(base+'/watch?q=Fargo');assert(await page.locator('.site-nav--inline').isVisible());assert.equal(await page.locator('nav[aria-label="Content type"]').count(),0);assert.deepEqual(errors,[]);
console.log('Mixed movie/TV search, sorting, detail modal, clear-to-browse and grouped mobile navigation/theme/link checks passed.');
}finally{await browser.close()}})().catch(e=>{console.error(e);process.exit(1)});
