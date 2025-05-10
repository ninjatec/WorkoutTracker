# WorkoutTracker

A comprehensive fitness tracking web application built with ASP.NET Core that allows users to track their workout sessions, exercises, and progress.

Production Site running on www.workouttracker.online

Current Version: 2.1.0.1

## Features

- **Enhanced Responsive Design**: Full mobile optimization across the application
  - Mobile-first CSS architecture with progressive enhancement
  - Responsive table transformations for better data consumption on mobile
  - Form optimizations for touch-friendly inputs and improved mobile UX
  - Mobile-specific layouts for complex data pages
  - Consistent viewport meta tags across all template types
  - CSS component system with modular stylesheets for maintainability
  - JavaScript utilities for automatically enhancing tables on mobile

- **API Ninjas Integration for Exercise Types**: Rich exercise database from external API
  - Comprehensive exercise data with over 600 available exercises
  - Detailed instructions for proper exercise execution
  - Categorization by muscle group, difficulty level, and equipment
  - Filtering by exercise type, muscle, difficulty, and equipment
  - Admin interface for searching and importing exercises
  - Automatic updates for API-sourced exercises
  - Visual indicators for API-sourced vs. manually created exercises
  - Related exercise suggestions based on muscle groups
  - Secure API key management with configuration
  - **Exercise enrichment tool to populate missing details in existing exercises**
  - **Bulk enrichment capability for multiple exercises at once**
  - **Intelligent matching of manually created exercises to API data**

- **Dynamic Log Level Configuration**: Runtime control of application logging levels
  - Admin interface for adjusting log levels without application restart
  - Global default log level configuration
  - Source-specific log level overrides for granular control
  - Persistent configuration stored in database
  - Real-time application of log level changes
  - Security controls with admin-only access

- **Workout Sharing**: Share workout data securely with others
  - Secure token-based sharing with expiration dates
  - Granular access controls for specific workout data
  - Usage limits with maximum view count options
  - Time-limited access with automatic expiration
  - Account-wide sharing options
  - User control to revoke access at any time
  - Read-only reports for shared workout data
  - Cookie-based token persistence for better navigation
  - IP-based security and rate limiting
  - Permission-based feature access control
  - **Accessible UI for token management with accordion-based interface**
  - **Modal-free design for improved accessibility and mobile experience**
  - **Inline editing with tabbed interface for token management**
  - Token usage statistics with remaining uses display
  - Visual indicators for token status and expiration
  - Bulk token management with filtering options

- **Workout Templates**: Create and reuse workout routines
  - Template-based workout creation for consistent routines
  - Customizable exercise sequences with order control
  - Default set values including recommended reps and weights
  - Public/private visibility options for templates
  - Categorization and tagging for organization
  - One-click workout creation from templates
  - Comprehensive template management interface
  - Mobile-optimized template browsing and creation
  - Search and filter functionality for large template libraries
  - **Self-scheduling capability for regular users**
  - **Calendar interface for planning personal workouts**
  - **Support for one-time and recurring workout schedules**
  - **Multiple recurrence patterns (daily, weekly, bi-weekly, monthly)**
  - **Reminder settings for upcoming workouts**

- **Coaching Features**: Comprehensive tools for fitness coaches to manage clients
  - Role-based permission system for coaches
  - Client invitation and management system
  - Workout template creation and assignment
  - **Workout scheduling with recurring patterns**
  - **Calendar interface for scheduling client sessions**
  - **Support for one-time and recurring workout schedules**
  - **Multiple recurrence patterns (daily, weekly, bi-weekly, monthly)**
  - **Reminder settings for upcoming workouts**
  - **Enhanced goal management with user and coach-created goals**
  - **Standardized goal categories (strength, cardio, weight, etc.)**
  - **Advanced goal progress tracking with automated calculations**
  - **Goal visibility controls between coaches and clients**

- **User Authentication**: Secure login and registration using ASP.NET Core Identity with email confirmation
- **Admin Dashboard**: Comprehensive admin dashboard with system metrics, user statistics, and quick actions
- **Admin User Management**: Complete CRUD operations for user accounts with:
  - Role assignment and management
  - Account details viewing and editing
  - Password reset functionality
  - Email confirmation controls
  - Account lockout management
  - Activity tracking with session counts and last activity date
  - Login history tracking with device and location information
  - Security controls to prevent removing the last admin user
