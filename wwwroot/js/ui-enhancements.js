/**
 * UI/UX Enhancements for WorkoutTracker
 * Implements JavaScript functionality needed for the UI/UX Enhancement Plan
 */

/**
 * Initialize all UI enhancement features
 */
document.addEventListener('DOMContentLoaded', function() {
    setupMobileNavigation();
    setupAccessibilityFeatures();
    setupFormEnhancements();
    setupToasts();
    setupLoadingIndicators();
    setupTouchGestures();
    setupCardInteractions();
});

/**
 * Mobile Navigation Setup
 * Handles the bottom navigation bar on mobile devices
 */
function setupMobileNavigation() {
    // Check if mobile navigation exists
    const mobileNav = document.querySelector('.mobile-nav');
    if (!mobileNav) return;

    // Handle active state for current page
    const currentPath = window.location.pathname;
    const mobileNavLinks = document.querySelectorAll('.mobile-nav-link');
    
    mobileNavLinks.forEach(link => {
        const href = link.getAttribute('href');
        if (href && currentPath.startsWith(href)) {
            link.classList.add('active');
            const item = link.closest('.mobile-nav-item');
            if (item) {
                item.classList.add('active');
            }
        }
    });

    // Handle hamburger menu for secondary navigation on mobile
    const hamburgerMenu = document.querySelector('.mobile-hamburger');
    const secondaryNav = document.querySelector('.secondary-mobile-nav');
    
    if (hamburgerMenu && secondaryNav) {
        hamburgerMenu.addEventListener('click', function() {
            secondaryNav.classList.toggle('show');
        });
    }
}

/**
 * Accessibility Features
 * Enhances the application with better accessibility
 */
function setupAccessibilityFeatures() {
    // Ensure all interactive elements have proper focus styling
    document.querySelectorAll('a, button, input, select, textarea, [role="button"]').forEach(element => {
        if (!element.classList.contains('no-focus-styling')) {
            element.addEventListener('focus', () => {
                element.setAttribute('data-focused', 'true');
            });
            
            element.addEventListener('blur', () => {
                element.removeAttribute('data-focused');
            });
        }
    });
    
    // Add ARIA labels to elements that need them
    document.querySelectorAll('.icon-only-btn').forEach(btn => {
        if (!btn.getAttribute('aria-label')) {
            const text = btn.textContent.trim();
            btn.setAttribute('aria-label', text || 'Button');
        }
    });
}

/**
 * Form Enhancements
 * Improves form UX with validation, multi-step functionality, etc.
 */
function setupFormEnhancements() {
    // Setup client-side validation for forms
    const forms = document.querySelectorAll('form.needs-validation');
    Array.from(forms).forEach(form => {
        form.addEventListener('submit', event => {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            
            form.classList.add('was-validated');
        }, false);
    });
    
    // Setup multi-step forms
    const multiStepForms = document.querySelectorAll('.multi-step-form');
    multiStepForms.forEach(form => {
        const steps = form.querySelectorAll('.form-step');
        const nextButtons = form.querySelectorAll('.step-next');
        const prevButtons = form.querySelectorAll('.step-prev');
        const progress = form.querySelector('.form-steps-progress');
        
        if (steps.length) {
            // Initialize the first step
            steps[0].classList.add('active');
            updateProgress(form, 0);
            
            // Setup next buttons
            nextButtons.forEach(button => {
                button.addEventListener('click', event => {
                    event.preventDefault();
                    const currentStep = form.querySelector('.form-step.active');
                    const currentIndex = Array.from(steps).indexOf(currentStep);
                    
                    if (validateStep(currentStep) && currentIndex < steps.length - 1) {
                        currentStep.classList.remove('active');
                        steps[currentIndex + 1].classList.add('active');
                        updateProgress(form, currentIndex + 1);
                    }
                });
            });
            
            // Setup previous buttons
            prevButtons.forEach(button => {
                button.addEventListener('click', event => {
                    event.preventDefault();
                    const currentStep = form.querySelector('.form-step.active');
                    const currentIndex = Array.from(steps).indexOf(currentStep);
                    
                    if (currentIndex > 0) {
                        currentStep.classList.remove('active');
                        steps[currentIndex - 1].classList.add('active');
                        updateProgress(form, currentIndex - 1);
                    }
                });
            });
        }
    });
    
    // Form helpers for mobile
    if (window.innerWidth <= 768) {
        // Add touch-friendly class to small buttons
        document.querySelectorAll('.btn-sm').forEach(btn => {
            btn.classList.remove('btn-sm');
            btn.classList.add('touch-friendly-btn');
        });
    }
}

