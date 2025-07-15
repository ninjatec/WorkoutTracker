# System Inventory

## Overview

WorkoutTracker is a fitness tracking application built with ASP.NET Core, using the Razor Pages architecture. This document provides a comprehensive inventory of the system components, their relationships, and key implementations.

## Inventory

This document maintains an up-to-date inventory of all features, components, and modules in the Workout Tracker system.

### Core Features
- User Authentication and Authorization
- Workout Session Management
- Exercise Tracking
- Set and Rep Recording
- Progress Visualization
- Sharing Capabilities
- Data Import/Export
- Report Generation
- Exercise Library with API Integration
- User Preference Management
- Blog System for Fitness Content
- Responsive Mobile-First UI
- Admin Dashboard
- Metric Collection
- Alert System
- Volume Calculation System
- Calorie Estimation System
- Version Management System
- Coach-Client Relationship Management
- Goal Tracking and Progress Management

### Technology Stack
- ASP.NET Core 9
- Entity Framework Core
- SQL Server Database
- Redis Cache
- Bootstrap 5
- SignalR for Real-time Updates
- Hangfire for Background Jobs
- Serilog for Structured Logging
- Chart.js for Data Visualization
- DataTables for Interactive Tables
- Azure/Kubernetes Deployment Support
  - Hardened security contexts with minimal privilege access
  - Complete host system isolation (network, PID, IPC namespaces)
  - Non-root container execution (user ID 1000)
  - All Linux capabilities dropped except NET_BIND_SERVICE
  - Privilege escalation prevention with explicit restrictions
  - Seccomp security profiles enabled at pod and container level
  - Process namespace isolation for security
  - Read-only root filesystem with memory-backed writable volumes
  - In-memory temporary storage for enhanced security
- Output Caching with Redis Backplane

### Application Components
- User Management
- Workout Management
- Exercise Library
- Authentication System
- Sharing System with Secure Tokens
- Interactive Progress Dashboard
  - Components:
    - Volume Progress visualization
    - Exercise Distribution charts
    - Personal Bests tracking
    - Workout Frequency visualization
  - Features:
    - Period-based filtering
    - Visual workout data analysis
    - Personal record tracking
    - Interactive charts with Chart.js
    - PDF/CSV export options
- Reporting Engine
- Admin Area
- Import/Export System
- Scheduling System
- Health Monitoring
  - Components:
    - Health Check Endpoints
    - Database Connection Monitoring
    - Google Analytics Integration
    - Prometheus Metrics
- Alerting System
- Responsive Design System
  - Components:
    - Mobile Bottom Navigation
    - Responsive Tables
    - Optimized Forms for Touch
    - Loading Indicators
    - Toast Notifications
    - Enhanced Visual Hierarchy
  - Features:
    - Device-specific layouts
    - Touch-friendly inputs
    - Visual feedback for user actions
    - Optimal touch target sizes
    - Swipe gestures for common actions
    - Consistent styling across devices
- Volume Calculation System
- Calorie Estimation System
- Version Management System
- Coach-Client Relationship System
- Goal Tracking System

### API Controllers
- GoalsController - Main controller for goal operations via MVC
- GoalsApiController - RESTful API controller for mobile integration 
- SharedWorkoutController - API for accessing shared workout data
- WorkoutScheduleController - Controller for workout scheduling operations
- HangfireController - Controller for background job management
- HangfireDiagnosticsController - Controller for job monitoring and diagnostics
- JobStatusController - Controller for tracking job status updates
- ReportsApiController - API for workout report data
- **WorkoutSetsApiController**: Handles CRUD operations for workout sets
- **WorkoutExercisesApiController**: Handles CRUD operations for workout exercises
- **DashboardApiController**: Provides data for dashboard visualizations

### Services
- DashboardService - Service providing dashboard metrics and chart data with caching
- DashboardRepository - Repository for dashboard-specific data access optimized for performance
- DashboardCachePolicy - Output cache configuration for dashboard data
- GoalOperationsService - Shared service for centralized goal operations with permission checking
- GoalQueryService - Service for optimized goal-related queries
- GoalProgressService - Service for tracking and updating goal progress
- LoginHistoryService - Service for tracking user login activity
- LoggingService - Service for dynamic log level management
- VersionService - Service for application version management
- AlertingService - Service for system alerts and notifications
- VolumeCalculationService - Service for workout volume calculations
- CalorieCalculationService - Service for workout calorie estimation
- QuickWorkoutService - Service for optimized gym workout tracking
- BlogService - Service for blog post management and retrieval
- BlogRepository - Repository for blog-related data access
- BlogImageUtility - Utility for handling blog image uploads and management

