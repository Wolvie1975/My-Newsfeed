// Article photos only: never enlarge feed thumbnails into feature photography.
(function () {
    'use strict';
    // Enhanced navigation can activate links inside noscript; gate that fallback explicitly.
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
        // A tiny image is removed on its first pass; the load listener classifies it again once it is detached.
        if (!state || !img.isConnected || !img.complete || img.currentSrc !== state.active || !img.naturalWidth) return;
        const width = img.naturalWidth,
            height = img.naturalHeight,
            ratio = width / height;
        const quality =
            width < 96 || height < 64 ? 'none' : width >= 640 && height >= 300 && ratio <= 2.2 ? 'wide' : 'compact';
        img.dataset.articleQuality = quality;
        const media = img.closest('[data-article-media]'),
            story = img.closest('[data-article-story]');
        // Large square and portrait photos still lead, cropped into a landscape frame.
        const cropped = ratio < 1.2;
        media.style.setProperty('--article-image-ratio', cropped ? '3 / 2' : width + ' / ' + height);
        img.classList.toggle('article-image--cropped', quality === 'wide' && cropped);
        media.style.setProperty('--article-image-width', width + 'px');
        if (story) {
            story.dataset.imageLayout = quality;
            // Record that this frame has been measured; cards retain their slot throughout loading.
            story.dataset.imageMeasured = '';
        }

        if (quality === 'none') img.remove();
    }

    function bind(img) {
        if (!img.matches(selector)) return;
        // Measure desktop card photos eagerly so hidden compact images can be classified.
        if (img.closest('.hc-card') && matchMedia('(min-width: 1024px)').matches) img.loading = 'eager';
        const original = img.src;
        if (images.get(img)?.active === original) {
            classify(img);
            return;
        }
        const story = img.closest('[data-article-story]');
        if (story) {
            story.dataset.imageLayout = 'compact';
            delete story.dataset.imageMeasured;
        }
        // Desktop headline cards show every good photo full width, so they ask BBC for its larger size too.
        const feature =
                story?.dataset.articleStory === 'feature' ||
                (story?.classList.contains('hc-card') && matchMedia('(min-width: 1024px)').matches),
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
            if (story) {
                story.dataset.imageLayout = 'none';
                story.dataset.imageMeasured = '';
            }
            img.remove();
        },
        true,
    );

    function scan(root = document) {
        document.documentElement.dataset.articleImages = 'enabled';
        if (root instanceof HTMLImageElement) bind(root);
        root.querySelectorAll?.(selector).forEach(bind);
        const empty = (story) => {
            if (!story.querySelector(selector)) {
                story.dataset.imageLayout = 'none';
                story.dataset.imageMeasured = '';
            }
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
