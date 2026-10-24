const assert=require('node:assert/strict'),{chromium}=require(process.env.PLAYWRIGHT_MODULE||'/tmp/mydailyfeed-cascade-check/node_modules/playwright');
const base=process.env.BASE_URL||'http://127.0.0.1:5314',root=process.env.SCREENSHOT_DIR;
const sections=[
 ['sports/news','Sports headlines','sports-news','#source'],
 ['sports/news/basketball','Sports headlines','mens-college-basketball-news','#source'],
 ['sports/news/big12','Sports headlines','sports-big12-news','#source'],
 ['sports/news/f1','Sports headlines','sports-f1-news','#source']
];
(async()=>{const browser=await chromium.launch();try{
 const p=await browser.newPage();const errors=[];p.on('pageerror',e=>errors.push(e.message));const movedSources=new Set();
 for(const [route,title,view,selector] of sections){
  await p.goto(base+'/'+route);assert.equal(await p.locator('h1').innerText(),title);
  assert.equal(await p.locator('.news-section-tabs a[aria-current="page"]').getAttribute('href'),route);
  const tab=p.locator('.news-section-tabs a').last();await tab.click();await p.waitForURL(base+'/sports/news/f1');
  await p.getByRole('heading',{name:'F1',exact:true}).waitFor();assert.equal(await p.locator('h2').first().innerText(),'F1');await p.goto(base+'/'+route);
  const ids=await p.locator(selector+' option').evaluateAll(ns=>ns.map(n=>n.value).filter(Boolean));assert(ids.length);ids.forEach(id=>movedSources.add(id));
  if(selector==='#source'){
   const category=await p.locator('form input[name="category"]').inputValue();
   const alias=await p.request.get(base+'/sports/news/category-'+category);assert.equal(alias.status(),200);


   const redirect=await p.request.get(base+'/news?category='+category+'&source='+ids[0],{maxRedirects:0});assert.equal(redirect.status(),302);
   const url=new URL(redirect.headers().location,base);assert.equal(url.pathname,'/'+route);assert.equal(url.searchParams.get('source'),ids[0]);
  }
  const before=await p.locator('.feed .article').count(),more=p.locator('[data-load-more]');
  if(await more.count()){
   const promise=p.waitForResponse(r=>r.url().includes('/feed/more?'));await more.click();const response=await promise;
   assert.equal(new URL(response.url()).searchParams.get('view'),'sports-hub-news');assert.equal(response.status(),200);
   const result=await response.json();if(result.nextHref)assert(result.nextHref.startsWith(route+'?'));
   await p.waitForFunction(n=>document.querySelectorAll('.feed .article').length>n,before);
   const allIds=await p.locator('.feed .article').evaluateAll(ns=>ns.map(n=>n.dataset.articleId));assert.equal(allIds.length,new Set(allIds).size);
   if(selector==='#source'){
    const params=new URL(response.url()).searchParams;params.set('view','entertainment-hub-news');assert.equal((await p.request.get(base+'/feed/more?'+params)).status(),404);params.set('view','news');const suppressed=await p.request.get(base+'/feed/more?'+params);
    assert.equal((await suppressed.json()).total,0);
   }
  }
  await p.selectOption(selector,ids[0]);await p.waitForURL(u=>u.searchParams.get('source')===ids[0]);assert.equal(new URL(p.url()).pathname,'/'+route);
  for(const width of [320,390,430,768,1440])for(const theme of ['light','dark']){
   await p.setViewportSize({width,height:900});await p.emulateMedia({colorScheme:theme});await p.goto(base+'/'+route);
   assert.equal(await p.locator('html').getAttribute('data-theme'),theme);assert(await p.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));assert(!await p.locator('#blazor-error-ui').isVisible());
   if(width<768){
    await p.locator('.mobile-nav > summary').click();const group=p.locator('[aria-labelledby="mobile-sports-heading"]'),link=group.locator('a[href="sports/news"]');
    await link.scrollIntoViewIfNeeded();assert.equal(await link.getAttribute('aria-current'),'page');assert((await link.boundingBox()).height>=44);
    assert.equal(await p.locator('[aria-labelledby="mobile-news-heading"] a[href="'+route+'"]').count(),0);
    assert.equal(await p.locator('.mobile-nav a[aria-current="page"]').count(),1);assert(await p.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
    await p.locator('.mobile-nav > summary').click();await p.waitForTimeout(250);
   }else{const sports=p.locator('.desktop-nav-group').filter({has:p.locator('summary',{hasText:'Sports'})});assert(await sports.locator('summary').evaluate(n=>n.classList.contains('is-current')))}
   if(root&&width<768)await p.screenshot({path:root+'/'+route.replaceAll('/','-')+'-'+width+'-'+theme+'.png'});
  }
 }
 for(const [route,label] of [['sports/mens-college-basketball/news','Men’s college basketball'],['college/news','Big 12 News'],['f1/news','F1 News']]){await p.goto(base+'/'+route);assert((await p.locator('main').innerText()).includes(label))}
 await p.goto(base+'/news?category=all');const names=await p.locator('#category option').allTextContents();
 for(const name of ['Sports','Big 12 News','F1 News',"Men's College Basketball News",'Entertainment','TV/Film'])assert(!names.includes(name),name);
 assert((await p.locator('#source option').evaluateAll(ns=>ns.map(n=>n.value))).every(id=>!movedSources.has(id)));
 const plain=await browser.newPage({javaScriptEnabled:false,viewport:{width:390,height:900}});
 for(const [route] of sections){await plain.goto(base+'/'+route);const more=plain.locator('[data-load-more]');if(await more.count()){await more.click();await plain.waitForURL(u=>u.searchParams.has('after'));assert.equal(new URL(plain.url()).pathname,'/'+route);assert(await plain.locator('.feed .article').count()>0)}assert(await plain.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));await plain.locator('.mobile-nav > summary').click();assert(await plain.locator('[aria-labelledby="mobile-sports-heading"] a[href="sports/news"]').count())}
 assert.deepEqual(errors,[]);await plain.close();console.log('Sports headline hub tabs, existing F1/Big12 readers, legacy links, source filters, scoped JS/no-JS paging, Headlines exclusion and mobile light/dark navigation passed.');
}finally{await browser.close()}})().catch(e=>{console.error(e);process.exit(1)});
