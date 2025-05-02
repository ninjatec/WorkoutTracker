
-- SQL Script to remove legacy Session tables as part of Phase 5: Cleanup and Testing
-- This script uses TRY/CATCH blocks to handle errors gracefully

-- Print database name to confirm connection
PRINT 'Connected to database: ' + DB_NAME();

-- First, print information about the tables we're going to drop
PRINT 'Checking for Session and Set tables...';
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Session')
    PRINT 'Session table exists and will be dropped';
ELSE
    PRINT 'Session table does not exist';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Set')
    PRINT 'Set table exists and will be dropped';
ELSE
    PRINT 'Set table does not exist';

-- Step 1: Drop all indexes on the tables we want to remove
PRINT 'Dropping indexes...';
DECLARE @dropIndex NVARCHAR(MAX);
DECLARE @indexName NVARCHAR(128);
DECLARE @tableName NVARCHAR(128);
DECLARE @schemaName NVARCHAR(128);

-- Get indexes on Rep related to SetsSetId
DECLARE indexCursor CURSOR FOR
    SELECT i.name, OBJECT_NAME(i.object_id), SCHEMA_NAME(t.schema_id)
    FROM sys.indexes i
    JOIN sys.tables t ON i.object_id = t.object_id
    JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
    WHERE OBJECT_NAME(i.object_id) = 'Rep'
    AND c.name = 'SetsSetId'
    AND i.name IS NOT NULL;

OPEN indexCursor;
FETCH NEXT FROM indexCursor INTO @indexName, @tableName, @schemaName;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Dropping index: ' + @indexName + ' on table: ' + @tableName;
    SET @dropIndex = 'BEGIN TRY DROP INDEX ' + QUOTENAME(@indexName) + ' ON ' + QUOTENAME(@schemaName) + '.' + QUOTENAME(@tableName) + '; END TRY BEGIN CATCH PRINT ''Could not drop index: ' + @indexName + ', error: '' + ERROR_MESSAGE(); END CATCH;';
    EXEC sp_executesql @dropIndex;
    FETCH NEXT FROM indexCursor INTO @indexName, @tableName, @schemaName;
END
CLOSE indexCursor;
DEALLOCATE indexCursor;

-- Get all indexes on Set table
DECLARE indexCursor CURSOR FOR
    SELECT i.name, OBJECT_NAME(i.object_id), SCHEMA_NAME(t.schema_id)
    FROM sys.indexes i
    JOIN sys.tables t ON i.object_id = t.object_id
    WHERE OBJECT_NAME(i.object_id) = 'Set'
    AND i.name IS NOT NULL
    AND i.is_primary_key = 0;

OPEN indexCursor;
FETCH NEXT FROM indexCursor INTO @indexName, @tableName, @schemaName;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Dropping index: ' + @indexName + ' on table: ' + @tableName;
    SET @dropIndex = 'BEGIN TRY DROP INDEX ' + QUOTENAME(@indexName) + ' ON ' + QUOTENAME(@schemaName) + '.' + QUOTENAME(@tableName) + '; END TRY BEGIN CATCH PRINT ''Could not drop index: ' + @indexName + ', error: '' + ERROR_MESSAGE(); END CATCH;';
    EXEC sp_executesql @dropIndex;
    FETCH NEXT FROM indexCursor INTO @indexName, @tableName, @schemaName;
END
CLOSE indexCursor;
DEALLOCATE indexCursor;

-- Get all indexes on Session table
DECLARE indexCursor CURSOR FOR
    SELECT i.name, OBJECT_NAME(i.object_id), SCHEMA_NAME(t.schema_id)
    FROM sys.indexes i
    JOIN sys.tables t ON i.object_id = t.object_id
    WHERE OBJECT_NAME(i.object_id) = 'Session'
    AND i.name IS NOT NULL
    AND i.is_primary_key = 0;

OPEN indexCursor;
FETCH NEXT FROM indexCursor INTO @indexName, @tableName, @schemaName;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Dropping index: ' + @indexName + ' on table: ' + @tableName;
    SET @dropIndex = 'BEGIN TRY DROP INDEX ' + QUOTENAME(@indexName) + ' ON ' + QUOTENAME(@schemaName) + '.' + QUOTENAME(@tableName) + '; END TRY BEGIN CATCH PRINT ''Could not drop index: ' + @indexName + ', error: '' + ERROR_MESSAGE(); END CATCH;';
    EXEC sp_executesql @dropIndex;
    FETCH NEXT FROM indexCursor INTO @indexName, @tableName, @schemaName;
END
CLOSE indexCursor;
DEALLOCATE indexCursor;

-- Step 2: Drop all foreign keys that reference the Session and Set tables
PRINT 'Dropping foreign keys...';
DECLARE @dropConstraint NVARCHAR(MAX);
DECLARE @constraintName NVARCHAR(128);

-- Get foreign keys that reference Set and Session
DECLARE constraintCursor CURSOR FOR
    SELECT f.name, OBJECT_NAME(f.parent_object_id), SCHEMA_NAME(t.schema_id)
    FROM sys.foreign_keys f
    JOIN sys.tables t ON f.parent_object_id = t.object_id
    WHERE OBJECT_NAME(f.referenced_object_id) IN ('Set', 'Session');

