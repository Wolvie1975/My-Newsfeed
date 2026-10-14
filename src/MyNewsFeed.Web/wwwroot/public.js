// Behaviour for the public feed: the light/dark toggle and "Load more". Loaded once for the whole document and
// driven by event delegation, so it keeps working after Blazor swaps page content during navigation
// (scripts inside swapped content are not re-run).
(function () {
    var root = document.documentElement;

    // Close the section menu on selection, outside clicks, or Escape.
    document.addEventListener('click', function (event) {
        document.querySelectorAll('[data-mobile-nav][open]').forEach(function (menu) {
            if (!menu.contains(event.target) || event.target.closest('a')) menu.open = false;
        });
    });
    document.addEventListener('keydown', function (event) {
        if (event.key !== 'Escape') return;
        document.querySelectorAll('[data-mobile-nav][open]').forEach(function (menu) {
            var hadFocus = menu.contains(document.activeElement);
            menu.open = false;
            if (hadFocus) menu.querySelector('summary').focus();
        });
    });

    // ---- theme ----------------------------------------------------------------------------------------------

    function currentTheme() {
        return root.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
    }

    function syncToggles() {
        var dark = currentTheme() === 'dark';
        document.querySelectorAll('[data-theme-toggle]').forEach(function (button) {
            button.setAttribute('aria-label', dark ? 'Switch to light mode' : 'Switch to dark mode');
            button.setAttribute('aria-pressed', String(dark));
        });
    }

    // The theme in force. Blazor's in-page navigation (News <-> Sports Events) copies the new page's <html> attributes
    // over the old ones, which has no data-theme, so the attribute is restored below whenever it is changed behind
    // our back. Without this, switching pages would fall back to the operating system's theme.
    var applied = currentTheme();

    function setTheme(theme, persist) {
        applied = theme;
        root.setAttribute('data-theme', theme);
        if (persist) {
            try { localStorage.setItem('theme', theme); } catch (e) { }
        }
        syncToggles();
    }

    new MutationObserver(function () {
        if (root.getAttribute('data-theme') !== applied) {
            root.setAttribute('data-theme', applied);
            syncToggles();
        }
    }).observe(root, { attributes: true, attributeFilter: ['data-theme'] });

    // Follow the operating system until the visitor makes their own choice.
    if (window.matchMedia) {
        var query = matchMedia('(prefers-color-scheme: dark)');
        if (query.addEventListener) {
            query.addEventListener('change', function (e) {
                var saved = null;
                try { saved = localStorage.getItem('theme'); } catch (err) { }
                if (!saved) setTheme(e.matches ? 'dark' : 'light', false);
            });
        }
    }

    // ---- saved articles -------------------------------------------------------------------------------------
    // Kept only in this browser. Each entry is a copy of what the card showed (from the button's data-* attributes),
    // because the scraper keeps only the latest articles per source and the database row will be gone later.

    var SAVED_KEY = 'saved-articles';
    var SAVED_MAX = 500;

    // Storage can be missing or throw (private windows, blocked site data); then the save buttons stay hidden.
    var storageOk = (function () {
        try { localStorage.setItem('saved-probe', '1'); localStorage.removeItem('saved-probe'); return true; }
        catch (e) { return false; }
    })();

    // Only http(s) addresses are ever used as a link or picture, even from our own storage.
    function safeUrl(value) {
        if (typeof value !== 'string' || !value) return null;
        try {
            var url = new URL(value);
            return url.protocol === 'http:' || url.protocol === 'https:' ? url.href : null;
        } catch (e) { return null; }
    }

    function text(value) { return typeof value === 'string' ? value : ''; }

    var savedCache = null;
    function readSaved() {
        if (savedCache !== null) return savedCache;
        if (!storageOk) return [];
        try {
            var list = JSON.parse(localStorage.getItem(SAVED_KEY) || '[]');
            savedCache = Array.isArray(list) ? list.filter(function (a) { return a && safeUrl(a.url); }) : [];
            return savedCache;
        } catch (e) { return []; }
    }

    function writeSaved(list) {
        try { var trimmed = list.slice(0, SAVED_MAX); localStorage.setItem(SAVED_KEY, JSON.stringify(trimmed)); savedCache = trimmed; return true; }
        catch (e) { return false; }
    }

    function fromButton(button) {
        return {
            url: button.getAttribute('data-url'),
            title: text(button.getAttribute('data-title')),
            source: text(button.getAttribute('data-source')),
            image: button.getAttribute('data-image') || null,
            summary: text(button.getAttribute('data-summary')),
            date: button.getAttribute('data-date') || null,
            dateLabel: button.getAttribute('data-date-label') || null,
            savedAt: new Date().toISOString(),
        };
    }

    function el(tag, className, content) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (content) node.textContent = content;
        return node;
    }

    // The same markup as a News card (FeedArticles.razor), built with DOM calls so saved text is never parsed as HTML.
    function savedCard(a) {
        var href = safeUrl(a.url);
        var card = el('article', 'article');

        var thumb = el('div', 'thumb');
        thumb.setAttribute('aria-hidden', 'true');
        var image = safeUrl(a.image);
        if (image) {
            var img = el('img');
            img.src = image; img.alt = ''; img.loading = 'lazy'; img.decoding = 'async';
            img.referrerPolicy = 'no-referrer';
            img.onerror = function () { img.remove(); };
            thumb.appendChild(img);
        }
        card.appendChild(thumb);

        var body = el('div', 'body');
        var title = el('h2', 'title');
        var link = el('a', null, text(a.title) || href);
        link.href = href; link.target = '_blank'; link.rel = 'noopener noreferrer';
        title.appendChild(link);
        body.appendChild(title);
        if (text(a.summary)) body.appendChild(el('p', 'summary', a.summary));

        var src = el('p', 'src');
        var source = text(a.source);
        var mark = el('span', 'src__mark', (source.match(/[A-Za-z0-9]/) || ['?'])[0].toUpperCase());
        mark.setAttribute('aria-hidden', 'true');
        src.appendChild(mark);
        src.appendChild(el('span', 'src__name', source));
        if (text(a.dateLabel)) {
            var sep = el('span', 'src__sep', '·');
            sep.setAttribute('aria-hidden', 'true');
            src.appendChild(sep);
            var time = el('time', null, a.dateLabel);
            if (text(a.date)) time.setAttribute('datetime', a.date);
            src.appendChild(time);
        }

        var share = el('button', 'share-button');
        share.type = 'button'; share.hidden = true;
        share.setAttribute('data-share-open', '');
        share.setAttribute('data-url', href);
        share.setAttribute('data-title', text(a.title));
        share.setAttribute('aria-label', 'Share article');
        share.title = 'Share article';
        share.innerHTML = SHARE_ICON;
        src.appendChild(share);

        // The card's own bookmark, pressed: clicking it removes the article from the list.
        var button = el('button', 'save');
        button.type = 'button';
        button.setAttribute('data-save', '');
        button.setAttribute('data-url', href);
        button.setAttribute('data-title', text(a.title));
        button.innerHTML = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h12a1 1 0 0 1 1 1v17l-7-4.5L5 21V4a1 1 0 0 1 1-1z"></path></svg>';
        src.appendChild(button);
        body.appendChild(src);

        var read = el('button', 'nf-btn nf-btn--secondary', a.read === true ? 'Mark unread' : 'Mark read');
        read.type = 'button'; read.setAttribute('data-mark-read', href);
        read.setAttribute('aria-pressed', String(a.read === true));
        body.appendChild(read);
        card.appendChild(body);
        return card;
    }

    // Draws the Saved page from storage. Skipped when nothing changed, because this runs on every DOM change
    // (and its own drawing is a DOM change).
    function renderSavedPage(list) {
        var container = document.querySelector('[data-saved-list]');
        if (!container) return;
        var filter = document.querySelector('[data-reading-filter]');
        var mode = filter ? filter.value : 'all';
        var total = list.length;
        list = list.filter(function (a) { return mode === 'all' || (mode === 'read' ? a.read === true : a.read !== true); });
        var signature = mode + ':' + total + ':' + list.map(function (a) { return a.url + ':' + (a.read === true); }).join('\n');
        var count = document.querySelector('[data-reading-count]');
        if (count) { count.hidden = total === 0; var label = mode === 'all' ? total + ' saved articles' : list.length + ' of ' + total + ' saved articles'; if (count.textContent !== label) count.textContent = label; }
        if (container.getAttribute('data-rendered') === signature) return;
        container.setAttribute('data-rendered', signature);

        container.textContent = '';
        list.forEach(function (a) { container.appendChild(savedCard(a)); });

        var empty = document.querySelector('[data-saved-empty]');
        if (empty) {
            empty.hidden = list.length > 0;
            var filtered = total > 0;
            empty.querySelector('[data-saved-empty-title]').textContent = filtered ? 'No articles match this filter' : 'Your reading list is empty';
            empty.querySelector('[data-saved-empty-description]').textContent = filtered ? 'Try viewing all saved articles.' : 'Tap the bookmark on a news article to save it for later in this browser.';
            empty.querySelector('[data-saved-empty-action]').hidden = filtered;
            empty.querySelector('[data-show-all-saved]').hidden = !filtered;
        }
    }

    function syncSaves(scope) {
        scope = scope || document;
        var list = readSaved();
        var saved = {};
        list.forEach(function (a) { saved[a.url] = true; });

        matchesWithin(scope, '[data-save]').forEach(function (button) {
            var on = saved[button.getAttribute('data-url')] === true;
            button.hidden = !storageOk;
            if (button.getAttribute('aria-pressed') !== String(on)) button.setAttribute('aria-pressed', String(on));
            var label = on ? 'Remove from saved' : 'Save for later';
            if (button.getAttribute('aria-label') !== label) {
                button.setAttribute('aria-label', label);
                button.setAttribute('title', label);
            }
        });

        matchesWithin(scope, '[data-saved-count]').forEach(function (badge) {
            var value = list.length > 99 ? '99+' : String(list.length);
            badge.hidden = list.length === 0;
            if (badge.textContent !== value) badge.textContent = value;
        });

        var toolbar = document.querySelector("[data-reading-toolbar]");
        if (toolbar) toolbar.hidden = !storageOk || list.length === 0;
        if (scope === document || matchesWithin(scope, '[data-saved-list]').length) renderSavedPage(list);
    }

    function toggleSave(button) {
        var url = button.getAttribute('data-url');
        if (!storageOk || !safeUrl(url)) return;
        var list = readSaved().map(function (article) { return Object.assign({}, article); });
        var index = list.findIndex(function (a) { return a.url === url; });
        var title = button.getAttribute('data-title') || 'Article';
        var message;
        if (index >= 0) {
            list.splice(index, 1);
            message = 'Removed from saved: ' + title;
        } else {
            list.unshift(fromButton(button));
            message = 'Saved: ' + title;
        }
        if (!writeSaved(list)) message = 'Could not save: this browser\'s storage is full or blocked.';

        announce(message);
        syncSaves();
    }

    // Tells screen readers what just happened, through the page's own status line. Pages without one (the home page)
    // get a hidden one, added once.
    function announce(message) {
        var status = document.querySelector('[data-saved-status]') || document.querySelector('[data-load-status]')
            || document.querySelector('[data-copy-status]');
        if (!status) {
            status = el('span', 'sr-only');
            status.setAttribute('role', 'status');
            status.setAttribute('data-copy-status', '');
            (document.querySelector('.pub') || document.body).appendChild(status);
        }
        status.textContent = message;
    }

    // ---- article sharing ------------------------------------------------------------------------------------

    var SHARE_ICON = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/><path d="m8.6 10.5 6.8-4M8.6 13.5l6.8 4"/></svg>';

    function matchesWithin(scope, selector) {
        var elements = Array.from(scope.querySelectorAll(selector));
        if (scope.nodeType === 1 && scope.matches(selector)) elements.unshift(scope);
        return elements;
    }

    function syncShares(scope) {
        matchesWithin(scope || document, '[data-share-open][hidden]').forEach(function (button) { button.hidden = false; });
    }

    function openShare(button) {
        var url = safeUrl(button.getAttribute('data-url'));
        var host = button.closest('.pub');
        if (!url || !host) return;
        var title = button.getAttribute('data-title') || 'Article';
        var dialog = el('dialog', 'article-share');
        var heading = el('h2', null, 'Share article');
        heading.id = 'article-share-title';
        dialog.setAttribute('aria-labelledby', heading.id);
        dialog.appendChild(heading);
        dialog.appendChild(el('p', 'article-share__title', title));
        var copy = el('button', 'nf-btn nf-btn--secondary', 'Copy link');
        copy.type = 'button';
        copy.addEventListener('click', function () {
            writeClipboard(url).then(function () { announce('Link copied: ' + title); dialog.close(); },
                function () { announce('Could not copy the link.'); });
        });
        dialog.appendChild(copy);
        var phone = (navigator.userAgentData && navigator.userAgentData.mobile === true)
            || /iPhone|iPod|Android.*Mobile/i.test(navigator.userAgent);
        if (phone) {
            var sms = el('a', 'nf-btn nf-btn--secondary', 'Send by SMS');
            sms.href = 'sms:' + (/iPhone|iPod/i.test(navigator.userAgent) ? '&' : '?')
                + 'body=' + encodeURIComponent(title + '\n' + url);
            dialog.appendChild(sms);
        }
        if (window.isSecureContext && typeof navigator.share === 'function') {
            var apps = el('button', 'nf-btn nf-btn--secondary', 'Share via apps');
            apps.type = 'button';
            apps.addEventListener('click', function () {
                try {
                    Promise.resolve(navigator.share({ title: title, url: url })).then(function () { dialog.close(); }, function (error) {
                        if (error.name !== 'AbortError') announce('Could not open sharing. Try copying the link.');
                    });
                } catch (error) { announce('Could not open sharing. Try copying the link.'); }
            });
            dialog.appendChild(apps);
        }
        if (phone) dialog.appendChild(el('p', 'article-share__hint', 'SMS opens your messaging app. Choose a recipient and send when you’re ready.'));
        var close = el('button', 'nf-btn nf-btn--secondary', 'Close');
        close.type = 'button';
        close.addEventListener('click', function () { dialog.close(); });
        dialog.appendChild(close);
        dialog.addEventListener('close', function () { dialog.remove(); if (button.isConnected) button.focus(); });
        host.appendChild(dialog);
        dialog.showModal();
    }

    // navigator.clipboard exists only on https and localhost; over plain http (the site on the LAN) fall back to
    // the older copy command on a temporary text box.
    function writeClipboard(value) {
        if (navigator.clipboard && window.isSecureContext) {
            return navigator.clipboard.writeText(value);
        }
        return new Promise(function (resolve, reject) {
            var box = el('textarea');
            box.value = value;
            box.setAttribute('readonly', '');
            box.style.position = 'fixed';
            box.style.opacity = '0';
            (document.querySelector('dialog[open]') || document.body).appendChild(box);
            box.select();
            var ok = false;
            try { ok = document.execCommand('copy'); } catch (e) { }
            box.remove();
            if (ok) resolve(); else reject(new Error('copy failed'));
        });
    }

    document.addEventListener('change', function (e) {
        if (e.target.matches('[data-reading-filter]')) syncSaves();
    });
    // Another tab saved or removed something.
    window.addEventListener('storage', function (e) { if (e.key === SAVED_KEY || e.key === null) { savedCache = null; syncSaves(); } });

    // Blazor re-renders the toggle button on navigation (and swaps in new pages with fresh save buttons); put labels,
    // pressed states and the saved list back in step.
    var addedScopes = new Set();
    var pending = false;
    new MutationObserver(function (records) {
        records.forEach(function (record) {
            record.addedNodes.forEach(function (node) {
                if (node.nodeType !== 1) return;
                var selector = '[data-share-open], [data-save], [data-saved-list], [data-saved-count], [data-theme-toggle]';
                if (node.matches(selector) || node.querySelector(selector)) addedScopes.add(node);
            });
        });
        if (pending || !addedScopes.size) return;
        pending = true;
        requestAnimationFrame(function () {
            pending = false;
            var scopes = Array.from(addedScopes); addedScopes.clear();
            scopes.filter(function (node) { return node.isConnected && !scopes.some(function (other) { return other !== node && other.contains(node); }); })
                .forEach(function (node) { syncSaves(node); syncShares(node); });
            syncToggles();
        });
    }).observe(document.body, { childList: true, subtree: true });
    syncToggles();
    syncSaves();
    syncShares();

    // ---- load more ------------------------------------------------------------------------------------------

    function loadMore(link) {
        var feed = document.querySelector('.feed');
        if (!feed || link.getAttribute('aria-busy') === 'true') {
            if (!feed) window.location.href = link.href;
            return;
        }

        // The link's own href is the no-JavaScript fallback; reuse its filters and cursor for the fetch.
        var endpoint = new URL('feed/more', document.baseURI);
        new URL(link.href).searchParams.forEach(function (value, key) { endpoint.searchParams.set(key, value); });
        endpoint.searchParams.set('day', feed.getAttribute('data-last-day') || '');
        endpoint.searchParams.set('view', link.getAttribute('data-feed-view') || 'news');

        var label = link.textContent;
        link.setAttribute('aria-busy', 'true');
        link.textContent = 'Loading…';

        fetch(endpoint, { headers: { 'Accept': 'application/json' }, credentials: 'same-origin' })
            .then(function (response) {
                if (!response.ok) throw new Error('HTTP ' + response.status);
                return response.json();
            })
            .then(function (data) {
                var before = feed.querySelectorAll('.article').length;
                feed.insertAdjacentHTML('beforeend', data.html);
                if (data.lastDay) feed.setAttribute('data-last-day', data.lastDay);

                document.querySelectorAll('[data-shown-total]').forEach(function (el) {
                    el.textContent = 'Showing ' + data.shown + ' of ' + data.total;
                });
                var status = document.querySelector('[data-load-status]');
                if (status) status.textContent = data.added + ' more articles loaded';

                if (data.nextHref) {
                    link.href = data.nextHref;
                    link.textContent = label;
                    link.removeAttribute('aria-busy');
                } else {
                    link.remove();
                }

                // Move focus to the first new article so keyboard and screen-reader users continue from there.
                var first = feed.querySelectorAll('.article')[before];
                var target = first && first.querySelector('.title a');
                if (target) target.focus();
            })
            .catch(function () {
                // Fall back to the plain link: a normal page load of the next batch.
                window.location.href = link.href;
            });
    }

    // Capture phase, so this runs before Blazor's own link handling: it must not also navigate to the
    // fallback URL when "Load more" is clicked.
    // Search and category forms are plain GET forms. Leave out empty fields so "All categories" and an empty search
    // give a clean URL instead of "?category=&q=". Capture phase: runs before the form is serialised.
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form.closest || !form.closest('.pub') || (form.method || '').toLowerCase() !== 'get') return;
        var blanks = [];
        form.querySelectorAll('select[name], input[name]').forEach(function (el) {
            if (el.value === '' && !el.disabled) { el.disabled = true; blanks.push(el); }
        });
        // The browser (or Blazor) reads the form during this same event, so re-enabling afterwards is safe.
        setTimeout(function () { blanks.forEach(function (el) { el.disabled = false; }); }, 0);
    }, true);

    document.addEventListener('click', function (e) {
        if (!e.target.closest) return;

        if (e.target.closest('[data-theme-toggle]')) {
            setTheme(currentTheme() === 'dark' ? 'light' : 'dark', true);
            return;
        }

        if (e.target.closest('[data-show-all-saved]')) { var filter = document.querySelector('[data-reading-filter]'); if (filter) filter.value = 'all'; syncSaves(); return; }

        var read = e.target.closest('[data-mark-read]');
        if (read) {
            var list = readSaved().map(function (article) { return Object.assign({}, article); });
            var article = list.find(function (a) { return a.url === read.getAttribute('data-mark-read'); });
            if (article) {
                article.read = article.read !== true;
                if (!writeSaved(list)) announce('Could not update reading status. Browser storage is full or blocked.');
                syncSaves();
            }
            return;
        }
        var save = e.target.closest('[data-save]');
        if (save) {
            toggleSave(save);
            return;
        }

        var share = e.target.closest('[data-share-open]');
        if (share) { openShare(share); return; }

        var more = e.target.closest('[data-load-more]');
        if (more && !(e.ctrlKey || e.metaKey || e.shiftKey || e.button > 0)) {
            e.preventDefault();
            e.stopPropagation();
            loadMore(more);
        }
    }, true);
})();
