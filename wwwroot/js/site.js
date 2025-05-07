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
    
    // Initialize Bootstrap components explicitly
    initBootstrapComponents();
});

/**
 * Explicitly initializes all Bootstrap 5 components that need JavaScript activation
 * This ensures all interactive elements work properly on all devices, especially mobile
 */
function initBootstrapComponents() {
    console.log("Initializing Bootstrap components for mobile compatibility");
    
    // Initialize all Bootstrap 5 tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
    
    // Initialize all Bootstrap 5 popovers
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
    
    // Fix for accordions in mobile view
    var accordionButtons = document.querySelectorAll('.accordion-button');
    accordionButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            // Ensure the click event works properly
            // This is especially important on mobile devices
            console.log("Accordion button clicked");
        });
        
        // Add a touch-specific handler that ensures proper functioning on mobile
        button.addEventListener('touchend', function(e) {
            // Prevent ghost clicks
            e.preventDefault();
            // Use Bootstrap's native collapse functionality by triggering a click
            setTimeout(() => {
                button.click();
            }, 10);
        }, { passive: false });
    });
    
    // Fix for dropdown menus in navbar on mobile
    var dropdownToggles = document.querySelectorAll('[data-bs-toggle="dropdown"]');
    dropdownToggles.forEach(toggle => {
        toggle.addEventListener('click', function(e) {
            console.log("Dropdown toggle clicked");
        });
        
        // Improve touch behavior for dropdowns
        toggle.addEventListener('touchend', function(e) {
            // Prevent the default touch behavior which can be inconsistent on mobile
            e.preventDefault();
            // Use Bootstrap's native dropdown functionality
            setTimeout(() => {
                toggle.click();
            }, 10);
        }, { passive: false });
    });
    
    // Fix for navbar toggler button on mobile
    var navbarTogglers = document.querySelectorAll('.navbar-toggler');
    navbarTogglers.forEach(toggler => {
        toggler.addEventListener('click', function(e) {
            console.log("Navbar toggler clicked");
        });
        
        // Ensure touchend works correctly on mobile for navbar toggle
        toggler.addEventListener('touchend', function(e) {
            // Prevent the default touch behavior which can be inconsistent on mobile
            e.preventDefault();
            // Use Bootstrap's native collapse functionality
            setTimeout(() => {
                toggler.click();
            }, 10);
        }, { passive: false });
    });
    
    // Fix for potential touch-to-click delay on mobile devices
    document.addEventListener('touchstart', function() {}, { passive: true });
    
    // MutationObserver to handle dynamically added Bootstrap components
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
                mutation.addedNodes.forEach(function(node) {
                    if (node.nodeType === 1) { // Element node
                        // Check for newly added accordions
                        const accordionButtons = node.querySelectorAll ? node.querySelectorAll('.accordion-button') : [];
                        accordionButtons.forEach(button => {
                            button.addEventListener('touchend', function(e) {
                                e.preventDefault();
                                setTimeout(() => {
                                    button.click();
                                }, 10);
                            }, { passive: false });
                        });
                        
                        // Check for newly added dropdowns
                        const dropdownToggles = node.querySelectorAll ? node.querySelectorAll('[data-bs-toggle="dropdown"]') : [];
                        dropdownToggles.forEach(toggle => {
                            toggle.addEventListener('touchend', function(e) {
                                e.preventDefault();
                                setTimeout(() => {
                                    toggle.click();
                                }, 10);
                            }, { passive: false });
                        });
                    }
                });
            }
        });
    });
    
    // Start observing for dynamic content changes
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
    
    console.log("Bootstrap components initialized");
}
