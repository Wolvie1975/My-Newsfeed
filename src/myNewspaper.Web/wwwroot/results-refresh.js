// Refresh same-day final scores while the Results page is visible.
window.setInterval(() => {
    if (!document.hidden && !document.querySelector('dialog[open]') && document.querySelector('[data-results-refresh]'))
        window.location.reload();
}, 120000);
