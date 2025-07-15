/**
 * Aikido Security Badge Error Handling
 * CSP-compliant fallback for Aikido security badge images
 */
(function() {
    'use strict';
    
    function setupAikidoBadgeErrorHandling() {
        const aikidoBadges = document.querySelectorAll('.aikido-security-badge');
        
        aikidoBadges.forEach(function(badge) {
            badge.addEventListener('error', function() {
                // Hide the image and show the fallback text
                this.style.display = 'none';
                const fallback = this.nextElementSibling;
                if (fallback && fallback.classList.contains('aikido-badge-fallback')) {
                    fallback.style.display = 'inline-block';
                }
            });
        });
    }
    
    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', setupAikidoBadgeErrorHandling);
    } else {
        setupAikidoBadgeErrorHandling();
    }
})();
