(function () {
    'use strict';
    let active;
    document.addEventListener('click', async function (event) {
        const link=event.target.closest('a[data-box-score]');
        if(!link || event.button!==0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || !window.HTMLDialogElement) return;
        const url=new URL(link.href,location.href);
        if(url.origin!==location.origin || !url.pathname.startsWith('/box-score/')) return;
        event.preventDefault();
        if(active) active.close();
        const dialog=document.createElement('dialog'); dialog.className='box-score-dialog';
        dialog.setAttribute('aria-label',link.getAttribute('aria-label') || link.textContent.trim());
        const header=document.createElement('div');header.className='box-score-dialog__head';
        const heading=document.createElement('h2');heading.textContent=link.textContent.trim();
        const close=document.createElement('button');close.type='button';close.className='nf-btn nf-btn--secondary';close.textContent='Close';
        const content=document.createElement('div');content.className='box-score-dialog__body';content.setAttribute('aria-busy','true');
        const status=document.createElement('p');status.setAttribute('role','status');status.textContent='Loading game statistics…';content.append(status);
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
            const detail=documentCopy.querySelector('[data-box-content]');if(!detail)throw Error('Unavailable');
            if(!dialog.open)return;
            content.replaceChildren(document.importNode(detail,true));
            const title=content.querySelector('h2');if(title){heading.textContent=title.textContent;title.remove();}
            const matchup=content.querySelector('.box-score-matchup');if(matchup){matchup.tabIndex=-1;matchup.focus();}
        }catch(error){if(!dialog.open)return;status.textContent='Game statistics could not be loaded. Please try again or open the detail page.';
            const fallback=document.createElement('a');fallback.href=url.href;fallback.textContent='Open game statistics';content.replaceChildren(status,fallback);
        }finally{content.removeAttribute('aria-busy');}
    });
    new MutationObserver(()=>{if(active && !document.querySelector('a[data-box-score]:focus') && !document.querySelector('.box-score-dialog')){active.close();active=null;}}).observe(document.body,{childList:true});
})();
