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


[ ] Implement a Workout Template function for workout plans
 - [ ] Create database models for workout templates
   - [ ] Design WorkoutTemplate model with name, description, and metadata fields
   - [ ] Create WorkoutTemplateExercise model for template exercise entries
   - [ ] Implement WorkoutTemplateSet model with default values for reps, weight, etc.
   - [ ] Add appropriate relationships between template models and existing models
   - [ ] Create migration for adding template tables to database
   - [ ] Add indexes for performance optimization

 - [ ] Build template management backend services
   - [ ] Create WorkoutTemplateService for CRUD operations
   - [ ] Implement template copying and duplication functionality
   - [ ] Add version control for templates with history tracking
   - [ ] Develop template sharing capability between users
   - [ ] Create template categorization and tagging system
   - [ ] Add template search and filtering functionality

 - [ ] Develop template user interface
   - [ ] Create Templates/Index.cshtml for browsing template library
   - [ ] Implement Templates/Create.cshtml for creating new templates
   - [ ] Build Templates/Edit.cshtml for modifying existing templates
   - [ ] Add Templates/Details.cshtml for viewing template details
   - [ ] Create UI components for template exercise/set management
   - [ ] Implement drag-and-drop exercise reordering in templates

 - [ ] Implement workout planning from templates
   - [ ] Add "Create Workout from Template" functionality to Sessions pages
   - [ ] Create weekly/monthly workout planning calendar view
   - [ ] Implement recurring workout scheduling from templates
   - [ ] Add template-based workout plan generation
   - [ ] Develop adaptive templates based on user progress
   - [ ] Create "Quick Start" functionality from favorite templates

 - [ ] Add template progression and periodization features
   - [ ] Implement progressive overload calculations for templates
   - [ ] Add periodization models (linear, undulating, block)
   - [ ] Create auto-adjustment of template variables based on performance
   - [ ] Implement deload week scheduling in template plans
   - [ ] Add template variation management for exercise rotation
   - [ ] Develop goal-based template progressions

 - [ ] Create template sharing and discovery
   - [ ] Build public template library with ratings and reviews
   - [ ] Implement template import/export functionality
   - [ ] Add template recommendations based on user goals
   - [ ] Create featured/trending templates section
   - [ ] Implement template access controls (public/private/shared)
   - [ ] Add template attribution and proper crediting for shared templates

 - [ ] Integrate templates with existing features
   - [ ] Connect templates with reporting for progress tracking
   - [ ] Update export functionality to include templates
   - [ ] Integrate with sharing functionality for collaborative planning
   - [ ] Update mobile views for template management
   - [ ] Ensure compatibility with existing workout tracking flow
   - [ ] Add template support to API endpoints

 - [ ] Implement template analytics and insights
   - [ ] Create performance metrics for template effectiveness
   - [ ] Add completion rate tracking for template workouts
   - [ ] Implement template comparison functionality
   - [ ] Develop template optimization suggestions
   - [ ] Create personalized template adaptation based on user data
   - [ ] Add visual progress tracking for template-based plans

 - [ ] Update documentation and testing
   - [ ] Update README.md with template functionality details
   - [ ] Add template components to inventory.md
   - [ ] Create comprehensive user guide for workout templates
   - [ ] Create sample templates for common workout programs

[ ] Implement a feature set for coaches to have the ability to manage other users and plan workouts for them, including viewing all workout data and reports.
 - [ ] Create coach role and permission system
   - [ ] Add Coach role to identity system
   - [ ] Create CoachPermission model to define granular access controls
   - [ ] Implement CoachClientRelationship model to manage coach-client connections
   - [ ] Add migrations for new coach-related database tables
   - [ ] Create coach-specific authorization attributes and policies
   - [ ] Implement user elevation workflow from regular user to coach status

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
  

