/**
 * Mobile Navigation Enhancement for WorkoutTracker
 * Provides specialized mobile navigation experiences including:
 * - Swipe gestures for common actions
 * - Pull-to-refresh functionality
 * - Context breadcrumb alternative
 * - Session navigation shortcuts
 * - Haptic feedback for interactive elements
 */
document.addEventListener('DOMContentLoaded', function() {
    initMobileNavigation();
    initContextNavigation();
    initSessionShortcuts();
    initSwipeGestures();
    initPullToRefresh();
    initHapticFeedback();
});

/**
 * Initialize mobile bottom navigation
 */
function initMobileNavigation() {
    // Set active state on current page
    const currentPath = window.location.pathname.toLowerCase();
    
    // Set appropriate nav item as active
    if (currentPath.includes('/index') || currentPath === '/' || currentPath === '') {
        document.getElementById('navHome')?.classList.add('active');
    } else if (currentPath.includes('/sessions/index')) {
        document.getElementById('navWorkouts')?.classList.add('active');
    } else if (currentPath.includes('/sessions/create')) {
        document.getElementById('navAdd')?.classList.add('active');
    } else if (currentPath.includes('/reports')) {
        document.getElementById('navReports')?.classList.add('active');
    }
}

/**
 * Initialize context navigation (alternative to breadcrumbs)
 */
function initContextNavigation() {
    const contextNav = document.querySelector('.mobile-context-nav');
    const contextLink = document.getElementById('currentContextPath');
    
    if (contextNav && contextLink) {
        // Determine context based on URL path
        const path = window.location.pathname.toLowerCase();
        const pathSegments = path.split('/').filter(p => p);
        
        // Empty or root path shows just home
        if (pathSegments.length === 0 || (pathSegments.length === 1 && pathSegments[0] === 'index')) {
            contextLink.innerHTML = '<i class="bi bi-house"></i> Home';
            contextLink.href = '/';
            contextLink.classList.add('active');
            return;
        }
        
        // Otherwise, set the context path
        let displayName = '';
        let icon = '';
        
        // Determine primary section and set icon + name
        if (path.includes('/sessions')) {
            displayName = pathSegments.length > 1 && pathSegments[1] !== 'index' ? 
                'Session Details' : 'Workouts';
            icon = 'bi-list-check';
        } else if (path.includes('/reports')) {
            displayName = 'Reports';
            icon = 'bi-graph-up';
        } else if (path.includes('/calculator')) {
            displayName = '1RM Calculator';
            icon = 'bi-calculator';
        } else if (path.includes('/help')) {
            displayName = 'Help Center';
            icon = 'bi-question-circle';
        } else if (path.includes('/feedback')) {
            displayName = 'Feedback';
            icon = 'bi-chat-dots';
        } else {
            // Default for unknown paths
            displayName = pathSegments[0].charAt(0).toUpperCase() + pathSegments[0].slice(1);
            icon = 'bi-folder';
        }
        
        // Update context navigation
        contextLink.innerHTML = `<i class="bi ${icon}"></i> ${displayName}`;
        contextLink.href = '/' + pathSegments[0];
        
        // Add additional context items for deeper paths
        if (pathSegments.length > 1 && pathSegments[1] !== 'index') {
            // For content with IDs, like Sessions/123
            if (!isNaN(pathSegments[pathSegments.length - 1])) {
                // This is likely a numeric ID, show appropriate label
                const idName = pathSegments[pathSegments.length - 1];
                const actionName = pathSegments.length > 2 ? 
                    pathSegments[pathSegments.length - 2].charAt(0).toUpperCase() + pathSegments[pathSegments.length - 2].slice(1) :
                    'Details';
                
                const detailItem = document.createElement('a');
                detailItem.classList.add('mobile-context-nav-item', 'active');
                detailItem.innerHTML = `<i class="bi bi-card-text"></i> ${actionName} #${idName}`;
                detailItem.href = path;
                contextNav.appendChild(detailItem);
            } else {
                // Action name (Edit, Create, etc.)
                const actionName = pathSegments[1].charAt(0).toUpperCase() + pathSegments[1].slice(1);
                const actionItem = document.createElement('a');
                actionItem.classList.add('mobile-context-nav-item', 'active');
                actionItem.innerHTML = `<i class="bi bi-pencil"></i> ${actionName}`;
                actionItem.href = path;
                contextNav.appendChild(actionItem);
            }
        } else {
            // Current section is active
            contextLink.classList.add('active');
        }
    }
}

