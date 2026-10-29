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

// Googleドライブの写真を扱う処理
async function handlePhotoRequest(request) {
    const cache = await caches.open(CACHE_NAME);
    const cached = await cache.match(request.url);

    try {
        // まずCORS方式で取得を試す。この方式なら成功・失敗を確認できるため、
        // 正常に取得できた写真だけを保存できる
        const corsResponse = await fetch(request.url, { mode: 'cors', credentials: 'omit' });

        if (corsResponse.ok) {
            await cache.put(request.url, corsResponse.clone());
            return corsResponse;
        }

        // 正常に取得できなかった場合は、保存済みの写真があればそちらを使う
        return cached || corsResponse;
    } catch (e) {
        // CORS方式で取得できない場合は、従来の方式に切り替える
        try {
            const response = await fetch(request);

            // この方式では成功・失敗を確認できないため、
            // すでに保存済みの写真は上書きしない
            if (!cached && (response.ok || response.type === 'opaque')) {
                cache.put(request.url, response.clone()).catch(() => { });
            }

            return cached || response;
        } catch (e2) {
            // オフラインなど。保存済みの写真があればそれを返す
            return cached || Response.error();
        }
    }
}

self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);

    if (event.request.method !== 'GET') {
        return;
    }

    // Googleスプレッドシート(Apps Script)への通信には一切介入しない
    // （302リダイレクトを正しく追従させるため）
    if (
        url.hostname.includes('script.google.com') ||
        url.hostname.includes('script.googleusercontent.com')
    ) {
        return;
    }

    // Googleドライブの写真
    if (
        url.hostname.includes('drive.google.com') ||
        url.hostname.includes('googleusercontent.com')
    ) {
        event.respondWith(handlePhotoRequest(event.request));
        return;
    }

    // アプリ本体のファイルは「まずネットワーク、失敗したらキャッシュ」
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