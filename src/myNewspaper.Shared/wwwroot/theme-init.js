// Runs in <head>, before first paint, so the page never flashes the wrong theme.
// With a profile open, the server puts its choices on <html> (data-theme-mode, data-color-scheme); otherwise (the
// edition picker, or profiles switched off) this browser's saved choice wins, then the operating system.
(function () {
    const html = document.documentElement;
    const fromProfile = html.hasAttribute('data-theme-mode');
    function readMode() {
        let saved = html.getAttribute('data-theme-mode');
        if (!saved) {
            try {
                saved = localStorage.getItem('theme');
            } catch (e) {}
        }
        return ['light', 'dark', 'sun'].includes(saved) ? saved : 'system';
    }
    function resolve(mode, now) {
        if (mode === 'light' || mode === 'dark') return mode;
        if (mode === 'sun' && window.myNewspaperSolar) {
            try {
                return myNewspaperSolar.theme(now || new Date(), myNewspaperSolar.location);
            } catch (e) {}
        }
        return window.matchMedia && matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    window.myNewspaperTheme = { readMode: readMode, resolve: resolve };
    document.documentElement.setAttribute('data-theme', resolve(readMode()));
    let scheme = fromProfile ? html.getAttribute('data-color-scheme') : null;
    if (!fromProfile) {
        try {
            scheme = localStorage.getItem('mynewsfeed-color-scheme');
        } catch (e) {}
    }
    document.documentElement.setAttribute(
        'data-color-scheme',
        [
            'ribbon',
            'monochrome',
            'kansas',
            'cardinals',
            'city',
            'christmas',
            'halloween',
            'st-patricks',
            'eighties',
            'nineties',
            'y2k',
            'desert',
            'coffee',
            'coastal',
            'olive',
            'golden-hour',
        ].includes(scheme)
            ? scheme
            : 'ribbon',
    );
})();