### DTOs
- GoalExportDto - Data transfer object for goal export and API integration
- GoalMilestoneDto - Data transfer object for goal milestone data in exports
- GoalProgressUpdateDto - Data transfer object for goal progress updates via API
- ShareTokenDto - Data transfer object for sharing workout data

### Data Models
- User
- WorkoutSession
- WorkoutExercise
- WorkoutSet
- ExerciseType
- SetType
- UserProfile
- ShareToken
- LoginHistory
- NotificationPreference
- Report
- Metric
- Alert
- AlertThreshold
- Notification
- AlertHistory
- WorkoutTemplate
- WorkoutTemplateExercise
- WorkoutTemplateSet
- CoachNote
- WorkoutSchedule
- ClientGroup
- CoachClientRelationship
- CoachClientPermission
- ClientGoal
- BlogPost
- BlogCategory
- BlogTag
- BlogPostCategory
- BlogPostTag
- GoalCategory
- GoalMilestone
- AppVersion
- WhitelistedIp

#### User
- Primary entity representing application users
- Links to ASP.NET Core Identity system
- Properties: UserId, Name, IdentityUserId, CreatedDate, LastModifiedDate
- Relationships: 
  - One-to-many with WorkoutSessions
  - One-to-many with Feedback
  - One-to-many with ClientGoals
  - Many-to-many with CoachClientRelationship (as coach or client)

#### WorkoutSession
- Represents a workout session
- Properties: WorkoutSessionId, Name, Description, StartDateTime, EndDateTime, Notes
  CompletedDate, Duration, IsCompleted, TemplatesUsed, WorkoutTemplateId
  TemplateAssignmentId, IsFromCoach, Status, UserId
- Relationships: 
  - Many-to-one with User
  - One-to-many with WorkoutExercises
  - Many-to-one with WorkoutTemplate (optional)

#### WorkoutExercise
- Represents an exercise performed during a workout session
- Properties: WorkoutExerciseId, WorkoutSessionId, ExerciseTypeId, SequenceNum, Name, Notes
- Relationships:
  - Many-to-one with WorkoutSession
  - Many-to-one with ExerciseType
  - One-to-many with WorkoutSets

#### WorkoutSet
- Represents a set of an exercise performed during a workout
- Properties: WorkoutSetId, WorkoutExerciseId, SetTypeId, Reps, Weight, SequenceNum, Notes
- Relationships:
  - Many-to-one with WorkoutExercise
  - Many-to-one with SetType

#### ExerciseType
- Catalog of available exercises
- Properties: ExerciseTypeId, Name, Description, MuscleGroup, Equipment, Difficulty,
  Instructions, IsApiSourced, ApiId, IsCustom, CreatedByUserId
- Relationships: 
  - One-to-many with WorkoutExercises
  - One-to-many with WorkoutTemplateExercises
  - Many-to-one with User (creator, optional)

#### SetType
- Categorizes sets (e.g., warm-up, normal, drop set)
- Properties: SettypeId, Name, Description
- Relationships: 
  - One-to-many with WorkoutSets
  - One-to-many with WorkoutTemplateSets

#### Feedback
- User submitted feedback, bug reports, and feature requests
- Properties: 
  - FeedbackId, Subject, Message, ContactEmail, SubmissionDate
  - Type (enum): BugReport, FeatureRequest, GeneralFeedback, Question
  - Status (enum): New, InProgress, Completed, Rejected
  - Priority (enum): Low, Medium, High, Critical
  - AdminNotes, UserId, PublicResponse
  - AssignedToAdminId, LastUpdated, EstimatedCompletionDate
  - IsPublished, Category, BrowserInfo, DeviceInfo
- Relationships: 
  - Many-to-one with User (optional)
  - Many-to-one with AdminUser via AssignedToAdminId
