using Microsoft.EntityFrameworkCore.Migrations;

namespace WorkoutTrackerWeb.Data.Migrations.WorkoutTrackerWeb
{
    public partial class AddNotesAndNameColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "WorkoutSessions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "WorkoutExercises",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "WorkoutExercises");
        }
    }
}
