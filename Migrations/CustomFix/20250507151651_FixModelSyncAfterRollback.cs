using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations.CustomFix
{
    /// <inheritdoc />
    public partial class FixModelSyncAfterRollback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First check if the constraint exists before trying to drop it
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.foreign_keys
                    WHERE object_id = OBJECT_ID(N'FK_WorkoutSchedules_WorkoutSessions_LastGeneratedSessionId')
                      AND parent_object_id = OBJECT_ID(N'WorkoutSchedules')
                )
                BEGIN
                    ALTER TABLE [WorkoutSchedules] DROP CONSTRAINT [FK_WorkoutSchedules_WorkoutSessions_LastGeneratedSessionId];
                END
            ");

            // Check if FK_WorkoutSessions_WorkoutSchedules_ScheduleId constraint exists
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.foreign_keys
                    WHERE object_id = OBJECT_ID(N'FK_WorkoutSessions_WorkoutSchedules_ScheduleId')
                      AND parent_object_id = OBJECT_ID(N'WorkoutSessions')
                )
                BEGIN
                    ALTER TABLE [WorkoutSessions] DROP CONSTRAINT [FK_WorkoutSessions_WorkoutSchedules_ScheduleId];
                END
            ");

            // Check if IX_WorkoutSchedules_LastGeneratedSessionId index exists
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.indexes
                    WHERE name = 'IX_WorkoutSchedules_LastGeneratedSessionId'
                      AND object_id = OBJECT_ID(N'WorkoutSchedules')
                )
                BEGIN
                    DROP INDEX [IX_WorkoutSchedules_LastGeneratedSessionId] ON [WorkoutSchedules]
                END
            ");

            // Check if IX_WorkoutFeedbacks_WorkoutSessionId index exists
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.indexes
                    WHERE name = 'IX_WorkoutFeedbacks_WorkoutSessionId'
                      AND object_id = OBJECT_ID(N'WorkoutFeedbacks')
                )
                BEGIN
                    DROP INDEX [IX_WorkoutFeedbacks_WorkoutSessionId] ON [WorkoutFeedbacks]
                END
            ");

            // Create new indexes
            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_LastGeneratedSessionId",
                table: "WorkoutSchedules",
                column: "LastGeneratedSessionId",
                unique: true,
                filter: "[LastGeneratedSessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutFeedbacks_WorkoutSessionId",
                table: "WorkoutFeedbacks",
                column: "WorkoutSessionId");

            // Add foreign keys
            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSchedules_WorkoutSessions_LastGeneratedSessionId",
                table: "WorkoutSchedules",
                column: "LastGeneratedSessionId",
                principalTable: "WorkoutSessions",
                principalColumn: "WorkoutSessionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_WorkoutSchedules_ScheduleId",
                table: "WorkoutSessions",
                column: "ScheduleId",
                principalTable: "WorkoutSchedules",
                principalColumn: "WorkoutScheduleId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Check if the constraints exist before trying to drop them
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.foreign_keys
                    WHERE object_id = OBJECT_ID(N'FK_WorkoutSchedules_WorkoutSessions_LastGeneratedSessionId')
                      AND parent_object_id = OBJECT_ID(N'WorkoutSchedules')
                )
                BEGIN
                    ALTER TABLE [WorkoutSchedules] DROP CONSTRAINT [FK_WorkoutSchedules_WorkoutSessions_LastGeneratedSessionId];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.foreign_keys
                    WHERE object_id = OBJECT_ID(N'FK_WorkoutSessions_WorkoutSchedules_ScheduleId')
                      AND parent_object_id = OBJECT_ID(N'WorkoutSessions')
                )
                BEGIN
                    ALTER TABLE [WorkoutSessions] DROP CONSTRAINT [FK_WorkoutSessions_WorkoutSchedules_ScheduleId];
                END
            ");

            // Check if index exists before dropping
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.indexes
                    WHERE name = 'IX_WorkoutSchedules_LastGeneratedSessionId'
                      AND object_id = OBJECT_ID(N'WorkoutSchedules')
                )
                BEGIN
                    DROP INDEX [IX_WorkoutSchedules_LastGeneratedSessionId] ON [WorkoutSchedules]
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.indexes
                    WHERE name = 'IX_WorkoutFeedbacks_WorkoutSessionId'
                      AND object_id = OBJECT_ID(N'WorkoutFeedbacks')
                )
                BEGIN
                    DROP INDEX [IX_WorkoutFeedbacks_WorkoutSessionId] ON [WorkoutFeedbacks]
                END
            ");

            // Create new indexes with original configuration
            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_LastGeneratedSessionId",
                table: "WorkoutSchedules",
                column: "LastGeneratedSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutFeedbacks_WorkoutSessionId",
                table: "WorkoutFeedbacks",
                column: "WorkoutSessionId",
                unique: true);

            // Add foreign keys back
            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSchedules_WorkoutSessions_LastGeneratedSessionId",
                table: "WorkoutSchedules",
                column: "LastGeneratedSessionId",
                principalTable: "WorkoutSessions",
                principalColumn: "WorkoutSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_WorkoutSchedules_ScheduleId",
                table: "WorkoutSessions",
                column: "ScheduleId",
                principalTable: "WorkoutSchedules",
                principalColumn: "WorkoutScheduleId");
        }
    }
}