/**
 * Initialize session shortcuts flyout menu
 */
function initSessionShortcuts() {
    const shortcutButton = document.getElementById('sessionShortcutButton');
    const shortcutMenu = document.getElementById('sessionShortcutMenu');
    
    if (shortcutButton && shortcutMenu) {
        shortcutButton.addEventListener('click', function() {
            shortcutMenu.classList.toggle('show');
            triggerHapticFeedback('medium');
        });
        
        // Close menu when clicking outside
        document.addEventListener('click', function(event) {
            if (!shortcutButton.contains(event.target) && !shortcutMenu.contains(event.target)) {
                shortcutMenu.classList.remove('show');
            }
        });
    }
}

/**
 * Initialize swipe gestures for common actions
 */
function initSwipeGestures() {
    // Enable swipe actions on tables and lists
    enableSwipeActionsOnListItems();
    enableSwipeActionsOnTableRows();
}

/**
 * Add swipe actions to list items (like in Sessions list)
 */
function enableSwipeActionsOnListItems() {
    // Target list items that can have actions (edit, delete)
    const listItems = document.querySelectorAll('.list-group-item:not(.list-group-item-action)');
    
    listItems.forEach(item => {
        let startX = 0;
        let currentX = 0;
        let isDragging = false;
        
        // Extract actions from item
        const editButton = item.querySelector('a[href*="Edit"]');
        const deleteButton = item.querySelector('a[href*="Delete"]');
        
        item.addEventListener('touchstart', function(e) {
            startX = e.touches[0].clientX;
            currentX = startX;
            isDragging = true;
        }, { passive: true });
        
        item.addEventListener('touchmove', function(e) {
            if (!isDragging) return;
            
            currentX = e.touches[0].clientX;
            const diffX = currentX - startX;
            
            // Limit swipe distance
            if (diffX < -120) diffX = -120;
            if (diffX > 0) diffX = 0; // Only allow left swipes
            
            // Apply transform
            item.style.transform = `translateX(${diffX}px)`;
            
            // Show/hide action feedback
            if (diffX <= -60) {
                // Show swipe action indicator if needed
                if (!item.querySelector('.swipe-action-indicator')) {
                    const indicator = document.createElement('div');
                    indicator.className = 'swipe-action-indicator';
                    indicator.innerHTML = deleteButton ? 
                        '<i class="bi bi-trash"></i> Delete' : 
                        '<i class="bi bi-pencil"></i> Edit';
                    item.appendChild(indicator);
                }
            } else {
                // Remove indicator if distance not sufficient
                const indicator = item.querySelector('.swipe-action-indicator');
                if (indicator) indicator.remove();
            }
        }, { passive: true });
        
        item.addEventListener('touchend', function() {
            isDragging = false;
            const diffX = currentX - startX;
            
            // Remove any indicators
            const indicator = item.querySelector('.swipe-action-indicator');
            if (indicator) indicator.remove();
            
            // If swiped far enough, trigger action
            if (diffX <= -60) {
                // Trigger appropriate action
                if (deleteButton) {
                    triggerHapticFeedback('strong');
                    deleteButton.click();
                } else if (editButton) {
                    triggerHapticFeedback('medium');
                    editButton.click();
                }
            }
            
            // Reset transform with animation
            item.style.transition = 'transform 0.3s ease';
            item.style.transform = 'translateX(0)';
            setTimeout(() => {
                item.style.transition = '';
            }, 300);
        });
    });
}

/**
 * Add swipe actions to table rows
 */
