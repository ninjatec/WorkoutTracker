using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class SetExerciseRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Excercise_Session_SessionsSessionId",
                table: "Excercise");

            migrationBuilder.DropForeignKey(
                name: "FK_Set_Excercise_ExcercisesExcerciseId",
                table: "Set");

            migrationBuilder.DropIndex(
                name: "IX_Set_ExcercisesExcerciseId",
                table: "Set");

            migrationBuilder.DropIndex(
                name: "IX_Excercise_SessionsSessionId",
                table: "Excercise");

            migrationBuilder.DropColumn(
                name: "ExcercisesExcerciseId",
                table: "Set");

            migrationBuilder.DropColumn(
                name: "SessionsSessionId",
                table: "Excercise");

            migrationBuilder.AddColumn<int>(
                name: "ExerciseId",
                table: "Set",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "Excercise",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Set_ExerciseId",
                table: "Set",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_Excercise_SessionId",
                table: "Excercise",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Excercise_Session_SessionId",
                table: "Excercise",
                column: "SessionId",
                principalTable: "Session",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Set_Excercise_ExerciseId",
                table: "Set",
                column: "ExerciseId",
                principalTable: "Excercise",
                principalColumn: "ExcerciseId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Excercise_Session_SessionId",
                table: "Excercise");

            migrationBuilder.DropForeignKey(
                name: "FK_Set_Excercise_ExerciseId",
                table: "Set");

            migrationBuilder.DropIndex(
                name: "IX_Set_ExerciseId",
                table: "Set");

            migrationBuilder.DropIndex(
                name: "IX_Excercise_SessionId",
                table: "Excercise");

            migrationBuilder.DropColumn(
                name: "ExerciseId",
                table: "Set");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "Excercise");

            migrationBuilder.AddColumn<int>(
                name: "ExcercisesExcerciseId",
                table: "Set",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionsSessionId",
                table: "Excercise",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Set_ExcercisesExcerciseId",
                table: "Set",
                column: "ExcercisesExcerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_Excercise_SessionsSessionId",
                table: "Excercise",
                column: "SessionsSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Excercise_Session_SessionsSessionId",
                table: "Excercise",
                column: "SessionsSessionId",
                principalTable: "Session",
                principalColumn: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Set_Excercise_ExcercisesExcerciseId",
                table: "Set",
                column: "ExcercisesExcerciseId",
                principalTable: "Excercise",
                principalColumn: "ExcerciseId");
        }
    }
}
