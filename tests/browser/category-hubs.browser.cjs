const assert=require('node:assert/strict'),{chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const base=process.env.BASE_URL||'http://127.0.0.1:5320',root=process.env.SCREENSHOT_DIR;
(async()=>{assert(process.env.CATEGORY_ADMIN_PASSWORD,'CATEGORY_ADMIN_PASSWORD required');const b=await chromium.launch();try{
const p=await b.newPage();const errors=[];p.on('pageerror',e=>errors.push(e.message));await p.goto(base+'/admin/categories');await p.locator('#password').fill(process.env.CATEGORY_ADMIN_PASSWORD);await p.getByRole('button',{name:'Sign in',exact:true}).click();await p.waitForURL('**/admin/categories');
for(const width of [320,390,430,768,1440])for(const theme of ['light','dark']){
 await p.setViewportSize({width,height:900});await p.emulateMedia({colorScheme:theme});await p.goto(base+'/admin/categories');await p.waitForLoadState('networkidle');
 assert(!await p.locator('#blazor-error-ui').isVisible());assert(await p.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
 assert.deepEqual(await p.locator('#new-category-hub option').evaluateAll(ns=>ns.map(n=>n.value)),['headlines','sports','entertainment']);
 for(const value of ['sports','entertainment','headlines'])await p.selectOption('#new-category-hub',value);
 const row=p.locator('tbody tr').first();
 for(let attempt=0;attempt<5&&!await p.getByLabel('Category hub',{exact:true}).count();attempt++){await row.getByRole('button',{name:'Edit',exact:true}).click();await p.waitForTimeout(300);}
 await p.getByLabel('Category hub',{exact:true}).selectOption('sports');await p.getByLabel('Category name',{exact:true}).fill('Draft preview only');
 assert(await p.getByLabel('Category hub',{exact:true}).evaluate(n=>n.getBoundingClientRect().height>=44));
 assert(await p.locator('article.content button').evaluateAll(ns=>ns.every(n=>n.getBoundingClientRect().height>=44)));
 if(width<=430)assert(await p.locator('.table-responsive').evaluate(n=>n.scrollWidth<=n.clientWidth));
 if(root&&width<=430)await p.screenshot({path:root+'/category-editor-'+width+'-'+theme+'.png'});
 await row.getByRole('button',{name:'Cancel',exact:true}).click();await p.getByLabel('Category hub',{exact:true}).waitFor({state:'detached'});assert.equal(await p.getByLabel('Category hub',{exact:true}).count(),0);
 assert(!(await row.innerText()).includes('Draft preview only'));
}
assert.deepEqual(errors,[]);console.log('Category Hub add/edit controls, cancel, 44px targets, 320/390/430/768/1440 light/dark and screenshot checks passed; no category data submitted.');
}finally{await b.close()}})().catch(e=>{console.error(e);process.exit(1)});