- Enhancements:
  - Priority tracking for feedback importance
  - Admin assignment for ownership and accountability
  - Timeline tracking with LastUpdated timestamps
  - Scheduling via EstimatedCompletionDate
  - Public/private response management
  - Publication status tracking
  - Device metadata collection for troubleshooting

#### HelpArticle
- Documentation articles in the help center
- Properties: Id, Title, ShortDescription, Content, HelpCategoryId, Tags, CreatedDate, LastModifiedDate, Version, DisplayOrder, IsFeatured, ViewCount, Slug, HasVideo, VideoUrl, IsPrintable
- Relationships: 
  - Many-to-one with HelpCategory
  - Many-to-many with self (RelatedArticles)

#### HelpCategory
- Categories for organizing help documentation
- Properties: Id, Name, Description, DisplayOrder, IconClass, Slug, ParentCategoryId
- Relationships:
  - One-to-many with HelpArticles
  - Self-referencing parent-child relationship (ParentCategory/ChildCategories)

#### GlossaryTerm
- Workout and fitness terminology definitions
- Properties: Id, Term, Definition, Example, CreatedDate, LastModifiedDate, Slug, Category
- Relationships: Many-to-many with self (RelatedTerms)

#### ShareToken
- Secure token for sharing workout data with others
- Properties: 
  - Id, Token, CreatedAt, ExpiresAt, IsActive
  - AccessCount, MaxAccessCount
  - AllowSessionAccess, AllowReportAccess, AllowCalculatorAccess
  - UserId, WorkoutSessionId (optional)
  - Name, Description
- Relationships:
  - Many-to-one with User (creator)
  - Many-to-one with WorkoutSession (optional)
- Features:
  - Time-limited access via expiration date
  - Usage tracking with access count
  - Optional maximum usage limit (MaxAccessCount)
  - Granular permissions for feature access
  - Computed properties for validity checks (IsValid, DaysUntilExpiration)
  - Session-specific or account-wide sharing
  - User-defined name and description for organization

#### LoginHistory
- Tracks user login activity for security and auditing purposes
- Properties: Id, IdentityUserId, LoginTime, IpAddress, UserAgent, IsSuccessful, DeviceType, Platform
- Captures both successful and failed login attempts
- Used for user activity monitoring and security analysis

#### AppVersion
- Tracks application version information
- Properties: 
  - VersionId, Major, Minor, Patch, BuildNumber
  - ReleaseDate, Description, GitCommitHash, IsCurrent
  - ReleaseNotes (detailed changelog information)
- Used for: 
  - Version tracking in application logs with Serilog enrichment
  - Displaying current version information in UI footer
  - Maintaining complete version history with visual timeline
  - Docker image tagging for Kubernetes deployments
  - Automated version capture during deployment process
  - Release notes documentation for each version
  - Rollback capability to previous versions
- Features:
  - Admin interface for version management
  - Current version indicator in application interface
  - Timeline visualization of version history
  - Git commit tracking with hash references
  - Detailed release notes with formatting support

#### Alert
- Represents system-generated alerts for various conditions
- Properties:
  - Id, Title, Message, Severity (Critical, Warning, Info)
  - CreatedAt, UpdatedAt, ResolvedAt, Status (Active, Acknowledged, Resolved)
  - Source, Category, AcknowledgedById, ResolvedById, RelatedEntityId, RelatedEntityType
  - AutoResolveAfter, IsAutoResolvable, ResolutionNotes, NotificationSent
- Relationships:
  - Many-to-one with User (acknowledger and resolver)
  - Polymorphic relationship to related entity (optional)
- Features:
  - Severity-based categorization
  - Status tracking workflow (Active → Acknowledged → Resolved)
  - Automatic resolution capability for transient issues
  - Audit fields tracking who acknowledged and resolved
  - Source identification for troubleshooting

#### AlertThreshold
- Defines conditions for generating alerts based on metrics
- Properties:
  - Id, Name, Description, MetricName, Comparison (GreaterThan, LessThan, Equal)
  - ThresholdValue, Severity, Enabled, Category
  - ConsecutiveOccurrences, EvaluationFrequency, LastEvaluated
  - CreatedBy, CreatedAt, UpdatedAt