- **Session Management**: Create and manage workout sessions with timestamps
- **Exercise Tracking**: Track exercises performed during each workout session
- **Set & Rep Management**: Record sets and repetitions for each exercise with weight tracking
- **Exercise Library**: Maintain a database of exercise types for reuse across workout sessions
- **Data Visualization**: Track progress over time through intuitive charts and graphs
- **Performance Analytics**: View success/failure rates for exercises with detailed breakdowns
- **One Rep Max Calculator**: Estimate maximum strength using seven scientific formulas based on weight and reps
- **Quick Workout Mode**: Optimized interface for fast workout tracking at the gym
  - Mobile-first design with touch-friendly controls
  - Quick-access tabs for recent and favorite exercises
  - Muscle group filtering for organized exercise selection
  - Minimal input required for fast logging during workouts
  - Recent sets view for tracking current workout
  - Streamlined UI optimized for gym use
  - Workout timer with configurable presets and vibration alerts
  - Exercise and rest period countdown functionality
  - Haptic feedback for timer interactions
  - Keyboard shortcuts for power users
- **Feedback System**: Submit bugs, feature requests, and general feedback with integrated email notifications
  - Multiple feedback types (Bug Report, Feature Request, General Feedback, Question)
  - Status tracking (New, In Progress, Completed, Rejected)
  - Priority levels (Low, Medium, High, Critical)
  - Admin assignment and ownership tracking
  - Public/private response management
  - Email notifications for status changes
  - Template responses for common scenarios
  - Admin dashboard with feedback analytics
  - Device and browser information collection
  - User contact tracking and communication
  - Estimated completion date scheduling
  - Publication controls for public visibility
- **Background Job Processing**: Long-running operations processed in the background
  - Real-time progress updates using SignalR
  - Prevents timeout errors during large data operations
  - Detailed progress tracking with percentage completion
  - Error handling with rollback capabilities
  - Admin dashboard for monitoring active jobs
  - Comprehensive job monitoring with statistics and history
  - Failed job management with retry capabilities
  - Server status monitoring for background processing
  - Detailed job information with arguments and execution timeline
  - Job filtering by status, type, and queue
  - **Role-based Hangfire processing with dedicated worker pods**
  - **Environment variable configuration for pod-specific roles**
  - **Centralized server configuration with diagnostic UI**
  - **Queue-based job processing with prioritization**
- **Health Monitoring**: Comprehensive health checks and metrics collection for production monitoring

## Technology Stack

- **Framework**: ASP.NET Core (Razor Pages)
- **Database**: SQL Server
- **ORM**: Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Frontend**: Bootstrap 5, HTML, CSS, JavaScript
- **Data Visualization**: Chart.js
- **Email Services**: SMTP integration for notifications and feedback
- **Background Processing**: Hangfire for long-running tasks
- **Real-time Communication**: SignalR for progress updates with Redis backplane for multi-container support
- **Containerization**: Docker support for deployment
- **Monitoring**: Prometheus metrics and health checks for Kubernetes
- **Cache**: Redis for SignalR scale-out and distributed caching

## Architecture

The application follows a clean architecture pattern with Razor Pages:

- **Models**: Define the core domain entities (User, WorkoutSession, WorkoutSet, Rep, ExerciseType, SetType, Feedback, HelpArticle, HelpCategory, GlossaryTerm)
- **Pages**: Razor Pages for the user interface, organized by feature areas
- **Services**: Business logic services for user management, email notifications, help content, and other functionality
- **Data Access**: Entity Framework Core for database operations

## Data Model

> **Note:** As of 2025-05-10, the legacy `Session` model has been fully replaced by `WorkoutSession`. All features, data access, and documentation now use `WorkoutSession`. The `Session` entity is retained only for backward compatibility in data exports (see `SessionExport` class in code comments).

The application uses the following entity relationships:

- **User**: Represents a registered user of the application
  - Contains personal information and is linked to ASP.NET Identity
  - Has many WorkoutSessions
  - Can submit Feedback
  
- **WorkoutSession**: Represents a workout session
  - Belongs to a User
  - Has many WorkoutExercises and WorkoutSets
  - Includes date, time, name, description, and notes
  - Properties: WorkoutSessionId, Name, Description, StartDateTime, EndDateTime, Notes, UserId, etc.
  
