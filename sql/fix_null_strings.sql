-- SQL Script to address NULL string columns in WorkoutTracker database
-- Identify and update NULL string values to empty strings

-- Update NULL name fields in WorkoutSession table
UPDATE WorkoutSession SET Name = '' WHERE Name IS NULL;

-- Update NULL description fields in WorkoutSession table
UPDATE WorkoutSession SET Description = '' WHERE Description IS NULL;

-- Update NULL notes fields in WorkoutSession table
UPDATE WorkoutSession SET Notes = '' WHERE Notes IS NULL;

-- Update NULL name fields in ExerciseType table
UPDATE ExerciseType SET Name = '' WHERE Name IS NULL;

-- Update NULL description fields in ExerciseType table
UPDATE ExerciseType SET Description = '' WHERE Description IS NULL;

-- Update NULL Type, Muscle, Equipment and Difficulty fields in ExerciseType table
UPDATE ExerciseType SET Type = '' WHERE Type IS NULL;
UPDATE ExerciseType SET Muscle = '' WHERE Muscle IS NULL;
UPDATE ExerciseType SET Equipment = '' WHERE Equipment IS NULL;
UPDATE ExerciseType SET Difficulty = '' WHERE Difficulty IS NULL;

-- Update NULL name fields in WorkoutExercise table 
UPDATE WorkoutExercise SET Name = '' WHERE Name IS NULL;

-- Update all other potentially problematic string fields
-- This is a safety net for any other string columns that might have NULL values
DECLARE @schema_name NVARCHAR(128) = N'dbo'
DECLARE @table_name NVARCHAR(128)
DECLARE @column_name NVARCHAR(128)
DECLARE @sql NVARCHAR(MAX)

-- Cursor to loop through all varchar/nvarchar columns in the database
DECLARE cursor_nullable_strings CURSOR FOR
SELECT 
    t.name as table_name,
    c.name as column_name
FROM 
    sys.columns c
INNER JOIN 
    sys.tables t ON c.object_id = t.object_id
INNER JOIN 
    sys.types ty ON c.user_type_id = ty.user_type_id
WHERE 
    (ty.name IN ('varchar', 'nvarchar', 'char', 'nchar', 'text', 'ntext'))
    AND c.is_nullable = 1
    AND t.type = 'U' -- User defined tables only
    AND SCHEMA_NAME(t.schema_id) = @schema_name

OPEN cursor_nullable_strings

FETCH NEXT FROM cursor_nullable_strings INTO @table_name, @column_name

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Build and execute update statement for each column
    SET @sql = N'UPDATE ' + QUOTENAME(@schema_name) + '.' + QUOTENAME(@table_name) + 
               N' SET ' + QUOTENAME(@column_name) + ' = '''' WHERE ' + 
               QUOTENAME(@column_name) + ' IS NULL'
    
    PRINT 'Executing: ' + @sql
    EXEC sp_executesql @sql
    
    FETCH NEXT FROM cursor_nullable_strings INTO @table_name, @column_name
END

CLOSE cursor_nullable_strings
DEALLOCATE cursor_nullable_strings