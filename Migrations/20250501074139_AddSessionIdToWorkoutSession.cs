using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionIdToWorkoutSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "WorkoutSessions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_SessionId",
                table: "WorkoutSessions",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_Session_SessionId",
                table: "WorkoutSessions",
                column: "SessionId",
                principalTable: "Session",
                principalColumn: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_Session_SessionId",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_SessionId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "WorkoutSessions");
        }
    }
}
