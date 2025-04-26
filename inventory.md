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
- Responsive Mobile-First UI
- Admin Dashboard
- Metric Collection
- Alert System
- Volume Calculation System
- Calorie Estimation System

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
- Output Caching with Redis Backplane

### Application Components
- User Management
- Workout Management
- Exercise Library
- Authentication System
- Sharing System with Secure Tokens
- Reporting Engine
- Admin Area
- Import/Export System
- Scheduling System
- Health Monitoring
- Alerting System
- Responsive Design System
- Volume Calculation System
- Calorie Estimation System

### Data Models
- User
- WorkoutSession
- Exercise
- Set
- Rep
- ExerciseType
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

#### User
- Primary entity representing application users
- Links to ASP.NET Core Identity system
- Properties: UserId, Name, IdentityUserId
- Relationships: 
  - One-to-many with Sessions
  - One-to-many with Feedback

#### Session
- Represents a workout session
- Properties: SessionId, Name, datetime, UserId
- Relationships: 
  - Many-to-one with User
  - One-to-many with Sets

#### Set
- Represents a group of repetitions for an exercise
- Properties: SetId, Description, Notes, NumberReps, Weight, ExerciseTypeId, SettypeId, SessionId
- Relationships:
  - Many-to-one with Session
  - Many-to-one with ExerciseType
  - Many-to-one with SetType
  - One-to-many with Reps

#### Rep
- Represents a single repetition within a set
- Properties: RepId, weight, repnumber, success, SetsSetId
- Relationships: Many-to-one with Set

#### ExerciseType
- Catalog of available exercises
- Properties: ExerciseTypeId, Name
- Relationships: One-to-many with Sets

#### SetType
- Categorizes sets (e.g., warm-up, normal, drop set)
- Properties: SettypeId, Name, Description
- Relationships: One-to-many with Sets

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
  - UserId, SessionId (optional)
  - Name, Description
- Relationships:
  - Many-to-one with User (creator)
  - Many-to-one with Session (optional, for session-specific sharing)
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
- Properties: VersionId, Major, Minor, Patch, BuildNumber, ReleaseDate, Description, GitCommitHash, IsCurrent, ReleaseNotes
- Used for: 
  - Version tracking in logs
  - Displaying current version information
  - Maintaining version history
  - Docker image tagging
  - Kubernetes deployment management

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
- Template for creating reusable workout plans
- Properties:
  - WorkoutTemplateId, Name, Description, CreatedDate, LastModifiedDate
  - IsPublic, Category, Tags, UserId
- Relationships:
  - Many-to-one with User (creator)
  - One-to-many with WorkoutTemplateExercise

#### WorkoutTemplateExercise
- Represents an exercise within a workout template
- Properties:
  - WorkoutTemplateExerciseId, WorkoutTemplateId, ExerciseTypeId
  - SequenceNum, Notes
- Relationships:
  - Many-to-one with WorkoutTemplate
  - Many-to-one with ExerciseType
  - One-to-many with WorkoutTemplateSet

#### WorkoutTemplateSet
- Represents default set values for a template exercise
- Properties:
  - WorkoutTemplateSetId, WorkoutTemplateExerciseId, SettypeId
  - DefaultReps, DefaultWeight, SequenceNum, Notes, Description
- Relationships:
  - Many-to-one with WorkoutTemplateExercise
  - Many-to-one with SetType

### Integrations
- Exercise API (API Ninjas)
- Email Service
- Export to CSV/JSON
- Import from TrainAI format
- Prometheus Metrics
- Health Checks

### Responsive Design Components

| Component | Purpose |
|-----------|---------|
| `responsive.css` | Main responsive stylesheet with mobile-first media queries |
| `responsive-tables.css` | Specialized component for mobile-friendly tables |
| `responsive-forms.css` | Mobile-optimized form components and layouts |
| `responsive-data.css` | Special layouts for data-heavy pages on mobile |
| `responsive-layouts.css` | Additional layout components for mobile adaptation |
| `responsive-tables.js` | JavaScript utility for table transformations |
| `mobile-navigation.js` | JavaScript for mobile navigation gestures and haptic feedback |
| `mobile-bottom-navigation` | Fixed bottom navigation bar component for critical actions |
| `mobile-context-navigation` | Alternative to traditional breadcrumbs for mobile |
| `mobile-swipe-gestures` | Touch gestures for common actions like edit and delete |
| `mobile-pull-to-refresh` | Pull-to-refresh functionality for data lists |
| `container-mobile-friendly` | Container class with optimized mobile padding/margins |
| `Enhanced Viewport Meta Tags` | Complete viewport configuration for mobile devices |

