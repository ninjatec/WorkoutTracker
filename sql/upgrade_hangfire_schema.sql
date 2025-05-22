-- Upgrade Hangfire schema from version 7 to version 9
-- Prevents "Violation of PRIMARY KEY constraint 'PK_HangFire_Schema'. Cannot insert duplicate key in object 'HangFire.Schema'. The duplicate key value is (9)"

SET NOCOUNT ON;
PRINT 'Starting Hangfire Schema upgrade';

-- Make sure we have the HangFire schema
IF EXISTS (SELECT * FROM sys.schemas WHERE name = 'HangFire')
BEGIN
    -- Check if we have version 7 in the Schema table
    IF EXISTS (SELECT * FROM [HangFire].[Schema] WHERE [Version] = 7)
    BEGIN
        BEGIN TRANSACTION;
        
        BEGIN TRY
            -- Delete version 7
            DELETE FROM [HangFire].[Schema] WHERE [Version] = 7;
            PRINT 'Removed schema version 7';
            
            -- Add version 9 if it doesn't exist
            IF NOT EXISTS (SELECT * FROM [HangFire].[Schema] WHERE [Version] = 9)
            BEGIN
                INSERT INTO [HangFire].[Schema] ([Version]) VALUES (9);
                PRINT 'Added schema version 9';
            END
            ELSE
            BEGIN
                PRINT 'Schema version 9 already exists';
            END
            
            COMMIT TRANSACTION;
            PRINT 'Successfully upgraded Hangfire schema from version 7 to version 9';
        END TRY
        BEGIN CATCH
            ROLLBACK TRANSACTION;
            PRINT 'Error upgrading Hangfire schema: ' + ERROR_MESSAGE();
        END CATCH
    END
    ELSE
    BEGIN
        -- Check if version 9 exists
        IF EXISTS (SELECT * FROM [HangFire].[Schema] WHERE [Version] = 9)
        BEGIN
            PRINT 'Schema is already at version 9 - no upgrade needed';
        END
        ELSE
        BEGIN
            -- If neither version 7 nor 9 exist, add version 9
            INSERT INTO [HangFire].[Schema] ([Version]) VALUES (9);
            PRINT 'Added schema version 9 (no previous version found)';
        END
    END
END
ELSE
BEGIN
    PRINT 'HangFire schema does not exist - run the fix_hangfire_schema.sql script first';
END

PRINT 'Hangfire Schema upgrade script completed';