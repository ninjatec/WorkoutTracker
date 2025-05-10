/**
 * Unregisters any existing service workers to remove offline functionality
 */
(function() {
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.getRegistrations().then(registrations => {
            for (let registration of registrations) {
                console.log('Unregistering service worker:', registration.scope);
                registration.unregister().then(success => {
                    if (success) {
                        console.log('Successfully unregistered service worker');
                        // Clear caches
                        if ('caches' in window) {
                            caches.keys().then(cacheNames => {
                                cacheNames.forEach(cacheName => {
                                    if (cacheName.includes('workouttracker')) {
                                        console.log('Deleting cache:', cacheName);
                                        caches.delete(cacheName);
                                    }
                                });
                            });
                        }
                    } else {
                        console.warn('Failed to unregister service worker');
                    }
                });
            }
        });
    }
})();