/**
 * Validates a form step
 * @param {HTMLElement} step - The form step element to validate
 * @return {boolean} - Whether the step is valid
 */
function validateStep(step) {
    const inputs = step.querySelectorAll('input, select, textarea');
    let isValid = true;
    
    inputs.forEach(input => {
        if (input.hasAttribute('required') && !input.value) {
            isValid = false;
            input.classList.add('is-invalid');
        } else {
            input.classList.remove('is-invalid');
        }
    });
    
    return isValid;
}

/**
 * Updates the progress indicator for multi-step forms
 * @param {HTMLElement} form - The form element
 * @param {number} currentIndex - The current step index
 */
function updateProgress(form, currentIndex) {
    const indicators = form.querySelectorAll('.step-circle');
    
    indicators.forEach((indicator, index) => {
        if (index < currentIndex) {
            indicator.classList.remove('active');
            indicator.classList.add('completed');
        } else if (index === currentIndex) {
            indicator.classList.add('active');
            indicator.classList.remove('completed');
        } else {
            indicator.classList.remove('active');
            indicator.classList.remove('completed');
        }
    });
}

/**
 * Toast Notifications
 * Sets up the toast notification system
 */
function setupToasts() {
    // Create toast container if it doesn't exist
    if (!document.querySelector('.toast-container')) {
        const toastContainer = document.createElement('div');
        toastContainer.className = 'toast-container';
        document.body.appendChild(toastContainer);
    }
    
    // Initialize Bootstrap toasts
    const toastElList = [].slice.call(document.querySelectorAll('.toast'));
    toastElList.map(function (toastEl) {
        return new bootstrap.Toast(toastEl);
    });
}

/**
 * Show a toast notification
 * @param {string} message - The message to display
 * @param {string} type - The type of toast (success, error, warning, info)
 * @param {number} duration - The duration to show the toast in ms
 */
function showToast(message, type = 'info', duration = 5000) {
    const container = document.querySelector('.toast-container');
    if (!container) return;
    
    const toast = document.createElement('div');
    toast.className = `toast align-items-center border-0 bg-${type} text-white`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    toast.setAttribute('data-bs-delay', duration);
    
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;
    
    container.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast);
    bsToast.show();
    
    // Remove toast after it's hidden
    toast.addEventListener('hidden.bs.toast', function () {
        toast.remove();
    });
}

/**
 * Loading Indicators
 * Manages the global loading indicator
 */
function setupLoadingIndicators() {
    // Create loading overlay if it doesn't exist
    if (!document.querySelector('.loading-overlay')) {
        const overlay = document.createElement('div');
        overlay.className = 'loading-overlay';
        overlay.innerHTML = `
            <div class="loading-spinner-container">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <div class="loading-message">Please wait...</div>
            </div>
        `;
        document.body.appendChild(overlay);
    }
    
    // Add listener to forms that should show loading
    document.querySelectorAll('form.show-loading-on-submit').forEach(form => {
        form.addEventListener('submit', () => {
            showLoading('Processing your request...');
        });
    });
    
    // Add listener to links that should show loading
    document.querySelectorAll('a.show-loading-on-click').forEach(link => {
        link.addEventListener('click', () => {
            showLoading();
        });
    });
}