- Relationships:
  - Many-to-one with User (creator)
  - One-to-many with Alerts
- Features:
  - Configurable thresholds for different metrics
  - Multiple comparison operators
  - Severity level assignment
  - Consecutive occurrence detection to prevent flapping
  - Configurable evaluation frequency

#### Notification
- Tracks alert notifications sent to users
- Properties:
  - Id, AlertId, UserId, Channel (Email, InApp, SMS)
  - SentAt, DeliveredAt, ReadAt, Status (Queued, Sent, Delivered, Read, Failed)
  - FailureReason, RetryCount, Content
- Relationships:
  - Many-to-one with Alert
  - Many-to-one with User (recipient)
- Features:
  - Multi-channel notification support
  - Delivery status tracking
  - Retry mechanism for failed notifications
  - Read receipt functionality
  - Historical record of notification content

#### NotificationPreference
- User preferences for alert notifications
- Properties:
  - Id, UserId, AlertCategory, Severity
  - EmailEnabled, InAppEnabled, SMSEnabled
  - QuietHoursStart, QuietHoursEnd, QuietHoursEnabled
  - CreatedAt, UpdatedAt
- Relationships:
  - Many-to-one with User
- Features:
  - Channel-specific opt-in/opt-out
  - Severity-based filtering
  - Category-based filtering
  - Quiet hours configuration for non-critical alerts
  - Default preferences with user overrides

#### WorkoutTemplate
- Template for creating reusable workout plans.
- Properties:
  - WorkoutTemplateId, Name, Description, CreatedDate, LastModifiedDate.
  - IsPublic, Category, Tags, UserId.
- Relationships:
  - Many-to-one with User (creator).
  - One-to-many with WorkoutTemplateExercise.
  - One-to-many with WorkoutSessions (workouts created from this template)

#### WorkoutTemplateExercise
- Represents an exercise within a workout template.
- Properties:
  - WorkoutTemplateExerciseId, WorkoutTemplateId, ExerciseTypeId.
  - SequenceNum, Notes.
- Relationships:
  - Many-to-one with WorkoutTemplate.
  - Many-to-one with ExerciseType.
  - One-to-many with WorkoutTemplateSet.

#### WorkoutTemplateSet
- Represents default set values for a template exercise.
- Properties:
  - WorkoutTemplateSetId, WorkoutTemplateExerciseId, SettypeId.
  - DefaultReps, DefaultWeight, SequenceNum, Notes, Description.
- Relationships:
  - Many-to-one with WorkoutTemplateExercise.
  - Many-to-one with SetType.

#### CoachNote
- Represents coaching notes attached to workouts for feedback and observations
- Properties:
  - Id, Content (renamed from noteText), CreatedDate (renamed from date)
  - CoachClientRelationshipId, IsVisibleToClient, Category
  - UpdatedDate (new field for tracking modifications)

#### BlogPost
- Represents a blog article with fitness-related content
- Properties:
  - Id, Title, Slug, Content, Summary, ImageUrl
  - AuthorId, Published, PublishedOn, CreatedOn, UpdatedOn, ViewCount
- Relationships:
  - Many-to-one with User (Author)
  - Many-to-many with BlogCategory through BlogPostCategory
  - Many-to-many with BlogTag through BlogPostTag

#### BlogCategory
- Represents a category for organizing blog posts
- Properties:
  - Id, Name, Slug, Description
- Relationships:
  - Many-to-many with BlogPost through BlogPostCategory

#### BlogTag
- Represents a tag for labeling and filtering blog posts
- Properties:
  - Id, Name, Slug
- Relationships:
  - Many-to-many with BlogPost through BlogPostTag

#### BlogPostCategory
- Junction entity for many-to-many relationship between BlogPost and BlogCategory
- Properties:
  - BlogPostId, BlogCategoryId
- Relationships:
  - Many-to-one with BlogPost
  - Many-to-one with BlogCategory

#### BlogPostTag
- Junction entity for many-to-many relationship between BlogPost and BlogTag
- Properties:
  - BlogPostId, BlogTagId
- Relationships:
  - Many-to-one with BlogPost
  - Many-to-one with BlogTag
