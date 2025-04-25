# Statement of Works
## Workout templates / planning

## Update help pages


---

[ ] Update sessions pages to support sequence numbers
 - [ ] Add migration to formally add SequenceNum column to Set table with default value
   - [ ] Create new migration for adding SequenceNum column to the Set table
   - [ ] Set default value to 0 for existing records
   - [ ] Add appropriate index for performance optimization
   - [ ] Update database schema documentation

 - [ ] Enhance Session UI to support exercise ordering
   - [ ] Update Sets/Create.cshtml to include sequence number input
   - [ ] Update Sets/Edit.cshtml to allow changing sequence number
   - [ ] Add drag-and-drop reordering capability to Sessions/Details.cshtml
   - [ ] Add "move up/down" buttons for accessibility
   - [ ] Add batch reordering functionality

 - [ ] Update sorting logic in controllers and views
   - [ ] Modify Sessions/Details.cshtml.cs to use SequenceNum in sorting
   - [ ] Update Set controller logic to maintain sequence numbers when adding/deleting sets
   - [ ] Ensure proper sequence when duplicating sets
   - [ ] Add auto-increment logic for new sets in a session

 - [ ] Implement sequence number management
   - [ ] Create service to handle reordering and renumbering sets
   - [ ] Add API endpoints for AJAX reordering
   - [ ] Implement optimistic concurrency for sequence updates
   - [ ] Cache sequence data for better performance

 - [ ] Enhance reporting to respect sequence order
   - [ ] Update Reports and Calculator pages to display sets in sequence order
   - [ ] Ensure Shared workout views display exercises in correct sequence
   - [ ] Add sequence number column to workout exports

---
---

[ ] Implement workout data sharing functionality
 - [x] Create shared workout link system
   - [x] Design and implement ShareToken model with expiry and access controls
   - [x] Create API endpoints for generating and managing share tokens
   - [x] Implement secure token validation mechanism
 - [x] Develop read-only views for shared workout data
   - [x] Create dedicated controllers and views for shared workout history
   - [x] Implement read-only reports page for shared access
   - [x] Add 1RM calculator view for shared access
   - [x] Fix type conversion issues in shared controllers
 - [x] Add user management for shared access
   - [x] Create UI for users to manage shared access tokens
   - [x] Implement ability to revoke access at any time
   - [x] Add optional expiry date/time for temporary sharing
 - [ ] Build guest access functionality
   - [ ] Create anonymous access mechanism with proper security controls
   - [ ] Implement session tracking for guest users
   - [ ] Add option to convert guest access to registered user
 - [ ] Update documentation
   - [ ] Update README.md with sharing functionality details
   - [ ] Update inventory.md with new sharing components
   - [ ] Create user guide for sharing workout data

---
---

