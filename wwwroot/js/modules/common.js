/**
 * Common JavaScript module for WorkoutTracker
 * Contains functionality used across all pages
 */
registerModule('common', async function() {
    // Initialize common functionality for all pages
    setupMobileDetection();
    setupNetworkStatusMonitoring();
    setupCommonEventHandlers();
    
    // Return resolved promise when initialization is complete
    return Promise.resolve();
}, []);

/**
 * Set up mobile device detection
 */
function setupMobileDetection() {
    // Detect if user is on mobile device
    const isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
    document.documentElement.classList.toggle('is-mobile-device', isMobile);
    
    // Store in sessionStorage for other modules to access
    sessionStorage.setItem('isMobileDevice', isMobile);
}

/**
 * Set up network status monitoring
 */
function setupNetworkStatusMonitoring() {
    function updateNetworkStatus() {
        const isOnline = navigator.onLine;
        document.documentElement.classList.toggle('is-offline', !isOnline);
        
        // Show/hide offline notification
        let offlineNotification = document.getElementById('offline-notification');
        
        if (!isOnline) {
            if (!offlineNotification) {
                offlineNotification = document.createElement('div');
                offlineNotification.id = 'offline-notification';
                offlineNotification.className = 'offline-notification';
                offlineNotification.innerHTML = `
                    <div class="offline-notification-content">
                        <i class="bi bi-wifi-off"></i> You're offline. Some features may be unavailable.
                    </div>
                `;
                document.body.appendChild(offlineNotification);
            }
            offlineNotification.classList.add('visible');
        } else if (offlineNotification) {
            offlineNotification.classList.remove('visible');
        }
        
        // Dispatch custom event for other modules
        window.dispatchEvent(new CustomEvent('networkStatusChanged', { 
            detail: { isOnline } 
        }));
    }
    
    // Add event listeners for online/offline events
    window.addEventListener('online', updateNetworkStatus);
    window.addEventListener('offline', updateNetworkStatus);
    
    // Initial check
    updateNetworkStatus();
    
    // Also add connection quality detection
    if ('connection' in navigator) {
        const connection = navigator.connection;
        
        function updateConnectionQuality() {
            const connectionType = connection.effectiveType || 'unknown';
            document.documentElement.dataset.connectionType = connectionType;
            
            // Store connection information for other modules
            sessionStorage.setItem('connectionType', connectionType);
            sessionStorage.setItem('connectionDownlink', connection.downlink);
            sessionStorage.setItem('connectionRtt', connection.rtt);
            
            // Dispatch custom event for other modules
            window.dispatchEvent(new CustomEvent('connectionQualityChanged', { 
                detail: { 
                    type: connectionType,
                    downlink: connection.downlink,
                    rtt: connection.rtt
                } 
            }));
        }
        
        // Listen for connection changes
        connection.addEventListener('change', updateConnectionQuality);
        
        // Initial check
        updateConnectionQuality();
    }
}

/**
 * Set up common event handlers
 */
function setupCommonEventHandlers() {
    // Enhance external links
    document.querySelectorAll('a[href^="http"]').forEach(link => {
        // Skip links that we've already processed
        if (link.classList.contains('external-link-processed')) return;
        
        // Mark link as external and processed
        link.classList.add('external-link');
        link.classList.add('external-link-processed');
        
        // Add rel attributes for security
        link.setAttribute('rel', 'noopener noreferrer');
        
        // Open in new tab if not already specified
        if (!link.hasAttribute('target')) {
            link.setAttribute('target', '_blank');
        }
    });
    
    // Automatically add novalidate to forms that should be validated by JS
    document.querySelectorAll('form[data-js-validate]').forEach(form => {
        form.setAttribute('novalidate', 'novalidate');
    });
}

// Add common CSS to document
(function addCommonStyles() {
    if (document.getElementById('common-module-styles')) return;
    
    const styleEl = document.createElement('style');
    styleEl.id = 'common-module-styles';
    styleEl.textContent = `
        .offline-notification {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            transform: translateY(-100%);
            transition: transform 0.3s ease-in-out;
            z-index: 9999;
        }
        
        .offline-notification.visible {
            transform: translateY(0);
        }
        
        .offline-notification-content {
            background-color: #f8d7da;
            color: #721c24;
            text-align: center;
            padding: 10px;
            font-weight: bold;
        }
        
        .external-link::after {
            content: '\\F132';
            font-family: 'bootstrap-icons';
            font-size: 0.75em;
            margin-left: 0.25em;
            vertical-align: text-top;
        }
        
        .is-offline .requires-network {
            opacity: 0.5;
            pointer-events: none;
        }
    `;
    document.head.appendChild(styleEl);
})();