/**
 * Shows the global loading indicator
 * @param {string} message - Optional message to display
 */
function showLoading(message = 'Loading...') {
    const overlay = document.querySelector('.loading-overlay');
    const messageElement = overlay.querySelector('.loading-message');
    
    if (messageElement) {
        messageElement.textContent = message;
    }
    
    overlay.classList.add('visible');
}

/**
 * Hides the global loading indicator
 */
function hideLoading() {
    const overlay = document.querySelector('.loading-overlay');
    overlay.classList.remove('visible');
}

/**
 * Touch Gestures
 * Sets up swipe gestures for mobile experiences
 */
function setupTouchGestures() {
    // Only set up touch gestures on mobile devices
    if (window.innerWidth > 768) return;
    
    // Find all elements that should have swipe gestures
    const swipeItems = document.querySelectorAll('.swipe-item');
    
    swipeItems.forEach(item => {
        let startX;
        let currentX;
        let threshold = 100; // Minimum swipe distance
        
        // Set up touch events
        item.addEventListener('touchstart', (e) => {
            startX = e.touches[0].clientX;
            item.classList.add('swiping');
        }, { passive: true });
        
        item.addEventListener('touchmove', (e) => {
            if (!startX) return;
            currentX = e.touches[0].clientX;
            const diffX = currentX - startX;
            
            // Prevent scrolling the page if we're swiping horizontally
            if (Math.abs(diffX) > 10) {
                e.preventDefault();
            }
            
            // Limit swipe to left only (for delete/edit) and cap at the threshold
            if (diffX < 0) {
                const translateX = Math.max(diffX, -threshold);
                item.style.transform = `translateX(${translateX}px)`;
            }
        }, { passive: false });
        
        item.addEventListener('touchend', () => {
            if (!startX || !currentX) {
                item.classList.remove('swiping');
                return;
            }
            
            const diffX = currentX - startX;
            
            // If swiped far enough, show action buttons
            if (diffX < -threshold / 2) {
                item.classList.add('swiped');
                // Vibrate if browser supports it
                if (navigator.vibrate) {
                    navigator.vibrate(50);
                }
            } else {
                item.style.transform = '';
                item.classList.remove('swiped');
            }
            
            item.classList.remove('swiping');
            startX = null;
            currentX = null;
        });
    });
    
    // Add handlers for the swipe action buttons
    document.querySelectorAll('.swipe-action-delete').forEach(btn => {
        btn.addEventListener('click', () => {
            const item = btn.closest('.swipe-item');
            if (item) {
                // You would typically show a confirmation dialog here
                const confirmDelete = confirm('Are you sure you want to delete this item?');
                if (confirmDelete) {
                    // Handle the delete action
                    // This might trigger a form submission or AJAX request
                }
                
                // Reset the swipe state
                item.style.transform = '';
                item.classList.remove('swiped');
            }
        });
    });
    
    document.querySelectorAll('.swipe-action-edit').forEach(btn => {
        btn.addEventListener('click', () => {
            const item = btn.closest('.swipe-item');
            if (item) {
                // Handle the edit action
                // This might navigate to an edit page or show a modal
                
                // Reset the swipe state
                item.style.transform = '';
                item.classList.remove('swiped');
            }
        });
    });
}

/**
 * Card Interactions
 * Sets up interactive behaviors for card elements
 */
function setupCardInteractions() {
    document.querySelectorAll('.card-interactive').forEach(card => {
        // If the card has a main action link or button
        const mainAction = card.querySelector('.card-action');
        if (mainAction) {
            card.addEventListener('click', (event) => {
                // Don't trigger if clicked on a button or link inside the card
                if (!event.target.closest('a, button, .form-control, .form-select')) {
                    mainAction.click();
                }
            });
        }
    });
}

// Export functions for use in other modules
window.WorkoutTrackerUX = {
    showToast,
    showLoading,
    hideLoading
};