[ ] Allow Import from apple via HealthKit
 - [ ] Set up HealthKit integration prerequisites
   - [ ] Add HealthKit entitlements to the iOS app manifest
   - [ ] Configure proper privacy descriptions in Info.plist for HealthKit data access
   - [ ] Create HealthKit authorization manager to request user permissions
   - [ ] Implement secure credential storage for HealthKit authorization tokens
   - [ ] Add appropriate error handling for permission denials

 - [ ] Create HealthKit data extraction service
   - [ ] Develop HealthKitService to connect to Apple's HealthKit API
   - [ ] Implement query functionality for workout data (HKWorkout objects)
   - [ ] Add support for extracting workout details including duration, energy burned, and distance
   - [ ] Create mappers for HKWorkoutActivityType to system ExerciseTypes
   - [ ] Implement date range filtering for selective imports
   - [ ] Add workout metadata extraction for comprehensive imports

 - [ ] Develop workout sample extraction
   - [ ] Create query handlers for HKSample objects to extract detailed metrics
   - [ ] Add support for reading heart rate data during workouts
   - [ ] Implement extraction of sets and reps from workout segments (HKWorkoutEvent)
   - [ ] Add weight and resistance reading from relevant samples
   - [ ] Create workout route extraction for GPS-based workouts

 - [ ] Implement HealthKit data transformation layer
   - [ ] Create HealthKitImportData wrapper class similar to TrainAIImportData
   - [ ] Develop HealthKitWorkout model to map Apple's workout data structure
   - [ ] Implement intelligent exercise type mapping with Levenshtein distance
   - [ ] Add workout metadata normalization for system compatibility
   - [ ] Create set detection algorithm for strength training workouts
   - [ ] Develop rep counting logic from motion/accelerometer data

 - [ ] Extend BackgroundJobService for HealthKit imports
   - [ ] Add QueueHealthKitImport method to background job service
   - [ ] Create ProcessHealthKitImportAsync method for background processing
   - [ ] Implement progress tracking with existing JobProgress infrastructure
   - [ ] Add error handling with appropriate user feedback
   - [ ] Implement connection resilience for long-running imports
   - [ ] Add duplicate detection similar to existing import functionality

 - [ ] Develop iOS/macOS companion app for HealthKit access
   - [ ] Create a simple iOS/macOS app to authorize HealthKit access
   - [ ] Implement secure data export functionality to JSON format
   - [ ] Add OAuth-based authentication with main web application
   - [ ] Create secure file transfer for workout data
   - [ ] Implement webhook callback for import completion notification
   - [ ] Add progress status display during export/import process

 - [ ] Create HealthKit import user interface
   - [ ] Create new ImportHealthKit.cshtml Razor Page
   - [ ] Implement file upload with drag-and-drop support
   - [ ] Add instructions for exporting from Apple Health
   - [ ] Create authentication flow for direct HealthKit access
   - [ ] Implement real-time progress updates using existing SignalR hub
   - [ ] Add success/failure notifications with detailed feedback

 - [ ] Implement data validation and mapping
   - [ ] Create schema validation for HealthKit JSON format
   - [ ] Implement data sanitization for imported values
   - [ ] Add unit conversion between Apple units and system units
   - [ ] Create intensity mapping from heart rate zones to workout intensity
   - [ ] Develop confidence scoring for import accuracy
   - [ ] Add import summary with mapping results

 - [ ] Update documentation and testing
   - [ ] Update README.md with HealthKit import instructions
   - [ ] Add HealthKit components to inventory.md
   - [ ] Create unit tests for HealthKit data parsing
   - [ ] Implement integration tests for the import process
   - [ ] Add user guide section for HealthKit import workflow
   - [ ] Document iOS companion app setup process

