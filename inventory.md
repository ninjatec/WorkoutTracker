# System Inventory

## Overview

WorkoutTracker is a fitness tracking application built with ASP.NET Core, using the Razor Pages architecture. This document provides a comprehensive inventory of the system components, their relationships, and key implementations.

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
| `/Services` | Application services and business logic |
| `/Services/VersionManagement` | Version tracking and management services |
| `/Hubs` | SignalR hubs for real-time communication |
| `/wwwroot` | Static assets (CSS, JS, images) |
| `/Areas/Identity` | Authentication and user management |
| `/Areas/Admin` | Administrative functions including version management |
| `/Migrations` | Database migration scripts |
| `/ViewComponents` | Reusable UI components including VersionInfo |
| `/Middleware` | Custom middleware including VersionLoggingMiddleware |
| `/scripts` | Build and deployment automation scripts |

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

## Database Schema

The application uses Entity Framework Core Code-First approach with SQL Server. Key migrations:

- Initial database creation (20250331083704_InitialCreate)
- Model tweaks and refinements (20250401122851_MdodelTweak)
- Session changes (20250402095313_SessionChanges, 20250402110115_SessionChanges2)
- Model changes (20250402112618_ModeChanges)
- Set-Exercise relationship changes (20250408121738_SetExerciseRelationship)
- Set type column fix (20250408134840_FixSetTypeColumn)
- Set reps tracking (20250408171116_AddNumberRepsToSet)
- Relationship updates (20250409060240_UpdateRepSetRelationship)
- Identity integration (20250409094804_AddIdentityUserIdToUser)
- Cascade delete configuration (20250409174517_UpdateSetRepCascadeDelete)
- Exercise refactoring to ExerciseType (20250410121945_RefactorExerciseToExerciseType, 20250410144522_CompleteExerciseTypeRefactor)
- Data Protection Keys (20250412090900_AddDataProtectionKeys)
- Feedback system implementation (20250413093357_AddFeedbackModel)
- Enhanced Feedback management (20250414174517_AddFeedbackEnhancedManagement)
- Help center implementation (20250413113858_AddHelpPagesModels)
- Login history tracking implementation (20250413_AddUserLoginHistory)
- Version management implementation (20250414094500_AddVersionManagement)

## Features and Workflows

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
   - TrainAIImportService & WorkoutDataService: Services with progress reporting
   - BackgroundJobsController: Admin controller for job monitoring dashboard
   - Job monitoring views: Index, History, Details, and ServerStatus

3. Job monitoring dashboard features:
   - Dashboard overview with job statistics and charts
   - Recent jobs listing with status indicators and timestamps
   - Job history with tabs for failed and succeeded jobs
   - Job details view with execution timeline and arguments
   - Server status monitoring with active workers and queues
   - Job filtering by status, type, and queue name
   - Retry functionality for failed jobs with error tracking
   - Visual status indicators for job states
   - Real-time statistics for job processing metrics

### Workout Management
1. Users create workout Sessions with date and name
2. Within Sessions, users add Sets for specific exercises
3. Sets are associated with ExerciseTypes and SetTypes
4. Each Set contains Reps with tracking for success/failure

### Workout Sharing API

1. Share Token Management API:
   - RESTful API endpoints for managing workout data sharing
   - Complete CRUD operations for share tokens
   - Token-based security model with expiration and access limits
   - Controller: `ShareTokenController`
   - DTOs: `ShareTokenDto`, `CreateShareTokenRequest`, `UpdateShareTokenRequest`
   - Service: `ShareTokenService` managing token generation and validation

2. Core API endpoints:
   - GET /api/ShareToken - Get all user's tokens (authenticated)
   - GET /api/ShareToken/{id} - Get specific token (authenticated)
   - POST /api/ShareToken - Create new token (authenticated)
   - POST /api/ShareToken/validate - Validate token (anonymous)
   - GET /api/ShareToken/validate/{permission} - Validate token permission (anonymous)
   - PUT /api/ShareToken/{id} - Update token (authenticated)
   - DELETE /api/ShareToken/{id} - Delete token (authenticated)
   - POST /api/ShareToken/{id}/revoke - Revoke token (authenticated)

3. Security features:
   - Cryptographically secure random token generation
   - Token validation with expiration checking
   - Usage tracking with access counting
   - Row-level security with user filtering
   - Validated request models with data annotations
   - Comprehensive error handling and logging
   - Rate limiting with token bucket algorithm for brute force protection
   - Distributed caching for token validation to reduce database load
   - IP address tracking and filtering for security audit
   - Custom `ShareTokenAuthorize` attribute for controller/action protection

