-- SQL Script to add indexes for optimizing workout reports queries
-- This will significantly improve performance of chart loading

-- Index for WorkoutSessions table to optimize date range queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkoutSessions_UserId_StartDateTime' AND object_id = OBJECT_ID('WorkoutSessions'))
BEGIN
    PRINT 'Creating index IX_WorkoutSessions_UserId_StartDateTime'
    CREATE NONCLUSTERED INDEX IX_WorkoutSessions_UserId_StartDateTime
    ON WorkoutSessions (UserId, StartDateTime)
    INCLUDE (WorkoutSessionId, Name)
END
ELSE
BEGIN
    PRINT 'Index IX_WorkoutSessions_UserId_StartDateTime already exists'
END

-- Index for WorkoutExercises to improve workout session and exercise type lookup
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkoutExercises_WorkoutSessionId_ExerciseTypeId' AND object_id = OBJECT_ID('WorkoutExercises'))
BEGIN
    PRINT 'Creating index IX_WorkoutExercises_WorkoutSessionId_ExerciseTypeId'
    CREATE NONCLUSTERED INDEX IX_WorkoutExercises_WorkoutSessionId_ExerciseTypeId
    ON WorkoutExercises (WorkoutSessionId, ExerciseTypeId)
END
ELSE
BEGIN
    PRINT 'Index IX_WorkoutExercises_WorkoutSessionId_ExerciseTypeId already exists'
END

-- Index for WorkoutExercises to improve exercise type filtering with includes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkoutExercises_ExerciseTypeId' AND object_id = OBJECT_ID('WorkoutExercises'))
BEGIN
    PRINT 'Creating index IX_WorkoutExercises_ExerciseTypeId'
    CREATE NONCLUSTERED INDEX IX_WorkoutExercises_ExerciseTypeId
    ON WorkoutExercises (ExerciseTypeId)
    INCLUDE (WorkoutExerciseId, WorkoutSessionId)
END
ELSE
BEGIN
    PRINT 'Index IX_WorkoutExercises_ExerciseTypeId already exists'
END

-- Index for WorkoutSets to optimize weight and reps calculations
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkoutSets_WorkoutExerciseId_Weight_Reps' AND object_id = OBJECT_ID('WorkoutSets'))
BEGIN
    PRINT 'Creating index IX_WorkoutSets_WorkoutExerciseId_Weight_Reps'
    CREATE NONCLUSTERED INDEX IX_WorkoutSets_WorkoutExerciseId_Weight_Reps
    ON WorkoutSets (WorkoutExerciseId)
    INCLUDE (Weight, Reps, IsCompleted)
END
ELSE
BEGIN
    PRINT 'Index IX_WorkoutSets_WorkoutExerciseId_Weight_Reps already exists'
END

-- Index for ExerciseType to optimize name and muscle group lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExerciseType_PrimaryMuscleGroup_Type' AND object_id = OBJECT_ID('ExerciseType'))
BEGIN
    PRINT 'Creating index IX_ExerciseType_PrimaryMuscleGroup_Type'
    CREATE NONCLUSTERED INDEX IX_ExerciseType_PrimaryMuscleGroup_Type
    ON ExerciseType (PrimaryMuscleGroup, Type)
    INCLUDE (Name)
END
ELSE
BEGIN
    PRINT 'Index IX_ExerciseType_PrimaryMuscleGroup_Type already exists'
END

-- Optimize calorie chart data queries with a covering index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkoutSets_IsCompleted_WeightReps' AND object_id = OBJECT_ID('WorkoutSets'))
BEGIN
    PRINT 'Creating index IX_WorkoutSets_IsCompleted_WeightReps'
    CREATE NONCLUSTERED INDEX IX_WorkoutSets_IsCompleted_WeightReps
    ON WorkoutSets (IsCompleted)
    INCLUDE (Weight, Reps) 
    WHERE Weight IS NOT NULL AND Reps IS NOT NULL
END
ELSE
BEGIN
    PRINT 'Index IX_WorkoutSets_IsCompleted_WeightReps already exists'
END

-- Additional index to speed up volume calculations across time periods
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkoutSets_WeightReps' AND object_id = OBJECT_ID('WorkoutSets'))
BEGIN
    PRINT 'Creating index IX_WorkoutSets_WeightReps'
    CREATE NONCLUSTERED INDEX IX_WorkoutSets_WeightReps
    ON WorkoutSets (WorkoutSetId)
    INCLUDE (Weight, Reps)
    WHERE Weight IS NOT NULL AND Reps IS NOT NULL
END
ELSE
BEGIN
    PRINT 'Index IX_WorkoutSets_WeightReps already exists'
END

-- Add index statistics update commands to help query optimizer
EXEC sp_updatestats
PRINT 'Updated index statistics'

PRINT 'All performance indexes have been created successfully'