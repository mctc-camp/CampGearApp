// キャッシュのバージョン。ファイルを更新して再公開する際はこの数字を上げてください
const CACHE_VERSION = 'v1';
const CACHE_NAME = `campgear-cache-${CACHE_VERSION}`;

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames
                    .filter(name => name.startsWith('campgear-cache-') && name !== CACHE_NAME)
                    .map(name => caches.delete(name))
            );
        }).then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', event => {
    if (event.request.method !== 'GET') {
        return;
    }

    event.respondWith(
        caches.match(event.request).then(cachedResponse => {
            if (cachedResponse) {
                return cachedResponse;
            }

            return fetch(event.request).then(response => {
                if (response.ok) {
                    const responseClone = response.clone();
                    caches.open(CACHE_NAME).then(cache => {
                        cache.put(event.request, responseClone);
                    });
                }
                return response;
            }).catch(() => caches.match('./index.html'));
        })
    );
});