- Relationships:
  - Many-to-one with CoachClientRelationship
- Features:
  - Coaching feedback mechanism with categorization
  - Date tracking for chronological organization
  - Privacy control with visibility settings for clients
  - Association with specific coach-client relationships
  - Category-based organization for different note types
  - Modification tracking with update timestamps

#### WorkoutSchedule
- Represents scheduled workouts for clients with support for recurring patterns
- Properties:
  - WorkoutScheduleId, Name, Description
  - TemplateAssignmentId, ClientUserId, CoachUserId
  - StartDate, EndDate, ScheduledDateTime
  - IsRecurring, RecurrencePattern (daily, weekly, biweekly, monthly)
  - RecurrenceDayOfWeek, RecurrenceDayOfMonth
  - SendReminder, ReminderHoursBefore, LastReminderSent
  - IsActive
- Relationships:
  - Many-to-one with TemplateAssignment (optional)
  - Many-to-one with User (client)
  - Many-to-one with User (coach)
- Features:
  - One-time workout scheduling
  - Recurring workout patterns (daily, weekly, bi-weekly, monthly)
  - Customizable reminder settings
  - Calendar-based interface for visualization
  - Active status tracking

#### ClientGroup
- Organizes coach clients into logical groups
- Properties:
  - Id, Name, Description, CoachId
  - CreatedDate, LastModifiedDate, ColorCode
- Relationships:
  - Many-to-one with User (coach)
  - One-to-many with CoachClientRelationship
- Features:
  - Visual organization with custom color codes
  - Grouping by training type, goals, or other criteria
  - Improved client management efficiency
  - Batch operations on grouped clients

#### CoachClientRelationship
- Manages the coaching relationship between users
- Properties:
  - Id, CoachId, ClientId, ClientGroupId (optional)
  - Status (enum): Pending, Active, Inactive, Declined, Terminated
  - CreatedDate, LastModifiedDate
  - StartDate, EndDate (optional for time-bound relationships)
  - InvitationToken, InvitationExpiryDate
- Relationships:
  - Many-to-one with User (coach)
  - Many-to-one with User (client)
  - Many-to-one with ClientGroup (optional)
  - One-to-one with CoachClientPermission
  - One-to-many with CoachNote
- Features:
  - Invitation workflow with secure tokens
  - Relationship lifecycle management
  - Time-bound coaching relationships
  - Invitation expiration for security
  - Group-based organization

#### CoachClientPermission
- Defines permission settings for coach-client relationships
- Properties:
  - Id, CoachClientRelationshipId
  - CanViewWorkouts, CanCreateWorkouts, CanModifyWorkouts, CanEditWorkouts, CanDeleteWorkouts
  - CanViewPersonalInfo, CanCreateGoals, CanViewReports
  - CanMessage, CanCreateTemplates, CanAssignTemplates
  - LastModifiedDate
- Relationships:
  - One-to-one with CoachClientRelationship
- Features:
  - Granular permission controls for coaching activities
  - Audit trail with modification timestamps

#### ClientGoal
- Represents fitness goals for users, can be created by coaches or users themselves
- Properties:
  - Id, Description, UserId, CoachClientRelationshipId (optional)
  - Category (enum): Uses GoalCategory enum (Strength, Cardio, Weight, etc.)
  - CustomCategory (for user-defined categories)
  - CreatedDate, TargetDate, CompletedDate
  - IsCoachCreated, IsVisibleToCoach
  - MeasurementType, MeasurementUnit
  - StartValue, CurrentValue, TargetValue
  - Notes, IsActive, IsCompleted
  - TrackingFrequency, LastProgressUpdate, CompletionCriteria
- Relationships:
  - Many-to-one with User (goal owner)
  - Many-to-one with CoachClientRelationship (optional, for coach-assigned goals)
  - One-to-many with GoalMilestone (progress tracking)
- Features:
  - Support for both coach-assigned and user-created goals
  - Standardized goal categories with custom category option
  - Automated progress tracking based on measurements or time
  - Goal visibility controls between coaches and users
  - Tracking mechanisms for various goal types (weight, reps, duration)
  - Customizable completion criteria
  - Progress calculation as percentage towards target

