/**
 * Home page JavaScript module for WorkoutTracker
 * Contains functionality specific to the homepage
 */
registerModule('home', async function() {
    // Initialize home page functionality
    setupHomePageFeatures();
    
    // Return resolved promise when initialization is complete
    return Promise.resolve();
}, ['common']);

/**
 * Set up features specific to the home page
 */
function setupHomePageFeatures() {
    // Add any home page specific functionality here
    console.log('Home page module initialized');
    
    // Example: Set up quick links interaction
    const quickLinks = document.querySelectorAll('.quick-link');
    if (quickLinks.length > 0) {
        quickLinks.forEach(link => {
            link.addEventListener('click', function(e) {
                // Add any special handling for quick links here if needed
                // For simple links, this isn't necessary
                console.log(`Quick link clicked: ${link.getAttribute('href')}`);
            });
        });
    }
    
    // Example: Handle any dashboard summary cards
    const summaryCards = document.querySelectorAll('.summary-card');
    if (summaryCards.length > 0) {
        summaryCards.forEach(card => {
            // Add hover effects or other interactions
            card.addEventListener('mouseenter', function() {
                card.classList.add('card-hover');
            });
            
            card.addEventListener('mouseleave', function() {
                card.classList.remove('card-hover');
            }, { passive: true }); // Using passive for better scroll performance
        });
    }
}

// Add home page specific CSS
(function addHomeStyles() {
    if (document.getElementById('home-module-styles')) return;
    
    const styleEl = document.createElement('style');
    styleEl.id = 'home-module-styles';
    styleEl.textContent = `
        .card-hover {
            transform: translateY(-5px);
            transition: transform 0.3s ease;
            box-shadow: 0 6px 12px rgba(0,0,0,0.1);
        }
        
        /* Additional home page specific styles can go here */
    `;
    
    document.head.appendChild(styleEl);
})();