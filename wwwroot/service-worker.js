/**
 * Service Worker for WorkoutTracker
 * Provides offline capabilities and asset caching
 */

// Cache versions - update these when files change
const STATIC_CACHE_VERSION = 'static-v1';
const DYNAMIC_CACHE_VERSION = 'dynamic-v1';
const ASSET_CACHE_VERSION = 'assets-v1';

// Resources to cache immediately during install
const STATIC_RESOURCES = [
    '/',
    '/offline',
    '/css/site.css',
    '/css/responsive.css',
    '/js/site.js',
    '/js/module-loader.js',
    '/js/modules/common.js',
    '/js/lazy-loading.js',
    '/js/progressive-images.js',
    '/js/responsive-tables.js',
    '/js/mobile-navigation.js',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/lib/jquery/dist/jquery.min.js',
    '/lib/microsoft/signalr/dist/browser/signalr.min.js',
    '/images/Logo_Without_Words.png',
    '/images/Logo_With_words.png',
    'https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.3/font/bootstrap-icons.css'
];

// Pages that should work offline
const CRITICAL_PAGES = [
    // Regular patterns
    /^\/Sessions\/Details\/[0-9]+$/,
    /^\/Sessions\/Index\/?$/,
    /^\/Sets\/Details\/[0-9]+$/,
    /^\/Calculator\/OneRepMax\/?$/,
    // Exact matches
    '/',
    '/Index',
    '/offline'
];

// Install event - cache static resources
self.addEventListener('install', event => {
    // Skip waiting forces the waiting service worker to become the active service worker
    self.skipWaiting();
    
    event.waitUntil(
        caches.open(STATIC_CACHE_VERSION)
            .then(cache => {
                console.log('[Service Worker] Pre-caching static resources');
                return cache.addAll(STATIC_RESOURCES);
            })
    );
});

// Activate event - clean up old caches
self.addEventListener('activate', event => {
    // Claim control immediately
    event.waitUntil(clients.claim());
    
    // Clean up old caches
    event.waitUntil(
        caches.keys()
            .then(keyList => {
                return Promise.all(keyList.map(key => {
                    // If cache is old, delete it
                    if (key !== STATIC_CACHE_VERSION && 
                        key !== DYNAMIC_CACHE_VERSION && 
                        key !== ASSET_CACHE_VERSION) {
                        console.log('[Service Worker] Removing old cache', key);
                        return caches.delete(key);
                    }
                }));
            })
    );
});

// Fetch event - serve from cache or network
self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);
    
    // Skip non-GET requests and browser extensions
    if (event.request.method !== 'GET' || 
        url.protocol !== 'http:' && url.protocol !== 'https:') {
        return;
    }
    
    // Skip requests to API endpoints and authentication-related URLs
    if (url.pathname.startsWith('/api/') || 
        url.pathname.includes('/Identity/') ||
        url.pathname.includes('/Account/') ||
        url.pathname.includes('/signin-')) {
        return;
    }
    
    // Handle critical pages - use network falling back to cache
    const isCriticalPage = isCriticalRequest(event.request);
    if (isCriticalPage) {
        event.respondWith(
            fetch(event.request)
                .then(response => {
                    // Cache successful responses
                    if (response.ok) {
                        cacheResponse(event.request, response.clone());
                    }
                    return response;
                })
                .catch(() => {
                    console.log('[Service Worker] Serving critical page from cache', event.request.url);
                    return caches.match(event.request)
                        .then(cachedResponse => {
                            if (cachedResponse) {
                                return cachedResponse;
                            }
                            // If not in cache, serve offline page
                            return caches.match('/offline');
                        });
                })
        );
        return;
    }
    
    // Handle static assets with Cache First strategy
    if (isStaticAsset(event.request)) {
        event.respondWith(
            caches.match(event.request)
                .then(cachedResponse => {
                    if (cachedResponse) {
                        // Return cached response and update in background
                        updateCacheInBackground(event.request);
                        return cachedResponse;
                    }
                    
                    // Not in cache, try network
                    return fetch(event.request)
                        .then(response => {
                            // Cache successful responses
                            if (response.ok) {
                                cacheResponse(event.request, response.clone());
                            }
                            return response;
                        })
                        .catch(() => {
                            // If it's an image, return a fallback
                            if (event.request.url.match(/\.(jpg|jpeg|png|gif|webp|svg)$/)) {
                                return caches.match('/images/offline-placeholder.png');
                            }
                            // For other static assets, fail silently
                            return new Response(null, { status: 404 });
                        });
                })
        );
        return;
    }
    
    // Default: Network First strategy with dynamic caching for other requests
    event.respondWith(
        fetch(event.request)
            .then(response => {
                // Cache successful responses that aren't for API calls
                if (response.ok && !url.pathname.startsWith('/api/')) {
                    cacheResponse(event.request, response.clone(), DYNAMIC_CACHE_VERSION);
                }
                return response;
            })
            .catch(() => {
                console.log('[Service Worker] Serving from cache after network failure', event.request.url);
                return caches.match(event.request)
                    .then(cachedResponse => {
                        if (cachedResponse) {
                            return cachedResponse;
                        }
                        
                        // If it's a page, serve the offline page
                        if (event.request.headers.get('Accept').includes('text/html')) {
                            return caches.match('/offline');
                        }
                        
                        // Otherwise fail silently
                        return new Response(null, { status: 404 });
                    });
            })
    );
});

