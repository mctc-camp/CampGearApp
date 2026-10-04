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

    // Googleスプレッドシート(Apps Script)への通信だけは、一切横取りしない。
    // 302リダイレクトを正しく追従させるため、ブラウザ本来の処理に完全に任せる。
    if (
        url.hostname.includes('script.google.com') ||
        url.hostname.includes('script.googleusercontent.com')
    ) {
        return;
    }

    // Googleドライブの写真表示は、オフラインでも見られるようキャッシュの対象にする。
    // 「まずネットワークから取得を試み、失敗したらキャッシュを使う」という安全な方式。
    if (
        url.hostname.includes('drive.google.com') ||
        url.hostname.includes('googleusercontent.com')
    ) {
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
        return;
    }

    // アプリ本体のファイル（HTML/CSS/JS/画像）は、キャッシュ優先＋裏側で更新する
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