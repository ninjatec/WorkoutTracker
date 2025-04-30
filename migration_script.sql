IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(128) NOT NULL,
    [ProviderKey] nvarchar(128) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(128) NOT NULL,
    [Name] nvarchar(128) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'00000000000000_CreateIdentitySchema', N'9.0.4');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250413170606_AddRolesAndAdminUser', N'9.0.4');

CREATE TABLE [Versions] (
    [VersionId] int NOT NULL IDENTITY,
    [Major] int NOT NULL,
    [Minor] int NOT NULL,
    [Patch] int NOT NULL,
    [BuildNumber] int NOT NULL,
    [ReleaseDate] datetime2 NOT NULL,
    [Description] nvarchar(100) NOT NULL,
    [GitCommitHash] nvarchar(40) NULL,
    [IsCurrent] bit NOT NULL,
    [ReleaseNotes] nvarchar(255) NULL,
    CONSTRAINT [PK_Versions] PRIMARY KEY ([VersionId])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250414074303_AddVersionModel', N'9.0.4');

CREATE TABLE [LogLevelSettings] (
    [Id] int NOT NULL IDENTITY,
    [DefaultLogLevel] int NOT NULL,
    [LastUpdated] datetime2 NOT NULL,
    [LastUpdatedBy] nvarchar(256) NOT NULL,
    CONSTRAINT [PK_LogLevelSettings] PRIMARY KEY ([Id])
);

CREATE TABLE [LogLevelOverrides] (
    [Id] int NOT NULL IDENTITY,
    [SourceContext] nvarchar(256) NOT NULL,
    [LogLevel] int NOT NULL,
    [LogLevelSettingsId] int NOT NULL,
    CONSTRAINT [PK_LogLevelOverrides] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LogLevelOverrides_LogLevelSettings_LogLevelSettingsId] FOREIGN KEY ([LogLevelSettingsId]) REFERENCES [LogLevelSettings] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_LogLevelOverrides_LogLevelSettingsId] ON [LogLevelOverrides] ([LogLevelSettingsId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250424053232_AddLogLevelSettings', N'9.0.4');

CREATE TABLE [WhitelistedIps] (
    [Id] int NOT NULL IDENTITY,
    [IpAddress] nvarchar(45) NOT NULL,
    [Description] nvarchar(255) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(100) NULL,
    CONSTRAINT [PK_WhitelistedIps] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_WhitelistedIps_IpAddress] ON [WhitelistedIps] ([IpAddress]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250427053752_AddWhitelistedIps', N'9.0.4');


                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE Name = 'CreatedDate'
                    AND Object_ID = Object_ID('AspNetUsers')
                )
                BEGIN
                    ALTER TABLE AspNetUsers ADD CreatedDate DATETIME2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
                END
            


                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE Name = 'LastModifiedDate'
                    AND Object_ID = Object_ID('AspNetUsers')
                )
                BEGIN
                    ALTER TABLE AspNetUsers ADD LastModifiedDate DATETIME2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
                END
            


                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ClientGroup' AND type = 'U')
                BEGIN
                    CREATE TABLE [ClientGroup] (
                        [Id] int NOT NULL IDENTITY,
                        [Name] nvarchar(100) NOT NULL,
                        [Description] nvarchar(500) NULL,
                        [CoachId] nvarchar(450) NOT NULL,
                        [CreatedDate] datetime2 NOT NULL,
                        [LastModifiedDate] datetime2 NOT NULL,
                        [ColorCode] nvarchar(10) NULL,
                        CONSTRAINT [PK_ClientGroup] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_ClientGroup_AspNetUsers_CoachId] FOREIGN KEY ([CoachId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_ClientGroup_CoachId] ON [ClientGroup] ([CoachId]);
                END
            


                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CoachClientRelationships' AND type = 'U')
                BEGIN
                    CREATE TABLE [CoachClientRelationships] (
                        [Id] int NOT NULL IDENTITY,
                        [CoachId] nvarchar(450) NOT NULL,
                        [ClientId] nvarchar(450) NOT NULL,
                        [Status] int NOT NULL,
                        [CreatedDate] datetime2 NOT NULL,
                        [LastModifiedDate] datetime2 NOT NULL,
                        [StartDate] datetime2 NULL,
                        [EndDate] datetime2 NULL,
                        [InvitationToken] nvarchar(max) NULL,
                        [InvitationExpiryDate] datetime2 NULL,
                        [ClientGroupId] int NULL,
                        CONSTRAINT [PK_CoachClientRelationships] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_CoachClientRelationships_AspNetUsers_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_CoachClientRelationships_AspNetUsers_CoachId] FOREIGN KEY ([CoachId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_CoachClientRelationships_ClientGroup_ClientGroupId] FOREIGN KEY ([ClientGroupId]) REFERENCES [ClientGroup] ([Id])
                    );

                    CREATE INDEX [IX_CoachClientRelationships_ClientGroupId] ON [CoachClientRelationships] ([ClientGroupId]);
                    CREATE INDEX [IX_CoachClientRelationships_ClientId] ON [CoachClientRelationships] ([ClientId]);
                    CREATE INDEX [IX_CoachClientRelationships_CoachId] ON [CoachClientRelationships] ([CoachId]);
                END
            


                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CoachClientPermissions' AND type = 'U')
                BEGIN
                    CREATE TABLE [CoachClientPermissions] (
                        [Id] int NOT NULL IDENTITY,
                        [CoachClientRelationshipId] int NOT NULL,
                        [CanViewWorkouts] bit NOT NULL,
                        [CanCreateWorkouts] bit NOT NULL,
                        [CanModifyWorkouts] bit NOT NULL,
                        [CanEditWorkouts] bit NOT NULL,
                        [CanDeleteWorkouts] bit NOT NULL,
                        [CanViewPersonalInfo] bit NOT NULL,
                        [CanCreateGoals] bit NOT NULL,
                        [CanViewReports] bit NOT NULL,
                        [CanMessage] bit NOT NULL,
                        [CanCreateTemplates] bit NOT NULL,
                        [CanAssignTemplates] bit NOT NULL,
                        [LastModifiedDate] datetime2 NOT NULL,
                        CONSTRAINT [PK_CoachClientPermissions] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_CoachClientPermissions_CoachClientRelationships_CoachClientRelationshipId] FOREIGN KEY ([CoachClientRelationshipId]) REFERENCES [CoachClientRelationships] ([Id]) ON DELETE CASCADE
                    );

                    CREATE UNIQUE INDEX [IX_CoachClientPermissions_CoachClientRelationshipId] ON [CoachClientPermissions] ([CoachClientRelationshipId]);
                END
            


                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CoachNote' AND type = 'U')
                BEGIN
                    CREATE TABLE [CoachNote] (
                        [Id] int NOT NULL IDENTITY,
                        [CoachClientRelationshipId] int NOT NULL,
                        [Content] nvarchar(max) NOT NULL,
                        [CreatedDate] datetime2 NOT NULL,
                        [UpdatedDate] datetime2 NULL,
                        [IsVisibleToClient] bit NOT NULL,
                        [Category] nvarchar(50) NULL,
                        CONSTRAINT [PK_CoachNote] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_CoachNote_CoachClientRelationships_CoachClientRelationshipId] FOREIGN KEY ([CoachClientRelationshipId]) REFERENCES [CoachClientRelationships] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_CoachNote_CoachClientRelationshipId] ON [CoachNote] ([CoachClientRelationshipId]);
                END
            

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250428105425_FixPendingModelChanges', N'9.0.4');

