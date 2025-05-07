// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener('DOMContentLoaded', function() {
    // SignalR Connection
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/importProgressHub", {
            skipNegotiation: false,
            transport: signalR.HttpTransportType.WebSockets,
            logger: signalR.LogLevel.Warning
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 20000]) // Retry with backoff
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Connection lifecycle event handlers
    connection.onreconnecting(error => {
        console.warn("SignalR reconnecting due to error:", error);
        const progressStatus = document.getElementById("operationStatus");
        if (progressStatus) {
            progressStatus.textContent = "Reconnecting to server...";
        }
    });
    
    connection.onreconnected(connectionId => {
        console.log("SignalR reconnected with ID:", connectionId);
        const progressStatus = document.getElementById("operationStatus");
        if (progressStatus) {
            progressStatus.textContent = "Reconnected. Continuing operation...";
        }
        
        // Re-register for job updates if we have a jobId
        const jobIdElement = document.getElementById("currentJobId");
        if (jobIdElement && jobIdElement.value) {
            connection.invoke("RegisterForJobUpdates", jobIdElement.value)
                .catch(err => console.error("Error registering for job updates after reconnect:", err));
        }
    });
    
    connection.onclose(error => {
        console.error("SignalR connection closed", error);
        const progressStatus = document.getElementById("operationStatus");
        if (progressStatus) {
            progressStatus.textContent = "Connection lost. Please reload the page.";
        }
    });

    // Register handler for connectionStatus events - match server casing
    connection.on("connectionStatus", (data) => {
        console.log("Connection status update:", data);
        // This handler ensures the connectionStatus event from the server is properly received
    });

    connection.on("UpdateProgress", (progress) => {
        const progressBar = document.getElementById("importProgress");
        const progressStatus = document.getElementById("importStatus");
        
        if (progressBar && progressStatus) {
            progressBar.style.width = `${progress.percentComplete}%`;
            progressBar.setAttribute("aria-valuenow", progress.percentComplete);
            
            let statusText = `Processing: ${progress.currentWorkout}`;
            if (progress.currentExercise) {
                statusText += ` - ${progress.currentExercise} (${progress.processedReps}/${progress.totalReps} reps)`;
            }
            progressStatus.textContent = statusText;
        }
    });

    connection.on("ReceiveProgress", (progress) => {
        const progressBar = document.getElementById("operationProgress");
        const progressStatus = document.getElementById("operationStatus");
        
        if (progressBar && progressStatus) {
            progressBar.style.width = `${progress.percentComplete}%`;
            progressBar.setAttribute("aria-valuenow", progress.percentComplete);
            progressStatus.textContent = progress.currentOperation || progress.status;
        }
    });

    // Start the connection with better error handling
    function startConnection() {
        connection.start()
            .then(() => {
                console.log("SignalR connected successfully.");
                // If we have a jobId element, register for updates
                const jobIdElement = document.getElementById("currentJobId");
                if (jobIdElement && jobIdElement.value) {
                    connection.invoke("RegisterForJobUpdates", jobIdElement.value)
                        .catch(err => console.error("Error registering for job updates:", err));
                }
            })
            .catch(err => {
                console.error("Error starting SignalR connection:", err);
                // Retry connection after 5 seconds
                setTimeout(startConnection, 5000);
            });
    }
    
    // Start the connection
    startConnection();
    
    // Initialize mobile touch enhancements
    initMobileTouchEnhancements();
});

/**
 * Enhances touch interactions across the entire application for mobile devices
 * This addresses specific issues with menu buttons, accordions, and other tap-based interactions
 */
function initMobileTouchEnhancements() {
    // Fix header menu button (common issue reported)
    const menuButtons = document.querySelectorAll('.navbar-toggler');
    menuButtons.forEach(button => {
        if (button) {
            button.addEventListener('touchend', function(e) {
                // Stop event only if it's not part of a scroll/swipe action
                if (!button.dataset.swiping) {
                    e.preventDefault();
                    e.stopPropagation();
                    // Force click event to trigger Bootstrap's toggle
                    button.click();
                }
            });
            
            // Track if we're in a swipe operation
            button.addEventListener('touchstart', function() {
                button.dataset.swiping = 'false';
            }, { passive: true });
            
            button.addEventListener('touchmove', function() {
                button.dataset.swiping = 'true';
            }, { passive: true });
        }
    });

    // General approach for all Bootstrap components that might need help on mobile
    // This handles components that might be added dynamically after page load
    document.addEventListener('touchend', function(e) {
        // Find the nearest interactive element
        let target = e.target;
        let interactiveElement = null;
        
        // Check if the target or any of its parents is a button, a, or any clickable element
        while (target && target !== document && !interactiveElement) {
            if (target.tagName === 'BUTTON' || 
                target.tagName === 'A' || 
                target.tagName === 'INPUT' ||
                target.hasAttribute('data-bs-toggle') ||
                target.hasAttribute('data-toggle') ||
                target.classList.contains('list-group-item-action') ||
                target.classList.contains('accordion-button') ||
                target.classList.contains('nav-link')) {
                interactiveElement = target;
                break;
            }
            target = target.parentNode;
        }
        
        // If we found an interactive element and it's not part of a swipe gesture
        if (interactiveElement && !interactiveElement.dataset.swiping) {
            // Handle Bootstrap specific components
            if (interactiveElement.hasAttribute('data-bs-toggle') || 
                interactiveElement.hasAttribute('data-toggle')) {
                // Special handling for certain toggle types
                const toggleType = interactiveElement.getAttribute('data-bs-toggle') || 
                                   interactiveElement.getAttribute('data-toggle');
                
                if (toggleType === 'dropdown' || toggleType === 'collapse') {
                    // Let Bootstrap handle these interactions
                    // Just ensure the click event fires
                    if (!e.defaultPrevented) {
                        interactiveElement.click();
                    }
                }
            }
        }
    }, { passive: false });

    // Add improved touch scroll handling for modal dialogs
    const modals = document.querySelectorAll('.modal');
    modals.forEach(modal => {
        if (!modal) return;
        
        modal.addEventListener('touchmove', function(e) {
            // Allow scrolling inside modal bodies
            const modalBody = modal.querySelector('.modal-body');
            if (modalBody && modalBody.contains(e.target)) {
                e.stopPropagation();
            }
        }, { passive: true });
    });
}
