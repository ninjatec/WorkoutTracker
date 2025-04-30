# WorkoutTracker: Copying Production Database to Development

This document outlines the complete process for creating a copy of the production SQL Server database for local development use. Follow these steps to ensure proper database configuration and data privacy.

## Prerequisites

- SQL Server Management Studio (SSMS) or Azure Data Studio
- Access to the production database server (credentials or Azure AD authentication)
- Sufficient disk space for the database backup
- SQL Server instance running locally (or in a development environment)
- Microsoft.Data.SqlClient installed in your project (already configured)

## Step 1: Create a Backup of the Production Databases

The WorkoutTracker application uses **two separate databases**:
1. The main application database (WorkoutTracker) - Contains workout data, user records, etc.
2. The Identity database (handled by ApplicationDbContext) - Contains ASP.NET Identity tables

### Option A: Using SQL Server Management Studio (SSMS)

1. Connect to the production SQL Server using SSMS
2. For each database (WorkoutTracker and Identity if separate):
   - Right-click the database > Tasks > Back Up
   - Set backup type to "Full"
   - Choose "Disk" as the destination
   - Add a file path for the backup (e.g., `WorkoutTracker_Prod_Backup.bak`)
   - Under Options, select "Verify backup when finished" and "Perform checksum"
   - Click OK to create the backup

### Option B: Using T-SQL Script

Connect to your production server and run the following script for each database:

```sql
-- For the main WorkoutTracker database
BACKUP DATABASE WorkoutTracker
TO DISK = N'/path/to/WorkoutTracker_Prod_Backup.bak'
WITH 
    DESCRIPTION = N'Full backup of WorkoutTracker production database',
    NOFORMAT, 
    INIT,
    NAME = N'WorkoutTracker-Full Database Backup', 
    SKIP, 
    NOREWIND, 
    NOUNLOAD,
    STATS = 10,
    CHECKSUM
GO

-- For the Identity database (if separate)
BACKUP DATABASE IdentityDB
TO DISK = N'/path/to/IdentityDB_Prod_Backup.bak'
WITH 
    DESCRIPTION = N'Full backup of Identity production database',
    NOFORMAT, 
    INIT,
    NAME = N'Identity-Full Database Backup', 
    SKIP, 
    NOREWIND, 
    NOUNLOAD,
    STATS = 10,
    CHECKSUM
GO
```

## Step 2: Data Privacy Considerations (GDPR Compliance)

Before restoring to a development environment, you should sanitize sensitive data:

1. Create a sanitization script (`sanitize_data.sql`) with the following content:

```sql
-- Sanitize personal information in AppUser table
UPDATE [dbo].[AspNetUsers]
SET 
    [Email] = 'dev-' + CONVERT(VARCHAR(10), [Id]) + '@example.com',
    [NormalizedEmail] = UPPER('dev-' + CONVERT(VARCHAR(10), [Id]) + '@example.com'),
    [PhoneNumber] = NULL,
    [PasswordHash] = 'DEV_ENVIRONMENT_SANITIZED_PASSWORD_HASH'
WHERE [Email] NOT LIKE 'dev-%@example.com'
GO

-- Optional: Set all passwords to a known value for testing
-- Note: This is the hash for the password 'Password123!'
UPDATE [dbo].[AspNetUsers]
SET [PasswordHash] = 'AQAAAAIAAYagAAAAELa9vEA5ZeAdcRbf6XhbT6Q8e9ICU21kOvCy0iYmPZt2o2n7xSWygCBUwAqwfkZnCA=='
GO

-- Update LoginHistory table to remove real IP addresses
UPDATE [dbo].[LoginHistory]
SET [IpAddress] = '127.0.0.1'
GO

-- Sanitize any contact email addresses in the Feedback table
UPDATE [dbo].[Feedback]
SET [ContactEmail] = 'feedback-' + CONVERT(VARCHAR(10), [FeedbackId]) + '@example.com'
WHERE [ContactEmail] IS NOT NULL
GO

-- Sanitize email addresses in SharedWorkouts
UPDATE [dbo].[ShareToken]
SET [Description] = 'Development Environment Share'
GO

-- NOTE: Add additional sanitization steps as needed for your specific data
```

## Step 3: Restore the Databases to Development Environment

### Option A: Using SQL Server Management Studio (SSMS)

1. Connect to your development SQL Server instance
2. Right-click on "Databases" > "Restore Database"
3. Select "Device" and choose your backup file
4. Modify the restore locations if needed (for data and log files)
5. Under Options, select "Overwrite the existing database"
6. Click OK to restore the database

### Option B: Using T-SQL Script

For each database, run the following script on your development SQL Server:

