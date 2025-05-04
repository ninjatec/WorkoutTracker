-- Create a copy of the production database for development
-- This script creates a copy of the production database for development purposes
-- It needs to be run with db owner permissions

-- Check if development database already exists and drop it if it does
IF EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = 'WorkoutTrackerWeb_Dev')
BEGIN
    ALTER DATABASE [WorkoutTrackerWeb_Dev] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [WorkoutTrackerWeb_Dev];
    PRINT 'Existing WorkoutTrackerWeb_Dev database has been dropped.';
END

-- Backup the production database
BACKUP DATABASE [WorkoutTrackerWeb]
TO DISK = 'C:\Temp\WorkoutTrackerWeb_Backup.bak'
WITH COMPRESSION, INIT, STATS = 10;
PRINT 'Production database has been backed up.';

-- Restore the backup as a new development database
RESTORE DATABASE [WorkoutTrackerWeb_Dev]
FROM DISK = 'C:\Temp\WorkoutTrackerWeb_Backup.bak'
WITH 
    MOVE 'WorkoutTrackerWeb' TO 'C:\SQLData\WorkoutTrackerWeb_Dev.mdf', 
    MOVE 'WorkoutTrackerWeb_log' TO 'C:\SQLLog\WorkoutTrackerWeb_Dev_log.ldf',
    REPLACE, STATS = 10;
PRINT 'Production database has been restored as WorkoutTrackerWeb_Dev.';

-- Set development database to SIMPLE recovery model for development purposes
ALTER DATABASE [WorkoutTrackerWeb_Dev] SET RECOVERY SIMPLE;
PRINT 'Development database has been set to SIMPLE recovery model.';

-- Mark this as a development database in a system table
USE [WorkoutTrackerWeb_Dev];
GO

-- Add a version table entry indicating this is a development copy
-- Only if a Versions table exists (from your ApplicationDbContext)
IF OBJECT_ID('dbo.Versions', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Versions WHERE VersionLabel = 'Environment')
    BEGIN
        INSERT INTO dbo.Versions (VersionNumber, VersionLabel, AppliedOn, Description)
        VALUES ('DEV', 'Environment', GETDATE(), 'This is a development copy of the production database');
    END
    ELSE
    BEGIN
        UPDATE dbo.Versions
        SET VersionNumber = 'DEV', 
            AppliedOn = GETDATE(),
            Description = 'This is a development copy of the production database'
        WHERE VersionLabel = 'Environment';
    END
    PRINT 'Development marker has been added to the Versions table.';
END

-- Optionally: Add database users and permissions for the dev database
-- This assumes your application uses the same credentials for both databases

PRINT 'Development database creation complete. WorkoutTrackerWeb_Dev is ready for use.';
GO