ALTER TABLE [CoachClientRelationships] DROP CONSTRAINT [FK_CoachClientRelationships_AspNetUsers_ClientId];

ALTER TABLE [CoachClientRelationships] DROP CONSTRAINT [FK_CoachClientRelationships_AspNetUsers_CoachId];

DROP INDEX [IX_CoachClientRelationships_CoachId] ON [CoachClientRelationships];

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CoachClientRelationships]') AND [c].[name] = N'ClientId');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [CoachClientRelationships] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [CoachClientRelationships] ALTER COLUMN [ClientId] nvarchar(450) NULL;

ALTER TABLE [CoachClientRelationships] ADD [InvitedEmail] nvarchar(256) NULL;

CREATE UNIQUE INDEX [IX_CoachClientRelationships_CoachId_ClientId] ON [CoachClientRelationships] ([CoachId], [ClientId]) WHERE [ClientId] IS NOT NULL;

CREATE UNIQUE INDEX [IX_AspNetUsers_UserName] ON [AspNetUsers] ([UserName]) WHERE [UserName] IS NOT NULL;

ALTER TABLE [CoachClientRelationships] ADD CONSTRAINT [FK_CoachClientRelationships_AspNetUsers_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [AspNetUsers] ([Id]);

ALTER TABLE [CoachClientRelationships] ADD CONSTRAINT [FK_CoachClientRelationships_AspNetUsers_CoachId] FOREIGN KEY ([CoachId]) REFERENCES [AspNetUsers] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250430140509_UpdateCoachClientRelationshipsFinal', N'9.0.4');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250430140635_UpdateCoachClientRelationships', N'9.0.4');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250430140812_UpdateCoachClientRelationshipsCarefully', N'9.0.4');

COMMIT;
GO