[ ] Allow Export to apple via HealthKit
 - [ ] Set up HealthKit integration prerequisites
   - [ ] Configure HealthKit entitlements in the iOS app manifest
   - [ ] Add appropriate privacy descriptions to Info.plist for writing to HealthKit
   - [ ] Implement HealthKit availability checking mechanism 
   - [ ] Add permission request flow for write access to HealthKit
   - [ ] Create error handling for permission denials and unsupported devices

 - [ ] Design and implement data transformation service
   - [ ] Create HealthKitExportService to handle data conversion
   - [ ] Develop mapping between system ExerciseTypes and HKWorkoutActivityType
   - [ ] Implement workout metadata formatting (duration, calories, distance)
   - [ ] Add support for exporting heart rate data when available
   - [ ] Create unit conversion utilities for system-to-Apple metrics
   - [ ] Implement JSON schema validation for export data

 - [ ] Develop workout data export core functionality
   - [ ] Create HKWorkout object builder from system workout sessions
   - [ ] Implement HKWorkoutBuilder for creating complex workouts
   - [ ] Add support for workout events to represent sets and reps
   - [ ] Implement route data generation for outdoor activities 
   - [ ] Create proper metadata tagging for exported workouts
   - [ ] Add data validation to ensure HealthKit compatibility

 - [ ] Implement export background processing
   - [ ] Extend BackgroundJobService with QueueHealthKitExport method
   - [ ] Create ProcessHealthKitExportAsync for handling exports in background
   - [ ] Implement job progress tracking with existing infrastructure
   - [ ] Add resilient connection handling for exports
   - [ ] Create retry mechanisms for failed export attempts
   - [ ] Add conflict resolution for duplicate workout detection

 - [ ] Develop iOS/macOS export functionality
   - [ ] Expand companion app with export capabilities
   - [ ] Add HKWorkoutSession creation from exported data
   - [ ] Implement batch workout saving to HealthKit
   - [ ] Add proper attribution and source tagging
   - [ ] Create robust error handling for failed saves
   - [ ] Implement secure authentication between web app and companion app

 - [ ] Create export user interface
   - [ ] Add export option to existing Export.cshtml page
   - [ ] Create ExportToHealthKit.cshtml page for dedicated HealthKit export
   - [ ] Implement workout selection interface for partial exports
   - [ ] Add date range filtering for selective exports
   - [ ] Create real-time progress tracking using existing SignalR hub
   - [ ] Add success/failure notifications with detailed feedback

 - [ ] Add export configuration options
   - [ ] Create user preferences for default export behavior
   - [ ] Implement workout type mapping configuration
   - [ ] Add toggles for including different data types (heart rate, routes, etc.)
   - [ ] Create data conflict resolution strategy settings
   - [ ] Implement export format customization options
   - [ ] Add scheduled/automatic export configuration

 - [ ] Implement security and privacy measures
   - [ ] Add secure data transmission between web app and companion app
   - [ ] Implement proper OAuth authentication flow
   - [ ] Create audit logging for all HealthKit exports
   - [ ] Add user consent confirmations before export
   - [ ] Implement data minimization to export only necessary data
   - [ ] Create privacy policy updates for HealthKit integration

 - [ ] Update documentation and testing
   - [ ] Update README.md with HealthKit export instructions
   - [ ] Add export components to inventory.md
   - [ ] Create unit tests for data transformation
   - [ ] Implement integration tests for export process
   - [ ] Add user guide for HealthKit export workflow
   - [ ] Document iOS companion app export functionality

## Admin Metrics Dashboard
[x] Implement comprehensive admin metrics dashboard
 - [x] Create dedicated metrics visualization page
   - [x] Design and implement Admin/Pages/Metrics/Index.cshtml Razor Page
   - [x] Create sidebar navigation link in _AdminLayout.cshtml
   - [x] Implement responsive design with Bootstrap 5 grid system
   - [x] Add role-based authorization requiring Admin role
   - [x] Create tab-based interface for different metric categories

 - [x] Implement real-time system metrics display
   - [x] Create SystemMetricsViewComponent for server statistics
   - [x] Add CPU, memory, and disk usage monitoring panels
   - [x] Implement database connection pool visualization
   - [x] Create Redis cache hit/miss ratio display
   - [x] Add Hangfire queue length and processing rate panels
   - [x] Implement auto-refresh functionality with configurable intervals

 - [x] Build user activity metrics section
   - [x] Create UserActivityViewComponent for engagement statistics
   - [x] Implement daily/weekly/monthly active user charts
   - [x] Add user registration trend visualization
   - [x] Create login success/failure rate display
   - [x] Implement user retention analysis charts
   - [x] Add cohort analysis for user engagement patterns
   - [x] Create geographic distribution map of user logins

 - [x] Develop workout statistics visualization
   - [x] Create WorkoutMetricsViewComponent for usage statistics
   - [x] Implement charts for sessions, sets, and reps created over time
   - [x] Add exercise type popularity visualization
   - [x] Create workout duration and intensity trend analysis
   - [x] Implement completion rate statistics for workout sessions
   - [x] Add personal record achievement rate visualization
   - [x] Create time-of-day workout pattern analysis

 - [x] Implement performance metrics dashboard
   - [x] Create PerformanceMetricsViewComponent for system performance
   - [x] Add HTTP request duration histograms by endpoint
   - [x] Implement database query performance visualization
   - [x] Create API response time tracking with percentiles
   - [x] Add page load time visualization by route
   - [x] Implement resource utilization trend charts
   - [x] Create error rate visualization with drill-down capability

 - [x] Add health check status dashboard
   - [x] Create HealthCheckViewComponent for service health visualization
   - [x] Implement service dependency diagram with status indicators
   - [x] Add historical uptime tracking with SLA calculation
   - [x] Create circuit breaker state visualization
   - [x] Implement health check response time tracking
   - [x] Add service dependency failure impact analysis

 - [x] Build alerting and notification system
   - [x] Create AlertingService for threshold-based notifications
   - [x] Implement alert configuration interface with thresholds
   - [x] Add email notification for critical metric thresholds
   - [x] Create in-app notification system for warnings
   - [x] Implement alert history and acknowledgment tracking
   - [x] Add escalation policy configuration for unresolved alerts

 - [x] Implement export and reporting functionality
   - [x] Create PDF/CSV export for dashboard metrics
   - [x] Implement scheduled report generation and delivery
   - [x] Add custom date range selection for metric analysis
   - [x] Create report template configuration system
   - [x] Implement comparison view for different time periods
   - [x] Add annotation capability for correlation analysis

 - [x] Enhance metric collection system
   - [x] Expand WorkoutTrackerMetrics class with additional metrics
   - [x] Add custom dimensions to existing metrics for better analysis
   - [x] Implement client-side performance metric collection
   - [x] Create business-focused KPI metrics collection
   - [x] Add custom metric definition interface for administrators
   - [x] Implement metric aggregation for high-cardinality data points

 - [x] Documentation and testing
   - [x] Create admin guide for metrics dashboard usage
   - [x] Add metrics dashboard components to inventory.md
   - [x] Implement unit tests for metric calculation logic
   - [x] Create integration tests for dashboard components
   - [x] Document alert threshold recommendations
   - [x] Add performance impact analysis of metric collection

   ## Expand excercise types 
