// Only app identity assets and the offline screen are cached. Admin pages, APIs and mutations always use the network.
const CACHE = 'mynewspaper-admin-pwa-v1';
const OFFLINE = '/offline.html';
const ASSETS = [
    OFFLINE,
    '/manifest.webmanifest',
    '/icons/newspaper-page-192.png',
    '/icons/newspaper-page-512.png',
    '/icons/newspaper-page-maskable-512.png',
];
self.addEventListener('install', (event) => {
    event.waitUntil(
        caches
            .open(CACHE)
            .then((cache) => cache.addAll(ASSETS))
            .then(() => self.skipWaiting()),
    );
});
self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches
            .keys()
            .then((keys) =>
                Promise.all(
                    keys
                        .filter((key) => key.startsWith('mynewspaper-admin-pwa-') && key !== CACHE)
                        .map((key) => caches.delete(key)),
                ),
            )
            .then(() => self.clients.claim()),
    );
});
self.addEventListener('fetch', (event) => {
    const url = new URL(event.request.url);
    if (url.origin !== self.location.origin || event.request.method !== 'GET') return;
    if (event.request.mode === 'navigate') {
        event.respondWith(fetch(event.request).catch(() => caches.open(CACHE).then((cache) => cache.match(OFFLINE))));
    } else if (ASSETS.includes(url.pathname) && !url.search) {
        event.respondWith(
            fetch(event.request).catch(() => caches.open(CACHE).then((cache) => cache.match(url.pathname))),
        );
    }
});
