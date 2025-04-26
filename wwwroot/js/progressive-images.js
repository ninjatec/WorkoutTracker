/**
 * Progressive Image Loading Implementation for WorkoutTracker
 * Provides smooth loading transitions for workout photos
 */
document.addEventListener('DOMContentLoaded', function() {
    initProgressiveImages();
    observeNewProgressiveImages();
});

/**
 * Initialize progressive loading for all images with progressive attribute
 */
function initProgressiveImages() {
    const progressiveImages = document.querySelectorAll('img[progressive]');
    progressiveImages.forEach(setupProgressiveImage);
}

/**
 * Setup progressive loading for a single image
 * @param {HTMLImageElement} img - The image element to set up
 */
function setupProgressiveImage(img) {
    // Skip if already processed
    if (img.classList.contains('progressive-processed')) return;
    
    // Mark as processed
    img.classList.add('progressive-processed');
    
    // Create wrapper div
    const wrapper = document.createElement('div');
    wrapper.className = 'progressive-image-wrapper';
    
    // Create a low-quality thumbnail version
    const thumbSrc = img.getAttribute('data-thumb') || generateThumbnailUrl(img.src);
    const thumb = new Image();
    thumb.className = 'progressive-image-thumb';
    thumb.src = thumbSrc;
    
    // Create a blurred placeholder
    const placeholder = document.createElement('div');
    placeholder.className = 'progressive-image-placeholder';
    
    // Replace the image with our wrapper and components
    img.parentNode.insertBefore(wrapper, img);
    wrapper.appendChild(placeholder);
    wrapper.appendChild(thumb);
    wrapper.appendChild(img);
    
    // Add loading class to the original image
    img.classList.add('progressive-image');
    img.classList.add('progressive-image-loading');
    
    // When thumbnail loads, use it as background for placeholder
    thumb.onload = function() {
        placeholder.style.backgroundImage = `url(${thumbSrc})`;
    };
    
    // When the main image loads, trigger the transition
    img.onload = function() {
        img.classList.remove('progressive-image-loading');
        img.classList.add('progressive-image-loaded');
    };
    
    // Add decoding and fetchpriority attributes
    img.setAttribute('decoding', 'async');
    img.setAttribute('fetchpriority', 'high');
}

/**
 * Generate a thumbnail URL from the original image URL
 * Uses the responsive images directory structure if available
 * @param {string} originalSrc - The original image source URL
 * @returns {string} - The thumbnail URL
 */
function generateThumbnailUrl(originalSrc) {
    // Check if we can use a responsive image variant
    if (originalSrc.includes('/images/')) {
        const parts = originalSrc.split('/');
        const filename = parts[parts.length - 1];
        const directory = parts.slice(0, parts.length - 1).join('/');
        
        // Try to use a responsive variant if available
        if (!directory.includes('/responsive/')) {
            const filenameWithoutExt = filename.substring(0, filename.lastIndexOf('.'));
            const extension = filename.substring(filename.lastIndexOf('.'));
            
            // Return the smallest responsive variant as thumbnail
            return `${directory}/responsive/${filenameWithoutExt}-320w${extension}`;
        }
    }
    
    // Fallback to original but add a low-quality parameter
    // This allows server-side processing to return a low-quality version
    if (originalSrc.includes('?')) {
        return `${originalSrc}&quality=10&blur=5`;
    } else {
        return `${originalSrc}?quality=10&blur=5`;
    }
}

/**
 * Observer for dynamically added progressive images
 */
function observeNewProgressiveImages() {
    // Check if MutationObserver is supported
    if (!('MutationObserver' in window)) return;
    
    // Create a mutation observer to watch for new images being added to the DOM
    const observer = new MutationObserver((mutations) => {
        let newProgressiveImages = [];
        
        mutations.forEach(mutation => {
            if (mutation.type === 'childList') {
                // Check if any new nodes were added
                mutation.addedNodes.forEach(node => {
                    // Check if the node is an element
                    if (node.nodeType === 1) {
                        // If it's an image with progressive attribute
                        if (node.tagName === 'IMG' && node.hasAttribute('progressive')) {
                            newProgressiveImages.push(node);
                        }
                        
                        // Find any progressive images in the added DOM tree
                        const childImages = node.querySelectorAll('img[progressive]');
                        childImages.forEach(img => newProgressiveImages.push(img));
                    }
                });
            }
        });
        
        // Set up all new progressive images
        newProgressiveImages.forEach(setupProgressiveImage);
    });
    
    // Start observing the document body for changes
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
}

/**
 * Add CSS for progressive image loading effects
 */
function addProgressiveImageStyles() {
    // Only add if not already present
    if (document.getElementById('progressive-image-styles')) return;
    
    const styleEl = document.createElement('style');
    styleEl.id = 'progressive-image-styles';
    styleEl.textContent = `
        .progressive-image-wrapper {
            position: relative;
            overflow: hidden;
            width: 100%;
            height: 0;
            padding-bottom: 56.25%; /* Default 16:9 aspect ratio */
        }
        
        .progressive-image-placeholder {
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background-size: cover;
            background-position: center;
            filter: blur(15px);
            transform: scale(1.1);
            transition: opacity 0.5s ease-out;
        }
        
        .progressive-image-thumb {
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            object-fit: cover;
            opacity: 0;
        }
        
        .progressive-image {
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            object-fit: cover;
            opacity: 0;
            transition: opacity 0.5s ease-in;
        }
        
        .progressive-image-loaded {
            opacity: 1;
        }
        
        .progressive-image-loaded ~ .progressive-image-placeholder {
            opacity: 0;
        }
    `;
    document.head.appendChild(styleEl);
}

// Initialize styles when script loads
addProgressiveImageStyles();