/**
 * Update a cached response in the background
 * @param {Request} request - The request to update
 */
function updateCacheInBackground(request) {
    setTimeout(() => {
        fetch(request).then(response => {
            if (response.ok) {
                cacheResponse(request, response);
            }
        });
    }, 1000);
}

/**
 * Cache a response
 * @param {Request} request - The request that was made
 * @param {Response} response - The response to cache
 * @param {string} cacheName - The cache to use (defaults to static cache)
 */
function cacheResponse(request, response, cacheName = STATIC_CACHE_VERSION) {
    // Only cache GET requests
    if (request.method !== 'GET') return;
    
    // Determine correct cache based on resource type
    let targetCache = cacheName;
    if (request.url.match(/\.(jpg|jpeg|png|gif|webp|svg)$/)) {
        targetCache = ASSET_CACHE_VERSION;
    }
    
    // Clone the response before caching
    const clonedResponse = response.clone();
    
    caches.open(targetCache).then(cache => {
        cache.put(request, clonedResponse);
    });
}

/**
 * Check if a request is for a critical page that should work offline
 * @param {Request} request - The request to check
 * @returns {boolean} - True if this is a critical page
 */
function isCriticalRequest(request) {
    // Only consider HTML requests for pages
    if (!request.headers.get('Accept').includes('text/html')) {
        return false;
    }
    
    const url = new URL(request.url);
    const path = url.pathname;
    
    // Check exact matches
    if (CRITICAL_PAGES.includes(path)) {
        return true;
    }
    
    // Check regex patterns
    for (const pattern of CRITICAL_PAGES) {
        if (pattern instanceof RegExp && pattern.test(path)) {
            return true;
        }
    }
    
    return false;
}

/**
 * Check if a request is for a static asset
 * @param {Request} request - The request to check
 * @returns {boolean} - True if this is a static asset
 */
function isStaticAsset(request) {
    const url = new URL(request.url);
    
    // Check file extensions
    if (url.pathname.match(/\.(css|js|png|jpg|jpeg|gif|webp|svg|woff|woff2|ttf|eot|ico)$/)) {
        return true;
    }
    
    // Check paths
    if (url.pathname.startsWith('/css/') ||
        url.pathname.startsWith('/js/') ||
        url.pathname.startsWith('/lib/') ||
        url.pathname.startsWith('/images/')) {
        return true;
    }
    
    // Check CDN URLs for common static assets
    if (url.hostname.includes('cdn.jsdelivr.net') ||
        url.hostname.includes('fonts.googleapis.com') ||
        url.hostname.includes('fonts.gstatic.com')) {
        return true;
    }
    
    return false;
}