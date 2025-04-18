# WorkoutTracker

A comprehensive fitness tracking web application built with ASP.NET Core that allows users to track their workout sessions, exercises, and progress.

Production Site running on wot.ninjatec.co.uk

## Features

- **Workout Sharing**: Share workout data securely with others
  - Secure token-based sharing with expiration dates
  - Granular access controls for specific workout data
  - Usage limits with maximum view count options
  - Time-limited access with automatic expiration
  - Session-specific or account-wide sharing options
  - User control to revoke access at any time
  - Read-only reports for shared workout data
  - Cookie-based token persistence for better navigation
  - IP-based security and rate limiting
  - Permission-based feature access control

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
- **Exercise Tracking**: Track exercises performed during each session
- **Set & Rep Management**: Record sets and repetitions for each exercise with weight tracking
- **Exercise Library**: Maintain a database of exercise types for reuse across sessions
- **Data Visualization**: Track progress over time through intuitive charts and graphs
- **Performance Analytics**: View success/failure rates for exercises with detailed breakdowns
- **One Rep Max Calculator**: Estimate maximum strength using seven scientific formulas based on weight and reps
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
- **Health Monitoring**: Comprehensive health checks and metrics collection for production monitoring
- **Help Center**: Complete help documentation with search, FAQs, glossary, and video tutorials
- **Seed Data**: Pre-populated exercise types, set types, help content, and glossary terms for immediate use
- **Version Management**: Complete version tracking system with:
  - Semantic versioning (major.minor.patch.build)
  - Version history display in the application footer
  - Admin version management interface
  - Automatic version tracking in logs for troubleshooting
  - Git commit hash tracking for traceability
  - Automated versioning during build and deployment
  - Docker image tagging by version
  - Database deployment version history

## Technology Stack

- **Framework**: ASP.NET Core (Razor Pages)
- **Database**: SQL Server
- **ORM**: Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Frontend**: Bootstrap, HTML, CSS, JavaScript
- **Data Visualization**: Chart.js
- **Email Services**: SMTP integration for notifications and feedback
- **Background Processing**: Hangfire for long-running tasks
- **Real-time Communication**: SignalR for progress updates with Redis backplane for multi-container support
- **Containerization**: Docker support for deployment
- **Monitoring**: Prometheus metrics and health checks for Kubernetes
- **Cache**: Redis for SignalR scale-out and distributed caching

## Architecture

The application follows a clean architecture pattern:

- **Models**: Define the core domain entities (User, Session, Set, Rep, ExerciseType, SetType, Feedback, HelpArticle, HelpCategory, GlossaryTerm)
- **Pages**: Razor Pages for the user interface
- **Services**: Business logic services for user management, email notifications, help content, and other functionality
- **Data Access**: Entity Framework Core for database operations

## Data Model

The application uses the following entity relationships:

- **User**: Represents a registered user of the application
  - Contains personal information and is linked to ASP.NET Identity
  - Has many Sessions
  - Can submit Feedback
  
- **Session**: Represents a workout session
  - Belongs to a User
  - Has many Sets
  - Includes date, time and name
  
- **Set**: Represents a set of exercises within a session
  - Belongs to a Session
  - Associated with an ExerciseType
  - Has a SetType (e.g., warm-up, normal, drop set)
  - Contains weight information
  - Has many Reps
  
- **Rep**: Represents individual repetitions within a set
  - Belongs to a Set
  - Includes weight and success tracking
  
- **ExerciseType**: Represents a type of exercise (e.g., bench press, squat)
  - Referenced by many Sets
  
- **SetType**: Represents a type of set (e.g., regular, warm-up, drop set)
  - Referenced by many Sets
  
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
  - Can be linked to a specific Session or share all user sessions
  - Contains expiration date for time-limited access
  - Includes usage tracking with access count
  - Optional maximum usage limit with automatic deactivation
  - Granular access controls for different features (sessions, reports, calculator)
  - Includes user-defined name and description for organization
  - Provides active status flag for manual revocation

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

- Implemented secure token validation system with rate limiting, permission verification, and cache optimization
- Implemented API endpoints for workout data sharing with token-based access controls
- Enhanced workout data deletion with improved progress tracking, connection resilience, and fallback status polling
- Fixed TrainAI import progress tracking to properly display real-time updates throughout the entire import process
- Enhanced job status monitoring with improved error handling and connection resilience
- Added fallback job status polling for situations where SignalR connections are interrupted
- Improved immediate feedback when starting background jobs
- Added distributed caching with Redis for report pages to improve performance and scalability
- Added SignalR Redis backplane support for multi-container WebSocket functionality
- Added comprehensive health checks for Redis monitoring in Kubernetes
- Added background job monitoring dashboard for tracking, analyzing and managing Hangfire jobs
- Added background job processing with Hangfire to eliminate Cloudflare 524 timeout errors
- Added real-time progress updates using SignalR for long-running operations
- Optimized TrainAI data import with batched processing and improved error handling
- Enhanced workout data deletion with background processing and progress tracking
