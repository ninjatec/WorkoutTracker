/**
 * Workout Timer Module for WorkoutTracker
 * Provides exercise and rest timer functionality with:
 * - Configurable time presets
 * - Visual countdown display
 * - Sound alerts
 * - Vibration alerts using the Vibration API
 * - Notification when timer completes
 * - Background tab support
 */

class WorkoutTimer {
    constructor(options = {}) {
        // DOM elements
        this.timerDisplay = null;
        this.timerControls = null;
        this.timerContainer = null;
        this.minutesDisplay = null;
        this.secondsDisplay = null;
        this.progressBar = null;
        
        // Timer state
        this.totalSeconds = 0;
        this.remainingSeconds = 0;
        this.interval = null;
        this.isRunning = false;
        this.isPaused = false;
        this.timerType = 'rest'; // 'rest' or 'exercise'
        
        // Default options
        this.options = {
            restPresets: [30, 60, 90, 120, 180, 300], // Rest presets in seconds
            exercisePresets: [30, 45, 60, 90, 120, 300], // Exercise presets in seconds
            autoStart: false,
            allowPause: true,
            showPresets: true,
            vibrate: true,
            sound: true,
            showNotification: true,
            containerSelector: '#timer-container',
            onTimerStart: null,
            onTimerPause: null,
            onTimerResume: null,
            onTimerStop: null,
            onTimerComplete: null,
            onTimerTick: null
        };
        
        // Override defaults with provided options
        Object.assign(this.options, options);
        
        // Initialize
        this._initializeTimerElements();
    }
    
    /**
     * Initializes timer elements from the DOM
     * @private
     */
    _initializeTimerElements() {
        // Select the timer elements from the DOM (now within the timer tab)
        this.minutesDisplay = document.querySelector('.timer-minutes');
        this.secondsDisplay = document.querySelector('.timer-seconds');
        this.progressBar = document.querySelector('.timer-progress-bar');
        this.startButton = document.querySelector('.timer-start');
        this.pauseButton = document.querySelector('.timer-pause');
        this.stopButton = document.querySelector('.timer-stop');
        this.vibrateToggle = document.getElementById('timerVibrateToggle');
        this.soundToggle = document.getElementById('timerSoundToggle');
        
        // Initialize timer display
        this._updateDisplay();
    }
    
    /**
     * Initializes the timer interface for the tab-based display
     * This method is called when the timer tab is shown
     */
    initializeTabInterface() {
        // Update button states based on current timer state
        this._updateButtonStates();
        
        // Update the display
        this._updateDisplay();
        
        // Update the progress bar
        this._updateProgressBar();
    }
    
    /**
     * Updates button states based on current timer state
     * @private
     */
    _updateButtonStates() {
        if (this.startButton && this.pauseButton && this.stopButton) {
            // Start button is disabled if timer is running or there's no time set
            this.startButton.disabled = this.isRunning || this.remainingSeconds <= 0;
            
            // Pause button is disabled if timer is not running
            this.pauseButton.disabled = !this.isRunning;
            
            // Update pause/resume button text based on state
            if (this.isPaused) {
                this.pauseButton.innerHTML = '<i class="bi bi-play-fill me-1"></i>Resume';
            } else {
                this.pauseButton.innerHTML = '<i class="bi bi-pause-fill me-1"></i>Pause';
            }
            
            // Stop button is disabled if timer is not running and not paused
            this.stopButton.disabled = !this.isRunning && !this.isPaused;
        }
    }
    
    /**
     * Updates the progress bar based on current timer state
     * @private
     */
    _updateProgressBar() {
        if (this.progressBar && this.totalSeconds > 0) {
            const percentage = (this.remainingSeconds / this.totalSeconds) * 100;
            this.progressBar.style.width = `${percentage}%`;
            
            // Change color based on how much time is left
            if (percentage <= 25) {
                this.progressBar.style.backgroundColor = '#dc3545'; // Danger/red
            } else if (percentage <= 50) {
                this.progressBar.style.backgroundColor = '#ffc107'; // Warning/yellow
            } else {
                this.progressBar.style.backgroundColor = '#0d6efd'; // Primary/blue
            }
        }
    }
    
