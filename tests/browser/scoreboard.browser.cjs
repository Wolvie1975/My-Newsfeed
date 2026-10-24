const assert=require('node:assert/strict');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE || '/tmp/mydailyfeed-cascade-check/node_modules/playwright');
const base=process.env.BASE_URL || 'http://127.0.0.1:5310';
(async()=>{
 const browser=await chromium.launch();
 try {
  const page=await browser.newPage(); const errors=[];page.on('pageerror',e=>errors.push(e.message));
  for(const width of [320,390,768,1440]) for(const theme of ['light','dark']) {
   await page.setViewportSize({width,height:1000});
   await page.goto(base+'/scoreboard'); await page.evaluate(t=>{localStorage.setItem('mynewsfeed-theme',t);document.documentElement.setAttribute('data-theme',t)},theme);
   assert(await page.locator('.scoreboard-game').count()>0);
   assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),`Overflow scoreboard ${width}/${theme}`);
   await page.goto(base+'/');assert.equal(await page.locator('[data-scoreboard] .scoreboard-game').count(),6);
   assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),`Overflow Home ${width}/${theme}`);
  }
  await page.goto(base+'/scoreboard');
  const choices=await page.locator('#scoreboard-show option').evaluateAll(o=>o.map(x=>x.value).filter(v=>v.startsWith('c:')));
  assert(choices.length>=10);assert.equal(choices.length,new Set(choices).size);
  const choice=choices.find(x=>x==='c:baseball|mlb')||choices[0];
  await page.selectOption('#scoreboard-show',choice);
  await page.waitForURL(/show=/);assert.equal(new URL(page.url()).searchParams.get('show'),choice);
  assert((await page.locator('[data-scoreboard-key]').evaluateAll(n=>n.map(x=>x.dataset.scoreboardKey))).every(k=>k.includes('|'+choice.slice(2)+'|')));
  const favorite=page.locator('[data-scoreboard-team]').first();const id=await favorite.getAttribute('data-scoreboard-team');
  await favorite.click();assert.equal(await favorite.getAttribute('aria-pressed'),'true');
  await page.reload();assert.equal(await page.locator('[data-scoreboard-team]').filter({visible:true}).first().getAttribute('aria-pressed'),'true');
  await page.goto(base+'/scoreboard');
  const keys=await page.locator('[data-scoreboard-key]').evaluateAll(n=>n.map(x=>x.dataset.scoreboardKey));assert.equal(keys.length,new Set(keys).size);
  const info=page.locator('.scoreboard-info').first();
  await info.locator('summary').click();assert(await info.evaluate(n=>n.open));
  // Trigger the visibility refresh without waiting five minutes.
  let requests=0;
  await page.route('**/scoreboard/fragment?**',async route=>{requests++;await route.abort()});
  await page.evaluate(()=>{document.activeElement?.blur();document.dispatchEvent(new Event('visibilitychange'))});
  await page.waitForFunction(()=>document.querySelector('.scoreboard-read-status').textContent.includes('could not be refreshed'));
  assert.equal(await page.locator('.scoreboard-game').count(),keys.length);
  await page.unroute('**/scoreboard/fragment?**');
  await page.evaluate(()=>document.dispatchEvent(new Event('visibilitychange')));
  await page.waitForFunction(()=>!document.querySelector('.scoreboard-read-status').textContent.trim());
  assert(await page.locator('[data-scoreboard-key]').filter({has:page.locator('.scoreboard-info[open]')}).count()>0, 'Expanded game info survives refresh');
  // Interval is five minutes and hidden pages suppress reads.
  await page.clock.install();const before=requests;
  await page.route('**/scoreboard/fragment?**',async route=>{requests++;await route.continue()});
  await page.clock.runFor(299000);assert.equal(requests,before);
  await page.evaluate(()=>Object.defineProperty(document,'hidden',{configurable:true,value:true}));
  await page.clock.runFor(1000);assert.equal(requests,before);
  await page.evaluate(()=>{Object.defineProperty(document,'hidden',{configurable:true,value:false});document.dispatchEvent(new Event('visibilitychange'))});
  await page.waitForTimeout(100);assert(requests>before);
  assert.deepEqual(errors,[]);
  await page.close();
  const nojs=await browser.newPage({javaScriptEnabled:false});await nojs.goto(base+'/scoreboard');
  assert(await nojs.locator('.scoreboard-game').count()>0);assert.equal(await nojs.locator('[data-scoreboard-team]:visible').count(),0);
  await nojs.selectOption('#scoreboard-show',choice);await nojs.getByRole('button',{name:'Apply',exact:true}).click();await nojs.waitForURL(/show=/);
  const previous=nojs.getByRole('link',{name:'← Previous day',exact:true});await previous.click();await nojs.waitForURL(/day=/);
  await nojs.getByRole('link',{name:'← Previous day',exact:true}).waitFor({state:'detached'});
  assert.equal(await nojs.getByRole('link',{name:'← Previous day',exact:true}).count(),0, 'Yesterday is the earliest browsable day');
  const box=nojs.locator('[data-box-score]').first();if(await box.count()){const response=await nojs.request.get(new URL(await box.getAttribute('href'),base+'/').href);assert.equal(response.status(),200)}
  console.log('Scoreboard: responsive/theme, coverage filtering, preferences, deduplication, failed-read retention/recovery, five-minute hidden-tab polling, and no-JS controls passed.');
 } finally {await browser.close()}
})().catch(e=>{console.error(e);process.exit(1)});