- **WorkoutSet**: Represents a set of exercises within a workout session
  - Belongs to a WorkoutSession
  - Associated with an ExerciseType
  - Has a SetType (e.g., warm-up, normal, drop set)
  - Contains weight information
  - Has many Reps
  
- **Rep**: Represents individual repetitions within a set
  - Belongs to a WorkoutSet
  - Includes weight and success tracking
  
- **ExerciseType**: Represents a type of exercise (e.g., bench press, squat)
  - Referenced by many WorkoutSets and WorkoutExercises
  
- **SetType**: Represents a type of set (e.g., regular, warm-up, drop set)
  - Referenced by many WorkoutSets
  
- **Feedback**: Represents user-submitted feedback
  - Can be linked to a User
  - Includes subject, message, contact email, submission date
  - Has type (bug report, feature request, general feedback, question)
  - Has status tracking (new, in progress, completed, rejected)
  - Includes priority levels (low, medium, high, critical)
  - Tracks admin assignment and ownership
  - Allows public/private response management
  - Sends email notifications for status changes
  - Provides template responses for common scenarios
  - Includes admin dashboard with feedback analytics
  - Collects device and browser information
  - Tracks user contact and communication
  - Allows estimated completion date scheduling
  - Provides publication controls for public visibility

- **HelpArticle**: Represents a help article in the help center
  - Belongs to a HelpCategory
  - Contains content, tags, and metadata
  - Can have related articles
  - Tracks view count and user feedback

- **HelpCategory**: Represents a category for organizing help articles
  - Has many HelpArticles
  - Can have parent/child relationships for hierarchical organization

- **GlossaryTerm**: Represents a workout terminology definition in the glossary
  - Contains the term, definition, and usage examples
  - Can be organized by category
  - Can reference related terms
  
- **ShareToken**: Represents a secure sharing link for workout data
  - Associated with a specific User who created the share
  - Can be linked to a specific WorkoutSession or share all user sessions
  - Contains expiration date for time-limited access
  - Includes usage tracking with access count
  - Optional maximum usage limit with automatic deactivation
  - Granular access controls for different features (sessions, reports, calculator)
  - Includes user-defined name and description for organization
  - Provides active status flag for manual revocation

- **WorkoutTemplate**: Represents a reusable workout template
  - Belongs to a User (creator)
  - Has many WorkoutTemplateExercises
  - Includes name, description, category, tags for organization
  - Contains metadata like creation date, modification date
  - Includes visibility settings (public/private)
  - Properties: WorkoutTemplateId, Name, Description, CreatedDate, LastModifiedDate, IsPublic, Category, Tags, UserId
  
- **WorkoutTemplateExercise**: Represents an exercise within a template
  - Belongs to a WorkoutTemplate
  - Associated with an ExerciseType
  - Has many WorkoutTemplateSets
  - Contains sequence number for ordering exercises
  - Properties: WorkoutTemplateExerciseId, WorkoutTemplateId, ExerciseTypeId, SequenceNum, Notes
  
- **WorkoutTemplateSet**: Represents default set values for a template exercise
  - Belongs to a WorkoutTemplateExercise
  - Associated with a SetType
  - Contains default values for reps and weight
  - Properties: WorkoutTemplateSetId, WorkoutTemplateExerciseId, SettypeId, DefaultReps, DefaultWeight, SequenceNum, Description, Notes

- **WorkoutExercise**: Represents an exercise performed during a workout session
  - Belongs to a WorkoutSession
  - Has a Name and Notes property
  - Properties: WorkoutExerciseId, WorkoutSessionId, ExerciseTypeId, SequenceNum, Name, Notes

## Workout Sharing

The application provides a secure system for sharing workout data with others:

- **Secure Token System**: Share workout data via unique, secure tokens
  - Randomly generated secure tokens for each share
  - Token validation with built-in security checks
  - Automatic token expiration after a set date
  - Optional usage limits with maximum view count

- **Access Controls**:
  - Share specific sessions or your entire workout history
  - Granular permission controls for different features:
    - Session history access
    - Report access
    - Calculator access
  - Temporary access with automatic expiration
  - Manual revocation capability at any time

- **User Management**:
  - Dashboard for viewing and managing all active shares
  - Track usage statistics for each shared link
  - Add descriptive names and notes to each share
  - Filter and sort shares by expiration, usage, and access type
  - One-click generation of new sharing links

- **Security Features**:
  - Query filtering to prevent data leakage
  - Anti-brute force protections
  - Rate limiting for token validation
  - Comprehensive activity logging
  - No authentication required for recipients (view-only access)

