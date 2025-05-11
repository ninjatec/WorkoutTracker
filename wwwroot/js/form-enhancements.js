/**
 * Form enhancements for WorkoutTracker
 * 
 * Features:
 * - Multi-step form handling
 * - Enhanced form validation
 * - Mobile optimizations for forms
 * - Accessible form interactions
 */

document.addEventListener('DOMContentLoaded', function() {
    initializeMultiStepForms();
    enhanceFormValidation();
    optimizeFormsForMobile();
    setupAccessibleFormElements();
});

/**
 * Initialize multi-step forms functionality
 */
function initializeMultiStepForms() {
    const multiStepForms = document.querySelectorAll('.multi-step-form');
    
    multiStepForms.forEach(form => {
        const steps = form.querySelectorAll('.form-step');
        const progressItems = form.querySelectorAll('.step-item');
        const nextButtons = form.querySelectorAll('.step-next');
        const prevButtons = form.querySelectorAll('.step-prev');
        
        // Initialize the first step
        if (steps.length > 0) {
            steps[0].classList.add('active');
            if (progressItems.length > 0) {
                progressItems[0].classList.add('active');
            }
        }
        
        // Next button click handler
        nextButtons.forEach(button => {
            button.addEventListener('click', function(e) {
                e.preventDefault();
                
                const currentStep = form.querySelector('.form-step.active');
                const currentIndex = Array.from(steps).indexOf(currentStep);
                
                // Validate current step before proceeding
                if (validateStep(currentStep) && currentIndex < steps.length - 1) {
                    // Hide current step
                    currentStep.classList.remove('active');
                    
                    // Show next step
                    steps[currentIndex + 1].classList.add('active');
                    
                    // Update progress
                    if (progressItems.length > 0) {
                        progressItems[currentIndex].classList.remove('active');
                        progressItems[currentIndex].classList.add('completed');
                        progressItems[currentIndex + 1].classList.add('active');
                    }
                    
                    // Scroll to top of step
                    steps[currentIndex + 1].scrollIntoView({ behavior: 'smooth', block: 'start' });
                }
            });
        });
        
        // Previous button click handler
        prevButtons.forEach(button => {
            button.addEventListener('click', function(e) {
                e.preventDefault();
                
                const currentStep = form.querySelector('.form-step.active');
                const currentIndex = Array.from(steps).indexOf(currentStep);
                
                if (currentIndex > 0) {
                    // Hide current step
                    currentStep.classList.remove('active');
                    
                    // Show previous step
                    steps[currentIndex - 1].classList.add('active');
                    
                    // Update progress
                    if (progressItems.length > 0) {
                        progressItems[currentIndex].classList.remove('active');
                        progressItems[currentIndex - 1].classList.remove('completed');
                        progressItems[currentIndex - 1].classList.add('active');
                    }
                    
                    // Scroll to top of step
                    steps[currentIndex - 1].scrollIntoView({ behavior: 'smooth', block: 'start' });
                }
            });
        });
    });
}

/**
 * Validates a single form step
 * @param {HTMLElement} step - The form step to validate
 * @returns {boolean} - Whether the step is valid
 */
function validateStep(step) {
    const requiredFields = step.querySelectorAll('[required]');
    let isValid = true;
    
    requiredFields.forEach(field => {
        // Clear previous validation
        field.classList.remove('is-invalid');
        
        // Check if field is empty
        if (!field.value.trim()) {
            field.classList.add('is-invalid');
            isValid = false;
            
            // Create or update feedback message
            let feedback = field.nextElementSibling;
            if (!feedback || !feedback.classList.contains('invalid-feedback')) {
                feedback = document.createElement('div');
                feedback.classList.add('invalid-feedback');
                field.parentNode.insertBefore(feedback, field.nextSibling);
            }
            feedback.textContent = 'This field is required.';
        }
        
        // For inputs with pattern attribute, check if pattern matches
        if (field.hasAttribute('pattern') && field.value.trim()) {
            const pattern = new RegExp(field.getAttribute('pattern'));
            if (!pattern.test(field.value)) {
                field.classList.add('is-invalid');
                isValid = false;
                
                // Create or update feedback message
                let feedback = field.nextElementSibling;
                if (!feedback || !feedback.classList.contains('invalid-feedback')) {
                    feedback = document.createElement('div');
                    feedback.classList.add('invalid-feedback');
                    field.parentNode.insertBefore(feedback, field.nextSibling);
                }
                feedback.textContent = field.getAttribute('data-error-message') || 'Please enter a valid value.';
            }
        }
    });
    
    return isValid;
}

/**
 * Enhance form validation
 */
function enhanceFormValidation() {
    const forms = document.querySelectorAll('form.needs-validation');
    
    forms.forEach(form => {
        // Add submit event listener
        form.addEventListener('submit', function(event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            
            form.classList.add('was-validated');
            
            // Focus the first invalid field
            const firstInvalid = form.querySelector(':invalid');
            if (firstInvalid) {
                firstInvalid.focus();
            }
        }, false);
        
        // Real-time validation for each input
        form.querySelectorAll('input, select, textarea').forEach(input => {
            input.addEventListener('input', function() {
                if (input.checkValidity()) {
                    input.classList.remove('is-invalid');
                    input.classList.add('is-valid');
                } else {
                    input.classList.remove('is-valid');
                    input.classList.add('is-invalid');
                }
            });
        });
    });
}

/**
 * Optimize forms for mobile devices
 */
function optimizeFormsForMobile() {
    // Only apply mobile optimizations on small screens
    if (window.innerWidth > 768) return;
    
    // Add touch-friendly class to small buttons in forms
    document.querySelectorAll('form .btn-sm').forEach(button => {
        button.classList.remove('btn-sm');
        button.classList.add('touch-friendly-btn');
    });
    
    // Enlarge checkboxes and radio buttons
    document.querySelectorAll('.form-check-input').forEach(input => {
        input.style.width = '1.5em';
        input.style.height = '1.5em';
    });
    
    // Adjust dropdowns for better touch targets
    document.querySelectorAll('.form-select').forEach(select => {
        select.classList.add('form-select-lg');
    });
    
    // Handle fixed positioning when keyboard is open
    const formInputs = document.querySelectorAll('input[type="text"], input[type="email"], input[type="number"], input[type="password"], textarea');
    formInputs.forEach(input => {
        input.addEventListener('focus', function() {
            document.body.classList.add('keyboard-open');
        });
        
        input.addEventListener('blur', function() {
            document.body.classList.remove('keyboard-open');
        });
    });
}

/**
 * Setup accessible form elements
 */
function setupAccessibleFormElements() {
    // Add ARIA attributes for better screen reader support
    document.querySelectorAll('.form-group').forEach(group => {
        const label = group.querySelector('label');
        const input = group.querySelector('input, select, textarea');
        
        if (label && input && !input.getAttribute('id')) {
            const id = 'form-input-' + Math.random().toString(36).substring(2, 9);
            input.setAttribute('id', id);
            label.setAttribute('for', id);
        }
    });
    
    // Add bolder contrast to placeholders (moved to CSS)
    
    // Add labels to inputs that only have placeholders
    document.querySelectorAll('input[placeholder]:not([aria-label])').forEach(input => {
        if (!input.previousElementSibling || input.previousElementSibling.tagName !== 'LABEL') {
            input.setAttribute('aria-label', input.getAttribute('placeholder'));
        }
    });
}

// Export functions for use in other scripts
window.FormEnhancements = {
    initializeMultiStepForms,
    validateStep,
    enhanceFormValidation
};