### Caching Components

| Component | Purpose |
|-----------|---------|
| `OutputCache Middleware` | ASP.NET Core middleware for caching page output to improve performance |
| `Redis OutputCache Provider` | Distributed cache provider for output caching using Redis in production |
| `Memory OutputCache Provider` | Local memory-based cache for development environments |
| `Cache Policies` | Specialized caching strategies for different content types |
| `Cache Tags` | Content identification for targeted cache invalidation |
| `CacheVaryByQuery/RouteValue` | Cache variations based on query parameters or route values |

### Caching Policies

| Policy | Duration | Purpose | Variations |
|--------|----------|---------|------------|
| `StaticContent` | 12 hours | Fully static pages with no dynamic content | None |
| `StaticContentWithId` | 12 hours | Static content that varies by route ID | ID route value |
| `HelpContent` | 24 hours | Help articles and categories | ID and category route values |
| `GlossaryContent` | 24 hours | Glossary pages with terminology definitions | None |
| `ExerciseLibrary` | 6 hours | Exercise library with periodic updates | Category and search query parameters |
| `SharedWorkoutReports` | 6 hours | Shared reports that vary by token and period | Token and period query parameters |
| `SharedWorkout` | 3 hours | Shared workout pages with token-based access | Token query parameter |

### Calculation and Metrics Components

#### VolumeCalculationService
- Provides standardized volume calculation for workout sessions and sets
- Features:
  - Multiple calculation methods (weight × reps, weight × reps × sets)
  - Support for bodyweight exercise volume calculation
  - Time-under-tension calculations for isometric exercises
  - Set-specific and session-wide volume metrics
  - Relative volume calculations (per muscle group/exercise type)
  - Caching mechanism for performance optimization
  - Unit conversion support (kg/lb)
  - Progressive overload tracking
  - Volume comparison with historical data
  - Visual representations with color-coded indicators

#### CalorieCalculationService
- Estimates calorie expenditure for workout sessions and individual sets
- Features:
  - Configurable MET (Metabolic Equivalent of Task) values for different exercise types
  - Multiple calculation methods (MET-based, heart rate-based when available)
  - User-specific calculations based on weight and other metrics
  - Duration-sensitive energy expenditure estimates
  - Intensity-adjusted calculations
  - Caching for optimized performance
  - Unit selection (kcal/kJ)
  - Integration with workout history for trend analysis
  - Visual indicators for calorie metrics
  - API endpoints for data access

#### Volume and Calorie Display Components
- UI components for presenting volume and calorie metrics
- Features:
  - Session summary displays with volume and calorie totals
  - Set-level detailed breakdowns
  - Visual progress indicators
  - Comparative metrics with previous sessions
  - Mobile-responsive designs
  - Interactive charts for data visualization
  - Unit toggle controls
  - Tooltip explanations for calculation methodology
  - API integration for data refresh

#### Volume and Calorie APIs
- REST endpoints for programmatic access to volume and calorie data
- Features:
  - Session metrics endpoints
  - Set-specific metrics
  - Filtering capabilities by date, exercise type, and metrics range
  - Aggregation endpoints for trend analysis
  - Batch calculation for performance optimization
  - Comprehensive documentation
  - Consistent error handling
  - Authentication and authorization with proper sharing controls

## Project Structure

### Core Areas

