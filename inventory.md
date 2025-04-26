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

## Features and Workflows

### Responsive Design System
1. Mobile-first architecture:
   - Design starts with mobile layouts and progressively enhances for larger screens
   - Uses standard breakpoints (576px, 768px, 992px, 1200px) for consistent behavior 
   - Component-based CSS structure for maintainability
   - Standardized viewport configuration across all templates

2. Responsive components:
   - Mobile-optimized navigation with collapsible elements
   - Responsive tables that transform to stacked layouts on small screens
   - Touch-friendly form elements with appropriate sizing
   - Mobile-specific layouts for data-dense pages
   - Adaptive containers with mobile-friendly spacing

3. Responsive utilities:
   - JavaScript helpers for dynamic content adaptation
   - Automatic table transformations for mobile displays
   - DataTables integration with responsive configuration
   - Touch event handling for mobile interactions
   - Visible/hidden utility classes for conditional content

4. Implementation approach:
   - CSS variables for consistent theming across breakpoints
   - Mobile-first media queries following Bootstrap conventions
   - Progressive enhancement methodology 
   - Component isolation for maintainability
   - Standardized naming conventions

5. Responsive layout improvements:
   - Container adaptations with mobile-friendly padding
   - Stacked layouts for forms on smaller screens
   - Mobile-optimized button and input sizing
   - Touch-friendly spacing between interactive elements
   - Improved viewport handling for better mobile rendering
   - Default font size adjustments for readability on small screens

### Authentication
- Email-based registration with confirmation
- User login/logout
- Profile management
- Login history tracking:
  - Records IP address, timestamp, and success status for all login attempts
  - Collects device and platform information using UAParser
  - Integrates with ASP.NET Core Identity's OnSignedIn event
  - Provides admin interface for reviewing login history by user
  - Tracks both successful and failed login attempts
  - Helps identify suspicious login patterns and potential security issues

### Background Processing System
1. Hangfire-based job processing for long-running operations:
   - Persistent job storage in SQL Server database
   - Automatic retry mechanism for failed jobs
   - Dashboard for monitoring job status and history
   - Distributed locking for preventing duplicate processing
   - Comprehensive monitoring dashboard with job statistics and history
   - Failed job management interface with retry capabilities
   - Server status monitoring with active workers and queues
   - Detailed job information including arguments and stack traces
   - Job filtering by status, type, and queue
   - Real-time server metrics and processing statistics

2. Key components:
   - BackgroundJobService: Central service for queuing and executing background jobs
   - ImportProgressHub: SignalR hub for real-time progress updates
   - JobProgress: Standardized progress reporting model for consistent UI updates
   - TrainAIImportService & WorkoutDataPortabilityService: Services with progress reporting
   - JobStatusController: REST API for polling job status when SignalR is unavailable
   - BackgroundJobs pages: Razor Pages implementation for job monitoring dashboard
   - Job monitoring views: Index, JobHistory, Details, and ServerStatus

3. Job monitoring features:
   - Dashboard overview with job statistics and charts
   - Recent jobs listing with status indicators and timestamps
   - Job history with tabs for failed and succeeded jobs
   - Job details view with execution timeline and arguments
   - Server status monitoring with active workers and queues
   - Job filtering by status, type, and queue name
   - Retry functionality for failed jobs with error tracking
   - Visual status indicators for job states
   - Real-time statistics for job processing metrics
   - Admin-only access with role-based authorization
   - Shared admin layout for consistent navigation and styling
   - Exception handling with friendly error messages
   - Background job management functions (retry, delete)
   - Responsive design for different screen sizes

### Workout Management
1. Users create workout Sessions with date and name
2. Within Sessions, users add Sets for specific exercises
3. Sets are associated with ExerciseTypes and SetTypes
4. Each Set contains Reps with tracking for success/failure

### Shared Workout Views

