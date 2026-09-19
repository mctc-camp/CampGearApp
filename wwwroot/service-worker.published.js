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
    const url = new URL(event.request.url);

    if (event.request.method !== 'GET') {
        return;
    }

    // Googleスプレッドシート(Apps Script)へのデータ通信は、絶対にキャッシュしない
    // 常に最新のデータをサーバーから取りに行く
    if (url.hostname.includes('script.google.com') || url.hostname.includes('script.googleusercontent.com')) {
        event.respondWith(fetch(event.request));
        return;
    }

    // アプリ本体のファイル（HTML/CSS/JS/画像）は、キャッシュ優先＋裏側で更新する
    event.respondWith(
        caches.match(event.request).then(cachedResponse => {
            const fetchPromise = fetch(event.request).then(networkResponse => {
                if (networkResponse.ok) {
                    const responseClone = networkResponse.clone();
                    caches.open(CACHE_NAME).then(cache => {
                        cache.put(event.request, responseClone);
                    });
                }
                return networkResponse;
            }).catch(() => cachedResponse);

            return cachedResponse || fetchPromise;
        })
    );
});