function enableSwipeActionsOnTableRows() {
    // Target table rows that aren't headers
    const tableRows = document.querySelectorAll('table:not(.no-swipe) tbody tr');
    
    tableRows.forEach(row => {
        let startX = 0;
        let currentX = 0;
        let isDragging = false;
        
        // Extract actions from row cells
        const actionCell = row.querySelector('td:last-child');
        const editButton = actionCell?.querySelector('a[href*="Edit"], button.btn-primary, a.btn-primary');
        const deleteButton = actionCell?.querySelector('a[href*="Delete"], button.btn-danger, a.btn-danger');
        
        // Skip if no action buttons found
        if (!editButton && !deleteButton) return;
        
        row.addEventListener('touchstart', function(e) {
            startX = e.touches[0].clientX;
            currentX = startX;
            isDragging = true;
        }, { passive: true });
        
        row.addEventListener('touchmove', function(e) {
            if (!isDragging) return;
            
            currentX = e.touches[0].clientX;
            const diffX = currentX - startX;
            
            // Limit swipe distance
            if (diffX < -120) diffX = -120;
            if (diffX > 0) diffX = 0; // Only allow left swipes
            
            // Apply transform
            row.style.transform = `translateX(${diffX}px)`;
            
            // Show/hide action feedback
            if (diffX <= -60) {
                // Show swipe action indicator if needed
                if (!row.querySelector('.swipe-action-indicator')) {
                    const indicator = document.createElement('div');
                    indicator.className = 'swipe-action-indicator';
                    indicator.innerHTML = deleteButton ? 
                        '<i class="bi bi-trash"></i> Delete' : 
                        '<i class="bi bi-pencil"></i> Edit';
                    row.appendChild(indicator);
                }
            } else {
                // Remove indicator if distance not sufficient
                const indicator = row.querySelector('.swipe-action-indicator');
                if (indicator) indicator.remove();
            }
        }, { passive: true });
        
        row.addEventListener('touchend', function() {
            isDragging = false;
            const diffX = currentX - startX;
            
            // Remove any indicators
            const indicator = row.querySelector('.swipe-action-indicator');
            if (indicator) indicator.remove();
            
            // If swiped far enough, trigger action
            if (diffX <= -60) {
                // Trigger appropriate action
                if (deleteButton) {
                    triggerHapticFeedback('strong');
                    deleteButton.click();
                } else if (editButton) {
                    triggerHapticFeedback('medium');
                    editButton.click();
                }
            }
            
            // Reset transform with animation
            row.style.transition = 'transform 0.3s ease';
            row.style.transform = 'translateX(0)';
            setTimeout(() => {
                row.style.transition = '';
            }, 300);
        });
    });
}

/**
 * Initialize pull-to-refresh functionality for data lists
 */
