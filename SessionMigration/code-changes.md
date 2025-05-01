# Session to WorkoutSession Migration - Code Changes Analysis

## Overview

This document identifies the components that need to be updated as part of the migration from Session to WorkoutSession models.

## 1. Controllers

The following controllers reference the Session model and will need to be updated:

| Controller | Method | Type of Reference |
|------------|--------|-------------------|
| SharedController | Index | Retrieves and displays Session data |
| SharedController | Session | Retrieves and displays Session details |
| SharedController | Reports | Retrieves Session data for reports |
| SharedWorkoutController | GetSessions | API endpoint returning Session data |
| SharedWorkoutController | GetSession | API endpoint returning specific Session |
| SharedWorkoutController | GetSets | API endpoint retrieving sets for a session |

## 2. Pages

The following Razor Pages reference the Session model and will need to be updated:

| Page | Purpose |
|------|---------|
| Pages/Sessions/Index | Lists all sessions |
| Pages/Sessions/Create | Creates a new session |
| Pages/Sessions/Edit | Edits an existing session |
| Pages/Sessions/Details | Shows session details |
| Pages/Sessions/Delete | Deletes a session |
| Pages/Sessions/Reschedule | Reschedules a missed session |
| Pages/Shared/Session | Displays shared session data |
| Pages/Shared/Index | Lists sessions in shared view |
| Areas/Coach/Pages/ClientDetail | Shows client sessions for coaches |

## 3. Services

The following services contain Session-related functionality:

| Service | Method | Description |
|---------|--------|-------------|
| UserService | GetUserSessionsAsync | Retrieves sessions for a specific user |
| VolumeCalculationService | CalculateSessionVolumeAsync | Calculates volume for a session |
| VolumeCalculationService | CalculateVolumeByExerciseTypeAsync | Calculates volume by exercise for a session |
| CalorieCalculationService | CalculateSessionCaloriesAsync | Calculates calories for a session |

## 4. Model References

Session model has a relationship with these models that need to be addressed:

| Related Model | Relationship Type | Migration Approach |
|--------------|------------------|-------------------|
| User | Many-to-one | Update to use WorkoutSession |
| Set | One-to-many | Map to WorkoutExercise/WorkoutSet |
| ShareToken | Referenced by | Update to reference WorkoutSession |

## 5. Key Component Analysis

### 5.1. Session-Related Pages

All pages in the `/Pages/Sessions/` folder will need to be significantly rewritten to use `WorkoutSession` instead of `Session`. This includes:

- Updating model bindings
- Updating property references
- Changing database queries
- Adjusting views to display `WorkoutSession` properties

### 5.2. Data Context

The data context has a `DbSet<Session>` that will need to be phased out. However, we need to ensure session-specific queries continue to work during the migration period.

### 5.3. Special Cases

The `/Pages/Sessions/Reschedule.cshtml.cs` file contains complex logic that manages both Session and WorkoutSession models together, showing the current linking between these models. This file serves as a good reference for building migration scripts.

### 5.4. API Endpoints

The `SharedWorkoutController` provides API access to session data, which will need to be updated to provide WorkoutSession data instead, with appropriate schema changes for API consumers.

## 6. ShareToken Service

The ShareToken model references SessionId, which will need to be updated to reference WorkoutSessionId.

## 7. Page-Level Implementation Notes

1. **Session Index Page:**
   - Currently uses pagination through `PaginatedList<Session>`
   - Links sessions with workout session status via `session.WorkoutSessionStatus = workoutSession.Status`
   - Will need to be rewritten to use WorkoutSession directly

2. **Session Details Page:**
   - Heavily references the Sets and Reps collections
   - Uses volume and calorie calculation services
   - Will need redesign to use new WorkoutExercise and WorkoutSet hierarchies

3. **SharedController:**
   - Provides HTML views for shared sessions
   - Uses the _ShareToken for access control
   - Will need updates for new model structure while preserving access control

## 8. Database Migration Considerations

- Creates a migration that adds a `SessionId` foreign key to WorkoutSession
- Creates a view that maps Session data to WorkoutSession format for backward compatibility
- Updates ShareToken table to reference WorkoutSessionId instead of SessionId
- Ensures no data is lost during migration