1. Razor Pages Architecture:
   - `Pages/Shared` folder contains read-only views for shared workout data:
     - `Index.cshtml`: Entry point listing available sessions
     - `Session.cshtml`: Detailed workout session view with exercise breakdown
     - `Reports.cshtml`: Statistical reports with visualizations
     - `Calculator.cshtml`: One-Rep Max calculator
     - `TokenRequired.cshtml`: Error view when token is missing
     - `InvalidToken.cshtml`: Error view for expired or invalid tokens
     - `_SharedLayout.cshtml`: Consolidated layout for shared content with MVC/Razor Pages compatibility

   - Page Models:
     - `SharedPageModel.cs`: Base page model with token validation logic
     - Feature-specific models inheriting from SharedPageModel

2. Layout Structure:
   - Consolidated layout system with Bootstrap 5 integration:
     - `_Layout.cshtml`: Main application layout with DataTables and Chart.js integration
     - `_SharedLayout.cshtml`: Specialized layout for shared workout content
     - `_AdminLayout.cshtml`: Admin-specific layout inheriting from main layout
   - Removed redundant MVC-specific layouts for better maintainability
   - Shared layouts support both ViewBag (MVC) and PageModel (Razor Pages) data access patterns

3. View Components:
   - Custom layout for shared access views
     - Simplified navigation with permission-based visibility
     - Token information display with expiration countdown
     - Usage tracking information for tokens with limits
     - Visual status indicators for remaining access time

4. Key Features:
   - Anonymous access via secure tokens
   - Read-only views of workout data
   - Session navigation with breadcrumbs
   - Visual exercise categorization by set type
   - Detailed rep tracking with success/failure indicators
   - Statistical reports with Chart.js visualizations
   - Token status information with visual indicators
   - Session-specific or account-wide data access
   - Cookie-based token persistence with secure configuration
   - Custom styling with dedicated shared.css

### Strength Calculation

1. One Rep Max Calculator uses seven scientific formulas to estimate maximum strength:
   - Brzycki: weight × (36 / (37 - reps))
   - Epley: weight × (1 + 0.0333 × reps)
   - Lander: (100 × weight) / (101.3 - 2.67123 × reps)
   - Lombardi: (weight × reps)^0.1
   - Mayhew: (100 × weight) / (52.2 + (41.9 × e^(-0.055 × reps)))
   - O'Conner: weight × (1 + 0.025 × reps)
   - Wathan: (100 × weight) / (48.8 + (53.8 × e^(-0.075 × reps)))
2. Calculator analyzes user's recent workout data to provide 1RM estimates
3. Results can be saved to the user's profile as a new session
4. WarmUp sets are excluded from calculations for more accurate results

### Feedback System
1. Users submit feedback through a structured form with types:
   - Bug Report
   - Feature Request
   - General Feedback
   - Question
2. Submissions are stored in the database and associated with the current user if logged in
3. Email notifications are automatically sent to administrators (marc.coxall@ninjatec.co.uk)
4. Administrators can view, update, and manage feedback entries through a comprehensive dashboard:
   - Statistics overview with counts by type and status
   - Visual charts showing feedback distribution and trends
   - Recent activity tracking for latest submissions and updates
   - Quick filters for common views (New, In Progress, Bug Reports, etc.)
5. Enhanced status management features:
   - Status workflow tracking (New → In Progress → Completed/Rejected)
   - Admin assignment capabilities for ownership and accountability
   - Timeline tracking with timestamps for all status changes
   - Priority levels from Low to Critical for better work management
   - Estimated completion date scheduling for planning
6. User communication enhancements:
   - Template-based responses for common situations
   - Public and private response separation (admin notes vs. public responses)
   - Email notifications for status updates to users
   - Publication controls for sharing feedback and responses publicly
7. Advanced management capabilities:
   - Device and browser information collection for debugging
   - Category organization for better classification
   - Search, filter, and sort functionality with multiple criteria
   - Pagination for handling large feedback volumes
   - Quick action buttons for common status changes
   - Modal-based interfaces for quick response and assignment changes

### Help Center System
1. Comprehensive documentation with multiple components:
   - Searchable help articles organized by categories
   - Frequently Asked Questions (FAQs) organized by topic
   - Workout terminology glossary with definitions and examples
   - Video tutorials for visual learning
