// Runs in <head>, before first paint, so the page never flashes the wrong theme.
// Saved choice wins; otherwise follow the operating system.
(function () {
    var theme = null;
    try { theme = localStorage.getItem('theme'); } catch (e) { }
    if (theme !== 'light' && theme !== 'dark') {
        theme = window.matchMedia && matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    document.documentElement.setAttribute('data-theme', theme);
})();
