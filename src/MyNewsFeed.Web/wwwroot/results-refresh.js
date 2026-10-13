// Refresh same-day final scores while the Results page is visible.
window.setInterval(function () {
    if (!document.hidden && document.querySelector('[data-results-refresh]')) window.location.reload();
}, 120000);