function initPullToRefresh() {
    // Only apply to relevant pages - those with lists or tables of data
    const hasDataTable = document.querySelector('table:not(.no-pull-refresh)');
    const hasDataList = document.querySelector('.list-group:not(.no-pull-refresh)');
    
    if (!hasDataTable && !hasDataList) return;
    
    let startY = 0;
    let currentY = 0;
    let isDragging = false;
    let refreshIndicator = null;
    
    // Create and add refresh indicator
    function createRefreshIndicator() {
        if (refreshIndicator) return;
        
        refreshIndicator = document.createElement('div');
        refreshIndicator.className = 'pull-refresh-indicator';
        refreshIndicator.innerHTML = '<div class="spinner"><i class="bi bi-arrow-clockwise"></i></div><span>Pull to refresh</span>';
        document.body.appendChild(refreshIndicator);
    }
    
    // Main content area to apply pull effect
    const mainContent = document.querySelector('main');
    
    // Only enable when at the top of the page
    document.addEventListener('touchstart', function(e) {
        if (window.scrollY === 0) {
            startY = e.touches[0].clientY;
            currentY = startY;
            isDragging = true;
            createRefreshIndicator();
        }
    }, { passive: true });
    
    document.addEventListener('touchmove', function(e) {
        if (!isDragging) return;
        
        currentY = e.touches[0].clientY;
        const diffY = currentY - startY;
        
        // Only allow pull down
        if (diffY <= 0) {
            return;
        }
        
        // Resist pulling too far
        const pullDistance = Math.min(diffY * 0.4, 80);
        
        // Apply transform to content
        mainContent.style.transform = `translateY(${pullDistance}px)`;
        
        // Update refresh indicator state
        if (refreshIndicator) {
            refreshIndicator.style.opacity = Math.min(pullDistance / 40, 1);
            refreshIndicator.style.transform = `translateY(${pullDistance / 2}px)`;
            
            if (pullDistance >= 60) {
                refreshIndicator.classList.add('ready');
                refreshIndicator.querySelector('span').textContent = 'Release to refresh';
            } else {
                refreshIndicator.classList.remove('ready');
                refreshIndicator.querySelector('span').textContent = 'Pull to refresh';
            }
        }
    }, { passive: true });
    
    document.addEventListener('touchend', function() {
        if (!isDragging) return;
        
        isDragging = false;
        const diffY = currentY - startY;
        
        // Reset transform with animation
        mainContent.style.transition = 'transform 0.3s ease';
        mainContent.style.transform = 'translateY(0)';
        
        // Trigger refresh if pulled far enough
        if (diffY >= 60) {
            triggerHapticFeedback('medium');
            
            if (refreshIndicator) {
                refreshIndicator.classList.add('refreshing');
                refreshIndicator.querySelector('span').textContent = 'Refreshing...';
                refreshIndicator.querySelector('.spinner').classList.add('spin');
            }
            
            // Reload the page after a brief delay
            setTimeout(() => {
                window.location.reload();
            }, 800);
        } else {
            // Reset indicator
            if (refreshIndicator) {
                refreshIndicator.style.opacity = '0';
            }
        }
        
        // Reset state
        setTimeout(() => {
            mainContent.style.transition = '';
            if (refreshIndicator) {
                refreshIndicator.remove();
                refreshIndicator = null;
            }
        }, 300);
    });
}

/**
 * Initialize and provide haptic feedback for interactive elements
 */
function initHapticFeedback() {
    // Add haptic feedback to all interactive elements
    document.querySelectorAll('a, button, input[type=button], input[type=submit], .list-group-item-action').forEach(element => {
        element.addEventListener('click', function() {
            triggerHapticFeedback('light');
        });
        
        // Add touchend event to ensure mobile devices respond properly
        element.addEventListener('touchend', function(e) {
            // Prevent default only if this is a simple tap (not part of a swipe)
            if (!element.dataset.swiping) {
                triggerHapticFeedback('light');
                // Only prevent default for buttons and anchors that don't have href="#"
                if (element.tagName === 'BUTTON' || 
                   (element.tagName === 'A' && (!element.getAttribute('href') || element.getAttribute('href') !== '#'))) {
                    e.preventDefault();
                }
                // Trigger the click event to ensure handlers are executed
                element.click();
            }
        });
        
        // Track if we're in a swipe operation
        element.addEventListener('touchstart', function() {
            element.dataset.swiping = 'false';
        });
        
        element.addEventListener('touchmove', function() {
            element.dataset.swiping = 'true';
        });
    });
    
    // Special actions get more intense feedback
    document.querySelectorAll('button.btn-danger, a.btn-danger, button[type=submit]').forEach(element => {
        element.addEventListener('click', function() {
            triggerHapticFeedback('medium');
        });
    });
    
    // Handle Bootstrap accordion headers specifically for mobile - they might be using data-bs-toggle="collapse"
    document.querySelectorAll('[data-bs-toggle="collapse"], [data-toggle="collapse"]').forEach(element => {
        element.addEventListener('touchend', function(e) {
            if (!element.dataset.swiping) {
                triggerHapticFeedback('light');
                // Don't prevent default to allow Bootstrap's collapse functionality to work
            }
        });
    });
}

/**
 * Trigger haptic feedback with different intensities
 * @param {string} intensity - 'light', 'medium', or 'strong'
 */
function triggerHapticFeedback(intensity = 'light') {
    // Check if the device supports vibration
    if (!navigator.vibrate) return;
    
    switch (intensity) {
        case 'light':
            navigator.vibrate(10);
            break;
        case 'medium':
            navigator.vibrate(25);
            break;
        case 'strong':
            navigator.vibrate([25, 30, 40]);
            break;
        default:
            navigator.vibrate(10);
    }
}