| Component | Purpose |
|-----------|---------|
| `/Data` | Contains database context and configuration |
| `/Models` | Domain models representing the core entities |
| `/Pages` | Razor Pages UI components |
| `/Pages/Calculator` | Calculator tools including One Rep Max Calculator |
| `/Pages/Feedback` | Feedback submission and management system |
| `/Pages/Help` | Help center with documentation, FAQs, glossary, and tutorials |
| `/Pages/Version` | Version history display and management |
| `/Pages/Account` | User account management including token sharing |
| `/Pages/Shared` | Workout sharing view components |
| `/Pages/Sessions` | Workout session management |
| `/Pages/DataPortability` | Data import/export functionality |
| `/Pages/HangfireDiagnostics` | Hangfire background job system diagnostics and repair tools |
| `/Pages/Api/JobStatus` | Razor Pages API for background job status monitoring |
| `/Pages/API/ShareToken` | Razor Pages API endpoints for token management and validation |
| `/wwwroot/css` | CSS stylesheets for application styling |
| `/wwwroot/css/responsive.css` | Mobile-first responsive design CSS components |
| `/wwwroot/css/responsive-tables.css` | Mobile-optimized table layouts |
| `/wwwroot/css/responsive-forms.css` | Touch-friendly form optimizations |
| `/wwwroot/css/responsive-data.css` | Mobile layouts for data-heavy pages |
| `/wwwroot/css/responsive-layouts.css` | General responsive layout utilities |
| `/wwwroot/js/responsive-tables.js` | JavaScript utilities for enhancing tables on mobile |

### Architecture Migration

The application has been migrated from MVC to Razor Pages architecture with the following key changes:

1. **Page-based Structure**: Replaced controller actions with page models for better encapsulation
2. **Handler Methods**: Implemented OnGet, OnPost, OnPostAsync patterns instead of controller actions
3. **Folder Organization**: Organized pages by feature area rather than controller/action patterns
4. **API Controllers**: Maintained some API controllers for REST endpoints and backward compatibility
5. **View Components**: Leveraged ViewComponents for reusable UI elements across pages
6. **Razor Pages API Endpoints**: Added API endpoints using Razor Pages handlers with JSON results

### API Surface

While the primary UI is built with Razor Pages, the application includes API functionality through:

1. **Data Sharing**: REST endpoints for secure token-based data sharing
2. **Background Jobs**: Razor Pages API endpoints for job status monitoring 
3. **Real-time Updates**: Endpoints supporting SignalR communication

### Configuration Files

| File | Purpose |
|------|---------|
| `appsettings.json` | Application configuration |
| `appsettings.Development.json` | Development-specific settings |
| `Program.cs` | Application startup and service configuration |
| `WorkoutTrackerWeb.csproj` | Project file with dependencies |
| `.NET User Secrets` | Development secrets storage (connection strings, sensitive configuration) |
| `hangfire_schema.sql` | SQL schema for Hangfire background processing |

### Deployment Files

| File | Purpose |
|------|---------|
| `Dockerfile` | Container definition for Docker deployments |
| `docker-compose.yml` | Multi-container definition for development |
| `docker-compose.debug.yml` | Container configuration for debugging |
| `/k8s/*` | Kubernetes deployment manifests |
| `/k8s/secrets.yaml` | Kubernetes secrets for production deployments |
| `/k8s/deployment.yaml` | Kubernetes deployment with health check configurations |
| `/k8s/redis.yaml` | Redis deployment for SignalR backplane |
| `/k8s/mapping.yaml` | Ambassador API gateway configuration for routing requests |
| `/k8s/ambassador.yaml` | Ambassador edge stack installation manifest |
| `/k8s/redis-helm.yaml` | Helm chart values for Redis installation |
| `/k8s/deploy-redis-helm.sh` | Script for deploying Redis using Helm |

### Multi-Container Architecture Components

| Component | Purpose |
|-----------|---------|
| Web Application | ASP.NET Core container instances running the main application |
| Redis | Provides distributed caching and SignalR backplane functionality |
| SQL Server | Persistent data storage with optimized connection pooling |
| Ambassador API Gateway | Edge routing with sticky sessions for SignalR connections |
| Health Checks | Container orchestration integration including Redis connectivity |

### Resilience Components

