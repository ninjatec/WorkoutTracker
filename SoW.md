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


[x] Implement a feature set for coaches to have the ability to manage other users and plan workouts for them, including viewing all workout data and reports.
 - [x] Create coach role and permission system
   - [x] Add Coach role to identity system
   - [x] Create CoachPermission model to define granular access controls
   - [x] Implement CoachClientRelationship model to manage coach-client connections
   - [x] Add migrations for new coach-related database tables
   - [x] Create coach-specific authorization attributes and policies
   - [x] Implement user elevation workflow from regular user to coach status

 - [ ] Develop client management for coaches
   - [ ] Create Clients/Index.cshtml for coaches to view their client roster
   - [ ] Implement client invitation system with secure tokens
   - [ ] Add client connection request workflow for users seeking coaches
   - [ ] Create client grouping and categorization features
   - [ ] Develop client profile view with relevant fitness metrics
   - [ ] Implement active/inactive client status management

 - [ ] Build coach dashboard and analytics
   - [ ] Create coach-specific dashboard with client overview
   - [ ] Implement client progress snapshots and alerts
   - [ ] Add client comparison views for group analysis
   - [ ] Create scheduled check-in management system
   - [ ] Develop client milestone tracking and celebration features
   - [ ] Implement performance trend analysis across client base

 - [ ] Implement workout programming for clients
   - [ ] Create interface for coaches to develop client-specific workout templates
   - [ ] Add ability to assign templates to individual clients or groups
   - [ ] Implement workout scheduling with notification system
   - [ ] Create workout adjustment tools based on client feedback
   - [ ] Add automated progression rules configurable by coaches
   - [ ] Develop exercise substitution suggestions based on equipment/limitations

 - [ ] Create feedback and communication system
   - [ ] Implement coach comments on individual workouts/exercises
   - [ ] Add structured feedback forms for clients to complete
   - [ ] Create RPE (Rate of Perceived Exertion) tracking for client feedback
   - [ ] Develop direct messaging system between coach and client
   - [ ] Add file/image sharing for form checks and demonstrations
   - [ ] Implement video annotation tools for technique feedback

 - [ ] Extend reporting system for coaches
   - [ ] Create client-specific report views with coach annotations
   - [ ] Implement comparative reporting across multiple clients
   - [ ] Add aggregate statistics for coach effectiveness metrics
   - [ ] Create exportable client reports for offline analysis
   - [ ] Develop custom report builder for coach-specific metrics
   - [ ] Add report scheduling with automatic delivery options

 - [ ] Implement client-specific goal setting
   - [ ] Create goal management interface for coaches
   - [ ] Add progress tracking toward client-specific goals
   - [ ] Implement smart goal suggestions based on client history
   - [ ] Develop goal achievement notifications and celebrations
   - [ ] Add collaborative goal setting workflow between coach and client
   - [ ] Create goal categorization (strength, hypertrophy, endurance, etc.)

 - [ ] Create coaching business tools
   - [ ] Implement client session tracking and billing features
   - [ ] Add coaching package management with automated renewals
   - [ ] Develop client onboarding workflow with assessments
   - [ ] Add coaching credential management and display
 
