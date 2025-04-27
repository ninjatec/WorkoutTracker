/**
 * Service Worker for WorkoutTracker
 * Provides offline functionality and caching
 */

// Cache names with versioning
const STATIC_CACHE_VERSION = 'workouttracker-static-v1.6';
const DYNAMIC_CACHE_VERSION = 'workouttracker-dynamic-v1.4';
const ASSET_CACHE_VERSION = 'workouttracker-assets-v1.3';

// Static resources to cache on install
const STATIC_RESOURCES = [
    '/',
    '/offline',
    '/css/site.css',
    '/css/responsive.css',
    '/css/shared.css',
    '/js/site.js',
    '/js/module-loader.js',
    '/js/modules/common.js',
    '/js/modules/home.js',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/lib/jquery/dist/jquery.min.js',
    '/images/Logo_Without_Words.png',
    '/images/Logo_With_words.png'
    // Removed bootstrap-icons files that don't exist
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

// Patterns for URLs that should ALWAYS use the network and never return the offline page
const NETWORK_ONLY_PATHS = [
    /^\/Areas\/Coach\/.*/,   // All Coach area pages
    /^\/Coach\/.*/           // Coach pages using route prefixes
];

// Install event - cache static resources
self.addEventListener('install', event => {
    // Skip waiting forces the waiting service worker to become the active service worker
    self.skipWaiting();
    
    event.waitUntil(
        caches.open(STATIC_CACHE_VERSION)
            .then(cache => {
                console.log('[Service Worker] Pre-caching static resources');
                // Use a more resilient approach that won't fail if one resource is missing
                return Promise.allSettled(
                    STATIC_RESOURCES.map(resource => {
                        return fetch(resource)
                            .then(response => {
                                if (response.ok) {
                                    return cache.put(resource, response);
                                }
                                console.warn(`[Service Worker] Failed to cache: ${resource} - Status: ${response.status}`);
                                return Promise.resolve(); // Continue even if this resource fails
                            })
                            .catch(error => {
                                console.warn(`[Service Worker] Failed to fetch: ${resource} - ${error.message}`);
                                return Promise.resolve(); // Continue even if this resource fails
                            });
                    })
                );
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
    
    // Check if the URL matches a network-only pattern (like Coach area)
    // These should never show offline page on failure
    if (isNetworkOnlyRequest(event.request)) {
        // For network-only requests, just pass through without offline fallback
        return;
    }
    
    // Handle critical pages - use network falling back to cache
    const isCriticalPage = isCriticalRequest(event.request);
    if (isCriticalPage) {
        event.respondWith(
            fetch(event.request)
                .then(response => {
                    // Cache a copy of the response
                    const responseClone = response.clone();
                    caches.open(DYNAMIC_CACHE_VERSION)
                        .then(cache => {
                            cache.put(event.request, responseClone);
                        });
                    return response;
                })
                .catch(() => {
                    // If network fails, try to return from cache
                    return caches.match(event.request)
                        .then(cachedResponse => {
                            if (cachedResponse) {
                                return cachedResponse;
                            }
                            // If not in cache, return offline page
                            return caches.match('/offline');
                        });
                })
        );
        return;
    }
    
    // For CSS, JS, and images - use cache first, then network (stale-while-revalidate)
    if (isAssetRequest(event.request)) {
        event.respondWith(
            caches.match(event.request)
                .then(cachedResponse => {
                    // Return cached version immediately if available
                    const fetchPromise = fetch(event.request)
                        .then(networkResponse => {
                            // Update cache with new version
                            caches.open(ASSET_CACHE_VERSION)
                                .then(cache => {
                                    cache.put(event.request, networkResponse.clone());
                                });
                            return networkResponse;
                        })
                        .catch(error => {
                            console.warn(`[Service Worker] Failed to fetch: ${event.request.url} - ${error.message}`);
                            return cachedResponse || new Response(null, { status: 404 });
                        });
                    return cachedResponse || fetchPromise;
                })
        );
        return;
    }
    
    // For other requests - network first with cache fallback
    event.respondWith(
        fetch(event.request)
            .then(response => {
                // Don't cache responses with error status codes
                if (!response.ok) {
                    throw Error(response.statusText);
                }
                
                // Cache a copy of successful responses
                const responseClone = response.clone();
                caches.open(DYNAMIC_CACHE_VERSION)
                    .then(cache => {
                        cache.put(event.request, responseClone);
                    });
                return response;
            })
            .catch(() => {
                // If network fails, try to return from cache
                return caches.match(event.request)
                    .then(cachedResponse => {
                        if (cachedResponse) {
                            return cachedResponse;
                        }
                        
                        // If it's a page request, return offline page
                        if (isHtmlRequest(event.request)) {
                            return caches.match('/offline');
                        }
                        
                        // For non-HTML requests with no cache, return nothing
                        return new Response(null, { status: 404 });
                    });
            })
    );
});

// Check if a request is for a critical page that should work offline
function isCriticalRequest(request) {
    const url = new URL(request.url);
    const path = url.pathname;
    
    // Check for exact matches
    if (CRITICAL_PAGES.includes(path)) {
        return true;
    }
    
    // Check for pattern matches
    return CRITICAL_PAGES.some(pattern => {
        if (typeof pattern === 'object' && pattern instanceof RegExp) {
            return pattern.test(path);
        }
        return false;
    });
}

// Check if a request should always use the network (e.g., Coach area pages)
function isNetworkOnlyRequest(request) {
    const url = new URL(request.url);
    const path = url.pathname;
    
    // Check if the path matches any network-only patterns
    return NETWORK_ONLY_PATHS.some(pattern => {
        if (typeof pattern === 'object' && pattern instanceof RegExp) {
            return pattern.test(path);
        }
        return pattern === path;
    });
}

// Check if the request is for an asset (CSS, JS, images, fonts)
function isAssetRequest(request) {
    const url = new URL(request.url);
    const path = url.pathname;
    
    return path.endsWith('.css') || 
           path.endsWith('.js') || 
           path.endsWith('.png') || 
           path.endsWith('.jpg') || 
           path.endsWith('.jpeg') || 
           path.endsWith('.gif') || 
           path.endsWith('.svg') || 
           path.endsWith('.woff') || 
           path.endsWith('.woff2') || 
           path.endsWith('.ttf') || 
           path.endsWith('.eot');
}

// Check if the request is for an HTML page
function isHtmlRequest(request) {
    const url = new URL(request.url);
    const path = url.pathname;
    
    // If path has no extension, it's likely a page
    const hasExtension = /\.[a-zA-Z0-9]+$/.test(path);
    return !hasExtension || path.endsWith('.html') || path.endsWith('.htm');
}