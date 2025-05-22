-- Add CaloriesBurned column to WorkoutSession table
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'WorkoutSession' 
    AND COLUMN_NAME = 'CaloriesBurned'
)
BEGIN
    PRINT 'Adding CaloriesBurned column to WorkoutSession table'
    ALTER TABLE WorkoutSession
    ADD CaloriesBurned DECIMAL(18, 2) NULL;
END
ELSE
BEGIN
    PRINT 'CaloriesBurned column already exists in WorkoutSession table'
END