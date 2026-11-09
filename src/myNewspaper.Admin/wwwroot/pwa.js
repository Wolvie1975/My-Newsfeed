if ('serviceWorker' in navigator && window.isSecureContext) {
    window.addEventListener('load', () => {
        navigator.serviceWorker
            .register('/service-worker.js', { scope: '/', updateViaCache: 'none' })
            .catch((error) => console.warn('myNewspaper Admin offline screen could not be enabled.', error));
    });
}