```sql
-- For WorkoutTracker database
USE [master]
GO

-- Kill any active connections to the database
DECLARE @kill varchar(8000) = '';  
SELECT @kill = @kill + 'kill ' + CONVERT(varchar(5), session_id) + ';'  
FROM sys.dm_exec_sessions
WHERE database_id = db_id('WorkoutTracker')

EXEC(@kill);

-- Restore the database
RESTORE DATABASE [WorkoutTracker]
FROM DISK = N'/path/to/WorkoutTracker_Prod_Backup.bak'
WITH 
    MOVE 'WorkoutTracker' TO 'C:\SQLData\WorkoutTracker.mdf',
    MOVE 'WorkoutTracker_log' TO 'C:\SQLData\WorkoutTracker_log.ldf',
    REPLACE,
    RECOVERY
GO

-- For Identity database (if separate)
USE [master]
GO

-- Kill any active connections
DECLARE @kill varchar(8000) = '';  
SELECT @kill = @kill + 'kill ' + CONVERT(varchar(5), session_id) + ';'  
FROM sys.dm_exec_sessions
WHERE database_id = db_id('IdentityDB')

EXEC(@kill);

-- Restore the database
RESTORE DATABASE [IdentityDB]
FROM DISK = N'/path/to/IdentityDB_Prod_Backup.bak'
WITH 
    MOVE 'IdentityDB' TO 'C:\SQLData\IdentityDB.mdf',
    MOVE 'IdentityDB_log' TO 'C:\SQLData\IdentityDB_log.ldf',
    REPLACE,
    RECOVERY
GO
```

> **Note**: Adjust the file paths according to your SQL Server's data directory.

## Step 4: Run Sanitization Script

After restoring the databases, run the sanitization script to protect sensitive data:

```sql
USE [WorkoutTracker]
GO

-- Execute the sanitization script
:r C:\path\to\sanitize_data.sql
GO
```

## Step 5: Verify Hangfire Schema

Ensure that the Hangfire schema is properly initialized. You can run the included schema creation script:

```sql
USE [WorkoutTracker]
GO

-- Check if Hangfire schema exists
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'HangFire')
BEGIN
    EXEC('CREATE SCHEMA [HangFire]')
END
GO

-- Execute the Hangfire schema creation script if needed
:r C:\path\to\hangfire_schema.sql
GO
```

## Step 6: Update Connection Strings in Development Environment

Update your `appsettings.Development.json` file with the correct connection strings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=WorkoutTracker;User Id=sa;Password=YourLocalPassword;TrustServerCertificate=True;",
    "WorkoutTrackerWebContext": "Server=localhost;Database=WorkoutTracker;User Id=sa;Password=YourLocalPassword;TrustServerCertificate=True;"
  }
}
```

If you're using separate databases for identity and workout data, adjust the connection strings accordingly.

## Step 7: Test the Database Connections

Run the following commands to ensure migrations and schema are properly applied:

```bash
# Verify Application DbContext
dotnet ef database update --context ApplicationDbContext

# Verify WorkoutTracker DbContext
dotnet ef database update --context WorkoutTrackerWebContext

