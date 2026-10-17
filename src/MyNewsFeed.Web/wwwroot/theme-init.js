// Runs in <head>, before first paint, so the page never flashes the wrong theme.
// Saved choice wins; otherwise follow the operating system.
(function () {
    var theme = null;
    try { theme = localStorage.getItem('theme'); } catch (e) { }
    if (theme !== 'light' && theme !== 'dark') {
        theme = window.matchMedia && matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    document.documentElement.setAttribute('data-theme', theme);
    var scheme;
    try { scheme = localStorage.getItem('mynewsfeed-color-scheme'); } catch (e) { }
    document.documentElement.setAttribute('data-color-scheme', ['monochrome', 'kansas', 'cardinals', 'city', 'christmas', 'halloween', 'st-patricks', 'eighties', 'nineties', 'y2k', 'desert', 'coffee'].includes(scheme) ? scheme : 'kansas');
})();
