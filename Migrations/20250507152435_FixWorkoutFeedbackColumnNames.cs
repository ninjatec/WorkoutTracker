using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class FixWorkoutFeedbackColumnNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First check if SessionId exists and WorkoutSessionId doesn't before trying to rename
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE Name = 'SessionId' AND OBJECT_ID = OBJECT_ID('WorkoutFeedbacks')
                )
                AND NOT EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE Name = 'WorkoutSessionId' AND OBJECT_ID = OBJECT_ID('WorkoutFeedbacks')
                )
                BEGIN
                    EXEC sp_rename 'WorkoutFeedbacks.SessionId', 'WorkoutSessionId', 'COLUMN';
                END
            ");

            // Handle foreign keys safely based on existence
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

            // Handle indexes safely based on existence
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

            // Check if WorkoutSessionId exists before creating index on it
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE Name = 'WorkoutSessionId' AND OBJECT_ID = OBJECT_ID('WorkoutFeedbacks')
                )
                AND NOT EXISTS (
                    SELECT * FROM sys.indexes
                    WHERE name = 'IX_WorkoutFeedbacks_WorkoutSessionId'
                      AND object_id = OBJECT_ID(N'WorkoutFeedbacks')
                )
                BEGIN
                    CREATE INDEX [IX_WorkoutFeedbacks_WorkoutSessionId] ON [WorkoutFeedbacks] ([WorkoutSessionId]);
                END
            ");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_LastGeneratedSessionId",
                table: "WorkoutSchedules",
                column: "LastGeneratedSessionId",
                unique: true,
                filter: "[LastGeneratedSessionId] IS NOT NULL");

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
            // Handle foreign keys safely based on existence
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

            // Handle indexes safely based on existence
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

            // Rename WorkoutSessionId back to SessionId if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE Name = 'WorkoutSessionId' AND OBJECT_ID = OBJECT_ID('WorkoutFeedbacks')
                )
                AND NOT EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE Name = 'SessionId' AND OBJECT_ID = OBJECT_ID('WorkoutFeedbacks')
                )
                BEGIN
                    EXEC sp_rename 'WorkoutFeedbacks.WorkoutSessionId', 'SessionId', 'COLUMN';
                END
            ");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_LastGeneratedSessionId",
                table: "WorkoutSchedules",
                column: "LastGeneratedSessionId");

            // Create index on SessionId if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE Name = 'SessionId' AND OBJECT_ID = OBJECT_ID('WorkoutFeedbacks')
                )
                AND NOT EXISTS (
                    SELECT * FROM sys.indexes
                    WHERE name = 'IX_WorkoutFeedbacks_SessionId'
                      AND object_id = OBJECT_ID(N'WorkoutFeedbacks')
                )
                BEGIN
                    CREATE INDEX [IX_WorkoutFeedbacks_SessionId] ON [WorkoutFeedbacks] ([SessionId]);
                END
            ");

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
