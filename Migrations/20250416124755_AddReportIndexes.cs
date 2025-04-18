using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddReportIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Index for sessions by user and date
            migrationBuilder.CreateIndex(
                name: "IX_Session_UserId_datetime",
                table: "Session",
                columns: new[] { "UserId", "datetime" });

            // Index for sets with weight and exercise type
            migrationBuilder.CreateIndex(
                name: "IX_Set_ExerciseTypeId_Weight_SessionId",
                table: "Set",
                columns: new[] { "ExerciseTypeId", "Weight", "SessionId" });

            // Index for reps success and set
            migrationBuilder.CreateIndex(
                name: "IX_Rep_success_SetsSetId",
                table: "Rep",
                columns: new[] { "success", "SetsSetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Session_UserId_datetime",
                table: "Session");

            migrationBuilder.DropIndex(
                name: "IX_Set_ExerciseTypeId_Weight_SessionId",
                table: "Set");

            migrationBuilder.DropIndex(
                name: "IX_Rep_success_SetsSetId",
                table: "Rep");
        }
    }
}
