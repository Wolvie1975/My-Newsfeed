// Runs in <head>, before first paint, so the page never flashes the wrong theme.
// Saved choice wins; otherwise follow the operating system.
(function () {
    function readMode() {
        var saved;
        try { saved = localStorage.getItem('theme'); } catch (e) { }
        return ['light', 'dark', 'sun'].includes(saved) ? saved : 'system';
    }
    function resolve(mode, now) {
        if (mode === 'light' || mode === 'dark') return mode;
        if (mode === 'sun' && window.MyDailyFeedSolar) {
            try { return MyDailyFeedSolar.theme(now || new Date(), MyDailyFeedSolar.location); } catch (e) { }
        }
        return window.matchMedia && matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    window.MyDailyFeedTheme = { readMode: readMode, resolve: resolve };
    document.documentElement.setAttribute('data-theme', resolve(readMode()));
    var scheme;
    try { scheme = localStorage.getItem('mynewsfeed-color-scheme'); } catch (e) { }
    document.documentElement.setAttribute('data-color-scheme', ['ribbon', 'monochrome', 'kansas', 'cardinals', 'city', 'christmas', 'halloween', 'st-patricks', 'eighties', 'nineties', 'y2k', 'desert', 'coffee'].includes(scheme) ? scheme : 'ribbon');
})();
