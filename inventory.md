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
| `/Services` | Application services and business logic |
| `/wwwroot` | Static assets (CSS, JS, images) |
| `/Areas/Identity` | Authentication and user management |
| `/Migrations` | Database migration scripts |

### Configuration Files

| File | Purpose |
|------|---------|
| `appsettings.json` | Application configuration |
| `appsettings.Development.json` | Development-specific settings |
| `Program.cs` | Application startup and service configuration |
| `WorkoutTrackerWeb.csproj` | Project file with dependencies |

### Deployment Files

| File | Purpose |
|------|---------|
| `Dockerfile` | Container definition for Docker deployments |
| `docker-compose.yml` | Multi-container definition for development |
| `docker-compose.debug.yml` | Container configuration for debugging |
| `/k8s/*` | Kubernetes deployment manifests |

## Data Model

### Core Entities

#### User
- Primary entity representing application users
- Links to ASP.NET Core Identity system
- Properties: UserId, Name, IdentityUserId
- Relationships: One-to-many with Sessions

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

## Features and Workflows

### Authentication
- Email-based registration with confirmation
- User login/logout
- Profile management

### Workout Management
1. Users create workout Sessions with date and name
2. Within Sessions, users add Sets for specific exercises
3. Sets are associated with ExerciseTypes and SetTypes
4. Each Set contains Reps with tracking for success/failure

### Data Entry Flow
1. Create User (register)
2. Create/select Session
3. Add Sets to Session (select ExerciseType and SetType)
4. Record Reps within Sets (with weight and success tracking)

## Technical Implementation Notes

### Data Access
- Entity Framework Core with SQL Server
- Code-First approach with migrations
- Configured relationships with cascade delete

### Authentication
- ASP.NET Core Identity with email confirmation
- Custom UserService for current user management

### UI Components
- Razor Pages with view models
- Bootstrap styling
- Form validation

### Deployment
- Docker containerization
- Kubernetes-ready configuration