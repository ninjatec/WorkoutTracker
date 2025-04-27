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
- CoachNote
- WorkoutSchedule

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

#### CoachNote
- Represents coaching notes attached to workouts for feedback and observations
- Properties:
  - Id, noteText, createdDate, workoutId
  - CoachId, IsPrivate
- Relationships:
  - Many-to-one with User (coach)
  - Many-to-one with WorkoutSession (workout)
- Features:
  - Coaching feedback mechanism
  - Date tracking for chronological organization
  - Privacy control for notes (visible to athlete vs. coach-only)
  - Association with specific workout sessions

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