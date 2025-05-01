using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class CompleteSessionMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DistanceMeters",
                table: "WorkoutSets",
                newName: "Distance");

            migrationBuilder.AddColumn<int>(
                name: "Intensity",
                table: "WorkoutSets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IterationNumber",
                table: "WorkoutSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NextIterationId",
                table: "WorkoutSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviousIterationId",
                table: "WorkoutSessions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_NextIterationId",
                table: "WorkoutSessions",
                column: "NextIterationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_PreviousIterationId",
                table: "WorkoutSessions",
                column: "PreviousIterationId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_WorkoutSessions_NextIterationId",
                table: "WorkoutSessions",
                column: "NextIterationId",
                principalTable: "WorkoutSessions",
                principalColumn: "WorkoutSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_WorkoutSessions_PreviousIterationId",
                table: "WorkoutSessions",
                column: "PreviousIterationId",
                principalTable: "WorkoutSessions",
                principalColumn: "WorkoutSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_WorkoutSessions_NextIterationId",
                table: "WorkoutSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_WorkoutSessions_PreviousIterationId",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_NextIterationId",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_PreviousIterationId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "Intensity",
                table: "WorkoutSets");

            migrationBuilder.DropColumn(
                name: "IterationNumber",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "NextIterationId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "PreviousIterationId",
                table: "WorkoutSessions");

            migrationBuilder.RenameColumn(
                name: "Distance",
                table: "WorkoutSets",
                newName: "DistanceMeters");
        }
    }
}
