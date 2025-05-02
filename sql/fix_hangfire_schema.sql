-- Fix for Hangfire Schema issue
-- Prevents "Violation of PRIMARY KEY constraint 'PK_HangFire_Schema'. Cannot insert duplicate key in object 'HangFire.Schema'. The duplicate key value is (9)"

PRINT 'Starting Hangfire Schema repair script';

-- Check if Hangfire schema exists
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'HangFire')
BEGIN
    EXEC('CREATE SCHEMA [HangFire]');
    PRINT 'Created HangFire schema';
END
ELSE
BEGIN
    PRINT 'HangFire schema already exists';
END

-- Check if Schema table exists and create it if missing
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[HangFire].[Schema]') AND type = N'U')
BEGIN
    CREATE TABLE [HangFire].[Schema] (
        [Version] [int] NOT NULL,
        CONSTRAINT [PK_HangFire_Schema] PRIMARY KEY CLUSTERED ([Version] ASC)
    );
    PRINT 'Created [HangFire].[Schema] table';
    
    -- Insert the latest schema version (9) since table was newly created
    INSERT INTO [HangFire].[Schema] ([Version]) VALUES (9);
    PRINT 'Inserted schema version 9';
END
ELSE
BEGIN
    PRINT '[HangFire].[Schema] table already exists';
    
    -- Check if version 9 already exists in the Schema table
    IF NOT EXISTS (SELECT * FROM [HangFire].[Schema] WHERE [Version] = 9)
    BEGIN
        -- Delete older versions and insert version 9
        DELETE FROM [HangFire].[Schema];
        INSERT INTO [HangFire].[Schema] ([Version]) VALUES (9);
        PRINT 'Cleaned up schema versions and inserted version 9';
    END
    ELSE
    BEGIN
        PRINT 'Schema version 9 already exists - no changes needed';
    END
END

PRINT 'Hangfire Schema repair completed';