[x] Expand excercise types with integration to https://www.api-ninjas.com/api/exercises
 - [x] Set up API Ninjas integration
   - [x] Create ExerciseApiService to handle API communication
   - [x] Implement secure API key storage and configuration
   - [x] Add rate limiting support to prevent API quota exhaustion
   - [x] Implement HTTP client factory pattern for resilient API calls
   - [x] Add proper error handling and logging for API failures

 - [x] Develop exercise data models
   - [x] Create ExerciseApiResponse model matching API Ninjas schema
   - [x] Implement mapping between API data and ExerciseType model
   - [x] Add support for extended attributes (muscle groups, difficulty, instructions)
   - [x] Create ExerciseTypeExtended model for storing additional metadata
   - [x] Update database schema to accommodate new exercise attributes

 - [x] Create Admin UI for exercise management
   - [x] Add "Import from API" button to ExerciseTypes/Index page
   - [x] Create exercise search interface with filtering options (muscle group, difficulty, type)
   - [x] Implement paginated results for large API responses
   - [x] Add preview functionality before importing exercises
   - [x] Create bulk selection and import capabilities
   - [x] Implement duplicate detection and handling

 - [x] Enhance exercise type views with new metadata
   - [x] Update ExerciseTypes/Details.cshtml to show extended attributes
   - [x] Add muscle group visualization with SVG body map
   - [x] Create collapsible exercise instructions section
   - [x] Implement difficulty indicator with visual cues
   - [x] Add equipment requirements section to exercise details
   - [x] Create related/alternative exercises section

 - [x] Implement background processing for large imports
   - [x] Add Hangfire job for bulk exercise import
   - [x] Create progress tracking with SignalR notifications
   - [x] Implement cancellation support for ongoing imports
   - [x] Add retry mechanism for failed API requests
   - [x] Create import history and audit trail

 - [x] Build exercise data caching system
   - [x] Implement local database cache of API responses
   - [x] Add cache invalidation strategy with reasonable TTL
   - [x] Create background refresh job for popular exercises
   - [x] Implement cache warming on application startup
   - [x] Add manual cache refresh option in admin interface

 - [x] Enhance user workout experience with new data
   - [x] Update set creation flow to show exercise instructions
   - [x] Add muscle group highlighting when selecting exercises
   - [x] Implement exercise difficulty warning for beginners
   - [x] Create "similar exercises" recommendations
   - [x] Add exercise rotation suggestions to avoid overtraining

 - [x] Develop reporting and analytics enhancements
   - [x] Create muscle group balance analysis in Reports page
   - [x] Add exercise variety metrics to user dashboard
   - [x] Implement exercise difficulty progression tracking
   - [x] Create workout completeness score based on muscle groups
   - [x] Add intelligent workout suggestions based on history

 - [x] Add documentation and testing
   - [x] Update README.md with API integration details
   - [x] Add new components to inventory.md
   - [x] Create unit tests for API service and mapping logic
   - [x] Implement integration tests for import process
   - [x] Add user guide for exercise search and details features


