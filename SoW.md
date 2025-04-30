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
   - [x] Implement the scheduling system for coaches similar to that used by normal users /Workouts/ScheduledWorkouts
   - [x] Implement a template system for coaches similar to a normal users system, integrating with schedules as with normal users
   - [ ] Create unified template management experience

--

 - [ ] Complete unified functionality for Goals for coaches and normal users this should integrate with the coaching dashboard
   - [x] Goal Model and Database Enhancements
     - [x] Extend ClientGoal model to support user-created goals (not just coach-assigned)
     - [x] Add fields for tracking completion criteria and automated progress
     - [x] Create migration for updated goal models and relationships
     - [x] Ensure database query filters properly handle goal visibility between users and coaches
     - [x] Implement goal categories (strength, cardio, weight loss, etc.)

   - [x] Coach Goal Management Interface
     - [x] Create Goals/Index.cshtml page for coaches to view all client goals
     - [x] Implement goal creation interface for coaches to assign goals to clients
     - [x] Add CRUD operations for coaches to manage client goals
     - [x] Develop goal progress tracking interface for coaches
     - [x] Add goal visualization components (progress bars, charts)
     - [x] Create filtering options for viewing goals by client, status, and category

   - [x] User Goal Management Interface
     - [x] Create personal goal management page for regular users
     - [x] Implement UI for users to create, update and track personal goals
     - [x] Add unified view for both coach-assigned and personal goals
     - [x] Create progress update functionality for users to record milestones

   - [x] Goal Progress Tracking System
     - [x] Create automated progress calculation based on workout data
     - [x] Implement tracking mechanisms specific to goal types (weight, reps, duration)
     - [x] Add manual progress update capabilities for non-trackable goals
     - [x] Create goal history and progress timeline views
     - [x] Implement comparison tools for goal progress over time
     - [x] Add milestone tracking for long-term goals

   - [x] Coach Dashboard Integration
     - [x] Enhance coach dashboard to show client goal summaries
     - [x] Add goal analytics to provide insights on client progress
     - [x] Create notifications for coaches when clients update goal progress
     - [x] Implement goal status indicators in client lists
     - [x] Add quick actions for coaches to provide feedback on goal progress

   - [x] User Dashboard Integration
     - [x] Add goal widgets to user dashboard
     - [x] Create goal reminders and notifications
     - [x] Implement integration with workout planning to align with goals
     - [x] Add motivation features like streak counting and milestone celebration
     - [x] Create social sharing options for completed goals

   - [x] Data Integration and APIs
     - [x] Create shared services for goal operations between coach and user interfaces
     - [x] Implement proper permission checking for goal CRUD operations
     - [x] Optimize goal-related queries for performance
     - [x] Add API endpoints for goal tracking mobile integration
     - [x] Implement goal data export functionality for reporting

---

 - [ ] Create logic and background job that converts scheduled workouts into workouts
   - [ ] Design and implement conversion service
     - [ ] Create ScheduledWorkoutProcessorService class
     - [ ] Implement methods to determine which scheduled workouts are due for conversion
     - [ ] Support different recurrence patterns (Once, Weekly, BiWeekly, Monthly)
     - [ ] Handle timezone considerations for global users
     - [ ] Add configuration options for advance creation (e.g., create workouts 24h in advance)
   
   - [ ] Build template-to-workout conversion logic
     - [ ] Implement factory method to create WorkoutSession from WorkoutTemplate
     - [ ] Transfer exercise data, sets, and program notes from template to workout
     - [ ] Set proper workout metadata (scheduled date, reference to template)
     - [ ] Create test cases for different template types
     
   - [ ] Create Hangfire background job
     - [ ] Implement recurring Hangfire job to process scheduled workouts
     - [ ] Configure appropriate schedule interval (e.g., hourly checks)
     - [ ] Create job dashboard for monitoring conversion status
     - [ ] Implement error handling and retry mechanisms
     - [ ] Add logging for conversion events and failures
   
   - [ ] Implement recurrence pattern logic
     - [ ] Build next occurrence calculation system for each pattern type
     - [ ] Handle special cases like month end dates and leap years
     - [ ] Ensure proper handling of recurring workouts with end dates
     - [ ] Implement logic for multiple days of the week (stored in MultipleDaysOfWeek)
   
   - [ ] Add status tracking for scheduled workouts
     - [ ] Create model property to track last generated workout
     - [ ] Update schedule status when workout is generated
     - [ ] Implement history tracking for recurring schedules
     - [ ] Update dashboard to show conversion status
   
   - [ ] Create notification system integration
     - [ ] Trigger notifications when workout is generated from schedule
     - [ ] Configure notification timing relative to scheduled date
     - [ ] Include template/schedule details in notification
     - [ ] Send workout reminders according to ReminderHoursBefore setting
   
   - [ ] Build system to handle missed workouts
     - [ ] Implement detection for missed workout schedule conversions
     - [ ] Create policy for handling missed schedule conversions (skip or create late)
     - [ ] Add configuration options for missed workout handling
     - [ ] Update client and coach interfaces for missed workout visibility


---

 - [ ] Recent Client Activity should show real data.

 ---

 - [ ] Create logic and background job for sending notifications
 
 ---

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

Make offlien version match the online version