OPEN constraintCursor;
FETCH NEXT FROM constraintCursor INTO @constraintName, @tableName, @schemaName;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Dropping constraint: ' + @constraintName + ' on table: ' + @tableName;
    SET @dropConstraint = 'BEGIN TRY ALTER TABLE ' + QUOTENAME(@schemaName) + '.' + QUOTENAME(@tableName) + ' DROP CONSTRAINT ' + QUOTENAME(@constraintName) + '; END TRY BEGIN CATCH PRINT ''Could not drop constraint: ' + @constraintName + ', error: '' + ERROR_MESSAGE(); END CATCH;';
    EXEC sp_executesql @dropConstraint;
    FETCH NEXT FROM constraintCursor INTO @constraintName, @tableName, @schemaName;
END
CLOSE constraintCursor;
DEALLOCATE constraintCursor;

-- Get foreign keys on Set and Session tables
DECLARE constraintCursor CURSOR FOR
    SELECT f.name, OBJECT_NAME(f.parent_object_id), SCHEMA_NAME(t.schema_id)
    FROM sys.foreign_keys f
    JOIN sys.tables t ON f.parent_object_id = t.object_id
    WHERE OBJECT_NAME(f.parent_object_id) IN ('Set', 'Session');

OPEN constraintCursor;
FETCH NEXT FROM constraintCursor INTO @constraintName, @tableName, @schemaName;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Dropping constraint: ' + @constraintName + ' on table: ' + @tableName;
    SET @dropConstraint = 'BEGIN TRY ALTER TABLE ' + QUOTENAME(@schemaName) + '.' + QUOTENAME(@tableName) + ' DROP CONSTRAINT ' + QUOTENAME(@constraintName) + '; END TRY BEGIN CATCH PRINT ''Could not drop constraint: ' + @constraintName + ', error: '' + ERROR_MESSAGE(); END CATCH;';
    EXEC sp_executesql @dropConstraint;
    FETCH NEXT FROM constraintCursor INTO @constraintName, @tableName, @schemaName;
END
CLOSE constraintCursor;
DEALLOCATE constraintCursor;

-- Step 3: Drop columns that reference the legacy tables
PRINT 'Dropping columns that reference legacy tables...';

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Rep') AND name = 'SetsSetId')
BEGIN
    PRINT 'Dropping SetsSetId column from Rep';
    BEGIN TRY
        ALTER TABLE Rep DROP COLUMN SetsSetId;
        PRINT 'Successfully dropped SetsSetId column from Rep';
    END TRY
    BEGIN CATCH
        PRINT 'Error dropping SetsSetId column from Rep: ' + ERROR_MESSAGE();
    END CATCH
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ShareToken') AND name = 'SessionId')
BEGIN
    PRINT 'Dropping SessionId column from ShareToken';
    BEGIN TRY
        ALTER TABLE ShareToken DROP COLUMN SessionId;
        PRINT 'Successfully dropped SessionId column from ShareToken';
    END TRY
    BEGIN CATCH
        PRINT 'Error dropping SessionId column from ShareToken: ' + ERROR_MESSAGE();
    END CATCH
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ProgressionHistories') AND name = 'SessionId')
BEGIN
    PRINT 'Dropping SessionId column from ProgressionHistories';
    BEGIN TRY
        ALTER TABLE ProgressionHistories DROP COLUMN SessionId;
        PRINT 'Successfully dropped SessionId column from ProgressionHistories';
    END TRY
    BEGIN CATCH
        PRINT 'Error dropping SessionId column from ProgressionHistories: ' + ERROR_MESSAGE();
    END CATCH
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutFeedbacks') AND name = 'SessionId')
BEGIN
    PRINT 'Dropping SessionId column from WorkoutFeedbacks';
    BEGIN TRY
        ALTER TABLE WorkoutFeedbacks DROP COLUMN SessionId;
        PRINT 'Successfully dropped SessionId column from WorkoutFeedbacks';
    END TRY
    BEGIN CATCH
        PRINT 'Error dropping SessionId column from WorkoutFeedbacks: ' + ERROR_MESSAGE();
    END CATCH
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutSessions') AND name = 'SessionId')
BEGIN
    PRINT 'Dropping SessionId column from WorkoutSessions';
    BEGIN TRY
        ALTER TABLE WorkoutSessions DROP COLUMN SessionId;
        PRINT 'Successfully dropped SessionId column from WorkoutSessions';
    END TRY
    BEGIN CATCH
        PRINT 'Error dropping SessionId column from WorkoutSessions: ' + ERROR_MESSAGE();
    END CATCH
END

-- Step 4: Drop the legacy tables
PRINT 'Dropping legacy tables...';
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Set')
BEGIN
    PRINT 'Dropping Set table';
    BEGIN TRY
        DROP TABLE [Set];
        PRINT 'Successfully dropped Set table';
    END TRY
    BEGIN CATCH
        PRINT 'Error dropping Set table: ' + ERROR_MESSAGE();
    END CATCH
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Session')
BEGIN
    PRINT 'Dropping Session table';
    BEGIN TRY
        DROP TABLE [Session];
        PRINT 'Successfully dropped Session table';
    END TRY
    BEGIN CATCH
        PRINT 'Error dropping Session table: ' + ERROR_MESSAGE();
    END CATCH
END

PRINT 'Script execution complete. Session Migration Phase 5 cleanup completed.'