## Admin Functions
[x] Log Level
- [x] Add the Ability to manage the log level and control from a link under the System Settings link from the admin dashboard
 - [x] Implement log level configuration system
   - [x] Create LogLevel configuration model/entity to store settings
   - [x] Add LoggingService to manage log level changes at runtime
   - [x] Implement configuration persistence in database
   - [x] Create interface for logging providers to adapt to level changes
   - [x] Add configuration reload mechanism for real-time changes

 - [x] Develop admin UI for log management
   - [x] Create LogLevel admin page accessible from System Settings
   - [x] Implement dropdown selection for global log level (Debug, Info, Warning, Error, Critical)
   - [x] Add category-specific log level controls for granular management


 - [x] Enhance logging infrastructure
   - [x] Implement structured logging with Serilog
   - [x] Add log enrichment with contextual information (user, request, etc.)
   - [x] Create log sinks for different output targets (file, console, database)

 - [x] Implement security and access controls
   - [x] Restrict log management to admin users only
   - [x] Add audit logging for log level changes

 - [x] Update documentation
   - [x] Document logging architecture in README.md
   - [x] Update inventory.md with new logging components

---
---

[x] Update the Import page DataPortability/Import to use background jobs worker hangfire
 - [x] Add background job support to standard JSON import functionality
   - [x] Refactor WorkoutDataPortabilityService to support async background processing
   - [x] Create job progress model similar to existing ImportTrainAI implementation
   - [x] Add support for SignalR progress updates in the WorkoutDataPortabilityService
   - [x] Implement proper error handling and job status reporting
   - [x] Update service to support both direct and background processing modes
 
 - [x] Enhance BackgroundJobService to handle JSON imports
   - [x] Add method to queue JSON imports as background jobs 
   - [x] Implement progress tracking for JSON imports
   - [x] Create job completion notification mechanism
   - [x] Add error handling and job failure recovery
   - [x] Ensure proper cleanup of resources after job completion

 - [x] Update Import page UI for background processing
   - [x] Add real-time progress tracking with SignalR
   - [x] Create progress bar and status indicators 
   - [x] Implement connection state display (connected/disconnected)
   - [x] Add fallback polling for job status when SignalR is unavailable
   - [x] Display detailed import progress information

 - [x] Integrate with ImportProgressHub for real-time updates
   - [x] Extend ImportProgressHub to support standard JSON imports
   - [x] Implement client-side SignalR connection in Import.cshtml
   - [x] Add code to register for job-specific updates
   - [x] Handle reconnection scenarios and progress synchronization
   - [x] Ensure proper job group management in the hub

 - [x] Update Import.cshtml.cs page model
   - [x] Refactor OnPostAsync to use background job processing
   - [x] Add JobId property to track background jobs
   - [x] Implement job status checking functionality 
   - [x] Add error message retrieval from failed jobs
   - [x] Provide initial progress update when job starts

 - [x] Implement job status persistence and monitoring
   - [x] Save job metadata for user reference
   - [x] Add job status polling endpoint for fallback scenarios
   - [x] Create job cancellation functionality
   - [x] Implement job cleanup for completed/failed imports
   - [x] Add job history display for users to track past imports

 - [x] Add testing and documentation
   - [x] Create unit tests for background job processing
   - [x] Test with large import files to verify performance
   - [x] Test error scenarios and recovery mechanisms
   - [x] Update inventory.md with new components
   - [x] Document the implementation in code comments

---
---

