BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    ALTER TABLE [CoachClientRelationships] DROP CONSTRAINT [FK_CoachClientRelationships_AspNetUsers_ClientId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    ALTER TABLE [CoachClientRelationships] DROP CONSTRAINT [FK_CoachClientRelationships_AspNetUsers_CoachId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    DROP INDEX [IX_CoachClientRelationships_CoachId] ON [CoachClientRelationships];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CoachClientRelationships]') AND [c].[name] = N'ClientId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [CoachClientRelationships] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [CoachClientRelationships] ALTER COLUMN [ClientId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    ALTER TABLE [CoachClientRelationships] ADD [AppUserId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    ALTER TABLE [CoachClientRelationships] ADD [AppUserId1] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    ALTER TABLE [CoachClientRelationships] ADD [InvitedEmail] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    CREATE TABLE [ClientGoal] (
        [Id] int NOT NULL IDENTITY,
        [CoachClientRelationshipId] int NULL,
        [UserId] nvarchar(max) NULL,
        [IsCoachCreated] bit NOT NULL,
        [Description] nvarchar(255) NOT NULL,
        [Category] int NOT NULL,
        [CustomCategory] nvarchar(50) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [TargetDate] datetime2 NOT NULL,
        [CompletedDate] datetime2 NULL,
        [MeasurementType] nvarchar(50) NULL,
        [StartValue] decimal(18,2) NULL,
        [CurrentValue] decimal(18,2) NULL,
        [TargetValue] decimal(18,2) NULL,
        [MeasurementUnit] nvarchar(20) NULL,
        [Notes] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [IsCompleted] bit NOT NULL,
        [IsVisibleToCoach] bit NOT NULL,
        [TrackingFrequency] nvarchar(20) NULL,
        [LastProgressUpdate] datetime2 NULL,
        [CompletionCriteria] nvarchar(30) NULL,
        CONSTRAINT [PK_ClientGoal] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ClientGoal_CoachClientRelationships_CoachClientRelationshipId] FOREIGN KEY ([CoachClientRelationshipId]) REFERENCES [CoachClientRelationships] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    CREATE INDEX [IX_CoachClientRelationships_AppUserId] ON [CoachClientRelationships] ([AppUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    CREATE INDEX [IX_CoachClientRelationships_AppUserId1] ON [CoachClientRelationships] ([AppUserId1]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CoachClientRelationships_CoachId_ClientId] ON [CoachClientRelationships] ([CoachId], [ClientId]) WHERE [ClientId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AspNetUsers_UserName] ON [AspNetUsers] ([UserName]) WHERE [UserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    CREATE INDEX [IX_ClientGoal_CoachClientRelationshipId] ON [ClientGoal] ([CoachClientRelationshipId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    ALTER TABLE [CoachClientRelationships] ADD CONSTRAINT [FK_CoachClientRelationships_AspNetUsers_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [AspNetUsers] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    ALTER TABLE [CoachClientRelationships] ADD CONSTRAINT [FK_CoachClientRelationships_AspNetUsers_AppUserId1] FOREIGN KEY ([AppUserId1]) REFERENCES [AspNetUsers] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    ALTER TABLE [CoachClientRelationships] ADD CONSTRAINT [FK_CoachClientRelationships_AspNetUsers_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [AspNetUsers] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    ALTER TABLE [CoachClientRelationships] ADD CONSTRAINT [FK_CoachClientRelationships_AspNetUsers_CoachId] FOREIGN KEY ([CoachId]) REFERENCES [AspNetUsers] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430134412_CombinedModelChanges'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250430134412_CombinedModelChanges', N'9.0.4');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430140034_UpdateCoachClientRelationshipsPreserveData'
)
BEGIN
    ALTER TABLE [CoachClientRelationships] DROP CONSTRAINT [FK_CoachClientRelationships_AspNetUsers_AppUserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430140034_UpdateCoachClientRelationshipsPreserveData'
)
BEGIN
    ALTER TABLE [CoachClientRelationships] DROP CONSTRAINT [FK_CoachClientRelationships_AspNetUsers_AppUserId1];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430140034_UpdateCoachClientRelationshipsPreserveData'
)
BEGIN
    DROP TABLE [ClientGoal];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430140034_UpdateCoachClientRelationshipsPreserveData'
)
BEGIN
    DROP INDEX [IX_CoachClientRelationships_AppUserId] ON [CoachClientRelationships];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430140034_UpdateCoachClientRelationshipsPreserveData'
)
BEGIN
    DROP INDEX [IX_CoachClientRelationships_AppUserId1] ON [CoachClientRelationships];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430140034_UpdateCoachClientRelationshipsPreserveData'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CoachClientRelationships]') AND [c].[name] = N'AppUserId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [CoachClientRelationships] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [CoachClientRelationships] DROP COLUMN [AppUserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430140034_UpdateCoachClientRelationshipsPreserveData'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CoachClientRelationships]') AND [c].[name] = N'AppUserId1');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [CoachClientRelationships] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [CoachClientRelationships] DROP COLUMN [AppUserId1];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250430140034_UpdateCoachClientRelationshipsPreserveData'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250430140034_UpdateCoachClientRelationshipsPreserveData', N'9.0.4');
END;

COMMIT;
GO