[ ] Add calculated total volume and estimate calories to Workout and Sets views
 - [ ] Design and implement calorie calculation service
   - [ ] Create CalorieCalculationService with configurable MET values for different exercise types
   - [ ] Implement calculation algorithm based on exercise intensity, duration, and user weight
   - [ ] Add support for different calculation methods (basic MET-based vs. heart rate-based when available)
   - [ ] Develop caching mechanism for performance optimization
   - [ ] Add unit tests for calorie calculation logic

 - [ ] Enhance volume calculation implementation
   - [ ] Create robust VolumeCalculationService for standardized volume metrics
   - [ ] Implement different volume calculation methods (weight × reps, weight × reps × sets)
   - [ ] Support specialized volume calculations for bodyweight exercises
   - [ ] Add time-under-tension calculations for isometric exercises
   - [ ] Implement relative volume calculations (volume per muscle group/exercise type)

 - [ ] Update database models and services
   - [ ] Extend Session model to include calculated TotalVolume and EstimatedCalories properties
   - [ ] Add caching support for volume and calorie calculations
   - [ ] Create migration for adding new fields if storing calculated values
   - [ ] Update repository/service layer to support new calculations
   - [ ] Implement automatic recalculation when related data changes

 - [ ] Update Sessions UI
   - [ ] Modify Sessions/Details.cshtml to display total volume and calories
   - [ ] Add visual indicators (charts/gauges) for volume and calorie metrics
   - [ ] Create collapsed/expandable detailed breakdown by exercise
   - [ ] Add comparison to previous session values (with change percentage)
   - [ ] Implement unit selection toggles (kg/lb for volume, kcal/kJ for energy)

 - [ ] Enhance Sets UI
   - [ ] Update Sets/Index.cshtml to include per-set volume metrics
   - [ ] Add progressive color coding based on volume/intensity
   - [ ] Implement set-specific calorie estimates
   - [ ] Create cumulative volume tracking within workout
   - [ ] Add visual indicators for volume progression across sets

 - [ ] Implement volume and calorie goals
   - [ ] Create user-configurable volume and calorie targets
   - [ ] Add progress visualization toward daily/weekly goals
   - [ ] Implement smart goal suggestions based on historical data
   - [ ] Add goal achievement notifications and celebrations
   - [ ] Create export options for goal tracking data

 - [ ] Extend reporting functionality
   - [ ] Update Reports/Index.cshtml to include volume and calorie trends
   - [ ] Add volume progression charts by exercise/muscle group
   - [ ] Create calorie expenditure analysis by workout type
   - [ ] Implement volume-to-results correlation analysis
   - [ ] Add comparative metrics against population averages

 - [ ] Update REST API endpoints
   - [ ] Extend existing workout API to include volume and calorie data
   - [ ] Add dedicated endpoints for volume/calorie metrics
   - [ ] Implement filtering and aggregation parameters
   - [ ] Create batch calculation endpoints for performance
   - [ ] Add proper documentation for new API features

 - [ ] Enhance mobile responsiveness
   - [ ] Optimize volume and calorie displays for mobile devices
   - [ ] Implement condensed views for small screens
   - [ ] Create mobile-specific interactive visualizations
   - [ ] Add quick-view indicators for key metrics
   - [ ] Ensure touch-friendly controls for all new UI elements

 - [ ] Update documentation
   - [ ] Add volume and calorie calculation methodology to help pages
   - [ ] Update README.md with new feature details
   - [ ] Add calculation components to inventory.md
   - [ ] Create user guide for interpreting volume/calorie metrics
   - [ ] Document API changes and examples

[ ] Make the reports page tabbed adding volume and calories reports
 - [ ] Design and implement tabbed interface for reports page
   - [ ] Create Bootstrap tab structure in Reports/Index.cshtml
   - [ ] Design responsive tab layout for both desktop and mobile views
   - [ ] Add tab navigation with appropriate icons and labels
   - [ ] Implement tab state persistence using query parameters or local storage
   - [ ] Add smooth transitions between tabs with animation
   - [ ] Ensure accessibility compliance for tab navigation

 - [ ] Implement volume analysis reports tab
   - [ ] Create VolumeAnalysisViewModel to aggregate volume metrics
   - [ ] Develop volume calculation service for different exercise types
   - [ ] Add volume trending chart showing progress over time
   - [ ] Implement volume breakdown by muscle group/exercise type
   - [ ] Create volume heatmap visualization for weekly patterns
   - [ ] Add relative volume analysis (volume per body weight)
   - [ ] Implement comparative volume metrics against previous periods

 - [ ] Develop calorie expenditure reports tab
   - [ ] Create CalorieAnalysisViewModel for energy expenditure metrics
   - [ ] Implement calorie calculation service using MET values and user data
   - [ ] Add calorie burn chart with trend analysis
   - [ ] Create breakdown of calories by workout type/intensity
   - [ ] Implement weekly/monthly calorie goal tracking
   - [ ] Add calorie expenditure forecasting based on planned workouts
   - [ ] Develop calorie visualization comparing different exercise types

 - [ ] Enhance existing personal records tab
   - [ ] Update personal records view to fit new tabbed interface
   - [ ] Add filtering options for record categories
   - [ ] Implement visual indicators for recent records
   - [ ] Create record history tracking to show progression
   - [ ] Add export functionality for personal records

 - [ ] Create progress tracking tab
   - [ ] Develop integrated view of key performance indicators
   - [ ] Implement customizable progress metrics selection
   - [ ] Add milestone tracking and celebration notifications
   - [ ] Create progress snapshot comparisons
   - [ ] Implement goal setting and tracking visualization

 - [ ] Update shared reports functionality
   - [ ] Ensure all new tabs work with shared workout reports
   - [ ] Implement appropriate permissions for different tabs
   - [ ] Add tab visibility toggle for shared report links
   - [ ] Create simplified view for shared access

 - [ ] Implement data loading optimization
   - [ ] Add lazy loading for tab content to improve performance
   - [ ] Implement background data loading for inactive tabs
   - [ ] Create caching mechanism for report data
   - [ ] Add loading indicators for data-intensive reports
   - [ ] Optimize database queries specific to each tab

 - [ ] Enhance the user experience
   - [ ] Create printable report view for each tab
   - [ ] Add export to PDF option for all report tabs
   - [ ] Implement date range picker for customizable reporting periods
   - [ ] Add data tooltips and help information for metrics
   - [ ] Create mobile-optimized views for each tab
