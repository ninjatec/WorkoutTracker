using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if the CreatedDate and LastModifiedDate columns already exist on AspNetUsers
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE Name = 'CreatedDate'
                    AND Object_ID = Object_ID('AspNetUsers')
                )
                BEGIN
                    ALTER TABLE AspNetUsers ADD CreatedDate DATETIME2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE Name = 'LastModifiedDate'
                    AND Object_ID = Object_ID('AspNetUsers')
                )
                BEGIN
                    ALTER TABLE AspNetUsers ADD LastModifiedDate DATETIME2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
                END
            ");

            // Check if the ClientGroup table already exists
            migrationBuilder.Sql(@"
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
            ");

            // Check if the CoachClientRelationships table already exists
            migrationBuilder.Sql(@"
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
            ");

            // Check if the CoachClientPermissions table already exists
            migrationBuilder.Sql(@"
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
            ");

            // Check if the CoachNote table already exists
            migrationBuilder.Sql(@"
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
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // We're using conditional SQL in the Up method, so we need to be careful in the Down method.
            // Only drop tables/columns if they were actually created by this migration.
            // Since we can't know for sure, we'll use conditional logic here too.
            
            // Note: Down migrations are typically for development scenarios.
            // In production, you typically only apply migrations going forward.
            
            // CoachNote table
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CoachNote' AND type = 'U')
                BEGIN
                    DROP TABLE [CoachNote];
                END
            ");
            
            // CoachClientPermissions table
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CoachClientPermissions' AND type = 'U')
                BEGIN
                    DROP TABLE [CoachClientPermissions];
                END
            ");
            
            // CoachClientRelationships table
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CoachClientRelationships' AND type = 'U')
                BEGIN
                    DROP TABLE [CoachClientRelationships];
                END
            ");
            
            // ClientGroup table
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ClientGroup' AND type = 'U')
                BEGIN
                    DROP TABLE [ClientGroup];
                END
            ");
            
            // Remove columns from AspNetUsers
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE Name = 'CreatedDate'
                    AND Object_ID = Object_ID('AspNetUsers')
                )
                BEGIN
                    ALTER TABLE AspNetUsers DROP COLUMN CreatedDate;
                END
            ");
            
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE Name = 'LastModifiedDate'
                    AND Object_ID = Object_ID('AspNetUsers')
                )
                BEGIN
                    ALTER TABLE AspNetUsers DROP COLUMN LastModifiedDate;
                END
            ");
        }
    }
}
