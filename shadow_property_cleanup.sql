-- This script handles the removal of shadow property constraints
-- and performs other database cleanup tasks related to the DB context merge

-- Step 1: Check if shadow property constraint exists and drop it if found
IF EXISTS (
    SELECT 1 
    FROM sys.foreign_keys 
    WHERE name = 'FK_CoachClientRelationships_AppUser_AppUserId'
)
BEGIN
    PRINT 'Found shadow property constraint FK_CoachClientRelationships_AppUser_AppUserId, removing it...';
    ALTER TABLE CoachClientRelationships DROP CONSTRAINT FK_CoachClientRelationships_AppUser_AppUserId;
    PRINT 'Shadow property constraint removed successfully';
END
ELSE
BEGIN
    PRINT 'Shadow property constraint FK_CoachClientRelationships_AppUser_AppUserId not found, continuing...';
END

-- Step 2: Check for potential shadow property column and remove if exists
IF EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE name = 'AppUserId' 
    AND object_id = OBJECT_ID('CoachClientRelationships')
)
BEGIN
    PRINT 'Found shadow property column AppUserId in CoachClientRelationships, removing it...';
    ALTER TABLE CoachClientRelationships DROP COLUMN AppUserId;
    PRINT 'Shadow property column removed successfully';
END
ELSE
BEGIN
    PRINT 'Shadow property column AppUserId not found in CoachClientRelationships, continuing...';
END

-- Step 3: Check for other potential shadow property constraints
DECLARE @ConstraintNames TABLE (ConstraintName NVARCHAR(128));

-- Find potential shadow property constraints
INSERT INTO @ConstraintNames
SELECT fk.name
FROM sys.foreign_keys fk
WHERE fk.name LIKE '%AppUserId%' OR fk.name LIKE '%AppUser_%'
   OR fk.name LIKE '%_AppUser' OR fk.name LIKE '%_AppUser_%';

-- Drop each constraint found
DECLARE @ConstraintName NVARCHAR(128);
DECLARE @TableName NVARCHAR(128);
DECLARE @SQL NVARCHAR(MAX);

DECLARE constraint_cursor CURSOR FOR 
SELECT fk.name, OBJECT_NAME(fk.parent_object_id)
FROM sys.foreign_keys fk
WHERE fk.name IN (SELECT ConstraintName FROM @ConstraintNames);

OPEN constraint_cursor;
FETCH NEXT FROM constraint_cursor INTO @ConstraintName, @TableName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @SQL = 'ALTER TABLE [' + @TableName + '] DROP CONSTRAINT [' + @ConstraintName + ']';
    PRINT 'Dropping constraint: ' + @ConstraintName + ' from table: ' + @TableName;
    
    BEGIN TRY
        EXEC sp_executesql @SQL;
        PRINT 'Successfully dropped constraint: ' + @ConstraintName;
    END TRY
    BEGIN CATCH
        PRINT 'Error dropping constraint: ' + @ConstraintName + ' - ' + ERROR_MESSAGE();
    END CATCH
    
    FETCH NEXT FROM constraint_cursor INTO @ConstraintName, @TableName;
END

CLOSE constraint_cursor;
DEALLOCATE constraint_cursor;

-- Step 4: Verify database tables - report tables that should exist
DECLARE @ValidationResult TABLE (
    TableName NVARCHAR(255),
    Status NVARCHAR(50)
);

-- Validate identity tables
INSERT INTO @ValidationResult
SELECT 'AspNetUsers', CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUsers') THEN 'Exists' ELSE 'Missing' END
UNION SELECT 'AspNetRoles', CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetRoles') THEN 'Exists' ELSE 'Missing' END
UNION SELECT 'AspNetUserRoles', CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserRoles') THEN 'Exists' ELSE 'Missing' END
UNION SELECT 'AspNetUserClaims', CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserClaims') THEN 'Exists' ELSE 'Missing' END
UNION SELECT 'AspNetUserLogins', CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserLogins') THEN 'Exists' ELSE 'Missing' END
UNION SELECT 'AspNetRoleClaims', CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetRoleClaims') THEN 'Exists' ELSE 'Missing' END
UNION SELECT 'AspNetUserTokens', CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserTokens') THEN 'Exists' ELSE 'Missing' END