## Reporting & Analytics

The application provides detailed workout analytics to help users track their progress:

- **Success/Failure Tracking**: Visualize successful vs. failed repetitions with pie charts
- **Exercise Performance**: Analyze performance by exercise type
- **Recent Performance**: Focus on the last 30 days of workout data
- **Success Rate Analysis**: View detailed breakdown of success rates by exercise

## User Feedback & Support

The application includes an integrated feedback system:

- **Feedback Submission**: Users can submit bug reports, feature requests, or general feedback
- **Email Notifications**: Automatic email notifications sent to administrators when feedback is submitted
- **Status Tracking**: Feedback is tracked with statuses (new, in progress, completed, rejected)
- **Admin Management**: Administrators can view, update, and respond to feedback
  - Dashboard with feedback analytics and trends
  - Advanced filtering, sorting, and search capabilities
  - Assignable feedback items to specific administrators
  - Priority levels from Low to Critical
  - Template responses for quick communication
  - Internal notes for admin-only discussions
- **User Updates**: Users receive email notifications when their feedback status changes
- **Public Responses**: Admin responses can be made visible to the submitter or published publicly
- **Feedback Categories**: Organization by categories for better management
- **Device Tracking**: Collection of browser and device information for bug reports
- **Scheduling**: Estimated completion dates for transparency

## Help Center

The application features a comprehensive help center:

- **Searchable Documentation**: Full-text search across all help content
- **Getting Started Guides**: Step-by-step tutorials for new users
- **Feature-specific Help**: Detailed documentation for all major features
- **Video Tutorials**: Visual guides for key workflows
- **Glossary**: Definitions of workout and fitness terminology
- **Frequently Asked Questions**: Categorized common questions and answers
- **Contextual Help**: Access relevant help from any page in the application
- **Printable Resources**: Printer-friendly help content for offline reference

## Monitoring & Health Checks

The application includes comprehensive health monitoring for production environments:

- **Health Check Endpoints**:
  - `/health`: General health status with detailed response
  - `/health/live`: Liveness check for basic app functionality
  - `/health/ready`: Readiness check for database connectivity
  - `/health/database`: Specific database health metrics

- **Kubernetes Integration**:
  - Liveness probe: Confirms the application is running properly
  - Readiness probe: Ensures the application is ready to handle requests
  - Startup probe: Allows proper initialization time

- **Metrics Collection**:
  - Prometheus metrics exposed at `/metrics` endpoint
  - Custom application metrics for workout sessions, sets, and reps
  - Performance metrics for HTTP requests and active users
  - System metrics for disk space and memory usage

## Deployment

The application includes Docker support for containerized deployment with:
- Docker Compose configuration for development and production
- Kubernetes manifests for orchestrated deployment

### Multi-Container Architecture

The application is designed to run in a multi-container environment for improved scalability and reliability:

#### Core Components
- **Web Application**: ASP.NET Core application running in multiple container instances
- **Redis**: Used for both distributed caching and SignalR backplane
- **SQL Server**: Database backend with connection pooling optimizations

#### Container Communication
- **SignalR Backplane**: Redis-backed communication between application instances
- **Distributed Cache**: Shared Redis cache for report data and session state
- **Database Connection Pooling**: Optimized SQL Server connections with resilience

#### Scaling Capabilities
- **Horizontal Scaling**: Multiple web application instances behind a load balancer
- **Persistent Connections**: SignalR connections maintained across container instances
- **Session Persistence**: User sessions maintained when scaling up/down
- **Connection Resilience**: Circuit breaker patterns for database and Redis connections

#### Deployment Configuration
- **Kubernetes Manifests**: Complete deployment configuration in `/k8s` directory
- **Resource Management**: CPU and memory limits defined for production workloads
- **Health Monitoring**: Liveness, readiness, and startup probes for container orchestration
- **Secrets Management**: Secure configuration via Kubernetes secrets

### Secrets Management

- Development: User Secrets for local development (dotnet user-secrets)
- Production: Kubernetes Secrets for sensitive configuration in production
- Connection strings and email configuration stored securely outside of source code

## Security Features
- Content Security Policy (CSP) headers
- Persistent data protection key storage
- Secure Docker configuration
- Comprehensive health checks and monitoring
- Secure secrets management

## Data Portability