| Component | Purpose |
|-----------|---------|
| `DbConnectionResilienceMiddleware` | Handles transient SQL Server connection issues |
| `RedisResilienceMiddleware` | Manages Redis connection failures with circuit breaker pattern |
| `DatabaseConnectionPoolHealthCheck` | Monitors database connection pool health |
| Connection Pooling Configuration | Settings in `appsettings.json` for optimized database connections |
| Retry Policies | Polly-based retry mechanisms for external service calls |
| Circuit Breakers | Prevents cascading failures by failing fast when services are unavailable |
| Health Checks | Comprehensive monitoring of all system dependencies |
| Redis Backplane | Ensures resilient SignalR communications across instances |

### Responsive Design Implementation

| Feature | Implementation |
|---------|---------------|
| Mobile-First CSS | Starts with mobile layouts and progressively enhances for larger screens |
| Mobile Navigation | Touch-friendly navigation with appropriate tap target sizes |
| Responsive Tables | Tables transform into stacked layouts on mobile via CSS and JavaScript |
| Mobile Form Controls | Specialized form layouts optimized for touch input on smaller screens |
| Viewport Configuration | Enhanced meta tags for better mobile rendering across all templates |
| Mobile-specific Layouts | Specialized layouts for data-heavy pages on mobile devices |
| Touch Optimizations | Larger touch targets and appropriate spacing for touch interfaces |
| Container Adaptations | Mobile-friendly container classes with optimized spacing |
| Responsive Utilities | Helper classes for responsive visibility and alignment |
| JavaScript Enhancements | Automatic table transformations for mobile display |
| Admin Responsive Design | Mobile-optimized admin panels with collapsible sidebars |
| Media Query Organization | Standardized breakpoints across all components |

## Data Model

### Core Entities

#### User
- Primary entity representing application users
- Links to ASP.NET Core Identity system
- Properties: UserId, Name, IdentityUserId
- Relationships: 
  - One-to-many with Sessions
  - One-to-many with Feedback

#### Session
- Represents a workout session
- Properties: SessionId, Name, datetime, UserId
- Relationships: 
  - Many-to-one with User
  - One-to-many with Sets

#### Set
- Represents a group of repetitions for an exercise
- Properties: SetId, Description, Notes, NumberReps, Weight, ExerciseTypeId, SettypeId, SessionId
- Relationships:
  - Many-to-one with Session
  - Many-to-one with ExerciseType
  - Many-to-one with SetType
  - One-to-many with Reps

#### Rep
- Represents a single repetition within a set
- Properties: RepId, weight, repnumber, success, SetsSetId
- Relationships: Many-to-one with Set

#### ExerciseType
- Catalog of available exercises
- Properties: ExerciseTypeId, Name
- Relationships: One-to-many with Sets

#### SetType
- Categorizes sets (e.g., warm-up, normal, drop set)
- Properties: SettypeId, Name, Description
- Relationships: One-to-many with Sets

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
  - UserId, SessionId (optional)
  - Name, Description
- Relationships:
  - Many-to-one with User (creator)
  - Many-to-one with Session (optional, for session-specific sharing)
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
- Properties: VersionId, Major, Minor, Patch, BuildNumber, ReleaseDate, Description, GitCommitHash, IsCurrent, ReleaseNotes
- Used for: 
  - Version tracking in logs
  - Displaying current version information
  - Maintaining version history
  - Docker image tagging
  - Kubernetes deployment management

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
- Template for creating reusable workout plans
- Properties:
  - WorkoutTemplateId, Name, Description, CreatedDate, LastModifiedDate
  - IsPublic, Category, Tags, UserId
- Relationships:
  - Many-to-one with User (creator)
  - One-to-many with WorkoutTemplateExercise

#### WorkoutTemplateExercise
- Represents an exercise within a workout template
- Properties:
  - WorkoutTemplateExerciseId, WorkoutTemplateId, ExerciseTypeId
  - SequenceNum, Notes
- Relationships:
  - Many-to-one with WorkoutTemplate
  - Many-to-one with ExerciseType
  - One-to-many with WorkoutTemplateSet

#### WorkoutTemplateSet
- Represents default set values for a template exercise
- Properties:
  - WorkoutTemplateSetId, WorkoutTemplateExerciseId, SettypeId
  - DefaultReps, DefaultWeight, SequenceNum, Notes, Description
- Relationships:
  - Many-to-one with WorkoutTemplateExercise
  - Many-to-one with SetType