-- Validate other tables
UNION SELECT 'LogLevelSettings', CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LogLevelSettings') THEN 'Exists' ELSE 'Missing' END
UNION SELECT 'LogLevelOverrides', CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LogLevelOverrides') THEN 'Exists' ELSE 'Missing' END
UNION SELECT 'WhitelistedIps', CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'WhitelistedIps') THEN 'Exists' ELSE 'Missing' END
UNION SELECT 'Versions', CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Versions') THEN 'Exists' ELSE 'Missing' END;

-- Report validation results for each table
SELECT TableName, Status FROM @ValidationResult ORDER BY TableName;

-- Step 5: Mark the migration as applied in EF Core's migration history
-- This allows us to skip the problematic migration and continue with future migrations
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250508185537_ConsolidatedDatabaseSchema')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20250508185537_ConsolidatedDatabaseSchema', '9.0.0-preview.1.24081.2');
    
    PRINT 'Migration marked as applied in __EFMigrationsHistory table';
END
ELSE
BEGIN
    PRINT 'Migration was already marked as applied in __EFMigrationsHistory table';
END

-- Script to clean up shadow property foreign keys that may be causing conflicts
-- This script safely checks for and drops problematic constraints

-- Check and drop FK_ClientActivities_AppUser_ClientId if it exists
IF EXISTS (
    SELECT * 
    FROM sys.foreign_keys 
    WHERE name = 'FK_ClientActivities_AppUser_ClientId'
)
BEGIN
    ALTER TABLE [ClientActivities] DROP CONSTRAINT [FK_ClientActivities_AppUser_ClientId];
    PRINT 'Dropped constraint: FK_ClientActivities_AppUser_ClientId';
END
ELSE
BEGIN
    PRINT 'Constraint FK_ClientActivities_AppUser_ClientId does not exist.';
END

-- Check and drop FK_ClientActivities_AppUser_CoachId if it exists
IF EXISTS (
    SELECT * 
    FROM sys.foreign_keys 
    WHERE name = 'FK_ClientActivities_AppUser_CoachId'
)
BEGIN
    ALTER TABLE [ClientActivities] DROP CONSTRAINT [FK_ClientActivities_AppUser_CoachId];
    PRINT 'Dropped constraint: FK_ClientActivities_AppUser_CoachId';
END
ELSE
BEGIN
    PRINT 'Constraint FK_ClientActivities_AppUser_CoachId does not exist.';
END

-- Other shadow property constraints that might be causing issues
-- Add any additional constraints here that need to be dropped

-- Create the proper relationships if they don't exist
-- This ensures the correct FK constraints are in place
IF NOT EXISTS (
    SELECT * 
    FROM sys.foreign_keys 
    WHERE name = 'FK_ClientActivities_AspNetUsers_ClientId'
)
BEGIN
    ALTER TABLE [ClientActivities] 
    ADD CONSTRAINT [FK_ClientActivities_AspNetUsers_ClientId] 
    FOREIGN KEY ([ClientId]) REFERENCES [AspNetUsers]([Id])
    ON DELETE NO ACTION;
    
    PRINT 'Added constraint: FK_ClientActivities_AspNetUsers_ClientId';
END
ELSE
BEGIN
    PRINT 'Constraint FK_ClientActivities_AspNetUsers_ClientId already exists.';
END

IF NOT EXISTS (
    SELECT * 
    FROM sys.foreign_keys 
    WHERE name = 'FK_ClientActivities_AspNetUsers_CoachId'
)
BEGIN
    ALTER TABLE [ClientActivities] 
    ADD CONSTRAINT [FK_ClientActivities_AspNetUsers_CoachId] 
    FOREIGN KEY ([CoachId]) REFERENCES [AspNetUsers]([Id])
    ON DELETE NO ACTION;
    
    PRINT 'Added constraint: FK_ClientActivities_AspNetUsers_CoachId';
END
ELSE
BEGIN
    PRINT 'Constraint FK_ClientActivities_AspNetUsers_CoachId already exists.';
END

-- Final check for any remaining shadow properties in the ClientActivities table
SELECT 
    c.name AS ColumnName,
    t.name AS TableName
FROM 
    sys.columns c
    JOIN sys.tables t ON c.object_id = t.object_id
WHERE 
    t.name = 'ClientActivities' 
    AND c.name LIKE '%1' -- Shadow properties often have numeric suffixes
    AND c.name NOT IN ('ClientId', 'CoachId'); -- Exclude legitimate columns

-- Print completion message
PRINT 'Shadow property cleanup complete.';

PRINT 'Database shadow property cleanup completed successfully';