The application provides comprehensive data portability features:

- **Export Data**: Download your workout data in JSON format
  - Filter by date range
  - Includes all related data (sessions, sets, reps)
  - Exports exercise types and set types
  - Complete personal records and progress data

- **Delete Data**: Efficiently remove all workout data while keeping your account
  - Confirmation required to prevent accidental deletion
  - Background processing to prevent timeouts
  - Real-time progress updates via SignalR
  - Batch processing for large datasets (100 records at a time)
  - Transactional deletion to ensure data consistency
  - Account remains active for future use

- **Import Data**: Upload workout data from JSON files
  - Intelligent duplicate detection
  - Background processing for large imports
  - Real-time progress tracking with completion percentage
  - Option to skip or replace existing data
  - Import preview with detailed summary
  - Automatic exercise type and set type matching

- **Data Security**:
  - User-specific data isolation
  - Secure file handling
  - Transaction-based imports with rollback
  - Data validation and sanitization

## Recent Updates

- **Enhanced Entity Framework Core Relationships**:
  - Fixed foreign key attribute conflicts between WorkoutSchedule and WorkoutSession
  - Properly configured relationships between CoachClientRelationship and AppUser
  - Added matching query filters for required relationship endpoints
  - Resolved shadow property conflicts with explicit configurations (see Technical Debt and Relationship Best Practices)
  - Added OrderBy clauses to ensure consistent query results
  - Created comprehensive documentation of EF Core relationship best practices
  - Improved performance and stability in multi-container deployments

- **Completed Session to WorkoutSession Migration**:
  - Migrated from legacy Session model to improved WorkoutSession structure
  - All features and data access now use WorkoutSession
  - No remaining code or documentation references to legacy Session entity
  - SessionExport class is legacy only (see code comments)

- **Enhanced Coach-Client Features**:
  - Implemented robust goal management system with progress tracking
  - Added client grouping for better organization
  - Created coach-client permissions system
  - Built note-taking tools with privacy controls
  - Added goal milestones with progress history

- **Improved Alerting and Monitoring System**:
  - Added comprehensive alert history tracking
  - Implemented circuit breaker patterns for database resilience
  - Created system uptime history monitoring
  - Built health monitoring dashboards
  - Added login history tracking for security auditing

- **Implemented Comprehensive Version Management System**:
  - Added AppVersion model for tracking application version history
  - Created version administration interface for managing releases
  - Implemented display of current version in UI footer
  - Added version history page with detailed release notes
  - Enabled setting any historical version as current
  - Created version tracking with git commit hash links
  - Implemented automatic version capture during deployment
  - Added rollback capability with previous version detection
  - Created timeline view for visualizing version history

- **Enhanced Mobile Navigation Experience**:
  - Implemented mobile-optimized bottom navigation bar for critical actions
  - Added swipe gestures for common actions like edit and delete
  - Created pull-to-refresh functionality for data lists and tables
  - Enhanced mobile menu interface with collapsible panels

- **Enhanced Mobile-First Responsive Design**:
  - Implemented mobile-first CSS architecture with progressive enhancement
  - Added touch-friendly optimizations for mobile interfaces
  - Optimized forms with larger touch targets and improved mobile spacing

- **Implemented Output Caching for Static Content**:
  - Added Redis-backed output caching for production environments
  - Configured memory-based output caching for development environments
  - Created specialized cache policies for different content types

## 2025-05-09: CSP Update
- Allowed https://static.cloudflareinsights.com in the script-src directive to support Cloudflare Insights beacon script loading (required for some analytics/monitoring integrations).

## 2025-05-10: Session to WorkoutSession migration complete
- All code, pages, and services now use WorkoutSession exclusively
- Obsolete MVC view removed
- SessionExport class marked as legacy only (see code comments)
- See logs/remove_obsolete_session_view_20250510.txt

## 2025-05-10: Configuration Cleanup
- All hardcoded and environment-specific configuration has been removed from the source code. All sensitive and environment-specific values are now managed via appsettings.json, environment variables, and user secrets. See Technical Debt section for details.

## 2025-05-10: UI Consistency & Output Caching
- Reviewed all Razor Pages for Bootstrap 5 consistency and output caching.
- Updated Reports/Index to use [OutputCache] with a 5-minute duration and policy, matching other static/report pages.
- Verified Bootstrap 5 usage and layout consistency in Reports/Index and shared layouts.
