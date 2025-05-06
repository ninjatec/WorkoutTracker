using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleIdToWorkoutSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScheduleId",
                table: "WorkoutSessions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_ScheduleId",
                table: "WorkoutSessions",
                column: "ScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_WorkoutSchedules_ScheduleId",
                table: "WorkoutSessions",
                column: "ScheduleId",
                principalTable: "WorkoutSchedules",
                principalColumn: "WorkoutScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_WorkoutSchedules_ScheduleId",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_ScheduleId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "ScheduleId",
                table: "WorkoutSessions");
        }
    }
}
