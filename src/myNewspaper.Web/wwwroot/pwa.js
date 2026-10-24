// Installation and the offline screen require a secure context (HTTPS, or localhost during development).
if ('serviceWorker' in navigator && window.isSecureContext) {
    window.addEventListener('load', () => {
        navigator.serviceWorker.register('/service-worker.js', { scope: '/', updateViaCache: 'none' })
            .catch(error => console.warn('myNewspaper offline screen could not be enabled.', error));
    });
}
