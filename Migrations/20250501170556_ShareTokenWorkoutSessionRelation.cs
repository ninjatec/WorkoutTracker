using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class ShareTokenWorkoutSessionRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create index for WorkoutSessionId
            migrationBuilder.CreateIndex(
                name: "IX_ShareToken_WorkoutSessionId",
                table: "ShareToken",
                column: "WorkoutSessionId"
            );

            // Add foreign key with NO ACTION delete behavior to avoid cascade cycles
            migrationBuilder.AddForeignKey(
                name: "FK_ShareToken_WorkoutSessions_WorkoutSessionId",
                table: "ShareToken",
                column: "WorkoutSessionId",
                principalTable: "WorkoutSessions",
                principalColumn: "WorkoutSessionId",
                onDelete: ReferentialAction.NoAction
            );

            // Update existing tokens to point to workout sessions
            migrationBuilder.Sql(@"
                UPDATE st
                SET st.WorkoutSessionId = ws.WorkoutSessionId
                FROM ShareToken st
                INNER JOIN WorkoutSessions ws ON ws.SessionId = st.SessionId
                WHERE st.SessionId IS NOT NULL
                AND st.WorkoutSessionId IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key first
            migrationBuilder.DropForeignKey(
                name: "FK_ShareToken_WorkoutSessions_WorkoutSessionId",
                table: "ShareToken"
            );

            // Drop index
            migrationBuilder.DropIndex(
                name: "IX_ShareToken_WorkoutSessionId",
                table: "ShareToken"
            );

            // Clear WorkoutSessionId values
            migrationBuilder.Sql(@"
                UPDATE ShareToken
                SET WorkoutSessionId = NULL;
            ");
        }
    }
}