    /**
     * Formats time in seconds to MM:SS format
     * @param {number} totalSeconds 
     * @returns {string} formatted time
     * @private
     */
    _formatTime(totalSeconds) {
        const minutes = Math.floor(totalSeconds / 60);
        const seconds = totalSeconds % 60;
        return `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
    }
    
    /**
     * Updates the timer display with the current time
     * @private
     */
    _updateDisplay() {
        if (this.minutesDisplay && this.secondsDisplay) {
            const minutes = Math.floor(this.remainingSeconds / 60);
            const seconds = this.remainingSeconds % 60;
            
            this.minutesDisplay.textContent = minutes.toString().padStart(2, '0');
            this.secondsDisplay.textContent = seconds.toString().padStart(2, '0');
            
            // Update document title if timer is running
            if (this.isRunning) {
                document.title = `(${this._formatTime(this.remainingSeconds)}) WorkoutTracker`;
            }
        }
    }
    
    /**
     * Creates a beep sound
     * @private
     */
    _playSound() {
        if (!this.options.sound || !this.soundToggle?.checked) return;
        
        try {
            // Create an oscillator for a beep sound
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = audioContext.createOscillator();
            const gainNode = audioContext.createGain();
            
            oscillator.connect(gainNode);
            gainNode.connect(audioContext.destination);
            
            // Set the type and frequency for the beep
            oscillator.type = 'sine';
            oscillator.frequency.value = 800;
            gainNode.gain.value = 0.1;
            
            oscillator.start();
            
            // Stop after a short duration
            setTimeout(() => {
                oscillator.stop();
                audioContext.close();
            }, 200);
        } catch (e) {
            console.warn('Error playing sound:', e);
        }
    }
    
    /**
     * Triggers vibration if available and enabled
     * @param {string} intensity - 'light', 'medium', or 'strong'
     * @private
     */
    _triggerVibration(intensity = 'light') {
        if (!this.options.vibrate || !this.vibrateToggle?.checked || !window.triggerHapticFeedback) return;
        
        window.triggerHapticFeedback(intensity);
    }
    
    /**
     * Shows a notification when the timer completes
     * @private
     */
    _showNotification() {
        if (!this.options.showNotification) return;
        
        // Check for notification permissions
        if ('Notification' in window) {
            if (Notification.permission === 'granted') {
                const title = this.timerType === 'rest' ? 'Rest Complete!' : 'Exercise Complete!';
                const message = this.timerType === 'rest' ? 'Time to start exercising again.' : 'Time for a rest.';
                const notification = new Notification(title, {
                    body: message,
                    icon: '/favicon.ico'
                });
                
                // Close the notification after 5 seconds
                setTimeout(() => notification.close(), 5000);
            } 
            else if (Notification.permission !== 'denied') {
                // Request permission
                Notification.requestPermission();
            }
        }
    }
    
    /**
     * Tick function that runs every second
     * @private
     */
    _tick() {
        if (this.remainingSeconds > 0) {
            this.remainingSeconds--;
            this._updateDisplay();
            this._updateProgressBar();
            
            // Play sounds at certain intervals
            if (this.remainingSeconds <= 3 && this.remainingSeconds > 0) {
                this._playSound();
                this._triggerVibration('medium');
            }
            
            // Callback
            if (typeof this.options.onTimerTick === 'function') {
                this.options.onTimerTick(this.remainingSeconds, this.totalSeconds);
            }
        } else {
            // Timer completed
            this.stop();
            
            // Play completion sound and vibration
            this._playSound();
            this._triggerVibration('alert');
            
            // Show notification
            this._showNotification();
            
            // Reset document title
            document.title = 'WorkoutTracker';
            
            // Callback
            if (typeof this.options.onTimerComplete === 'function') {
                this.options.onTimerComplete();
            }
        }
    }
    
    /**
     * Sets the timer type (rest or exercise)
     * @param {string} type - 'rest' or 'exercise'
     * @returns {WorkoutTimer} - The timer instance
     */
    setTimerType(type) {
        if (type !== 'rest' && type !== 'exercise') return this;
        
        this.timerType = type;
        
        return this;
    }
    
    /**
     * Sets the timer for a specific duration
     * @param {number} seconds - Duration in seconds
     * @returns {WorkoutTimer} - The timer instance
     */
    setTime(seconds) {
        if (!seconds || seconds <= 0) return this;
        
        this.totalSeconds = seconds;
        this.remainingSeconds = seconds;
        
        this._updateDisplay();
        this._updateProgressBar();
        this._updateButtonStates();
        
        return this;
    }
    
    /**
     * Starts the timer
     * @returns {WorkoutTimer} - The timer instance
     */
    start() {
        if (this.isRunning || this.remainingSeconds <= 0) return this;
        
        // Clear any existing interval
        if (this.interval) {
            clearInterval(this.interval);
        }
        
        // Start the timer
        this.isRunning = true;
        this.isPaused = false;
        this.interval = setInterval(() => this._tick(), 1000);
        
        // Update button states
        this._updateButtonStates();
        
        // Play sound and vibration
        this._playSound();
        this._triggerVibration('light');
        
        // Callback
        if (typeof this.options.onTimerStart === 'function') {
            this.options.onTimerStart(this.totalSeconds);
        }
        
        return this;
    }
    
    /**
     * Pauses the timer
     * @returns {WorkoutTimer} - The timer instance
     */
    pause() {
        if (!this.isRunning || this.isPaused) return this;
        
        clearInterval(this.interval);
        this.isPaused = true;
        
        // Update button states
        this._updateButtonStates();
        
        // Play sound and vibration
        this._playSound();
        this._triggerVibration('light');
        
        // Reset document title
        document.title = 'WorkoutTracker';
        
        // Callback
        if (typeof this.options.onTimerPause === 'function') {
            this.options.onTimerPause(this.remainingSeconds, this.totalSeconds);
        }
        
        return this;
    }
    
    /**
     * Resumes the timer from a paused state
     * @returns {WorkoutTimer} - The timer instance
     */
    resume() {
        if (!this.isPaused) return this;
        
        this.interval = setInterval(() => this._tick(), 1000);
        this.isPaused = false;
        
        // Update button states
        this._updateButtonStates();
        
        // Play sound and vibration
        this._playSound();
        this._triggerVibration('light');
        
        // Callback
        if (typeof this.options.onTimerResume === 'function') {
            this.options.onTimerResume(this.remainingSeconds, this.totalSeconds);
        }
        
        return this;
    }
    
    /**
     * Stops the timer
     * @returns {WorkoutTimer} - The timer instance
     */
    stop() {
        if (!this.isRunning && !this.isPaused) return this;
        
        clearInterval(this.interval);
        this.isRunning = false;
        this.isPaused = false;
        
        // Reset to initial values
        this.remainingSeconds = this.totalSeconds;
        
        // Update display
        this._updateDisplay();
        this._updateProgressBar();
        
        // Update button states
        this._updateButtonStates();
        
        // Reset document title
        document.title = 'WorkoutTracker';
        
        // Play sound and vibration
        this._playSound();
        this._triggerVibration('light');
        
        // Callback
        if (typeof this.options.onTimerStop === 'function') {
            this.options.onTimerStop(this.remainingSeconds, this.totalSeconds);
        }
        
        return this;
    }
}

// Initialize timer on DOMContentLoaded
document.addEventListener('DOMContentLoaded', function() {
    // Create a global workoutTimer instance
    window.workoutTimer = new WorkoutTimer();
});