#### GoalCategory
- Enum defining standard categories for fitness goals
- Values:
  - Strength: Goals focused on increasing strength (e.g., increase max squat weight)
  - Cardio: Goals focused on cardiovascular fitness (e.g., complete 5K run)
  - Hypertrophy: Goals focused on muscle building and size
  - Weight: Goals focused on weight management (lose/gain weight)
  - Technique: Goals focused on exercise technique improvement
  - Mobility: Goals focused on flexibility and joint mobility
  - Endurance: Goals focused on stamina and endurance
  - Performance: Goals focused on athletic performance metrics
  - Wellness: Goals focused on overall health and wellbeing
  - Other: Custom goals that don't fit standard categories
- Features:
  - Display name attributes for UI presentation
  - Standardized categorization for consistent reporting
  - Support for filtering and grouping goals by category

#### GoalMilestone
- Records progress points toward a goal
- Properties:
  - Id, GoalId, Value, Date
  - Notes, ProgressPercentage
  - IsAutomaticUpdate, Source
  - ReferenceId (for linking to related entities)
- Relationships:
  - Many-to-one with ClientGoal
- Features:
  - Timestamped tracking of progress
  - Support for automatic and manual progress updates
  - Notes for contextualizing progress
  - Source tracking for data provenance

#### WhitelistedIp
- IP addresses allowed for specific operations
- Properties:
  - Id, IpAddress, Description
  - IsActive, CreatedAt, CreatedById
  - LastUsedAt
- Relationships:
  - Many-to-one with User (creator)
- Features:
  - Used for securing admin operations
  - Access logging for security auditing
  - Description for documenting purpose

### Components and Modules

#### Pages
- **Sessions/Details.cshtml**: 
  - Main workout session details page
  - Allows viewing all exercise and set data for a workout
  - Supports editing workout details
  - Provides CRUD operations for sets (add, edit, delete, clone)
  - Supports reordering of sets
  - Allows deletion of entire exercises including associated sets
  - Interactive accordion UI for exercises
  - Mobile responsive design

### Known Technical Debt

#### Entity Framework Model Relationship Issues
- ~~**ForeignKey Attribute Conflicts**: Navigation properties 'WorkoutSchedule.LastGeneratedSession' and 'WorkoutSession.Schedule' have ForeignKey attributes on both sides, causing Entity Framework to create two separate relationships.~~ **FIXED**
- ~~**Undefined Foreign Key Properties**: Multiple relationships between 'CoachClientRelationship' and 'AppUser' lack properly configured foreign key properties, resulting in shadow property creation.~~ **FIXED & MONITORED (2025-05-10)**
- ~~**Global Query Filter Conflicts**: Several entities have global query filters defined but are the required end of relationships, potentially leading to missing data in query results:~~ **FIXED**
  - ~~CoachClientRelationship → CoachClientPermission~~ **FIXED**
  - ~~WorkoutFeedback → ExerciseFeedback~~ **FIXED**
  - ~~ProgressionRule → ProgressionHistory~~ **FIXED**
  - ~~WorkoutSession → WorkoutExercise~~ **FIXED**
- ~~**Shadow Foreign Key Property Conflicts**: Several shadow foreign key properties were created due to conflicts with existing properties:~~ **FIXED**
  - ~~CoachClientRelationship.AppUserId1~~ **FIXED**
  - ~~WorkoutFeedback.WorkoutSessionId1~~ **FIXED**
  - ~~WorkoutSchedule.TemplateAssignmentId1~~ **FIXED**
  - ~~WorkoutExercise.ExerciseTypeId1~~ **FIXED**
- ~~**Query Execution Issues**: Some queries use First/FirstOrDefault without OrderBy clauses, potentially resulting in unpredictable data retrieval.~~ **FIXED**

#### Database Schema Migration Issues
- **Missing Columns**: Some columns referenced in code don't exist in the database schema:
  - Notes column in WorkoutSessions table (now exists)
  - Name column in WorkoutExercises table (now exists)
- **Missing Tables**: ExerciseTypes table doesn't exist in the database but is referenced in code.