2. Content organization and discovery features:
   - Full-text search across all help content
   - Category-based navigation for browsing documentation
   - Featured articles highlighting important topics
   - Related articles suggestions for further reading
3. User experience enhancements:
   - Contextual help access from any page
   - Breadcrumb navigation for location awareness
   - Printer-friendly content formatting
   - Article feedback and rating system
   - Content sharing functionality
4. Administration features:
   - View counts for content popularity tracking
   - Content versioning linked to application releases
   - Content management interface for administrators

### Version Management System
1. Semantic versioning implementation with:
   - Major version: Breaking changes or significant feature additions
   - Minor version: Non-breaking feature additions
   - Patch version: Bug fixes and minor improvements
   - Build number: Incremental build counter
   
2. Version tracking in application:
   - Current version displayed in footer of all pages
   - Version history page accessible from footer
   - Admin interface for version management
   - Version middleware for embedding version in logs
   
3. Automated versioning workflow:
   - `update-version.sh`: Script to increment version during build
   - Version tracking via Git commit hashes for traceability
   - `publish_to_prod.sh`: Automated Docker build and deploy with version tags
   - Kubernetes deployment updates with version-specific images
   
4. Version logging for troubleshooting:
   - Every log entry includes version information
   - VersionLoggingMiddleware injects version into log context
   - Error logs include version for easier reproduction and fixes
   
5. Version management interface:
   - Admin area for managing versions
   - Adding new versions with release notes
   - Setting a specific version as current
   - Viewing version history with detailed information

### Data Entry Flow
1. Create User (register)
2. Create/select Session
3. Add Sets to Session (select ExerciseType and SetType)
4. Record Reps within Sets (with weight and success tracking)

### Data Import/Export

Planned functionality for workout data portability:

1. Structured data export system:
   - JSON schema for structured workout data
   - Flexible export options (all data, date ranges, specific sessions)
   - Complete workout data extraction with related entities
   - Downloadable files with consistent naming conventions
   - Export history tracking

2. Import functionality:
   - File upload with format validation
   - JSON schema validation
   - Duplicate detection and handling
   - Exercise type and set type mapping
   - Transaction-based import process
   - Import logging and auditing
   - Rollback on failure
   - Import summary reporting
   - TrainAI CSV import support with auto-mapping
   - Multiple import formats (JSON, CSV)
   - Background processing for large imports using Hangfire
   - Real-time progress tracking via SignalR
   - Resilient connection handling with automatic retries
   - Fallback job status polling when SignalR is unavailable
   - Improved job status monitoring with detailed state tracking
   - Immediate visual feedback when starting background jobs
   - Cross-browser compatibility with enhanced error handling
   - Job progress persistence for page refreshes or reconnects
   - Shared ImportProgressHub for consistent progress reporting

3. Data deletion functionality:
   - Transactional deletion of all user workout data
   - User confirmation with checkbox validation
   - Account preservation while removing workout history
   - Comprehensive cleanup of sessions, sets, and reps
   - Database transaction support for data consistency
   - Access through Data dropdown in main navigation
   - TempData-based success message handling

4. Workout sharing features:
   - Secure token-based sharing system with ShareToken model
   - Customizable access permissions (session viewing, reports, calculator)
   - Time-limited access with automatic expiration dates
   - Usage tracking with access counts and optional maximum limits
   - Session-specific or account-wide sharing options
   - User management dashboard for tracking and revoking shares
   - Descriptive naming and notes for share organization
   - Random secure token generation
   - Database-enforced query filtering for data security
   - Manual revocation controls

5. Third-party integration:
   - Support for common fitness app formats
   - API endpoints for programmatic access
   - Webhook integration
   - Scheduled export automation
   - Spreadsheet export formats

6. Migration tooling:
   - Bulk import from other fitness services
   - Data transformation utilities
   - Cleanup tools for imported data
   - Rollback functionality for failed imports

TrainAI Import processing features:
- Batch processing of 100 reps at a time
- Database transaction support
- Real-time progress tracking using SignalR
- Enhanced progress UI showing:
  - Overall completion percentage
  - Current workout and exercise
  - Total reps processed
  - Import summary on completion

