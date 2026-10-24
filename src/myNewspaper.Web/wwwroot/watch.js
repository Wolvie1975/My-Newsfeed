(function () {
    'use strict';
    let active;
    document.addEventListener('click', async function (event) {
        const link=event.target.closest('a[data-watch-title]');
        if(!link || event.button!==0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || !window.HTMLDialogElement) return;
        const url=new URL(link.href,location.href);
        if(url.origin!==location.origin || !url.pathname.startsWith('/watch/')) return;
        event.preventDefault();
        event.stopPropagation();
        if(active) active.close();
        const dialog=document.createElement('dialog'); dialog.className='box-score-dialog';
        dialog.setAttribute('aria-label',link.getAttribute('aria-label') || link.textContent.trim());
        const header=document.createElement('div');header.className='box-score-dialog__head';
        const heading=document.createElement('h2');heading.textContent=link.textContent.trim();
        const close=document.createElement('button');close.type='button';close.className='nf-btn nf-btn--secondary';close.textContent='Close';
        const content=document.createElement('div');content.className='box-score-dialog__body';content.setAttribute('aria-busy','true');
        const status=document.createElement('p');status.setAttribute('role','status');status.textContent='Loading viewing options…';content.append(status);
        const controller=new AbortController();
        header.append(heading,close);dialog.append(header,content);document.querySelector('.pub').append(dialog);active=dialog;
        close.addEventListener('click',()=>dialog.close());
        dialog.addEventListener('close',()=>{controller.abort();dialog.remove();if(active===dialog)active=null;if(link.isConnected)link.focus();},{once:true});
        dialog.addEventListener('click',e=>{if(e.target!==dialog)return;const r=dialog.getBoundingClientRect();if(e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom)dialog.close();});
        dialog.showModal();close.focus();
        try {
            const response=await fetch(url.href,{signal:controller.signal});
            if(!response.ok)throw Error('Unavailable');
            const documentCopy=new DOMParser().parseFromString(await response.text(),'text/html');
            const detail=documentCopy.querySelector('[data-watch-content]');if(!detail)throw Error('Unavailable');
            if(!dialog.open)return;
            content.replaceChildren(document.importNode(detail,true));
            const title=content.querySelector('h2');if(title){heading.textContent=title.textContent;title.remove();}
            const matchup=content.querySelector('.watch-detail');if(matchup){matchup.tabIndex=-1;matchup.focus({preventScroll:true});dialog.scrollTop=0;}
        }catch(error){if(!dialog.open)return;status.textContent='Viewing options could not be loaded. Please try again or open the title page.';
            const fallback=document.createElement('a');fallback.href=url.href;fallback.textContent='Open viewing options';content.replaceChildren(status,fallback);
        }finally{content.removeAttribute('aria-busy');}
    }, true);
    new MutationObserver(()=>{if(active && !document.querySelector('a[data-watch-title]:focus') && !document.querySelector('.box-score-dialog')){active.close();active=null;}}).observe(document.body,{childList:true});
})();

// Browser-local service preferences; GET forms also work without JavaScript.
(function(){
    const key='mynewsfeed-watch-services-v1';
    function restore(){
        const settings=document.querySelector('[data-watch-settings]');
        if(settings){
            const url=new URL(location.href);let ids=[];
            try{ids=JSON.parse(localStorage.getItem(key)||'[]');}catch{}
            if(!Array.isArray(ids))ids=[];
            if(!url.searchParams.has('service')&&!url.searchParams.has('services'))settings.querySelectorAll('input[name=service]').forEach(n=>n.checked=ids.includes(Number(n.value)));
            const selected=[...settings.querySelectorAll('input[name=service]:checked')].map(n=>n.value);
            settings.querySelector('[data-watch-settings-link]').href='watch?services='+selected.join(',');
        }
        if(location.pathname!='/watch')return;
        const url=new URL(location.href);
        if(url.searchParams.has('service')||url.searchParams.has('services'))return;
        try{const ids=JSON.parse(localStorage.getItem(key)||'[]');if(Array.isArray(ids)){
            const valid=[...new Set(ids.filter(n=>Number.isInteger(n)&&n>0))].slice(0,20);
            if(valid.length){url.searchParams.set('services',valid.join(','));location.replace(url.href);}
        }}catch{}
    }
    document.addEventListener('submit',function(e){
        const form=e.target.closest('[data-watch-services]');if(!form)return;
        const ids=[...form.querySelectorAll('input[name=service]:checked')].map(n=>Number(n.value));
        const status=form.querySelector('[data-watch-services-status]');
        if(form.hasAttribute('data-watch-settings'))e.preventDefault();
        if(ids.length>20){e.preventDefault();status.textContent='Please choose up to 20 services.';return;}
        if(form.hasAttribute('data-watch-settings'))form.querySelector('[data-watch-settings-link]').href='watch?services='+ids.join(',');
        try{localStorage.setItem(key,JSON.stringify(ids));status.textContent='Services saved in this browser.';}catch{status.textContent='Selections will apply, but could not be remembered in this browser.';}
    },true);
    document.addEventListener('click',function(e){
        const button=e.target.closest('[data-watch-services-clear]');if(!button)return;
        const form=button.closest('form');form.querySelectorAll('input[name=service]').forEach(n=>n.checked=false);form.requestSubmit();
    });
    if(window.Blazor?.addEventListener)window.Blazor.addEventListener('enhancedload',restore);restore();
})();
