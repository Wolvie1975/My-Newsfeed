const assert=require('node:assert/strict');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'/tmp/mydailyfeed-cascade-check/node_modules/playwright');
const base=process.env.BASE_URL||'http://127.0.0.1:5312';
(async()=>{const browser=await chromium.launch();try{
 for(const js of [true,false]){
  const page=await browser.newPage({javaScriptEnabled:js});
  await page.goto(base+'/scoreboard');
  assert.equal(await page.locator('.scoreboard-filter select').count(),2);
  assert.equal(await page.locator('#scoreboard-follow,#scoreboard-competition').count(),0);
  const show=page.getByRole('combobox',{name:'Show',exact:true});
  const groups=await show.locator('optgroup').evaluateAll(ns=>ns.map(n=>n.label));
  assert.deepEqual(groups,['Leagues','Conferences','Teams']);
  const values=await show.locator('option').evaluateAll(ns=>ns.map(n=>n.value));
  assert.equal(values.length,new Set(values).size);
  const conference=await show.locator('optgroup[label="Conferences"] option').first().getAttribute('value');
  const team=await show.locator('optgroup[label="Teams"] option').first().getAttribute('value');
  for(const value of ['c:baseball|mlb',conference,team,'']){
   await show.selectOption(value);
   if(!js)await page.getByRole('button',{name:'Apply',exact:true}).click();
   await page.waitForURL(u=>(u.searchParams.get('show')||'')===value);
   assert(!new URL(page.url()).searchParams.has('competition'));
   assert(!new URL(page.url()).searchParams.has('follow'));
  }
  // Old team links override an incompatible league and canonicalize date links.
  await page.goto(base+'/scoreboard?competition=baseball%7Cmlb&follow='+team.slice(2)+'&status=final');
  assert.equal(await show.inputValue(),team);
  const href=await page.getByRole('link',{name:'← Previous day',exact:true}).getAttribute('href');
  const dateUrl=new URL(href,base+'/');
  assert.equal(dateUrl.searchParams.get('show'),team);
  assert.equal(dateUrl.searchParams.get('status'),'final');
  assert(!dateUrl.searchParams.has('competition')&&!dateUrl.searchParams.has('follow'));
  await page.close();
 }
 console.log('Grouped Show options, unique league choices, league/conference/team/all submission, legacy conflict resolution and canonical date links passed.');
}finally{await browser.close()}})().catch(e=>{console.error(e);process.exit(1)});
