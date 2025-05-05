/**
 * SignalRConnectionManager
 * 
 * Handles SignalR connection management with enhanced resilience features:
 * - Automatic reconnection with exponential backoff
 * - Connection state management and event handling
 * - Message batching for high-frequency updates
 * - Mobile connection optimizations
 * - Connection ID persistence for reconnecting to job groups
 */
class SignalRConnectionManager {
    /**
     * Creates a new SignalRConnectionManager
     * @param {string} hubUrl - The URL of the SignalR hub
     * @param {Object} options - Configuration options
     */
    constructor(hubUrl, options = {}) {
        this.hubUrl = hubUrl;
        this.connection = null;
        this.connectionId = null;
        this.previousConnectionId = null;
        this.jobId = null;
        this.isConnecting = false;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = options.maxReconnectAttempts || 10;
        this.baseReconnectDelay = options.baseReconnectDelay || 1000; // 1 second
        this.maxReconnectDelay = options.maxReconnectDelay || 30000; // 30 seconds
        this.eventHandlers = {
            connected: [],
            disconnected: [],
            reconnecting: [],
            reconnected: [],
            progress: [],
            connectionError: []
        };
        this.pollInterval = null;
        this.pollIntervalMs = options.pollIntervalMs || 2000;
        this.connectionOptions = {
            isMobileClient: this._isMobileDevice(),
            enableReducedHeartbeat: this._isMobileDevice(),
            isMeteredConnection: options.isMeteredConnection || false,
            enableDataSavingMode: options.enableDataSavingMode || false
        };
        this.lastProgressUpdate = null;
        this.lastProgressTimestamp = 0;
        this.uiUpdateMinimumIntervalMs = 100; // Don't update UI more than 10 times per second
        this.autoReconnect = options.autoReconnect !== undefined ? options.autoReconnect : true;

        // Cache connection ID in sessionStorage for reconnection
        this._restorePreviousConnection();

        // Setup message processing
        this.messageQueue = [];
        this.isProcessingQueue = false;
    }

    /**
     * Check if current device is mobile
     * @returns {boolean} True if mobile device detected
     */
    _isMobileDevice() {
        return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
    }

    /**
     * Restore previous connection info from storage
     */
    _restorePreviousConnection() {
        try {
            this.previousConnectionId = sessionStorage.getItem('signalr-connection-id');
            this.jobId = sessionStorage.getItem('signalr-job-id');
            
            if (this.previousConnectionId) {
                console.log(`Restored previous connection ID: ${this.previousConnectionId}`);
            }
        } catch (e) {
            console.warn("Error accessing sessionStorage:", e);
        }
    }

    /**
     * Save current connection info to storage
     */
    _saveConnectionInfo() {
        try {
            if (this.connectionId) {
                sessionStorage.setItem('signalr-connection-id', this.connectionId);
            }
            if (this.jobId) {
                sessionStorage.setItem('signalr-job-id', this.jobId);
            }
        } catch (e) {
            console.warn("Error accessing sessionStorage:", e);
        }
    }

    /**
     * Start the connection
     * @returns {Promise} Promise that resolves when connected
     */
    async start() {
        if (this.isConnected || this.isConnecting) {
            return;
        }

        this.isConnecting = true;

        try {
            // Create the connection
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(this.hubUrl)
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: retryContext => {
                        // Exponential backoff with jitter
                        if (retryContext.previousRetryCount > this.maxReconnectAttempts) {
                            return null; // Stop trying after max attempts
                        }

                        // Calculate delay with exponential backoff
                        const baseDelay = Math.min(
                            this.maxReconnectDelay,
                            this.baseReconnectDelay * Math.pow(2, retryContext.previousRetryCount)
                        );
                        
                        // Add jitter to prevent all clients reconnecting at once
                        const jitter = Math.random() * 0.3 * baseDelay;
                        return baseDelay + jitter;
                    }
                })
                .build();

            // Set up event handling before starting the connection
            this._setupEvents();
                
            // Start the connection
            await this.connection.start();
            this.isConnected = true;
            this.isConnecting = false;
            this.reconnectAttempts = 0;
            this.connectionId = this.connection.connectionId;
            
            console.log(`Connected to SignalR hub. Connection ID: ${this.connectionId}`);
            this._saveConnectionInfo();
            
            // Send connection options to the server
            await this.connection.invoke("SetConnectionOptions", this.connectionOptions);
            console.log("Sent connection options to server");
            
            // If we have a previous job ID, attempt to reconnect to it
            if (this.jobId && this.previousConnectionId) {
                try {
                    await this.connection.invoke("ReconnectToJobGroup", this.previousConnectionId, this.jobId);
                    console.log(`Reconnected to job ${this.jobId}`);
                } catch (err) {
                    console.warn(`Failed to reconnect to job ${this.jobId}:`, err);
                }
            }
            
            // Start mobile ping if needed
            if (this.connectionOptions.isMobileClient) {
                this._startMobilePing();
            }
            
