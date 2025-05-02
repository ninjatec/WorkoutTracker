# Session to WorkoutSession Migration
## Statement of Works

## Overview

This document outlines the step-by-step process for migrating from the legacy `Session` table to the new `WorkoutSession` table in the WorkoutTracker application. The migration aims to consolidate all workout tracking functionality into the newer, more feature-rich `WorkoutSession` model.

## Current Architecture

The application currently uses two parallel data models for tracking workout sessions:

1. **Legacy Model**: `Session` → `Set` → `Rep`
2. **New Model**: `WorkoutSession` → `WorkoutExercise` → `WorkoutSet`

The current implementation maintains both models and synchronizes data between them. This dual approach creates complexity, potential data inconsistency, and maintenance overhead.

## Migration Goals

- Migrate all functionality to use the `WorkoutSession` model exclusively
- Preserve all historical workout data
- Update all relevant components (Quick Workouts, Scheduled Workouts, Data Import/Export)
- Minimize downtime during the migration
- Simplify the codebase by removing legacy model dependencies

## Pre-Migration Tasks

1. **Code Audit**
   - Identify all code paths that reference the `Session` table
   - Document all services, controllers, and pages that interact with Sessions
   - Review all relationships between `Session` and other entities

2. **Data Assessment**
   - Validate that all existing `Session` records have corresponding `WorkoutSession` records
   - Identify any orphaned records or inconsistencies

3. **Backup**
   - Complete database backup
   - Code repository snapshot

## Migration Steps

### Phase 1: Preparation and Documentation [COMPLETED]

1. **Create Migration Branch**
   - Create a new git branch for the migration work
   - Document all planned changes

2. **Extend WorkoutSession Model**
   - Add any missing fields needed to fully replace Session functionality
   - Create a migration script to add these fields

3. **Create Database Views**
   - Create views to simplify querying during the transition period

### Phase 2: Service Layer Updates [COMPLETED]

1. **Update WorkoutDataService**
   - Modify the data export service to use WorkoutSession directly
   - Update the data import service to create WorkoutSession records

2. **Update QuickWorkoutService**
   - Refactor to use WorkoutSession instead of Session
   - Update methods for creating and retrieving workout sessions

3. **Update ScheduledWorkoutProcessorService**
   - Remove dual-creation of Session and WorkoutSession
   - Update all methods to work with WorkoutSession only

4. **Update VolumeCalculationService**
   - Modify to calculate volume from WorkoutSets instead of Sets

### Phase 3: Controller and Page Updates [COMPLETED]

1. **Update Session-Related Pages**
   - Modify all Razor Pages that currently use the Session model
   - Update controllers to use the WorkoutSession-based services

2. **Update ViewModels and DTOs**
   - Update all ViewModels to use WorkoutSession properties
   - Update all DTOs that contain Session-related data

### Phase 4: Data Migration [COMPLETED]

1. **Sync Missing Data** ✓
   - Created script to ensure all Session records have WorkoutSession equivalents
   - Verified data integrity between the models
   - Successfully migrated all historical data

2. **Update Foreign Keys** ✓
   - Modified tables that reference SessionId to reference WorkoutSessionId
   - Updated ShareToken table schema and data
   - Verified all foreign key relationships

### Phase 5: Cleanup and Testing [COMPLETED]

1. **Remove Obsolete Code**
   - Remove Session-related services that are no longer needed
   - Clean up duplicated code that maintained both models

2. **Run Integration Tests**
   - Ensure all functionality works with the new model
   - Test all edge cases and user workflows

3. **Prepare for Database Schema Change**
   - Create migration scripts to remove legacy tables once migration is complete
   - Applied database schema changes to remove legacy Session tables

### Phase 6: Deployment

1. **Deployment Planning**
   - Schedule maintenance window
   - Prepare rollback plan

2. **Execute Migration**
   - Deploy updated code
   - Run final data migration scripts
   - Verify all functionality

## Detailed Implementation Guide

### 1. Database Migration Scripts

#### 1.1 Create Missing WorkoutSession Records

```sql
-- Find Sessions without corresponding WorkoutSession
INSERT INTO WorkoutSessions (Name, Description, UserId, StartDateTime, Status, SessionId)
SELECT s.Name, s.Notes, s.UserId, s.datetime, 'Migrated', s.SessionId
FROM Session s
LEFT JOIN WorkoutSessions ws ON ws.SessionId = s.SessionId
WHERE ws.WorkoutSessionId IS NULL;
```

