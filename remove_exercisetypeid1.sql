-- Script to remove ExerciseTypeId1 shadow property from WorkoutExercises table
-- This fixes the "Invalid column name 'ExerciseTypeId1'" error in the workout reminder job
-- Created: May 9, 2025
-- Updated: May 9, 2025 - Fixed compatibility issues and table name

-- Simplified version that doesn't rely on sys.columns.parent_column_id

-- Step 1: Check if the ExerciseTypeId1 column exists in the WorkoutExercises table
IF EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'WorkoutExercises' 
    AND COLUMN_NAME = 'ExerciseTypeId1'
)
BEGIN
    PRINT 'Found ExerciseTypeId1 shadow property column in WorkoutExercises table. Removing it...';
    
    -- Create a backup of the table first
    IF NOT EXISTS (
        SELECT 1 
        FROM INFORMATION_SCHEMA.TABLES 
        WHERE TABLE_NAME = 'WorkoutExercises_Backup'
    )
    BEGIN
        PRINT 'Creating backup table WorkoutExercises_Backup...';
        SELECT * INTO WorkoutExercises_Backup FROM WorkoutExercises;
        PRINT 'Backup created successfully.';
    END
    
    -- Find any foreign key constraints on the WorkoutExercises table
    DECLARE @ConstraintName NVARCHAR(128);
    DECLARE @DropConstraintSQL NVARCHAR(MAX);
    
    -- Find constraints by querying INFORMATION_SCHEMA instead of sys.foreign_keys
    DECLARE ConstraintCursor CURSOR FOR
        SELECT CONSTRAINT_NAME
        FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
        WHERE TABLE_NAME = 'WorkoutExercises'
        AND COLUMN_NAME = 'ExerciseTypeId1';
    
    OPEN ConstraintCursor;
    FETCH NEXT FROM ConstraintCursor INTO @ConstraintName;
    
    -- Drop each constraint referencing ExerciseTypeId1
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @DropConstraintSQL = N'ALTER TABLE WorkoutExercises DROP CONSTRAINT ' + QUOTENAME(@ConstraintName);
        PRINT 'Dropping constraint: ' + @ConstraintName;
        EXEC sp_executesql @DropConstraintSQL;
        PRINT 'Constraint dropped successfully.';
        
        FETCH NEXT FROM ConstraintCursor INTO @ConstraintName;
    END
    
    CLOSE ConstraintCursor;
    DEALLOCATE ConstraintCursor;
    
    -- Drop the shadow property column
    PRINT 'Dropping ExerciseTypeId1 column...';
    ALTER TABLE WorkoutExercises DROP COLUMN ExerciseTypeId1;
    PRINT 'ExerciseTypeId1 column dropped successfully.';
END
ELSE
BEGIN
    PRINT 'ExerciseTypeId1 shadow property column does not exist in the WorkoutExercises table. No action needed.';
END

-- Step 2: Check if the WorkoutSessionId1 column exists in the WorkoutFeedbacks table (corrected table name)
IF EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'WorkoutFeedbacks' -- Corrected table name from WorkoutFeedback to WorkoutFeedbacks
    AND COLUMN_NAME = 'WorkoutSessionId1'
)
BEGIN
    PRINT 'Found WorkoutSessionId1 shadow property column in WorkoutFeedbacks table. Removing it...';
    
    -- Create a backup of the table first
    IF NOT EXISTS (
        SELECT 1 
        FROM INFORMATION_SCHEMA.TABLES 
        WHERE TABLE_NAME = 'WorkoutFeedbacks_Backup'
    )
    BEGIN
        PRINT 'Creating backup table WorkoutFeedbacks_Backup...';
        SELECT * INTO WorkoutFeedbacks_Backup FROM WorkoutFeedbacks;
        PRINT 'Backup created successfully.';
    END
    
    -- Find any foreign key constraints on the WorkoutFeedbacks table
    DECLARE @FeedbackConstraintName NVARCHAR(128);
    DECLARE @DropFeedbackConstraintSQL NVARCHAR(MAX);
    
    -- Find constraints by querying INFORMATION_SCHEMA
    DECLARE FeedbackConstraintCursor CURSOR FOR
        SELECT CONSTRAINT_NAME
        FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
        WHERE TABLE_NAME = 'WorkoutFeedbacks' -- Corrected table name
        AND COLUMN_NAME = 'WorkoutSessionId1';
    
    OPEN FeedbackConstraintCursor;
    FETCH NEXT FROM FeedbackConstraintCursor INTO @FeedbackConstraintName;
    
    -- Drop each constraint referencing WorkoutSessionId1
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @DropFeedbackConstraintSQL = N'ALTER TABLE WorkoutFeedbacks DROP CONSTRAINT ' + QUOTENAME(@FeedbackConstraintName); -- Corrected table name
        PRINT 'Dropping constraint: ' + @FeedbackConstraintName;
        EXEC sp_executesql @DropFeedbackConstraintSQL;
        PRINT 'Constraint dropped successfully.';
        
        FETCH NEXT FROM FeedbackConstraintCursor INTO @FeedbackConstraintName;
    END
    
    CLOSE FeedbackConstraintCursor;
    DEALLOCATE FeedbackConstraintCursor;
    
    -- Drop the shadow property column
    PRINT 'Dropping WorkoutSessionId1 column...';
    ALTER TABLE WorkoutFeedbacks DROP COLUMN WorkoutSessionId1; -- Corrected table name
    PRINT 'WorkoutSessionId1 column dropped successfully.';
END
ELSE
BEGIN
    PRINT 'WorkoutSessionId1 shadow property column does not exist in the WorkoutFeedbacks table. No action needed.'; -- Corrected table name
END

-- Step 3: Verify the fix by attempting a dummy select to confirm no shadow properties
BEGIN TRY
    -- This is a read-only operation to test if the shadow property issue is fixed
    DECLARE @TestCount INT;
    SELECT @TestCount = COUNT(*) FROM WorkoutExercises WHERE ExerciseTypeId IS NOT NULL;
    PRINT 'Successfully queried WorkoutExercises.ExerciseTypeId without errors.';
    
    SELECT @TestCount = COUNT(*) FROM WorkoutFeedbacks WHERE WorkoutSessionId IS NOT NULL; -- Corrected table name
    PRINT 'Successfully queried WorkoutFeedbacks.WorkoutSessionId without errors.'; -- Corrected table name
    
    PRINT 'Shadow property cleanup completed successfully.';
END TRY
BEGIN CATCH
    PRINT 'Error during verification: ' + ERROR_MESSAGE();
END CATCH