            // Trigger connected event
            this._triggerEvent('connected', { connectionId: this.connectionId });
        } catch (err) {
            this.isConnecting = false;
            console.error("Error starting SignalR connection:", err);
            this._triggerEvent('connectionError', { error: err });

            // Start polling fallback if connection failed
            if (!this.pollInterval) {
                this._startPolling();
            }
            
            throw err;
        }
    }

    /**
     * Set up event handlers for the connection
     */
    _setupEvents() {
        if (!this.connection) return;

        // Handle reconnecting event
        this.connection.onreconnecting(error => {
            this.isConnected = false;
            this.isConnecting = true;
            this.reconnectAttempts++;
            console.log(`SignalR reconnecting (Attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts})`);
            this._triggerEvent('reconnecting', { 
                error, 
                attempt: this.reconnectAttempts,
                maxAttempts: this.maxReconnectAttempts 
            });
            
            // Start polling fallback if we're trying to reconnect
            if (!this.pollInterval) {
                this._startPolling();
            }
        });

        // Handle reconnected event
        this.connection.onreconnected(connectionId => {
            this.isConnected = true;
            this.isConnecting = false;
            this.connectionId = connectionId;
            console.log(`SignalR reconnected. New connection ID: ${connectionId}`);
            this._saveConnectionInfo();
            
            // Try to reconnect to job if we have one
            if (this.jobId && this.previousConnectionId) {
                this.connection.invoke("ReconnectToJobGroup", this.previousConnectionId, this.jobId)
                    .catch(err => console.warn("Error reconnecting to job group:", err));
            }
            
            // Stop polling fallback as we're reconnected
            this._stopPolling();
            
            this._triggerEvent('reconnected', { connectionId });
        });

        // Handle disconnected event
        this.connection.onclose(error => {
            this.isConnected = false;
            this.isConnecting = false;
            console.log("SignalR connection closed");
            
            if (error) {
                console.error("SignalR disconnection error:", error);
            }
            
            // Store previous connection ID for potential reconnection
            if (this.connectionId) {
                this.previousConnectionId = this.connectionId;
                this._saveConnectionInfo();
            }
            
            this._triggerEvent('disconnected', { error });
            
            // Auto reconnect if enabled
            if (this.autoReconnect && this.reconnectAttempts < this.maxReconnectAttempts) {
                setTimeout(() => {
                    console.log("Attempting manual reconnection...");
                    this.start().catch(err => {
                        console.warn("Manual reconnection failed:", err);
                    });
                }, this._getNextRetryDelay());
            } else if (!this.pollInterval) {
                // Start polling fallback if auto-reconnect is disabled or failed
                this._startPolling();
            }
        });
        
        // Handle progress updates from the server
        this.connection.on("receiveProgress", progress => {
            this._handleProgressUpdate(progress);
        });
        
        // Handle connection status responses
        this.connection.on("ConnectionStatus", status => {
            console.log("Connection status:", status);
        });
        
        // Handle job registration responses
        this.connection.on("JobRegistrationStatus", status => {
            console.log("Job registration status:", status);
        });
    }
    
    /**
     * Handle incoming progress updates with batching support
     * @param {Object} progress - The progress update
     */
    _handleProgressUpdate(progress) {
        if (!progress) return;
        
        // Store the update
        this.lastProgressUpdate = progress;
        const now = Date.now();
        
        // Check if we should throttle UI updates
        const timeSinceLastUpdate = now - this.lastProgressTimestamp;
        let shouldUpdateUI = true;
        
        // Apply throttling rules if we have batch info
        if (progress.batchInfo) {
            if (progress.batchInfo.clientConfig && 
                progress.batchInfo.clientConfig.minimumUiUpdateIntervalMs) {
                // Respect server's requested update interval
                const minInterval = progress.batchInfo.clientConfig.minimumUiUpdateIntervalMs;
                shouldUpdateUI = timeSinceLastUpdate >= minInterval;
            }
        }
        
        // Queue the message for processing
        this.messageQueue.push(progress);
        
        // Process the queue if we're not already processing and should update UI
        if (!this.isProcessingQueue && shouldUpdateUI) {
            this._processMessageQueue();
        }
    }
    
    /**
     * Process the queue of progress updates
     */
    async _processMessageQueue() {
        if (this.isProcessingQueue || this.messageQueue.length === 0) return;
        
        this.isProcessingQueue = true;
        
        try {
            // Take the newest message from the queue
            const latestMessage = this.messageQueue.pop();
            
            // Clear the queue since we're only processing the latest update
            this.messageQueue = [];
            
            // Update timestamp and trigger event
            this.lastProgressTimestamp = Date.now();
            this._triggerEvent('progress', latestMessage);
        } finally {
            this.isProcessingQueue = false;
        }
    }
    
    /**
     * Calculate retry delay with exponential backoff
     * @returns {number} Delay in milliseconds
     */
    _getNextRetryDelay() {
        const baseDelay = Math.min(
            this.maxReconnectDelay,
            this.baseReconnectDelay * Math.pow(2, this.reconnectAttempts)
        );
        
        // Add jitter to prevent all clients reconnecting at once
        const jitter = Math.random() * 0.3 * baseDelay;
        return baseDelay + jitter;
    }
    
    /**
     * Start ping mechanism for mobile clients to keep connection alive
     */
    _startMobilePing() {
        // Clear any existing timer
        if (this._pingTimer) {
            clearInterval(this._pingTimer);
        }
        
        // Send ping every 20 seconds to keep the connection alive
        // This is especially important for mobile devices that aggressively
        // close inactive connections to save battery
        this._pingTimer = setInterval(() => {
            if (this.isConnected && this.connection) {
                this.connection.invoke("Ping").catch(err => {
                    console.warn("Error sending ping:", err);
                });
            }
        }, 20000); // 20 seconds
    }
    
    /**
     * Stop the ping mechanism
     */
    _stopMobilePing() {
        if (this._pingTimer) {
            clearInterval(this._pingTimer);
            this._pingTimer = null;
        }
    }

    /**
     * Stop the SignalR connection
     */
    async stop() {
        this._stopPolling();
        this._stopMobilePing();
        
        if (this.connection) {
            try {
                await this.connection.stop();
                this.isConnected = false;
                this.isConnecting = false;
                console.log("SignalR connection stopped");
            } catch (err) {
                console.error("Error stopping SignalR connection:", err);
            }
        }
    }
    
    /**
     * Register for updates for a specific job
     * @param {string} jobId - The ID of the job to register for
     */
    async registerForJob(jobId) {
        if (!jobId) {
            console.error("Cannot register for job updates: No job ID provided");
            return false;
        }
        
        this.jobId = jobId;
        this._saveConnectionInfo();
        
        if (this.isConnected && this.connection) {
            try {
                await this.connection.invoke("RegisterForJobUpdates", jobId);
                console.log(`Registered for updates for job ${jobId}`);
                return true;
            } catch (err) {
                console.error(`Error registering for job ${jobId} updates:`, err);
                return false;
            }
        } else {
            console.warn("Cannot register for job: Not connected to SignalR hub");
            // Try to connect first
            try {
                await this.start();
                return await this.registerForJob(jobId);
            } catch (err) {
                console.error("Failed to connect and register for job:", err);
                return false;
            }
        }
    }
    
    /**
     * Start polling as a fallback when SignalR is not available
     * This provides a degraded but functional experience when WebSockets are blocked
     */
    _startPolling() {
        if (this.pollInterval) return;
        
        console.log("Starting polling fallback");
        
        this.pollInterval = setInterval(() => {
            if (!this.jobId) return;
            
            // Fetch job status using regular AJAX
            fetch(`/api/jobs/status/${this.jobId}`)
                .then(response => {
                    if (!response.ok) {
                        throw new Error(`HTTP error ${response.status}`);
                    }
                    return response.json();
                })
                .then(progress => {
                    if (progress) {
                        this._handleProgressUpdate(progress);
                    }
                })
                .catch(err => {
                    console.warn("Error polling job status:", err);
                });
        }, this.pollIntervalMs);
    }
    
    /**
     * Stop the polling fallback
     */
    _stopPolling() {
        if (this.pollInterval) {
            clearInterval(this.pollInterval);
            this.pollInterval = null;
        }
    }

    /**
     * Add an event handler
     * @param {string} eventName - Name of the event to listen for
     * @param {Function} handler - Function to call when event occurs
     */
    on(eventName, handler) {
        if (!this.eventHandlers[eventName]) {
            this.eventHandlers[eventName] = [];
        }
        this.eventHandlers[eventName].push(handler);
        return this;
    }

    /**
     * Remove an event handler
     * @param {string} eventName - Name of the event
     * @param {Function} handler - Handler to remove
     */
    off(eventName, handler) {
        if (!this.eventHandlers[eventName]) return this;
        
        this.eventHandlers[eventName] = this.eventHandlers[eventName].filter(h => h !== handler);
        return this;
    }

    /**
     * Trigger an event
     * @param {string} eventName - Name of the event to trigger
     * @param {Object} data - Data to pass to handlers
     */
    _triggerEvent(eventName, data) {
        if (!this.eventHandlers[eventName]) return;
        
        for (const handler of this.eventHandlers[eventName]) {
            try {
                handler(data);
            } catch (err) {
                console.error(`Error in ${eventName} handler:`, err);
            }
        }
    }

    /**
     * Check if currently connected to SignalR hub
     * @returns {boolean} True if connected
     */
    isConnectionActive() {
        return this.isConnected && !!this.connection;
    }
    
    /**
     * Get the latest progress update
     * @returns {Object} The latest progress update or null
     */
    getLatestProgress() {
        return this.lastProgressUpdate;
    }
}