# If any errors occur, you might need to apply missing migrations
```

## Step 8: Start the Application and Verify Functionality

1. Run the application:
   ```bash
   dotnet run
   ```

2. Test key functionality to ensure the database is working correctly:
   - Login with a sanitized user account
   - View workout history
   - Create a new workout
   - Test administrative functions if applicable

## Troubleshooting

### Identity Errors

If you encounter identity-related issues:

1. Verify that ASP.NET Identity tables are correctly restored
2. Check that the password hash is properly set in the sanitization script
3. Create a new admin user through the EF tools if needed:
   ```bash
   dotnet run -- create-admin --email admin@example.com --password Admin123!
   ```

### Entity Framework Migration Errors

If you see migration errors:

1. Check that all migrations are applied:
   ```bash
   dotnet ef migrations list --context WorkoutTrackerWebContext
   dotnet ef migrations list --context ApplicationDbContext
   ```

2. Apply any missing migrations:
   ```bash
   dotnet ef database update --context WorkoutTrackerWebContext
   dotnet ef database update --context ApplicationDbContext
   ```

### Database Connection Issues

If you encounter connection problems:

1. Verify SQL Server is running and accessible
2. Check that the connection strings in appsettings.Development.json are correct
3. Ensure firewall settings allow connections to the SQL Server
4. Test connection with SSMS or Azure Data Studio

## Data Retention Policy

Remember that development copies of production data should:

1. Be stored securely, even in development environments
2. Not be retained longer than necessary
3. Never be used in public demos without complete anonymization
4. Be deleted when no longer needed for development

## Regular Refresh Process

To keep your development database updated with production schema and data:

1. Schedule regular database refreshes (monthly or as needed)
2. Automate the backup, restore, and sanitization steps if possible
3. Document any schema changes made in production between refreshes
4. Use a versioned sanitization script to handle evolving schema

## Security Considerations

1. Never store production database backups on unsecured devices
2. Encrypt backup files during transfer
3. Apply sanitization immediately after restore
4. Restrict access to the development database
5. Never use real production credentials in development

## Data Encryption Considerations

### Production Database Encryption

The WorkoutTracker application utilizes several encryption mechanisms that need special handling when copying to development:

1. **Transparent Data Encryption (TDE)**

   The production database uses SQL Server TDE to encrypt data at rest. When working with TDE-protected databases:
   
   ```sql
   -- Check if TDE is enabled on the production database
   SELECT DB_NAME(database_id) AS DatabaseName, encryption_state
   FROM sys.dm_database_encryption_keys;
   ```
   
   For development purposes, you may disable TDE after restoring:
   
   ```sql
   -- Disable TDE in the development environment if necessary
   USE master;
   GO
   ALTER DATABASE WorkoutTracker SET ENCRYPTION OFF;
   GO
   ```

2. **Column-Level Encryption**

   Sensitive user information in the database is encrypted using column-level encryption. The sanitization script needs to handle these fields:
   
   ```sql
   -- Update the sanitize_data.sql script to include these steps
   
   -- For encrypted columns, either decrypt or replace with development test values
   UPDATE [dbo].[User]
   SET [EncryptedPersonalInfo] = ENCRYPTBYPASSPHRASE('DevEnvironmentKey', 'Test Personal Info')
   WHERE [UserId] > 0;
   ```

3. **Always Encrypted Columns**

   If using SQL Server Always Encrypted:
   
   ```sql
   -- Always Encrypted requires access to the certificate or key
   -- For development, either:
   -- 1. Create new development keys, or
   -- 2. Export/import the non-production certificates
   
   -- Example PowerShell command to export a certificate (run on production)
   # $cert = Get-SqlColumnMasterKey -Name "CMK_Name" -InputObject $database
   # Export-SqlColumnEncryptionCertificate -Certificate $cert -Path "C:\temp\dev_cert.pfx" -Password (ConvertTo-SecureString -String "YourPassword" -Force -AsPlainText)
   
   -- Then import on development machine
   ```

4. **Data Protection API (DPAPI) Keys**

   The application uses ASP.NET Core Data Protection API for protecting cookies and tokens. The keys are stored in the `DataProtectionKeys` table:
   
   ```sql
   -- After database restore, you may need to reset or import the DPAPI keys
   -- Option 1: Clear existing keys to let the application generate new ones
   TRUNCATE TABLE [dbo].[DataProtectionKeys];
   
   -- Option 2: Import development environment keys if you have them
   -- This requires a separate import script
   ```

5. **Connection String Encryption**

   Ensure that connection strings in the development environment use appropriate security:
   
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=WorkoutTracker;User Id=sa;Password=YourLocalPassword;TrustServerCertificate=True;Encrypt=True;",
       "WorkoutTrackerWebContext": "Server=localhost;Database=WorkoutTracker;User Id=sa;Password=YourLocalPassword;TrustServerCertificate=True;Encrypt=True;"
     }
   }
   ```

6. **Encryption for Shared Tokens**

   WorkoutTracker uses encrypted share tokens that need special handling:
   
   ```sql
   -- Update token encryption keys in development
   UPDATE [dbo].[ShareToken]
   SET [Token] = 'DEV-' + CONVERT(VARCHAR(10), [Id]) + '-TOKEN'
   WHERE [Id] > 0;
   ```

7. **Key Management**

   If key rotation is implemented in production:
   
   ```sql
   -- Simplify key rotation in development
   UPDATE [dbo].[KeyRotation]
   SET [CurrentKeyVersion] = 1,
       [LastRotated] = GETDATE()
   WHERE [Id] > 0;
   ```

### Development Environment Security

When working with copies of production data, implement these additional security measures:

1. **Enable Transparent Data Encryption for local development** if working with highly sensitive data
2. **Use SQL Server's built-in encryption functions** consistently between environments
3. **Implement key rotation** in development to match production patterns
4. **Encrypt backup files** when transferring between environments using tools like:
   ```bash
   # Using OpenSSL to encrypt backup files
   openssl enc -aes-256-cbc -salt -in WorkoutTracker_Prod_Backup.bak -out WorkoutTracker_Prod_Backup.bak.enc -k YourSecurePassword
   
   # Decrypt before restore
   openssl enc -d -aes-256-cbc -in WorkoutTracker_Prod_Backup.bak.enc -out WorkoutTracker_Prod_Backup.bak -k YourSecurePassword
   ```

5. **Consider database snapshots** instead of full copies for short-term testing

By properly handling these encryption considerations, you'll maintain security while ensuring the development environment accurately reflects production behavior.