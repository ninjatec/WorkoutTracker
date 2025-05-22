-- SQL script to populate AppUser table with existing users from AspNetUsers
-- For use with SQL Server

-- Check if there are existing AspNetUsers that don't have corresponding entries in AppUser
IF EXISTS (
    SELECT u.Id
    FROM AspNetUsers u
    LEFT JOIN AppUser au ON au.Id = u.Id
    WHERE au.Id IS NULL
)
BEGIN
    PRINT 'Found AspNetUsers without corresponding AppUser entries. Creating them now...';

    -- Insert corresponding rows into AppUser for each AspNetUser that doesn't have one
    INSERT INTO AppUser (
        Id,
        UserName,
        NormalizedUserName,
        Email,
        NormalizedEmail,
        EmailConfirmed,
        PasswordHash,
        SecurityStamp,
        ConcurrencyStamp,
        PhoneNumber,
        PhoneNumberConfirmed,
        TwoFactorEnabled,
        LockoutEnd,
        LockoutEnabled,
        AccessFailedCount,
        CreatedDate,
        LastModifiedDate
    )
    SELECT 
        u.Id,
        u.UserName,
        u.NormalizedUserName,
        u.Email,
        u.NormalizedEmail,
        u.EmailConfirmed,
        u.PasswordHash,
        u.SecurityStamp,
        u.ConcurrencyStamp,
        u.PhoneNumber,
        u.PhoneNumberConfirmed,
        u.TwoFactorEnabled,
        u.LockoutEnd,
        u.LockoutEnabled,
        u.AccessFailedCount,
        GETUTCDATE() AS CreatedDate,
        GETUTCDATE() AS LastModifiedDate
    FROM AspNetUsers u
    LEFT JOIN AppUser au ON au.Id = u.Id
    WHERE au.Id IS NULL;

    -- Count how many users were added
    DECLARE @UsersAdded INT;
    SET @UsersAdded = @@ROWCOUNT;
    PRINT 'Successfully added ' + CAST(@UsersAdded AS VARCHAR) + ' users to the AppUser table.';
END
ELSE
BEGIN
    PRINT 'All AspNetUsers already have corresponding entries in the AppUser table.';
END

-- Optionally update the User table to link to identity users
-- This assumes the User table has Name values that need to be connected to AspNetUsers
-- Run this only if your User table needs to be updated

IF EXISTS (
    SELECT u.UserId 
    FROM [User] u
    LEFT JOIN AspNetUsers au ON au.Email = u.Name
    WHERE u.IdentityUserId IS NULL AND au.Id IS NOT NULL
)
BEGIN
    PRINT 'Found Users without IdentityUserId that can be linked to AspNetUsers by name. Updating them now...';
    
    -- Update User table to set IdentityUserId based on matching names/emails
    UPDATE u
    SET u.IdentityUserId = au.Id
    FROM [User] u
    INNER JOIN AspNetUsers au ON au.Email = u.Name
    WHERE u.IdentityUserId IS NULL;
    
    -- Count how many users were linked
    DECLARE @UsersLinked INT;
    SET @UsersLinked = @@ROWCOUNT;
    PRINT 'Successfully linked ' + CAST(@UsersLinked AS VARCHAR) + ' users to their identity accounts.';
END
ELSE
BEGIN
    PRINT 'No Users found that need linking to AspNetUsers based on name matching.';
END

PRINT 'AppUser population process completed.';
