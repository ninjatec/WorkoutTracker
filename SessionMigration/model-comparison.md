# Session to WorkoutSession Migration - Model Analysis

## Initial Model Comparison

This document analyzes the differences between the Session and WorkoutSession models to identify any missing fields that need to be added to the WorkoutSession model.

## Session Model Properties

| Property Name   | Data Type     | Description                                | Mapped in WorkoutSession? |
|----------------|---------------|--------------------------------------------|-----------------------|
| SessionId      | int           | Primary key                                | Yes (as WorkoutSessionId) |
| Name           | string        | Name of the session                        | Yes |
| datetime       | DateTime      | Date and time of the session               | Yes (as StartDateTime) |
| StartDateTime  | DateTime      | Start time of the session                  | Yes (duplicate of datetime) |
| endtime        | DateTime?     | End time of the session (nullable)         | Yes (as EndDateTime) |
| UserId         | int           | Foreign key to User                        | Yes |
| Notes          | string        | Additional notes for the session           | Yes (as Description) |

## WorkoutSession Model Properties

| Property Name        | Data Type     | Description                                | Mapped from Session? |
|--------------------|---------------|--------------------------------------------|-----------------------|
| WorkoutSessionId   | int           | Primary key                                | Yes (from SessionId) |
| UserId             | int           | Foreign key to User                        | Yes |
| Name               | string        | Name of the session                        | Yes |
| Description        | string        | Additional notes for the session           | Yes (from Notes) |
| StartDateTime      | DateTime      | Start time of the session                  | Yes (from datetime/StartDateTime) |
| EndDateTime        | DateTime?     | End time of the session (nullable)         | Yes (from endtime) |
| CompletedDate      | DateTime?     | Date when the workout was completed        | No (new in WorkoutSession) |
| Duration           | int           | Duration in minutes                        | Computed in Session as TotalWorkoutTime |
| IsCompleted        | bool          | Flag indicating if workout is completed    | No (new in WorkoutSession) |
| TemplatesUsed      | string        | Templates used for this session            | No (new in WorkoutSession) |
| WorkoutTemplateId  | int?          | Foreign key to WorkoutTemplate             | No (new in WorkoutSession) |
| TemplateAssignmentId | int?        | Foreign key to TemplateAssignment         | No (new in WorkoutSession) |
| StartTime          | DateTime?     | Another start time field (possibly redundant) | Possibly duplicate of StartDateTime |
| IsFromCoach        | bool          | Flag indicating if from a coach            | No (new in WorkoutSession) |
| Status             | string        | Status of the workout session              | No (new in WorkoutSession) |

## Missing Properties in WorkoutSession

None detected. The WorkoutSession model already contains all properties from the Session model, though some are named differently.

## Properties for the Migration Model Extension

One additional property is needed to track the relationship between old Session and new WorkoutSession records:

| Property Name | Data Type | Description |
|--------------|-----------|-------------|
| SessionId    | int?      | Foreign key to the original Session (for tracking during migration) |

## Navigation Properties

**Session Navigation Properties:**
- User (one-to-many relationship)
- Sets (one-to-many relationship)

**WorkoutSession Navigation Properties:**
- User (one-to-many relationship)
- WorkoutTemplate (many-to-one relationship)
- WorkoutExercises (one-to-many relationship)
- WorkoutFeedback (one-to-one relationship)

The main difference in relationships is:
- Session -> Set -> Rep hierarchy
- WorkoutSession -> WorkoutExercise -> WorkoutSet hierarchy

## Computed Properties

Session has several NotMapped computed properties:
- TotalWorkoutTime
- TotalVolume
- EstimatedCalories
- WorkoutSessionStatus (used to store status from linked WorkoutSession)

These will need to be recreated in the appropriate services when using WorkoutSession.

## Data Migration Considerations

For a successful migration:
1. Each Session record should have a corresponding WorkoutSession record
2. Sets from Session should be mapped to WorkoutExercises + WorkoutSets in the new model
3. Reps should be converted to appropriate fields in WorkoutSets
4. Any code that references Session should be updated to use WorkoutSession instead
