// Behaviour for the public feed: the light/dark toggle and "Load more". Loaded once for the whole document and
// driven by event delegation, so it keeps working after Blazor swaps page content during navigation
// (scripts inside swapped content are not re-run).
(function () {
    var root = document.documentElement;

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

    function setTheme(theme, persist) {
        root.setAttribute('data-theme', theme);
        if (persist) {
            try { localStorage.setItem('theme', theme); } catch (e) { }
        }
        syncToggles();
    }

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

    // Blazor re-renders the toggle button on navigation; put its label back in step with the theme.
    var pending = false;
    new MutationObserver(function () {
        if (pending) return;
        pending = true;
        requestAnimationFrame(function () { pending = false; syncToggles(); });
    }).observe(document.body, { childList: true, subtree: true });
    syncToggles();

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
    document.addEventListener('click', function (e) {
        if (!e.target.closest) return;

        if (e.target.closest('[data-theme-toggle]')) {
            setTheme(currentTheme() === 'dark' ? 'light' : 'dark', true);
            return;
        }

        var more = e.target.closest('[data-load-more]');
        if (more && !(e.ctrlKey || e.metaKey || e.shiftKey || e.button > 0)) {
            e.preventDefault();
            e.stopPropagation();
            loadMore(more);
        }
    }, true);
})();
