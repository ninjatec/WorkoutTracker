/**
 * Service Worker Registration for WorkoutTracker
 * Registers the service worker for offline capabilities
 */
(function() {
    // Service Worker version tracking for manual updates
    const CURRENT_SW_VERSION = 'v1.7'; // Match this with service-worker.js version
    const LAST_SW_VERSION_KEY = 'workout-tracker-sw-version';
    
    // Only register if service worker is supported
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', () => {
            // Check if we need to force update the service worker
            const lastVersion = localStorage.getItem(LAST_SW_VERSION_KEY);
            if (lastVersion !== CURRENT_SW_VERSION) {
                console.log(`Service worker version changed: ${lastVersion || 'none'} -> ${CURRENT_SW_VERSION}`);
                unregisterAndReRegisterServiceWorker();
                localStorage.setItem(LAST_SW_VERSION_KEY, CURRENT_SW_VERSION);
            } else {
                registerServiceWorker();
            }
            
            checkForAppUpdate();
        });
    }
    
    /**
     * Unregister existing service worker and then register a new one
     * Used when we've made significant changes to the service worker
     */
    function unregisterAndReRegisterServiceWorker() {
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
                                // Register new service worker after clearing caches
                                setTimeout(() => {
                                    registerServiceWorker();
                                }, 1000);
                            });
                        } else {
                            // If caches API not available, just register the new worker
                            registerServiceWorker();
                        }
                    } else {
                        console.warn('Failed to unregister service worker');
                        // Try to register anyway
                        registerServiceWorker();
                    }
                });
            }
        });
    }
    
    /**
     * Register the service worker
     */
    function registerServiceWorker() {
        navigator.serviceWorker.register('/service-worker.js')
            .then(registration => {
                console.log('ServiceWorker registration successful with scope: ', registration.scope);
                
                // Check if there's an update on registration success
                registration.addEventListener('updatefound', () => {
                    console.log('New service worker installing...');
                    
                    const newWorker = registration.installing;
                    newWorker.addEventListener('statechange', () => {
                        if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                            console.log('New service worker installed, update available');
                            showUpdateNotification();
                        }
                    });
                });
            })
            .catch(error => {
                console.error('ServiceWorker registration failed: ', error);
            });
    }
    
    /**
     * Check for app updates periodically
     */
    function checkForAppUpdate() {
        // Check for updates every 30 minutes when the app is open
        setInterval(() => {
            if (navigator.onLine) {
                console.log('Checking for app updates...');
                
                if (navigator.serviceWorker.controller) {
                    navigator.serviceWorker.controller.postMessage({ action: 'CHECK_FOR_UPDATES' });
                }
                
                // Also check if the registration needs updating
                navigator.serviceWorker.getRegistration().then(registration => {
                    if (registration) {
                        registration.update();
                    }
                });
            }
        }, 30 * 60 * 1000); // 30 minutes
    }
    
    /**
     * Show notification when an update is available
     */
    function showUpdateNotification() {
        // Create update notification if not already shown
        if (document.getElementById('update-notification')) return;
        
        const notification = document.createElement('div');
        notification.id = 'update-notification';
        notification.className = 'update-notification';
        notification.innerHTML = `
            <div class="update-notification-content">
                <span>An update is available for WorkoutTracker.</span>
                <button id="update-reload-button" class="btn btn-sm btn-primary ms-2">Update Now</button>
                <button id="update-dismiss-button" class="btn btn-sm btn-link">Later</button>
            </div>
        `;
        
        document.body.appendChild(notification);
        
        // Add event listeners
        document.getElementById('update-reload-button').addEventListener('click', () => {
            window.location.reload();
        });
        
        document.getElementById('update-dismiss-button').addEventListener('click', () => {
            notification.remove();
        });
        
        // Show the notification after a small delay
        setTimeout(() => {
            notification.classList.add('visible');
        }, 500);
    }
    
    /**
     * Listen for messages from the service worker
     */
    navigator.serviceWorker.addEventListener('message', event => {
        if (event.data && event.data.action === 'UPDATE_AVAILABLE') {
            showUpdateNotification();
        }
    });
    
    /**
     * Add CSS for the update notification
     */
    function addUpdateNotificationStyles() {
        if (document.getElementById('update-notification-styles')) return;
        
        const styleEl = document.createElement('style');
        styleEl.id = 'update-notification-styles';
        styleEl.textContent = `
            .update-notification {
                position: fixed;
                bottom: 20px;
                right: 20px;
                transform: translateY(150%);
                transition: transform 0.3s ease-in-out;
                z-index: 9999;
                max-width: 90%;
            }
            
            .update-notification.visible {
                transform: translateY(0);
            }
            
            .update-notification-content {
                background-color: #007bff;
                color: white;
                padding: 12px 20px;
                border-radius: 4px;
                box-shadow: 0 4px 8px rgba(0,0,0,0.2);
                display: flex;
                align-items: center;
                flex-wrap: wrap;
            }
            
            #update-dismiss-button {
                color: rgba(255,255,255,0.8);
            }
            
            @media (max-width: 576px) {
                .update-notification {
                    bottom: 10px;
                    right: 10px;
                    left: 10px;
                    max-width: none;
                }
                
                .update-notification-content {
                    flex-direction: column;
                    align-items: stretch;
                }
                
                .update-notification-content button {
                    margin-top: 8px;
                }
            }
        `;
        
        document.head.appendChild(styleEl);
    }
    
    // Add styles for update notification
    addUpdateNotificationStyles();
})();