[x] Add calculated total volume and estimate calories to Workout and Sets views
 - [x] Design and implement calorie calculation service
   - [x] Create CalorieCalculationService with configurable MET values for different exercise types
   - [x] Implement calculation algorithm based on exercise intensity, duration, and user weight
   - [x] Add support for different calculation methods (basic MET-based vs. heart rate-based when available)
   - [x] Develop caching mechanism for performance optimization
   - [x] Add unit tests for calorie calculation logic

 - [x] Enhance volume calculation implementation
   - [x] Create robust VolumeCalculationService for standardized volume metrics
   - [x] Implement different volume calculation methods (weight × reps, weight × reps × sets)
   - [x] Support specialized volume calculations for bodyweight exercises
   - [x] Add time-under-tension calculations for isometric exercises
   - [x] Implement relative volume calculations (volume per muscle group/exercise type)

 - [x] Update database models and services
   - [x] Extend Session model to include calculated TotalVolume and EstimatedCalories properties
   - [x] Add caching support for volume and calorie calculations
   - [x] Create migration for adding new fields if storing calculated values
   - [x] Update repository/service layer to support new calculations
   - [x] Implement automatic recalculation when related data changes

 - [x] Update Sessions UI
   - [x] Modify Sessions/Details.cshtml to display total volume and calories
   - [x] Add visual indicators (charts/gauges) for volume and calorie metrics
   - [x] Create collapsed/expandable detailed breakdown by exercise
   - [x] Add comparison to previous session values (with change percentage)
   - [x] Implement unit selection toggles (kg/lb for volume, kcal/kJ for energy)

 - [x] Enhance Sets UI
   - [x] Update Sets/Index.cshtml to include per-set volume metrics
   - [x] Add progressive color coding based on volume/intensity
   - [x] Implement set-specific calorie estimates
   - [x] Create cumulative volume tracking within workout
   - [x] Add visual indicators for volume progression across sets

 - [x] Extend reporting functionality
   - [x] Update Reports/Index.cshtml to include volume and calorie trends
   - [x] Add volume progression charts by exercise/muscle group
   - [x] Create calorie expenditure analysis by workout type
   - [x] Implement volume-to-results correlation analysis
   - [x] Add comparative metrics against population averages

 - [x] Update REST API endpoints
   - [x] Extend existing workout API to include volume and calorie data
   - [x] Add dedicated endpoints for volume/calorie metrics
   - [x] Implement filtering and aggregation parameters
   - [x] Create batch calculation endpoints for performance
   - [x] Add proper documentation for new API features

 - [x] Enhance mobile responsiveness
   - [x] Optimize volume and calorie displays for mobile devices
   - [x] Implement condensed views for small screens
   - [x] Create mobile-specific interactive visualizations
   - [x] Add quick-view indicators for key metrics
   - [x] Ensure touch-friendly controls for all new UI elements

 - [x] Update documentation
   - [x] Add volume and calorie calculation methodology to help pages
   - [x] Update README.md with new feature details
   - [x] Add calculation components to inventory.md
   - [x] Create user guide for interpreting volume/calorie metrics
   - [x] Document API changes and examples

[x] Make the reports page an accordian adding volume and calories reports
 - [x] Design and implement tabbed interface for reports page
   - [x] Create accordian structure in Reports/Index.cshtml
   - [x] Design responsive accordian layout for both desktop and mobile views
   - [x] Add accordian navigation with appropriate icons and labels

 - [x] Implement volume analysis reports tab
   - [x] Create VolumeAnalysisViewModel to aggregate volume metrics
   - [x] Develop volume calculation service for different exercise types
   - [x] Add volume trending chart showing progress over time
   - [x] Implement volume breakdown by muscle group/exercise type
   - [x] Create volume heatmap visualization for weekly patterns
   - [x] Add relative volume analysis (volume per body weight)
   - [x] Implement comparative volume metrics against previous periods

 - [x] Develop calorie expenditure reports tab
   - [x] Create CalorieAnalysisViewModel for energy expenditure metrics
   - [x] Implement calorie calculation service using MET values and user data
   - [x] Add calorie burn chart with trend analysis
   - [x] Create breakdown of calories by workout type/intensity
   - [x] Implement weekly/monthly calorie goal tracking
   - [x] Add calorie expenditure forecasting based on planned workouts
   - [x] Develop calorie visualization comparing different exercise types

 - [x] Enhance existing personal records
   - [x] Update personal records view to fit new tabbed interface
   - [x] Add filtering options for record categories
   - [x] Implement visual indicators for recent records
   - [x] Create record history tracking to show progression
   - [x] Add export functionality for personal records

 - [x] Create progress tracking accordian
   - [x] Develop integrated view of key performance indicators
   - [x] Implement customizable progress metrics selection
   - [x] Add milestone tracking and celebration notifications
   - [x] Create progress snapshot comparisons
   - [x] Implement goal setting and tracking visualization

 - [x] Enhance the user experience
   - [x] Add export to PDF option for all report tabs
   - [x] Add data tooltips and help information for metrics
   - [x] Create mobile-optimized views for each section
