using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileTemplateAssignmentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Skip adding ClientRelationshipId column since it already exists
            // migrationBuilder.AddColumn<int>(
            //     name: "ClientRelationshipId",
            //     table: "TemplateAssignments",
            //     type: "int",
            //     nullable: true);

            // Skip adding LastModifiedDate column to ClientGroups since it already exists
            // migrationBuilder.AddColumn<DateTime>(
            //     name: "LastModifiedDate",
            //     table: "ClientGroups",
            //     type: "datetime2",
            //     nullable: false,
            //     defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Skip adding date columns to AppUser since they already exist
            // migrationBuilder.AddColumn<DateTime>(
            //     name: "CreatedDate",
            //     table: "AppUser",
            //     type: "datetime2",
            //     nullable: false,
            //     defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
            // 
            // migrationBuilder.AddColumn<DateTime>(
            //     name: "LastModifiedDate",
            //     table: "AppUser",
            //     type: "datetime2",
            //     nullable: false,
            //     defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Check if ClientGroupMembers table exists before creating it
            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClientGroupMembers')
                BEGIN
                    CREATE TABLE [ClientGroupMembers] (
                        [Id] int NOT NULL IDENTITY,
                        [ClientGroupId] int NOT NULL,
                        [CoachClientRelationshipId] int NOT NULL,
                        [AddedDate] datetime2 NOT NULL,
                        CONSTRAINT [PK_ClientGroupMembers] PRIMARY KEY ([Id])
                    );

                    ALTER TABLE [ClientGroupMembers] ADD CONSTRAINT [FK_ClientGroupMembers_ClientGroups_ClientGroupId]
                    FOREIGN KEY ([ClientGroupId]) REFERENCES [ClientGroups] ([Id]) ON DELETE CASCADE;

                    ALTER TABLE [ClientGroupMembers] ADD CONSTRAINT [FK_ClientGroupMembers_CoachClientRelationships_CoachClientRelationshipId]
                    FOREIGN KEY ([CoachClientRelationshipId]) REFERENCES [CoachClientRelationships] ([Id]) ON DELETE CASCADE;

                    CREATE INDEX [IX_ClientGroupMembers_ClientGroupId] ON [ClientGroupMembers] ([ClientGroupId]);
                    CREATE INDEX [IX_ClientGroupMembers_CoachClientRelationshipId] ON [ClientGroupMembers] ([CoachClientRelationshipId]);
                END");

            // Skip adding index if it already exists
            // Check if index exists before creating it
            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TemplateAssignments_ClientRelationshipId')
                BEGIN
                    CREATE INDEX [IX_TemplateAssignments_ClientRelationshipId] ON [TemplateAssignments] ([ClientRelationshipId]);
                END");

            // Skip adding foreign key if it already exists
            // Check if constraint exists before creating it
            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TemplateAssignments_CoachClientRelationships_ClientRelationshipId')
                BEGIN
                    ALTER TABLE [TemplateAssignments] ADD CONSTRAINT [FK_TemplateAssignments_CoachClientRelationships_ClientRelationshipId] 
                    FOREIGN KEY ([ClientRelationshipId]) REFERENCES [CoachClientRelationships] ([Id]) ON DELETE SET NULL;
                END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Skip dropping foreign key since we're conditionally adding it in Up
            migrationBuilder.Sql(
                @"IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TemplateAssignments_CoachClientRelationships_ClientRelationshipId')
                BEGIN
                    ALTER TABLE [TemplateAssignments] DROP CONSTRAINT [FK_TemplateAssignments_CoachClientRelationships_ClientRelationshipId];
                END");

            // Drop ClientGroupMembers table if we created it
            migrationBuilder.Sql(
                @"IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ClientGroupMembers')
                BEGIN
                    DROP TABLE [ClientGroupMembers];
                END");

            // Skip dropping index since we're conditionally adding it in Up
            migrationBuilder.Sql(
                @"IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TemplateAssignments_ClientRelationshipId')
                BEGIN
                    DROP INDEX [IX_TemplateAssignments_ClientRelationshipId] ON [TemplateAssignments];
                END");

            // Skip removing column since we didn't add it
            // migrationBuilder.DropColumn(
            //     name: "ClientRelationshipId",
            //     table: "TemplateAssignments");

            // Skip removing columns since we didn't add them
            // migrationBuilder.DropColumn(
            //     name: "LastModifiedDate",
            //     table: "ClientGroups");
            // 
            // migrationBuilder.DropColumn(
            //     name: "CreatedDate",
            //     table: "AppUser");
            // 
            // migrationBuilder.DropColumn(
            //     name: "LastModifiedDate",
            //     table: "AppUser");
        }
    }
}