Performance optimizations:
- Batched database operations for reps
- Transaction handling with rollback support
- Optimized exercise type lookups with similarity matching
- Minimal database round trips
- Efficient progress reporting

The import process follows these steps:
1. CSV parsing with CsvHelper for robust file handling
2. Workout data extraction with exercise mapping
3. Exercise type matching with Levenshtein distance for similarity
4. Batch processing of reps (100 at a time)
5. Transaction handling for data consistency
6. Real-time progress updates via SignalR

### Data Portability System

1. Export functionality:
   - JSON-based data export format
   - Date range filtering capability
   - Export schema versioning
   - Custom serialization handling
   - Progress and statistics inclusion
   - Complete data relationships preservation

2. Import functionality:
   - File upload with format validation
   - JSON schema validation
   - Duplicate detection and handling
   - Exercise type and set type mapping
   - Transaction-based import process
   - Import logging and auditing
   - Rollback on failure
   - Import summary reporting
   - TrainAI CSV import support with auto-mapping
   - Multiple import formats (JSON, CSV)
   - Background processing for large imports using Hangfire
   - Real-time progress tracking via SignalR
   - Resilient connection handling with automatic retries
   - Fallback job status polling when SignalR is unavailable
   - Improved job status monitoring with detailed state tracking
   - Immediate visual feedback when starting background jobs
   - Cross-browser compatibility with enhanced error handling
   - Job progress persistence for page refreshes or reconnects
   - Shared ImportProgressHub for consistent progress reporting

3. Implementation components:
   - WorkoutExport model for data serialization
   - WorkoutDataPortabilityService for JSON import/export
   - TrainAIImportService for CSV parsing and import
   - BackgroundJobService for long-running operations
   - ImportProgressHub for real-time progress updates
   - Export/Import Razor Pages with unified interface
   - Secure file handling
   - Progress tracking
   - Error handling and validation
   - User-specific data isolation

4. Data deletion components:
   - WorkoutDataService with batched deletion
   - Background processing for large data operations
   - Transaction support for integrity
   - Real-time progress tracking
   - SignalR integration for UI updates
   - Connection resilience with automatic reconnection
   - Fallback job status API polling when SignalR is unavailable
   - Detailed progress metrics with processed items counts
   - Connection status indicator for better UX
   - Immediate visual feedback when starting the delete operation
   - Transactional deletion to ensure all-or-nothing data removal

5. Export schema:
   - Version information
   - User details (name, email)
   - Sessions with timestamps
   - Sets with exercise and set types
   - Reps with weights and success tracking
   - Exercise type catalog

### Alert System

1. **Alert Management**:
   - Comprehensive alerting system for system health and user activity monitoring
   - Threshold-based alert generation for various metrics
   - Multi-level severity categorization (Critical, Warning, Info)
   - Status workflow tracking (Active → Acknowledged → Resolved)
   - Assignment and ownership tracking
   - Resolution notes and audit trail

2. **Alert Components**:
   - `AlertService`: Central service for alert creation, acknowledgment, and resolution
   - `AlertThresholdService`: Service for evaluating metric thresholds and triggering alerts
   - `NotificationService`: Multi-channel notification delivery system
   - `AlertBackgroundService`: Background service for periodic threshold evaluation
   - `AlertsController`: API endpoints for alert management
   - `AlertHub`: SignalR hub for real-time alert notifications
   - `/Areas/Admin/Pages/Alerts/`: Razor Pages for alert management interface

3. **Alert Dashboard**:
   - Real-time display of active alerts with severity indicators
   - Filtering by status, severity, category, and date range
   - Bulk acknowledgment and resolution capabilities
   - Timeline visualization for alert history
   - Trend analysis for recurring issues
   - User-specific views based on assigned alerts

4. **Notification System**:
   - Multi-channel delivery (Email, In-App, SMS)
   - User preference management for notification channels
   - Quiet hours configuration for non-critical alerts
   - Delivery tracking with read receipts
   - Email template system for consistent formatting
   - Batching for preventing notification storms
   - Throttling for high-volume alert situations

