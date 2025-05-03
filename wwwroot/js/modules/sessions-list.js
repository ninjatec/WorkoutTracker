/**
 * Sessions List JavaScript module for WorkoutTracker
 * Contains functionality for the Sessions Index page
 */
registerModule('sessions-list', async function() {
    // Initialize sessions list page functionality
    setupSessionsList();
    setupFilterControls();
    setupSortingControls();
    
    // Return resolved promise when initialization is complete
    return Promise.resolve();
}, ['common']);

/**
 * Set up sessions list features
 */
function setupSessionsList() {
    console.log('Sessions list module initialized');
    
    // Enhance session rows with interactive features
    enhanceSessionRows();
    
    // Make table responsive on mobile devices
    setupResponsiveTable();
}

/**
 * Set up filter controls
 */
function setupFilterControls() {
    const searchForm = document.querySelector('form[method="get"]');
    const searchInput = document.querySelector('input[name="SearchString"]');
    
    if (searchForm && searchInput) {
        // Add responsive search behavior
        searchInput.addEventListener('keyup', function(e) {
            // If user presses Enter, submit the form
            if (e.key === 'Enter') {
                searchForm.submit();
            }
        });
        
        // Clear button functionality
        const clearBtn = document.querySelector('a.btn-secondary');
        if (clearBtn) {
            clearBtn.addEventListener('click', function(e) {
                searchInput.value = '';
            });
        }
    }
}

/**
 * Set up sorting controls
 */
function setupSortingControls() {
    // Add indicators for current sort direction
    const currentSort = new URLSearchParams(window.location.search).get('sortOrder');
    
    if (currentSort) {
        // Find the header that corresponds to the current sort
        const headers = document.querySelectorAll('th a');
        headers.forEach(header => {
            const headerSort = header.getAttribute('href').match(/sortOrder=([^&]*)/);
            if (headerSort && headerSort[1] === currentSort) {
                // Add an indicator for the current sort
                const indicator = document.createElement('span');
                indicator.className = 'ms-1';
                
                if (currentSort.includes('_desc')) {
                    indicator.innerHTML = '▼';
                } else {
                    indicator.innerHTML = '▲';
                }
                
                header.appendChild(indicator);
            }
        });
    }
}

/**
 * Enhance session rows with interactive features
 */
function enhanceSessionRows() {
    const sessionRows = document.querySelectorAll('tbody tr');
    
    sessionRows.forEach(row => {
        // Add hover effect
        row.addEventListener('mouseenter', function() {
            row.classList.add('table-hover');
        });
        
        row.addEventListener('mouseleave', function() {
            row.classList.remove('table-hover');
        });
        
        // Quick actions on mobile - detect touch devices
        if ('ontouchstart' in window) {
            // Add tap or swipe actions for mobile if needed
            setupMobileRowInteractions(row);
        }
    });
}

/**
 * Set up mobile-specific row interactions
 * @param {HTMLElement} row - The table row element
 */
function setupMobileRowInteractions(row) {
    // Example: Double tap to show action menu
    let lastTap = 0;
    
    row.addEventListener('touchend', function(e) {
        const currentTime = new Date().getTime();
        const tapLength = currentTime - lastTap;
        
        if (tapLength < 500 && tapLength > 0) {
            // Double tap detected
            e.preventDefault();
            
            // Show a quick action menu
            const detailsLink = row.querySelector('a[href*="./Details"]');
            if (detailsLink) {
                // Navigate to details page on double tap
                window.location.href = detailsLink.href;
            }
        }
        
        lastTap = currentTime;
    });
}

/**
 * Make table responsive for mobile devices
 */
function setupResponsiveTable() {
    const table = document.querySelector('table.table');
    
    if (table) {
        if (window.innerWidth < 768) {
            // For small screens, add data attributes to cells for responsive display
            const headerCells = table.querySelectorAll('thead th');
            const headerTexts = Array.from(headerCells).map(th => th.textContent.trim());
            
            const bodyRows = table.querySelectorAll('tbody tr');
            
            bodyRows.forEach(row => {
                const cells = row.querySelectorAll('td');
                
                cells.forEach((cell, index) => {
                    if (index < headerTexts.length) {
                        cell.setAttribute('data-label', headerTexts[index]);
                    }
                });
            });
        }
    }
}

// Add sessions list specific CSS
(function addSessionsListStyles() {
    if (document.getElementById('sessions-list-module-styles')) return;
    
    const styleEl = document.createElement('style');
    styleEl.id = 'sessions-list-module-styles';
    styleEl.textContent = `
        .table-hover {
            background-color: rgba(0, 123, 255, 0.05);
        }
        
        /* Responsive table styles for mobile */
        @media (max-width: 767.98px) {
            .table-responsive {
                overflow-x: visible;
            }
            
            /* Optional: Add more mobile-specific styles here */
        }
    `;
    
    document.head.appendChild(styleEl);
})();