#### 1.2 Create WorkoutExercise Records

```sql
-- Create WorkoutExercise records from Sets
INSERT INTO WorkoutExercises (WorkoutSessionId, ExerciseTypeId, SequenceNum, OrderIndex)
SELECT ws.WorkoutSessionId, s.ExerciseTypeId, s.SequenceNum, s.SequenceNum
FROM Set s
JOIN Session sess ON s.SessionId = sess.SessionId
JOIN WorkoutSessions ws ON ws.SessionId = sess.SessionId
WHERE NOT EXISTS (
    SELECT 1
    FROM WorkoutExercises we
    WHERE we.WorkoutSessionId = ws.WorkoutSessionId
    AND we.ExerciseTypeId = s.ExerciseTypeId
    AND we.SequenceNum = s.SequenceNum
)
GROUP BY ws.WorkoutSessionId, s.ExerciseTypeId, s.SequenceNum;
```

#### 1.3 Create WorkoutSet Records

```sql
-- Create WorkoutSet records from Sets
INSERT INTO WorkoutSets (WorkoutExerciseId, SettypeId, SequenceNum, SetNumber, Reps, Weight, Notes)
SELECT 
    we.WorkoutExerciseId,
    s.SettypeId,
    s.SequenceNum,
    ROW_NUMBER() OVER (PARTITION BY we.WorkoutExerciseId ORDER BY s.SequenceNum),
    s.NumberReps,
    s.Weight,
    s.Notes
FROM Set s
JOIN Session sess ON s.SessionId = sess.SessionId
JOIN WorkoutSessions ws ON ws.SessionId = sess.SessionId
JOIN WorkoutExercises we ON we.WorkoutSessionId = ws.WorkoutSessionId AND we.ExerciseTypeId = s.ExerciseTypeId
WHERE NOT EXISTS (
    SELECT 1
    FROM WorkoutSets wss
    WHERE wss.WorkoutExerciseId = we.WorkoutExerciseId
    AND wss.SequenceNum = s.SequenceNum;
```

### 2. Code Changes Map

The following components need to be updated:

#### 2.1 Models

- Ensure `WorkoutSession` has all needed fields from `Session`
- Add any missing navigation properties

#### 2.2 Services

- **QuickWorkoutService**: Update all Session-related methods
- **WorkoutDataService**: Modify export/import/delete functionality
- **ScheduledWorkoutProcessorService**: Remove dual model creation
- **VolumeCalculationService**: Update volume calculations

#### 2.3 Controllers

- Update controllers to use WorkoutSession instead of Session

#### 2.4 Pages

- Update all Session-related Razor Pages
- Modify forms to use WorkoutSession properties

### 3. Testing Plan

1. **Unit Tests**
   - Test all services with modified code
   - Verify data consistency through model changes

2. **Integration Tests**
   - Test end-to-end user workflows
   - Verify scheduled workouts still function properly

3. **UI Tests**
   - Test all pages that previously used Session
   - Verify data display is correct

## Rollback Plan

In case of critical issues, the rollback strategy will be:

1. Restore code from pre-migration branch
2. Keep WorkoutSession records created during migration
3. Restore any Session-related tables that were removed

## Timeline

- **Phase 1-2**: 1 week
- **Phase 3-4**: 1 week
- **Phase 5-6**: 3 days

Total estimated time: 2.5 weeks

## Step-by-Step Guide for Implementation

1. **Create migration branch**
   ```
   git checkout -b feature/session-to-workoutsession-migration
   ```

2. **Create EF Core migration for any model changes**
   ```
   dotnet ef migrations add CompleteWorkoutSessionModel --context WorkoutTrackerWebContext
   dotnet ef database update --context WorkoutTrackerWebContext
   ```

3. **Run data migration scripts** to sync data between models

4. **Update Services one by one**:
   - Start with lower-level services
   - Then update dependent services
   - Finally update UI components

5. **Run comprehensive tests**

6. **Create cleanup migration**
   ```
   dotnet ef migrations add RemoveSessionTables --context WorkoutTrackerWebContext
   ```

7. **Deploy to production during maintenance window**

8. **Verify functionality and monitor for issues**

## Conclusion

This migration will simplify the codebase by removing redundancy and standardizing on the more feature-rich `WorkoutSession` model. While there are many components to update, the benefit of a cleaner architecture and more maintainable codebase justifies the effort.

Upon completion, the application will be easier to extend and maintain, with a more straightforward data model that aligns with the evolving requirements of the WorkoutTracker application.