5. **Threshold Configuration**:
   - Admin interface for threshold management
   - Support for multiple comparison operators
   - Consecutive occurrence detection to prevent alert flapping
   - Time-based thresholds for trend detection
   - Category and severity assignment for generated alerts
   - Enable/disable toggle for individual thresholds
   - Testing capability for threshold validation

6. **Implementation Details**:
   - Integration with system metrics collection
   - Database-backed alert storage for persistence
   - SignalR for real-time notifications
   - Email integration for critical alerts
   - Mobile-responsive alert dashboard
   - Role-based access for alert management
   - Historical alert data for trend analysis
   - Auto-resolution capability for transient issues

7. **Alert Types**:
   - System health (CPU, memory, disk usage)
   - Database performance (connection pool, query times)
   - API response time anomalies
   - Authentication anomalies (failed login attempts)
   - Background job failures
   - Data import/export issues
   - User activity patterns (suspicious login locations)
   - Redis cache performance degradation
   - Feature usage statistics
   - Error rate monitoring

### Alert System Components

| Component | Purpose |
|-----------|---------|
| `Alert` | Entity model for representing system-generated alerts |
| `AlertThreshold` | Entity model for threshold-based alert generation |
| `Notification` | Entity model for tracking alert notifications sent to users |
| `NotificationPreference` | Entity model for user notification preferences |
| `IAlertService` | Interface for alert management contract |
| `AlertService` | Service implementation for alert CRUD operations |
| `IAlertThresholdService` | Interface for threshold evaluation |
| `AlertThresholdService` | Service for evaluating metrics against thresholds |
| `INotificationService` | Interface for notification delivery |
| `NotificationService` | Service for multi-channel alert notifications |
| `AlertBackgroundService` | Background service for periodic threshold evaluation |
| `AlertHub` | SignalR hub for real-time alert notifications |
| `/Areas/Admin/Pages/Alerts/` | Alert management Razor Pages |
| `/api/Alerts` | REST API endpoints for alert interaction |

### Alert System Features

The alerting system provides comprehensive monitoring and notification capabilities:

1. **Alert Management**:
   - CRUD operations for system-generated alerts
   - Status workflow (Active → Acknowledged → Resolved)
   - Severity-based categorization (Critical, Warning, Info)
   - Source and category tracking for organization
   - Resolution notes and audit trail
   - Automatic and manual resolution support

2. **Threshold Configuration**:
   - Admin interface for threshold management
   - Multiple comparison operators (>, <, =, etc.)
   - Consecutive occurrence detection
   - Evaluation frequency configuration
   - Automatic alert generation when thresholds are exceeded
   - Category and severity assignment

3. **Notification System**:
   - Multi-channel delivery (Email, In-App, SMS)
   - User preference management by alert category and severity
   - Quiet hours configuration
   - Delivery status tracking
   - Retry mechanism for failed notifications
   - Read receipt functionality

4. **Admin Dashboard**:
   - Real-time alert monitoring dashboard
   - Filtering by status, severity, and category
   - Bulk operations for alert management
   - Timeline visualization of alert history
   - Trend analysis for recurring issues
   - Notification log review

5. **Integration Points**:
   - SignalR for real-time updates
   - Email service for notification delivery
   - System metrics collection for threshold evaluation
   - Health check system for automatic alerting
   - Background job system for failure alerting
   - Database performance monitoring
   - User authentication anomaly detection

6. **Security Features**:
   - Role-based access control
   - Audit logging for all alert actions
   - Secure notification delivery
   - Rate limiting for notification sending
   - IP-based anomaly detection for login alerts

7. **Implementation Details**:
   - Entity Framework Core for data persistence
   - SignalR for real-time communications
   - Background services for automated threshold checking
   - Email templates for consistent notification formatting
   - Responsive design for mobile compatibility
   - Chart.js for trend visualization
   - Bootstrap 5 for UI components
   - Role-based security with ASP.NET Core Identity