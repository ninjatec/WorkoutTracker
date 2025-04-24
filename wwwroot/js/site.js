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
});
