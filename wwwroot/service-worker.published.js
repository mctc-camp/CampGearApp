const CACHE_VERSION = 'v1';
const CACHE_NAME = `campgear-cache-${CACHE_VERSION}`;

self.addEventListener('install', event => {
    self.skipWaiting();
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

    // Google関連の通信には一切介入しない（リダイレクトを妨げないため）
    if (
        url.hostname.includes('script.google.com') ||
        url.hostname.includes('script.googleusercontent.com') ||
        url.hostname.includes('drive.google.com') ||
        url.hostname.includes('googleusercontent.com')
    ) {
        return;
    }

    // それ以外（アプリ本体のファイル）は、
    // 「まずネットワークから取得。失敗した場合のみキャッシュを使う」というシンプルな方式にする。
    // これによりファイルの二重取得や適用順序の問題を避ける。
    event.respondWith(
        fetch(event.request)
            .then(networkResponse => {
                if (networkResponse.ok) {
                    const responseClone = networkResponse.clone();
                    caches.open(CACHE_NAME).then(cache => {
                        cache.put(event.request, responseClone);
                    });
                }
                return networkResponse;
            })
            .catch(() => caches.match(event.request))
    );
});