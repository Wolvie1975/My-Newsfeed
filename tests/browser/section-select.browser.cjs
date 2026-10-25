const assert=require('node:assert/strict'),{chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const base=process.env.BASE_URL||'http://127.0.0.1:5325',root=process.env.SCREENSHOT_DIR;
(async()=>{const b=await chromium.launch();try{const p=await b.newPage();const errors=[];p.on('pageerror',e=>errors.push(e.message));
const routes=['/college?sport=football','/college/results?sport=mens-basketball','/college/standings?sport=volleyball','/college/rankings?sport=soccer','/college/news','/f1','/f1/standings','/f1/news','/watch?kind=tv&sort=newest','/local','/mls/results'];
for(const width of [320,390,430,768,1440])for(const theme of ['light','dark'])for(const route of routes){
await p.setViewportSize({width,height:900});await p.emulateMedia({colorScheme:theme});await p.goto(base+route);assert(!await p.locator('#blazor-error-ui').isVisible());assert(await p.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
// Where to watch uses Movies/TV toggle links by user request (October 25, 2026).
assert.equal(await p.locator(route.startsWith('/watch')?'nav.f1-tabs,nav[aria-label="Browse genres"],nav.soccer-league-links':'nav.f1-tabs,nav[aria-label="Content type"],nav[aria-label="Browse genres"],nav.soccer-league-links').count(),0);
assert(await p.locator('.section-select select').evaluateAll(ns=>ns.every(n=>n.getBoundingClientRect().height>=44&&n.getBoundingClientRect().width<=innerWidth)));
if(root&&width<=430&&['/college?sport=football','/f1','/watch?kind=tv&sort=newest'].includes(route))await p.screenshot({path:root+'/'+route.split('?')[0].slice(1)+'-'+width+'-'+theme+'.png'});
}
// Exercise every College view for all four sports and preserve selection both ways.
for(const sport of ['football','mens-basketball','volleyball','soccer']){
await p.goto(base+'/college?sport='+sport);
for(const view of ['results','standings','rankings','news','schedule']){
const target=(view==='schedule'?'/college':'/college/'+view)+'?sport='+sport;await p.selectOption('#college-view',target);await p.waitForURL(base+target);assert.equal(await p.locator('#college-sport').inputValue(),sport);assert.equal(await p.locator('#college-view').inputValue(),target);
}
}
await p.goto(base+'/college/results?sport=football');await p.selectOption('#college-sport','soccer');await p.waitForURL(u=>u.pathname==='/college/results'&&u.searchParams.get('sport')==='soccer');
await p.goto(base+'/f1');await p.selectOption('#f1-view','/f1/standings');await p.waitForURL(base+'/f1/standings');if(await p.locator('#f1-table').count()){await p.selectOption('#f1-table','/f1/standings?table=constructors');await p.waitForURL('**table=constructors');}
const plain=await b.newPage({javaScriptEnabled:false,viewport:{width:320,height:900}});for(const [route,id,value] of [['/college?sport=soccer','college-view','/college/results?sport=soccer'],['/f1','f1-view','/f1/results'],['/local','local-section','/local#local-events']]){
await plain.goto(base+route);await plain.selectOption('#'+id,value);await plain.locator('form').filter({has:plain.locator('#'+id)}).getByRole('button',{name:'Go',exact:true}).click();await plain.waitForURL(base+value);
}
for(const target of ['https://example.com','//example.com','/\\example.com','javascript:alert(1)']){const r=await p.request.get(base+'/navigate?to='+encodeURIComponent(target),{maxRedirects:0});assert.equal(r.status(),302);assert.equal(r.headers().location,'/');}
assert.deepEqual(errors,[]);console.log('Section dropdowns: phone 320/390/430 light/dark and 768/1440, all College sports/views, F1 tables, no-JS Go and local-only redirects passed.');
}finally{await b.close()}})().catch(e=>{console.error(e);process.exit(1)});