[ ] Implement Mobile Optimization Improvements
 - [ ] Enhance responsive design fundamentals
   - [ ] Conduct responsive design audit across all pages
   - [ ] Implement consistent viewport settings across application
   - [ ] Create mobile-first media query structure in CSS files
   - [ ] Add touch-friendly spacing for UI elements (min 44×44px touch targets)
   - [ ] Implement proper form field sizing for mobile input
   - [ ] Optimize table displays with responsive tables pattern
   - [ ] Create mobile-specific layouts for complex data pages

 - [ ] Improve mobile navigation experience
   - [ ] Redesign main navigation with collapsible mobile menu
   - [ ] Create bottom navigation bar for critical mobile actions
   - [ ] Implement swipe gestures for common actions (delete, edit)
   - [ ] Add pull-to-refresh functionality for data lists
   - [ ] Create breadcrumb alternative for mobile context awareness
   - [ ] Implement session navigation shortcuts
   - [ ] Add haptic feedback for interactive elements

 - [ ] Optimize performance for mobile networks
   - [ ] Implement image lazy loading across the application
   - [ ] Create mobile-optimized image sizes with srcset
   - [ ] Add progressive image loading for workout photos
   - [ ] Implement code splitting for JavaScript bundles
   - [ ] Create offline capabilities for critical functionality
   - [ ] Add data prefetching for common navigation paths
   - [ ] Implement service worker for asset caching

 - [ ] Enhance mobile data input efficiency
   - [ ] Create specialized mobile input patterns for workout tracking
   - [ ] Add quick-add functionality for common exercises
   - [ ] Implement voice input for workout logging
   - [ ] Create mobile-optimized date/time selectors
   - [ ] Add barcode/QR scanning for equipment identification
   - [ ] Implement smart defaults based on user history
   - [ ] Create mobile-friendly validation with instant feedback

 - [ ] Implement progressive web app capabilities
   - [ ] Create web app manifest for home screen installation
   - [ ] Add offline mode with IndexedDB data storage
   - [ ] Implement background sync for offline data submission
   - [ ] Create push notification support for workout reminders
   - [ ] Add splash screen and branded loading experience
   - [ ] Implement screen orientation handling
   - [ ] Create "back to app" notifications for multi-tasking

 - [ ] Enhance mobile accessibility
   - [ ] Ensure color contrast meets WCAG AA standard on mobile
   - [ ] Optimize screen reader flows for mobile navigation
   - [ ] Add voice commands for hands-free operation
   - [ ] Implement reduced motion option for animations
   - [ ] Create large text mode for better readability
   - [ ] Add keyboard compatibility for Bluetooth keyboards
   - [ ] Implement focus management for screen reader users

 - [ ] Develop mobile-specific features
   - [ ] Create quick workout mode optimized for gym use
   - [ ] Add timer functionality with vibration alerts
   - [ ] Implement rest interval countdown timer
   - [ ] Create one-handed operation mode
   - [ ] Add gym equipment proximity detection
   - [ ] Implement "gym mode" with optimized battery usage
   - [ ] Create simplified dashboard for mobile users

 - [ ] Implement device integration
   - [ ] Add device orientation for exercise form checking
   - [ ] Create heart rate monitor Bluetooth integration
   - [ ] Implement smart watch companion functionality
   - [ ] Add step counter integration for cardio tracking
   - [ ] Create camera integration for form recording
   - [ ] Implement GPS tracking for outdoor workouts
   - [ ] Add wearable device data synchronization

 - [ ] Optimize mobile testing infrastructure
   - [ ] Create device testing matrix for major mobile platforms
   - [ ] Implement mobile-specific automated UI tests
   - [ ] Add performance benchmarking for mobile devices
   - [ ] Create real device testing pipeline
   - [ ] Implement mobile-specific error tracking
   - [ ] Add mobile network condition simulation
   - [ ] Create testing documentation for mobile features

 - [ ] Update documentation and user guidance
   - [ ] Create mobile-specific help content
   - [ ] Add mobile screenshots to documentation
   - [ ] Implement contextual help for mobile interfaces
   - [ ] Create mobile user onboarding experience
   - [ ] Add mobile-specific tips and tricks section
   - [ ] Update README.md with mobile optimization details
   - [ ] Add mobile components to inventory.md