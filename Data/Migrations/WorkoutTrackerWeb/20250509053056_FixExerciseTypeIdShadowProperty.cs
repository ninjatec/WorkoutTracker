using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Data.Migrations.WorkoutTrackerWeb
{
    /// <inheritdoc />
    public partial class FixExerciseTypeIdShadowProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutExercises_Equipment_EquipmentId",
                table: "WorkoutExercises");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutExercises_Equipment_EquipmentId",
                table: "WorkoutExercises",
                column: "EquipmentId",
                principalTable: "Equipment",
                principalColumn: "EquipmentId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutExercises_Equipment_EquipmentId",
                table: "WorkoutExercises");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutExercises_Equipment_EquipmentId",
                table: "WorkoutExercises",
                column: "EquipmentId",
                principalTable: "Equipment",
                principalColumn: "EquipmentId");
        }
    }
}
