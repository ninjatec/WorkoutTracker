using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Data.Migrations.WorkoutTrackerWeb
{
    /// <inheritdoc />
    public partial class ConsolidatedDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration is a consolidation migration that ensures our schema is properly configured
            // All tables should already exist at this point, so we'll use SQL directly to perform any necessary validations

            // First, explicitly check for and handle the problematic shadow property constraint
            migrationBuilder.Sql(@"
                -- First check if the problematic constraint exists in database
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
            ");

            // Execute a script to verify table existence and report status
            migrationBuilder.Sql(@"
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

                -- Log validation results for debugging
                DECLARE @ResultsMessage NVARCHAR(MAX) = 'Database Schema Validation:';
                SELECT @ResultsMessage = @ResultsMessage + CHAR(13) + CHAR(10) + TableName + ': ' + Status
                FROM @ValidationResult;

                PRINT @ResultsMessage;
            ");
            
            // Handle migration from AppUser table to AspNetUsers if needed
            migrationBuilder.Sql(@"
                -- Check if AppUser exists but AspNetUsers doesn't
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AppUser') 
                AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUsers')
                BEGIN
                    PRINT 'Migrating AppUser to AspNetUsers';
                    
                    -- Handle any foreign keys to AppUser
                    DECLARE @ForeignKeySql NVARCHAR(MAX) = '';

                    SELECT @ForeignKeySql = @ForeignKeySql + 'ALTER TABLE ' + OBJECT_NAME(fk.parent_object_id) + ' DROP CONSTRAINT ' + fk.name + '; '
                    FROM sys.foreign_keys fk
                    INNER JOIN sys.tables t ON fk.referenced_object_id = t.object_id
                    WHERE t.name = 'AppUser';

                    -- Execute the dynamic SQL to drop foreign keys if any exists
                    IF LEN(@ForeignKeySql) > 0
                    BEGIN
                        EXEC sp_executesql @ForeignKeySql;
                    END
                    
                    -- Rename AppUser to AspNetUsers
                    EXEC sp_rename 'AppUser', 'AspNetUsers';
                    
                    -- Adjust column properties if needed
                    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'UserName')
                    BEGIN
                        ALTER TABLE AspNetUsers ALTER COLUMN UserName NVARCHAR(256) NULL;
                        ALTER TABLE AspNetUsers ALTER COLUMN NormalizedUserName NVARCHAR(256) NULL;
                        ALTER TABLE AspNetUsers ALTER COLUMN Email NVARCHAR(256) NULL;
                        ALTER TABLE AspNetUsers ALTER COLUMN NormalizedEmail NVARCHAR(256) NULL;
                    END
                    
                    -- Add back the primary key if needed
                    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_AspNetUsers')
                    BEGIN
                        ALTER TABLE AspNetUsers ADD CONSTRAINT PK_AspNetUsers PRIMARY KEY CLUSTERED (Id);
                    END
                    
                    PRINT 'AppUser migration completed';
                END
            ");
            
            // Create any missing Identity tables
            migrationBuilder.Sql(@"
                -- Create AspNetRoles if it doesn't exist
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetRoles')
                BEGIN
                    CREATE TABLE AspNetRoles (
                        Id NVARCHAR(450) NOT NULL,
                        Name NVARCHAR(256) NULL,
                        NormalizedName NVARCHAR(256) NULL,
                        ConcurrencyStamp NVARCHAR(MAX) NULL,
                        CONSTRAINT PK_AspNetRoles PRIMARY KEY (Id)
                    );
                    
                    CREATE UNIQUE INDEX RoleNameIndex ON AspNetRoles (NormalizedName) WHERE NormalizedName IS NOT NULL;
                END

                -- Create AspNetUserClaims if it doesn't exist
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserClaims')
                BEGIN
                    CREATE TABLE AspNetUserClaims (
                        Id INT IDENTITY(1,1) NOT NULL,
                        UserId NVARCHAR(450) NOT NULL,
                        ClaimType NVARCHAR(MAX) NULL,
                        ClaimValue NVARCHAR(MAX) NULL,
                        CONSTRAINT PK_AspNetUserClaims PRIMARY KEY (Id)
                    );
                    
                    CREATE INDEX IX_AspNetUserClaims_UserId ON AspNetUserClaims (UserId);
                    
                    ALTER TABLE AspNetUserClaims ADD CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId
                        FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE;
                END

                -- Create AspNetUserLogins if it doesn't exist
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserLogins')
                BEGIN
                    CREATE TABLE AspNetUserLogins (
                        LoginProvider NVARCHAR(450) NOT NULL,
                        ProviderKey NVARCHAR(450) NOT NULL,
                        ProviderDisplayName NVARCHAR(MAX) NULL,
                        UserId NVARCHAR(450) NOT NULL,
                        CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey)
                    );
                    
                    CREATE INDEX IX_AspNetUserLogins_UserId ON AspNetUserLogins (UserId);
                    
                    ALTER TABLE AspNetUserLogins ADD CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId
                        FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE;
                END

                -- Create AspNetUserTokens if it doesn't exist
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserTokens')
                BEGIN
                    CREATE TABLE AspNetUserTokens (
                        UserId NVARCHAR(450) NOT NULL,
                        LoginProvider NVARCHAR(450) NOT NULL,
                        Name NVARCHAR(450) NOT NULL,
                        Value NVARCHAR(MAX) NULL,
                        CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name)
                    );
                    
                    ALTER TABLE AspNetUserTokens ADD CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId
                        FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE;
                END

                -- Create AspNetRoleClaims if it doesn't exist
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetRoleClaims')
                BEGIN
                    CREATE TABLE AspNetRoleClaims (
                        Id INT IDENTITY(1,1) NOT NULL,
                        RoleId NVARCHAR(450) NOT NULL,
                        ClaimType NVARCHAR(MAX) NULL,
                        ClaimValue NVARCHAR(MAX) NULL,
                        CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY (Id)
                    );
                    
                    CREATE INDEX IX_AspNetRoleClaims_RoleId ON AspNetRoleClaims (RoleId);
                    
                    ALTER TABLE AspNetRoleClaims ADD CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId
                        FOREIGN KEY (RoleId) REFERENCES AspNetRoles (Id) ON DELETE CASCADE;
                END

                -- Create AspNetUserRoles if it doesn't exist
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserRoles')
                BEGIN
                    CREATE TABLE AspNetUserRoles (
                        UserId NVARCHAR(450) NOT NULL,
                        RoleId NVARCHAR(450) NOT NULL,
                        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId)
                    );
                    
                    CREATE INDEX IX_AspNetUserRoles_RoleId ON AspNetUserRoles (RoleId);
                    
                    ALTER TABLE AspNetUserRoles ADD CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId
                        FOREIGN KEY (RoleId) REFERENCES AspNetRoles (Id) ON DELETE CASCADE;
                        
                    ALTER TABLE AspNetUserRoles ADD CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId
                        FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE;
                END
            ");
            
            // Create other tables if they don't exist
            migrationBuilder.Sql(@"
                -- Create LogLevelSettings if it doesn't exist
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LogLevelSettings')
                BEGIN
                    CREATE TABLE LogLevelSettings (
                        Id INT IDENTITY(1,1) NOT NULL,
                        DefaultLogLevel INT NOT NULL,
                        LastUpdated DATETIME2 NOT NULL,
                        LastUpdatedBy NVARCHAR(256) NOT NULL,
                        CONSTRAINT PK_LogLevelSettings PRIMARY KEY (Id)
                    );
                END
                
                -- Create LogLevelOverrides if it doesn't exist
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LogLevelOverrides')
                BEGIN
                    CREATE TABLE LogLevelOverrides (
                        Id INT IDENTITY(1,1) NOT NULL,
                        SourceContext NVARCHAR(256) NOT NULL,
                        LogLevel INT NOT NULL,
                        LogLevelSettingsId INT NOT NULL,
                        CONSTRAINT PK_LogLevelOverrides PRIMARY KEY (Id)
                    );
                    
                    CREATE INDEX IX_LogLevelOverrides_LogLevelSettingsId ON LogLevelOverrides (LogLevelSettingsId);
                    
                    ALTER TABLE LogLevelOverrides ADD CONSTRAINT FK_LogLevelOverrides_LogLevelSettings_LogLevelSettingsId
                        FOREIGN KEY (LogLevelSettingsId) REFERENCES LogLevelSettings (Id) ON DELETE CASCADE;
                END
                
                -- Create WhitelistedIps if it doesn't exist
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'WhitelistedIps')
                BEGIN
                    CREATE TABLE WhitelistedIps (
                        Id INT IDENTITY(1,1) NOT NULL,
                        IpAddress NVARCHAR(45) NOT NULL,
                        Description NVARCHAR(255) NULL,
                        CreatedAt DATETIME2 NOT NULL,
                        CreatedBy NVARCHAR(100) NULL,
                        CONSTRAINT PK_WhitelistedIps PRIMARY KEY (Id)
                    );
                    
                    CREATE UNIQUE INDEX IX_WhitelistedIps_IpAddress ON WhitelistedIps (IpAddress);
                END
                
                -- Create Versions if it doesn't exist
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Versions')
                BEGIN
                    CREATE TABLE Versions (
                        VersionId INT IDENTITY(1,1) NOT NULL,
                        Major INT NOT NULL,
                        Minor INT NOT NULL,
                        Patch INT NOT NULL,
                        BuildNumber INT NOT NULL,
                        ReleaseDate DATETIME2 NOT NULL,
                        Description NVARCHAR(100) NOT NULL,
                        GitCommitHash NVARCHAR(40) NULL,
                        IsCurrent BIT NOT NULL,
                        ReleaseNotes NVARCHAR(255) NULL,
                        CONSTRAINT PK_Versions PRIMARY KEY (VersionId)
                    );
                END
            ");

            // Create/Check indexes for AspNetUsers
            migrationBuilder.Sql(@"
                -- Create EmailIndex on AspNetUsers if it doesn't exist
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'EmailIndex' AND object_id = OBJECT_ID('AspNetUsers'))
                    AND EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUsers')
                BEGIN
                    CREATE INDEX EmailIndex ON AspNetUsers (NormalizedEmail);
                END

                -- Create UserNameIndex on AspNetUsers if it doesn't exist
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UserNameIndex' AND object_id = OBJECT_ID('AspNetUsers'))
                    AND EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUsers')
                BEGIN
                    CREATE UNIQUE INDEX UserNameIndex ON AspNetUsers (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
                END
                
                -- Create UserName index on AspNetUsers if it doesn't exist
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetUsers_UserName' AND object_id = OBJECT_ID('AspNetUsers'))
                    AND EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUsers')
                BEGIN
                    CREATE UNIQUE INDEX IX_AspNetUsers_UserName ON AspNetUsers (UserName) WHERE UserName IS NOT NULL;
                END
            ");

            // Update foreign keys for ClientActivities, ClientGroups and other tables to reference AspNetUsers
            migrationBuilder.Sql(@"
                -- Add foreign keys for ClientActivities if needed
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ClientActivities')
                    AND EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUsers')
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ClientActivities_AspNetUsers_ClientId')
                    BEGIN
                        ALTER TABLE ClientActivities ADD CONSTRAINT FK_ClientActivities_AspNetUsers_ClientId
                            FOREIGN KEY (ClientId) REFERENCES AspNetUsers (Id);
                    END

                    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ClientActivities_AspNetUsers_CoachId')
                    BEGIN
                        ALTER TABLE ClientActivities ADD CONSTRAINT FK_ClientActivities_AspNetUsers_CoachId
                            FOREIGN KEY (CoachId) REFERENCES AspNetUsers (Id);
                    END
                END

                -- Add foreign keys for ClientGroups if needed
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ClientGroups')
                    AND EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUsers')
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ClientGroups_AspNetUsers_CoachId')
                    BEGIN
                        ALTER TABLE ClientGroups ADD CONSTRAINT FK_ClientGroups_AspNetUsers_CoachId
                            FOREIGN KEY (CoachId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE;
                    END
                END

                -- Add foreign keys for CoachClientRelationships if needed
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CoachClientRelationships')
                    AND EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUsers')
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CoachClientRelationships_AspNetUsers_ClientId')
                    BEGIN
                        ALTER TABLE CoachClientRelationships ADD CONSTRAINT FK_CoachClientRelationships_AspNetUsers_ClientId
                            FOREIGN KEY (ClientId) REFERENCES AspNetUsers (Id);
                    END

                    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CoachClientRelationships_AspNetUsers_CoachId')
                    BEGIN
                        ALTER TABLE CoachClientRelationships ADD CONSTRAINT FK_CoachClientRelationships_AspNetUsers_CoachId
                            FOREIGN KEY (CoachId) REFERENCES AspNetUsers (Id);
                    END
                END

                -- Add foreign keys for GoalFeedback if needed
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GoalFeedback')
                    AND EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUsers')
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GoalFeedback_AspNetUsers_CoachId')
                    BEGIN
                        ALTER TABLE GoalFeedback ADD CONSTRAINT FK_GoalFeedback_AspNetUsers_CoachId
                            FOREIGN KEY (CoachId) REFERENCES AspNetUsers (Id);
                    END
                END
            ");
            
            // Final verification script to log any potential issues
            migrationBuilder.Sql(@"
                DECLARE @MissingTables TABLE (TableName NVARCHAR(255));
                
                -- Check if all expected tables exist
                INSERT INTO @MissingTables
                SELECT 'AspNetUsers' WHERE NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUsers')
                UNION SELECT 'AspNetRoles' WHERE NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetRoles')
                UNION SELECT 'AspNetUserRoles' WHERE NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserRoles')
                UNION SELECT 'AspNetUserClaims' WHERE NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserClaims')
                UNION SELECT 'AspNetUserLogins' WHERE NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserLogins')
                UNION SELECT 'AspNetRoleClaims' WHERE NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetRoleClaims')
                UNION SELECT 'AspNetUserTokens' WHERE NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserTokens')
                UNION SELECT 'LogLevelSettings' WHERE NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LogLevelSettings')
                UNION SELECT 'LogLevelOverrides' WHERE NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LogLevelOverrides')
                UNION SELECT 'WhitelistedIps' WHERE NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'WhitelistedIps')
                UNION SELECT 'Versions' WHERE NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Versions');

                -- Log warning if any tables are missing
                IF EXISTS (SELECT 1 FROM @MissingTables)
                BEGIN
                    DECLARE @MissingTablesMessage NVARCHAR(MAX) = 'WARNING: Some required tables are still missing:';
                    SELECT @MissingTablesMessage = @MissingTablesMessage + CHAR(13) + CHAR(10) + TableName
                    FROM @MissingTables;
                    
                    PRINT @MissingTablesMessage;
                END
                ELSE
                BEGIN
                    PRINT 'Database schema consolidation completed successfully. All required tables are present.';
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This migration is a schema consolidation and should not be rolled back
            migrationBuilder.Sql("-- This is a database consolidation migration. Rolling back is not recommended.");
        }
    }
}