### Entity Framework Core Best Practices
- **Relationship Configuration**: The project now follows a set of documented best practices for configuring entity relationships:
  - Explicit foreign key configurations to avoid shadow properties
  - Matching query filters on both sides of required relationships
  - Proper one-to-one relationship configuration
  - Careful cascade delete behavior specification
  - OrderBy clauses with First/FirstOrDefault operators
- **Documentation**: Comprehensive relationship configuration best practices are documented in `/Documentation/migrations/RelationshipBestPractices.md`

## 2025-05-09
- Content Security Policy (CSP) updated: Added https://static.cloudflareinsights.com to script-src to allow Cloudflare Insights beacon script.

## 2025-05-10
- Obsolete MVC view Views/Shared/Session.cshtml removed (2025-05-10)
- All controllers, Razor Pages, and services now reference WorkoutSession exclusively
- SessionExport class marked as legacy (see code comments)
- No remaining code or documentation references to legacy Session entity
- See logs/remove_obsolete_session_view_20250510.txt for details
- 2025-05-10: Removed all hardcoded and environment-specific configuration from source code. See TECHNICAL_DEBT.md for details.

## 2025-05-10: UI Consistency & Output Caching
- Reviewed all Razor Pages for Bootstrap 5 and output caching consistency.
- Reports/Index now uses [OutputCache] with a 5-minute duration and policy.
- Bootstrap 5 usage and layout confirmed consistent in Reports/Index and shared layouts.

## 2025-05-12: Hangfire Background Job Fixes
- Fixed issue with Hangfire scheduled job creating duplicate workout sessions for edited/completed workouts.
- Enhanced workout session duplicate detection in `ScheduledWorkoutProcessorService.cs` to handle all workout statuses.
- Improved logging in workout scheduling services to provide better diagnostic information.
- Verified all scheduled job registration and processing workflows.

## 2025-05-17: Blog Feature Implementation
- Added blog system for sharing workout tips, nutrition advice, and fitness knowledge
- Created database models for blog posts, categories, and tags
- Implemented admin area pages for blog management
- Added TinyMCE rich text editor integration for content creation
- Created image upload functionality for blog posts
- Implemented SEO-friendly URL routing with slugs
- Added responsive public-facing blog pages
- Integrated blog navigation into the main menu

## 2025-06-11: Google Analytics Integration
- Added Google Analytics 4 tracking code to the main layout template
- Configured to track user behavior and site usage across all pages
- Integrated with existing monitoring and health check system
- Provides insights on user engagement, retention, and site performance
- Tracking ID: G-FPNTMYLWPW
- Implementation leverages standard gtag.js script
- Compliant with privacy policy requirements

## 2025-06-11: Google Analytics Layout Coverage Fix
- Fixed Google Analytics tracking not being detected on www.workouttracker.online
- Added Google Analytics tracking code to all layout templates:
  - `/Pages/Shared/_Layout.cshtml` - Main Razor Pages layout
  - `/Pages/Shared/_SharedLayout.cshtml` - Shared/public pages layout
  - `/Areas/Coach/Pages/Shared/_Layout.cshtml` - Coach portal layout
  - `/Views/Shared/_SharedLayout.cshtml` - MVC views shared layout
- Google Analytics code was previously only in `/Views/Shared/_Layout.cshtml`
- All layouts now include consistent G-FPNTMYLWPW tracking implementation
- Verified build compilation succeeds with changes
- Analytics should now be detected across all site sections

## 2025-07-15: MessagePack Security Update
- Updated MessagePack from 2.5.187 to 3.0.300 to address security vulnerabilities
- Updated MessagePack.Annotations from 2.5.187 to 3.0.300 for compatibility
- Added explicit package references to ensure consistent versions across all dependencies
- Verified build compilation succeeds with updated packages
- Confirmed no vulnerable packages remain in the project dependencies

## 2025-07-15: Comprehensive Package Updates
- Updated MessagePack from 3.0.300 to 3.1.4 (latest version)
- Updated MessagePack.Annotations from 3.0.300 to 3.1.4 (latest version)
- Updated all Microsoft ASP.NET Core packages from 9.0.6 to 9.0.7:
  - Microsoft.AspNetCore.DataProtection.EntityFrameworkCore
  - Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
  - Microsoft.AspNetCore.Identity.EntityFrameworkCore
  - Microsoft.AspNetCore.Identity.UI
  - Microsoft.AspNetCore.OutputCaching.StackExchangeRedis
  - Microsoft.AspNetCore.SignalR.StackExchangeRedis
