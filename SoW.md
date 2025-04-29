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
  
 - [ ] Complete unified functionality for Goals for coaches and normal users this should integrate with the coaching dashboard
   - [ ] Goal Model and Database Enhancements
     - [ ] Extend ClientGoal model to support user-created goals (not just coach-assigned)
     - [ ] Add fields for tracking completion criteria and automated progress
     - [ ] Create migration for updated goal models and relationships
     - [ ] Ensure database query filters properly handle goal visibility between users and coaches
     - [ ] Implement goal categories (strength, cardio, weight loss, etc.)

   - [ ] Coach Goal Management Interface
     - [ ] Create Goals/Index.cshtml page for coaches to view all client goals
     - [ ] Implement goal creation interface for coaches to assign goals to clients
     - [ ] Add CRUD operations for coaches to manage client goals
     - [ ] Develop goal progress tracking interface for coaches
     - [ ] Add goal visualization components (progress bars, charts)
     - [ ] Create filtering options for viewing goals by client, status, and category

   - [ ] User Goal Management Interface
     - [ ] Create personal goal management page for regular users
     - [ ] Implement UI for users to create, update and track personal goals
     - [ ] Add unified view for both coach-assigned and personal goals
     - [ ] Create progress update functionality for users to record milestones
     - [ ] Implement goal suggestions based on workout history
     - [ ] Add celebration/achievement notifications when goals are completed

   - [ ] Goal Progress Tracking System
     - [ ] Create automated progress calculation based on workout data
     - [ ] Implement tracking mechanisms specific to goal types (weight, reps, duration)
     - [ ] Add manual progress update capabilities for non-trackable goals
     - [ ] Create goal history and progress timeline views
     - [ ] Implement comparison tools for goal progress over time
     - [ ] Add milestone tracking for long-term goals

   - [ ] Coach Dashboard Integration
     - [ ] Enhance coach dashboard to show client goal summaries
     - [ ] Add goal analytics to provide insights on client progress
     - [ ] Create notifications for coaches when clients update goal progress
     - [ ] Implement goal status indicators in client lists
     - [ ] Add quick actions for coaches to provide feedback on goal progress

   - [ ] User Dashboard Integration
     - [ ] Add goal widgets to user dashboard
     - [ ] Create goal reminders and notifications
     - [ ] Implement integration with workout planning to align with goals
     - [ ] Add motivation features like streak counting and milestone celebration
     - [ ] Create social sharing options for completed goals

   - [ ] Data Integration and APIs
     - [ ] Create shared services for goal operations between coach and user interfaces
     - [ ] Implement proper permission checking for goal CRUD operations
     - [ ] Optimize goal-related queries for performance
     - [ ] Add API endpoints for goal tracking mobile integration
     - [ ] Implement goal data export functionality for reporting

 - [ ] Recent Client Activity should show real data.

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