4. Validation infrastructure:
   - `TokenRateLimiter`: Implements token bucket algorithm for rate limiting
   - `TokenValidationService`: Centralized validation with caching and security
   - `ShareTokenAuthorizeAttribute`: Authorization filter for controllers
   - `ShareTokenExtensions`: Helper extension methods for HttpContext
   - Multiple token acquisition methods (query, header, cookie)
   - Granular permission validation for different access types
   
5. Service capabilities:
   - Token generation with configurable expiry
   - Session-specific or account-wide sharing
   - Granular feature access controls
   - Access count tracking and limiting
   - Token revocation (soft deletion)
   - User-specific token management
   - Cache invalidation for token updates, deletions, and revocations

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
   - Background processing for large imports
   - Real-time progress tracking via SignalR
   - Resilient connection handling with automatic retries
   - Fallback job status polling when SignalR is unavailable
   - Improved job status monitoring with detailed state tracking
   - Immediate visual feedback when starting background jobs

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
   - Background processing for large imports
   - Real-time progress tracking via SignalR
   - Resilient connection handling with automatic retries
   - Fallback job status polling when SignalR is unavailable
   - Improved job status monitoring with detailed state tracking
   - Immediate visual feedback when starting background jobs

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

## Third-party Packages

| Package | Purpose |
|---------|---------|
| Entity Framework Core | ORM for data access |
| ASP.NET Core Identity | Authentication and authorization |
| Hangfire | Background job processing |
| SignalR | Real-time client-server communication |
| SignalR.StackExchangeRedis | Redis backplane for multi-container SignalR scaling |
| StackExchange.Redis | Redis client for .NET |
| AspNetCore.HealthChecks.Redis | Redis health monitoring for Kubernetes |
| AspNetCore.HealthChecks.System | System metrics health checks |
| AspNetCore.HealthChecks.SqlServer | SQL Server health monitoring |
| Chart.js | Data visualization |
| Bootstrap | UI framework |
| CsvHelper | CSV file parsing and processing |
| UAParser | User agent parsing for device detection |

## Application Infrastructure

### Distributed Caching with Redis

1. Architecture:
   - Redis server deployed as a StatefulSet in Kubernetes
   - Redis Sentinel for high availability
   - Environment-specific caching strategy:
     - Development: SQL Server distributed cache 
     - Production: Redis distributed cache
   - Cache invalidation upon data modifications
   - Per-user cache isolation with namespaced keys

2. Key components:
   - Redis configuration in Program.cs
   - StackExchangeRedis integration
   - Distributed cache implementation in Reports pages
   - Cache invalidation in data services
   - JSON serialization for complex objects
   - Sliding and absolute expiration policies

3. Report page caching:
   - Component-based caching strategy with separate cache keys
   - User-specific cache isolation
   - Configurable cache duration (5 minutes sliding, 30 minutes absolute)
   - Cached components:
     - Overall workout status (success/failure metrics)
     - Exercise-specific performance metrics
     - Recent exercise activity (30 days)
     - Weight progression data
     - Personal records with pagination

4. Performance benefits:
   - Reduced database load for report generation
   - Faster page load times for complex reports
   - Scalable across multiple application instances
   - Resilient caching with automatic reconnection
   - Memory optimization through targeted component caching

5. Cache invalidation:
   - Strategic invalidation when workout data changes
   - Granular component-level invalidation
   - Error handling for cache operations
   - Clean separation of concerns with helper methods

### SignalR Scaling with Redis Backplane

1. Architecture:
   - Redis server deployed as a separate container in Kubernetes
   - SignalR configured to use Redis as a backplane for cross-container message delivery
   - Health monitoring for Redis with Kubernetes probes
   - Resilient connection handling with automatic reconnection

2. Key components:
   - Redis deployment in Kubernetes (`/k8s/redis.yaml`)
   - Redis connection string configuration via environment variables
   - SignalR Redis backplane configuration in `Program.cs`
   - Health checks for Redis monitoring

3. Benefits:
   - Enables WebSocket functionality across multiple container instances
   - Maintains client connections during rolling updates or scaling events
   - Supports horizontal scaling of the application
   - Provides resilience against temporary Redis connection issues
   - Enables real-time updates for progress tracking across all instances
   - Improves availability and reliability for real-time features

4. Implementation details:
   - Exponential retry policy for Redis connection issues
   - Connection event logging for troubleshooting
   - Kubernetes health checks integration
   - Environment variable configuration for flexible deployment

5. Integration with existing features:
   - Real-time progress updates for background operations
   - ImportProgressHub scaled across multiple containers
   - Consistent user experience during container scaling events