- Updated all Entity Framework Core packages from 9.0.6 to 9.0.7:
  - Microsoft.EntityFrameworkCore.Design
  - Microsoft.EntityFrameworkCore.SQLite
  - Microsoft.EntityFrameworkCore.SqlServer
  - Microsoft.EntityFrameworkCore.Tools
- Updated Microsoft Extensions packages from 9.0.6 to 9.0.7:
  - Microsoft.Extensions.Caching.SqlServer
  - Microsoft.Extensions.Caching.StackExchangeRedis
  - Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore
- Updated Polly from 8.6.1 to 8.6.2
- Updated QuestPDF from 2025.5.1 to 2025.7.0
- All packages successfully updated with no breaking changes
- Build compilation verified successful
- No security vulnerabilities remain in project dependencies
- OpenTelemetry beta packages left unchanged as latest versions not available at sources

## 2025-07-15: OpenTelemetry Package Analysis and Updates
- Investigated availability of GA (stable) versions for OpenTelemetry instrumentation packages
- **Finding**: Core OpenTelemetry packages (1.12.0) have stable GA versions available
- **Finding**: Instrumentation packages remain in beta/pre-release phase:
  - OpenTelemetry.Instrumentation.EntityFrameworkCore: Only beta versions available (latest: 1.12.0-beta.2)
  - OpenTelemetry.Instrumentation.SqlClient: Only beta versions available (latest: 1.12.0-beta.2) 
  - OpenTelemetry.Instrumentation.StackExchangeRedis: Only beta versions available (latest: 1.12.0-beta.1)
- **Action Taken**: Updated OpenTelemetry.Instrumentation.SqlClient from 1.12.0-beta.1 to 1.12.0-beta.2
- **Recommendation**: Continue monitoring for stable releases of instrumentation packages
- **Status**: All OpenTelemetry packages are at their latest available versions
- **Note**: This is common in the OpenTelemetry ecosystem - core libraries stabilize first, instrumentation packages follow later
- Build compilation successful with all current package versions

## 2025-07-15: Read-Only Root Filesystem Security Enhancement
- **Security Improvement**: Enabled read-only root filesystem for all containers
- **Changes Made**:
  - Updated `readOnlyRootFilesystem: true` in both main deployment and Hangfire worker
  - Added in-memory volume mounts for writable directories:
    - `/app/logs` - 100Mi memory limit for application logs
    - `/app/temp` - 200Mi memory limit for temporary files  
    - `/tmp` - 100Mi memory limit for system temp files
  - Configured `TMPDIR=/app/temp` environment variable
- **Security Benefits**:
  - Prevents malicious file system modifications
  - Reduces attack surface for container breakout attempts
  - Ensures temporary data is cleared on container restart
  - Memory-backed storage prevents persistence of sensitive data
- **Impact**: Enhanced container security posture while maintaining full application functionality

## 2025-07-15: Zen Firewall Enhancement with User Context
- **Security Improvement**: Enhanced Aikido Zen Firewall with proper user context tracking
- **Changes Made**:
  - Added user context middleware before `UseZenFirewall()` to properly associate security events with authenticated users
  - Implemented automatic user ID and name extraction from authentication context
  - Configured proper middleware ordering: `UseRouting()` → User context setup → `UseZenFirewall()`
  - Added `using Aikido.Zen;` namespace for `Zen.SetUser()` functionality
  - Integrated with existing `AppUserExtensions.GetUserId()` extension method for consistent user identification
- **Security Benefits**:
  - Enhanced threat detection and attribution by linking security events to specific user accounts
  - Improved security monitoring and incident response capabilities
  - Better audit trails for security investigations
  - More accurate threat intelligence and behavior analysis
- **Implementation Details**:
  - User context is set only for authenticated users (`IsAuthenticated == true`)
  - Falls back to "Anonymous" for display name when username is null
  - Middleware executes after routing but before authorization for optimal security coverage
- **Impact**: Significantly improved security monitoring and threat detection capabilities while maintaining zero impact on application performance
