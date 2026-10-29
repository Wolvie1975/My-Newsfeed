// Article photos only: never enlarge feed thumbnails into feature photography.
(function () {
    'use strict';
    const selector = '[data-article-media] > img';
    const images = new WeakMap();

    function preferred(original, feature) {
        const url = new URL(original, document.baseURI);
        // Verified BBC image service sizing. Keep the same asset, query and host;
        // other publishers' URLs are left intact rather than guessing their CDN rules.
        if (feature && url.protocol === 'https:' && url.hostname === 'ichef.bbci.co.uk') {
            const match = url.pathname.match(/^\/ace\/standard\/(\d+)\/(.+)$/);
            if (match && Number(match[1]) < 1024) url.pathname = '/ace/standard/1024/' + match[2];
        }
        return url.href;
    }

    function classify(img) {
        const state = images.get(img);
        if (!state || !img.complete || img.currentSrc !== state.active || !img.naturalWidth) return;
        const width = img.naturalWidth,
            height = img.naturalHeight,
            ratio = width / height;
        const quality =
            width < 96 || height < 64
                ? 'none'
                : width >= 640 && height >= 300 && ratio >= 1.5 && ratio <= 2.2
                  ? 'wide'
                  : 'compact';
        img.dataset.articleQuality = quality;
        const media = img.closest('[data-article-media]'),
            story = img.closest('[data-article-story]');
        media.style.setProperty('--article-image-ratio', width + ' / ' + height);
        media.style.setProperty('--article-image-width', width + 'px');
        if (story) story.dataset.imageLayout = quality;
        if (quality === 'none') img.remove();
    }

    function bind(img) {
        if (!img.matches(selector)) return;
        const original = img.src;
        if (images.get(img)?.active === original) {
            classify(img);
            return;
        }
        const story = img.closest('[data-article-story]');
        if (story) story.dataset.imageLayout = 'compact';
        const feature = story?.dataset.articleStory === 'feature',
            active = preferred(original, feature);
        images.set(img, { original, active, fallback: false });
        if (active !== original) img.src = active;
        else classify(img);
    }

    document.addEventListener(
        'load',
        (event) => {
            if (event.target instanceof HTMLImageElement && event.target.matches(selector)) {
                bind(event.target);
                classify(event.target);
            }
        },
        true,
    );
    document.addEventListener(
        'error',
        (event) => {
            const img = event.target;
            if (!(img instanceof HTMLImageElement) || !img.matches(selector)) return;
            event.stopImmediatePropagation();
            const state = images.get(img);
            if (state && state.active !== state.original && !state.fallback) {
                // Preserve the original instead of the inline broken-image removal.
                state.fallback = true;
                state.active = state.original;
                img.src = state.original;
                return;
            }
            const story = img.closest('[data-article-story]');
            if (story) story.dataset.imageLayout = 'none';
            img.remove();
        },
        true,
    );

    function scan(root = document) {
        if (root instanceof HTMLImageElement) bind(root);
        root.querySelectorAll?.(selector).forEach(bind);
        const empty = (story) => {
            if (!story.querySelector(selector)) story.dataset.imageLayout = 'none';
        };
        if (root.matches?.('[data-article-story]')) empty(root);
        root.querySelectorAll?.('[data-article-story]').forEach(empty);
    }
    new MutationObserver((records) => {
        for (const record of records) {
            if (record.type === 'attributes') bind(record.target);
            else
                record.addedNodes.forEach((node) => {
                    if (node.nodeType === 1) scan(node);
                });
        }
    }).observe(document.body, { childList: true, subtree: true, attributes: true, attributeFilter: ['src'] });
    if (window.Blazor?.addEventListener) window.Blazor.addEventListener('enhancedload', () => scan());
    scan();
})();
