/**
 * Prefetching Module for WorkoutTracker
 * Preloads pages that are likely to be visited next to improve navigation speed
 */
registerModule('prefetching', async function() {
    // Initialize prefetching when network conditions allow
    setupPrefetching();
    
    // Return resolved promise when initialization is complete
    return Promise.resolve();
}, ['common']);

/**
 * Set up prefetching of likely-to-visit pages
 */
function setupPrefetching() {
    // Don't prefetch on slow connections
    if (isSlowConnection()) {
        console.log('Prefetching disabled due to slow connection');
        return;
    }
    
    // Listen for connection quality changes
    window.addEventListener('connectionQualityChanged', (event) => {
        const { type, downlink } = event.detail;
        if (type === '4g' && downlink > 1.5) {
            addPrefetchLinks();
        } else {
            removePrefetchLinks();
        }
    });
    
    // Initialize prefetch links
    addPrefetchLinks();
    setupPageSpecificPrefetching();
    
    // Set up navigation tracking to improve future prefetching
    trackUserNavigation();
}

/**
 * Add prefetch links based on current page
 */
function addPrefetchLinks() {
    // Common prefetch targets for all pages
    const commonPrefetchTargets = [
        '/Sessions/Index',
        '/Sessions/Create',
        '/Reports/Index',
        '/Calculator/OneRepMax'
    ];
    
    // Page-specific prefetch targets
    const pageSpecificPrefetchTargets = getPrefetchTargetsForCurrentPage();
    
    // Combine all targets, remove duplicates, and remove current page
    const currentPath = window.location.pathname;
    const allTargets = [...new Set([...commonPrefetchTargets, ...pageSpecificPrefetchTargets])]
        .filter(path => path !== currentPath);
    
    // Limit to 5 prefetch targets to avoid excessive requests
    const limitedTargets = allTargets.slice(0, 5);
    
    // Remove existing prefetch links
    removePrefetchLinks();
    
    // Add prefetch links
    limitedTargets.forEach(path => {
        const link = document.createElement('link');
        link.rel = 'prefetch';
        link.href = path;
        link.className = 'prefetch-link';
        document.head.appendChild(link);
    });
    
    console.log('Prefetching enabled for:', limitedTargets);
}

/**
 * Remove all prefetch links
 */
function removePrefetchLinks() {
    document.querySelectorAll('link.prefetch-link').forEach(link => {
        link.remove();
    });
}

/**
 * Check if the user has a slow connection
 * @returns {boolean} - True if the connection is slow
 */
function isSlowConnection() {
    // Get connection info from sessionStorage (set by common module)
    const connectionType = sessionStorage.getItem('connectionType');
    const downlink = parseFloat(sessionStorage.getItem('connectionDownlink')) || 0;
    const rtt = parseFloat(sessionStorage.getItem('connectionRtt')) || 0;
    
    // Don't prefetch on slow connections
    if (connectionType === '2g' || connectionType === 'slow-2g') {
        return true;
    }
    
    // Don't prefetch on slow 3g
    if (connectionType === '3g' && downlink < 0.5) {
        return true;
    }
    
    // Don't prefetch on high latency connections
    if (rtt > 500) {
        return true;
    }
    
    // Don't prefetch if user has checked "Save Data" option
    if (navigator.connection && navigator.connection.saveData) {
        return true;
    }
    
    return false;
}

/**
 * Get prefetch targets based on the current page
 * @returns {string[]} - Array of paths to prefetch
 */
function getPrefetchTargetsForCurrentPage() {
    const path = window.location.pathname.toLowerCase();
    
    // Prefetch targets for specific pages
    const prefetchMap = {
        '/': ['/Sessions/Index', '/Reports/Index'],
        '/index': ['/Sessions/Index', '/Reports/Index'],
        '/sessions/index': ['/Sessions/Create', '/Sessions/Details/recent'],
        '/sessions/create': ['/ExerciseTypes/Index', '/Sessions/Index'],
        '/sessions/details': ['/Sets/Create', '/Reports/Index'],
        '/sets/create': ['/ExerciseTypes/Index', '/Sessions/Details/current'],
        '/sets/edit': ['/ExerciseTypes/Index', '/Sessions/Details/current'],
        '/reports/index': ['/Reports/Progress', '/Reports/Personal'],
        '/calculator/onerepmax': ['/Sessions/Create', '/Reports/Index']
    };
    
    // Find exact matches
    if (prefetchMap[path]) {
        return prefetchMap[path];
    }
    
    // Find partial matches
    for (const [pagePath, targets] of Object.entries(prefetchMap)) {
        if (path.includes(pagePath) && pagePath !== '/') {
            return targets;
        }
    }
    
    return [];
}

/**
 * Setup page-specific prefetching based on user interaction
 */
function setupPageSpecificPrefetching() {
    const path = window.location.pathname.toLowerCase();
    
    // For Sessions/Index, prefetch first few session details when hovered
    if (path.includes('/sessions/index')) {
        // Wait for the page to fully render
        setTimeout(() => {
            document.querySelectorAll('a[href^="/Sessions/Details/"]').forEach((link, index) => {
                // Only prefetch the first 3 session links
                if (index < 3) {
                    link.addEventListener('mouseenter', function() {
                        prefetchOnHover(this.href);
                    });
                    link.addEventListener('touchstart', function() {
                        prefetchOnHover(this.href);
                    });
                }
            });
        }, 1000);
    }
    
    // For Sets/Create or Edit, prefetch exercise type details when searching
    if (path.includes('/sets/create') || path.includes('/sets/edit')) {
        const exerciseSelect = document.getElementById('ExerciseTypeId');
        if (exerciseSelect) {
            exerciseSelect.addEventListener('change', function() {
                const exerciseId = this.value;
                if (exerciseId) {
                    prefetchOnHover(`/ExerciseTypes/Details/${exerciseId}`);
                }
            });
        }
    }
}

/**
 * Prefetch a specific URL when hovered or otherwise interacted with
 * @param {string} url - The URL to prefetch
 */
function prefetchOnHover(url) {
    // Check if already prefetched
    if (document.querySelector(`link[href="${url}"]`)) {
        return;
    }
    
    // Don't prefetch if on a slow connection
    if (isSlowConnection()) {
        return;
    }
    
    // Add prefetch link
    const link = document.createElement('link');
    link.rel = 'prefetch';
    link.href = url;
    link.className = 'prefetch-link dynamic';
    document.head.appendChild(link);
}

/**
 * Track user navigation to improve future prefetching
 */
function trackUserNavigation() {
    // Store previous page for tracking navigation patterns
    if (sessionStorage.getItem('currentPage')) {
        const previousPage = sessionStorage.getItem('currentPage');
        const currentPage = window.location.pathname;
        
        // Skip if it's the same page (refresh)
        if (previousPage !== currentPage) {
            // Get navigation history or initialize empty object
            let navigationHistory = JSON.parse(localStorage.getItem('navigationHistory') || '{}');
            
            // Record this navigation path
            if (!navigationHistory[previousPage]) {
                navigationHistory[previousPage] = {};
            }
            
            if (!navigationHistory[previousPage][currentPage]) {
                navigationHistory[previousPage][currentPage] = 0;
            }
            
            navigationHistory[previousPage][currentPage]++;
            
            // Store updated history
            localStorage.setItem('navigationHistory', JSON.stringify(navigationHistory));
        }
    }
    
    // Update current page
    sessionStorage.setItem('currentPage', window.location.pathname);
}