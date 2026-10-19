// Run the championship rendering test with BASKETBALL_PREVIEW_FILE first.
// This fixture check uses generated HTML, never a production cache row or route.
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright');const assert=require('node:assert/strict');
const {pathToFileURL}=require('node:url');
(async()=>{const browser=await chromium.launch();try{
 const page=await browser.newPage();await page.goto(pathToFileURL(process.env.BASKETBALL_PREVIEW_FILE||'/tmp/mynewsfeed-basketball-preview.html').href);
 for(const theme of ['light','dark'])for(const width of [390,768,1440]){
  await page.setViewportSize({width,height:900});await page.evaluate(t=>document.documentElement.dataset.theme=t,theme);
  const panels=page.locator('.basketball-team-panel');assert.equal(await panels.count(),2);
  for(const [index,name,players,points]of [[0,'UConn Huskies',9,'63'],[1,'Michigan Wolverines',8,'69']]){
   const panel=panels.nth(index);assert.equal(await panel.locator('h4').textContent(),name);assert.equal(await panel.locator('tbody tr').count(),players);
   assert.deepEqual(await panel.locator('thead th').allTextContents(),['Player','MIN','PTS','FG','3PT','FT','REB','AST','TO','STL','BLK','OREB','DREB','PF']);
   const totals=await panel.locator('tfoot td').allTextContents();assert.equal(totals[0],'—');assert.equal(totals[1],points);
   const scroll=panel.locator('.box-table-scroll');await scroll.focus();assert(await scroll.evaluate(n=>n===document.activeElement));
   if(width===390){assert(await scroll.evaluate(n=>n.scrollWidth>n.clientWidth));await scroll.evaluate(n=>n.scrollLeft=n.scrollWidth);assert(await scroll.evaluate(n=>n.scrollLeft>0));}
  }
  const a=await panels.nth(0).boundingBox(),b=await panels.nth(1).boundingBox();if(width>=1000)assert(Math.abs(a.y-b.y)<2);else assert(b.y>=a.y+a.height);
  assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
  if(process.env.SCREENSHOT_DIR)await page.screenshot({path:`${process.env.SCREENSHOT_DIR}/basketball-${width}-${theme}.png`});
 }
 console.log('Six championship fixture width/theme layouts passed: team matching, all 13 columns, 17 players, supplied totals, desktop pairing and keyboard-accessible mobile scrolling.');
}finally{await browser.close()}})().catch(e=>{console.error(e);process.exit(1)});
