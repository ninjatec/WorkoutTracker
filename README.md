# WorkoutTracker

A comprehensive fitness tracking web application built with ASP.NET Core that allows users to track their workout sessions, exercises, and progress.

## Features

- **User Authentication**: Secure login and registration using ASP.NET Core Identity with email confirmation
- **Session Management**: Create and manage workout sessions with timestamps
- **Exercise Tracking**: Track exercises performed during each session
- **Set & Rep Management**: Record sets and repetitions for each exercise with weight tracking
- **Exercise Library**: Maintain a database of exercise types for reuse across sessions
- **Data Visualization**: Track progress over time through sets and repetitions

## Technology Stack

- **Framework**: ASP.NET Core (Razor Pages)
- **Database**: SQL Server
- **ORM**: Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Frontend**: Bootstrap, HTML, CSS, JavaScript
- **Containerization**: Docker support for deployment

## Architecture

The application follows a clean architecture pattern:

- **Models**: Define the core domain entities (User, Session, Set, Rep, ExerciseType, SetType)
- **Pages**: Razor Pages for the user interface
- **Services**: Business logic services for user management and other functionality
- **Data Access**: Entity Framework Core for database operations

## Data Model

The application uses the following entity relationships:

- **User**: Represents a registered user of the application
  - Contains personal information and is linked to ASP.NET Identity
  - Has many Sessions
  
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

## Deployment

The application includes Docker support for containerized deployment with:
- Docker Compose configuration for development and production
- Kubernetes manifests for orchestrated deployment

## Recent Updates

- Added email verification for user registration
- Weight tracking for sets and reps
- Refactored exercise tracking to use exercise types
- Added session date/time information to Set and Rep pages
- Set up cascading delete relationships between entities
