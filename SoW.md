# Statement of Works
## Workout templates / planning

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

[x] Implement a feature set for coaches to have the ability to manage other users and plan workouts for them, including viewing all workout data and reports.
 - [x] Create coach role and permission system
   - [x] Add Coach role to identity system
   - [x] Create CoachPermission model to define granular access controls
   - [x] Implement CoachClientRelationship model to manage coach-client connections
   - [x] Add migrations for new coach-related database tables
   - [x] Create coach-specific authorization attributes and policies
   - [x] Implement user elevation workflow from regular user to coach status

 - [x] Develop client management for coaches
   - [x] Create Clients/Index.cshtml for coaches to view their client roster
   - [x] Implement client invitation system with secure tokens
   - [x] Add client connection request workflow for users seeking coaches
   - [x] Create client grouping and categorization features
   - [x] Develop client profile view with relevant fitness metrics
   - [x] Implement active/inactive client status management

 - [x] Implement workout programming for clients
   - [x] Create interface for coaches to develop client-specific workout templates
     - [x] Design WorkoutTemplate, WorkoutTemplateExercise, and WorkoutTemplateSet models
     - [x] Create database migrations for template tables with appropriate relationships
     - [x] Develop Templates/Create.cshtml page for coaches to build new templates
     - [x] Add exercise selection with muscle group filtering
     - [x] Implement set parameters with default values (reps, weight, rest periods)
     - [x] Create sequence management for ordering exercises within templates
   - [x] Add ability to assign templates to individual clients or groups
     - [x] Create TemplateAssignment model to link templates with clients/groups
     - [x] Develop assignment interface in Templates/Assign.cshtml
     - [x] Implement bulk assignment to client groups
     - [x] Add email notifications for new template assignments
     - [x] Create client dashboard for viewing assigned templates
     - [x] Implement access controls based on coach-client relationship


   - [x] Implement workout scheduling
     - [x] Create WorkoutSchedule model to manage recurring or one-time sessions
     - [x] Develop calendar interface for scheduling client workouts
     - [x] Create recurring workout patterns (weekly, bi-weekly, monthly)

  - [ ] Implement Notification system - Useing Existing Email functionality
     - [ ] Add email notification system for upcoming workouts
     - [ ] Implement reminder customization (timing, frequency)
     - [ ] Create recurring workout patterns (weekly, bi-weekly, monthly)
  

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
 
---

[ ] Migrate Coaching Functionality to Razor Pages
 - [x] API Endpoints Migration
   - [x] Identify coaching-related API endpoints in WorkoutScheduleController
   - [x] Move GetAvailableTemplates endpoint to Razor Page handler
   - [x] Update client-side code to use new Razor Page endpoints
   - [x] Implement proper model binding for template filtering
   - [x] Migrate any remaining coaching API functionality to Razor Pages

 - [x] Client Management Refinement
   - [x] Standardize all client invitation processes to use Razor Pages
   - [x] Complete client group management functionality
   - [x] Ensure consistent error handling across client management workflows
   - [x] Standardize validation across all coaching client features

 - [x] Workout Template System Completion
   - [x] Refine template assignment workflow with Razor Pages
   - [x] Complete scheduling options for assigned workouts
   - [ ] Standardize template filtering and search functionality
   - [ ] Create unified template management experience

 - [ ] Dashboard and Analytics Enhancement
   - [ ] Implement proper data loading and caching for dashboard
   - [ ] Create analytics pages for tracking client progress
   - [ ] Implement data visualization components
   - [ ] Optimize dashboard performance for multi-pod deployment

 - [ ] Navigation and Layout Standardization
   - [ ] Consolidate coaching navigation structure
   - [ ] Ensure consistent layout across all coaching pages
   - [ ] Standardize URL patterns for coaching functionality
   - [ ] Implement responsive design for all coaching interfaces
   - [ ] Create consistent breadcrumb navigation

 - [ ] JavaScript Refactoring
   - [ ] Update coach.js to work with Razor Page handlers
   - [ ] Refactor AJAX calls to use Razor Page endpoints
   - [ ] Implement proper error handling for client-side functions
   - [ ] Optimize JavaScript performance for coaching functionality
   - [ ] Standardize client-side validation

 - [ ] Data Access Layer Standardization
   - [ ] Review CoachingService implementation for Razor Pages support
   - [ ] Implement consistent caching strategy for coaching data
   - [ ] Standardize data access patterns across coaching features
   - [ ] Optimize database queries for coaching functionality
   - [ ] Implement proper connection pooling for multi-pod deployment

 - [ ] Authentication and Authorization Refinement
   - [ ] Update CoachAuthorizeAttribute for Razor Pages
   - [ ] Implement consistent permission checking across pages
   - [ ] Ensure proper role-based access control throughout
   - [ ] Add audit logging for sensitive coaching operations
   - [ ] Implement proper security for coaching data access
---
add user name
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