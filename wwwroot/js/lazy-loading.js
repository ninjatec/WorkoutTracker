/**
 * Lazy Loading Implementation for WorkoutTracker
 * Provides optimized image loading for better mobile performance
 */
document.addEventListener('DOMContentLoaded', function() {
    initLazyLoading();
    observeNewContent();
});

/**
 * Initialize lazy loading for all images
 */
function initLazyLoading() {
    // Check for native lazy loading support
    if ('loading' in HTMLImageElement.prototype) {
        // Use native lazy loading for all images that don't have it yet
        const images = document.querySelectorAll('img:not([loading])');
        images.forEach(img => {
            img.setAttribute('loading', 'lazy');
        });
    } else {
        // Fallback for browsers that don't support native lazy loading
        implementIntersectionObserverLazyLoad();
    }
}

/**
 * Use Intersection Observer API as fallback for browsers without native lazy loading
 */
function implementIntersectionObserverLazyLoad() {
    // Only run if IntersectionObserver is supported
    if (!('IntersectionObserver' in window)) {
        // Load all images immediately as ultimate fallback
        loadAllImagesImmediately();
        return;
    }

    // Target all images with data-src attribute
    const lazyImages = document.querySelectorAll('img[data-src]');
    
    const imageObserver = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                img.src = img.dataset.src;
                
                // If there's also a srcset defined, set that too
                if (img.dataset.srcset) {
                    img.srcset = img.dataset.srcset;
                }
                
                // Listen for load event to add loaded class for any CSS transitions
                img.addEventListener('load', () => {
                    img.classList.add('loaded');
                });
                
                // Remove the image from observation after loading
                observer.unobserve(img);
            }
        });
    }, {
        rootMargin: '50px 0px', // Start loading images 50px before they enter viewport
        threshold: 0.01
    });
    
    // Observe all targeted images
    lazyImages.forEach(img => {
        imageObserver.observe(img);
    });
}

/**
 * Ultimate fallback - load all images immediately
 */
function loadAllImagesImmediately() {
    const lazyImages = document.querySelectorAll('img[data-src]');
    lazyImages.forEach(img => {
        img.src = img.dataset.src;
        if (img.dataset.srcset) {
            img.srcset = img.dataset.srcset;
        }
    });
}

/**
 * Observer for dynamically added content
 */
function observeNewContent() {
    // Check if MutationObserver is supported
    if (!('MutationObserver' in window)) return;
    
    // Create a mutation observer to watch for new images being added to the DOM
    const observer = new MutationObserver((mutations) => {
        let shouldReinit = false;
        
        mutations.forEach(mutation => {
            if (mutation.type === 'childList') {
                // Check if any new nodes were added
                mutation.addedNodes.forEach(node => {
                    // Check if the node is an element
                    if (node.nodeType === 1) {
                        // If it's an image, or contains images, reinitialize lazy loading
                        if (node.tagName === 'IMG' || node.querySelectorAll('img').length > 0) {
                            shouldReinit = true;
                        }
                    }
                });
            }
        });
        
        if (shouldReinit) {
            initLazyLoading();
        }
    });
    
    // Start observing the document body for changes
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
}

/**
 * Add CSS for any lazy loading transitions or effects
 */
function addLazyLoadingStyles() {
    // Only add if not already present
    if (document.getElementById('lazy-loading-styles')) return;
    
    const styleEl = document.createElement('style');
    styleEl.id = 'lazy-loading-styles';
    styleEl.textContent = `
        img.loading {
            opacity: 0;
            transition: opacity 0.3s ease-in;
        }
        img.loaded {
            opacity: 1;
        }
        .image-placeholder {
            background-color: #e9ecef;
            position: relative;
            overflow: hidden;
        }
        .image-placeholder::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: linear-gradient(90deg, transparent, rgba(255,255,255,0.2), transparent);
            animation: placeholder-shimmer 1.5s infinite;
        }
        @keyframes placeholder-shimmer {
            0% { transform: translateX(-100%); }
            100% { transform: translateX(100%); }
        }
    `;
    document.head.appendChild(styleEl);
}

// Initialize styles when script loads
addLazyLoadingStyles();