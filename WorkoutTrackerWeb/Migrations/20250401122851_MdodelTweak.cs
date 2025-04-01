using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class MdodelTweak : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Excercise_Session_SessionId",
                table: "Excercise");

            migrationBuilder.DropForeignKey(
                name: "FK_Rep_Set_SetId",
                table: "Rep");

            migrationBuilder.DropForeignKey(
                name: "FK_Set_Excercise_ExcerciseId",
                table: "Set");

            migrationBuilder.RenameColumn(
                name: "ExcerciseId",
                table: "Set",
                newName: "ExcercisesExcerciseId");

            migrationBuilder.RenameIndex(
                name: "IX_Set_ExcerciseId",
                table: "Set",
                newName: "IX_Set_ExcercisesExcerciseId");

            migrationBuilder.RenameColumn(
                name: "SetId",
                table: "Rep",
                newName: "SetsSetId");

            migrationBuilder.RenameIndex(
                name: "IX_Rep_SetId",
                table: "Rep",
                newName: "IX_Rep_SetsSetId");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "Excercise",
                newName: "SessionsSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_Excercise_SessionId",
                table: "Excercise",
                newName: "IX_Excercise_SessionsSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Excercise_Session_SessionsSessionId",
                table: "Excercise",
                column: "SessionsSessionId",
                principalTable: "Session",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rep_Set_SetsSetId",
                table: "Rep",
                column: "SetsSetId",
                principalTable: "Set",
                principalColumn: "SetId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Set_Excercise_ExcercisesExcerciseId",
                table: "Set",
                column: "ExcercisesExcerciseId",
                principalTable: "Excercise",
                principalColumn: "ExcerciseId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Excercise_Session_SessionsSessionId",
                table: "Excercise");

            migrationBuilder.DropForeignKey(
                name: "FK_Rep_Set_SetsSetId",
                table: "Rep");

            migrationBuilder.DropForeignKey(
                name: "FK_Set_Excercise_ExcercisesExcerciseId",
                table: "Set");

            migrationBuilder.RenameColumn(
                name: "ExcercisesExcerciseId",
                table: "Set",
                newName: "ExcerciseId");

            migrationBuilder.RenameIndex(
                name: "IX_Set_ExcercisesExcerciseId",
                table: "Set",
                newName: "IX_Set_ExcerciseId");

            migrationBuilder.RenameColumn(
                name: "SetsSetId",
                table: "Rep",
                newName: "SetId");

            migrationBuilder.RenameIndex(
                name: "IX_Rep_SetsSetId",
                table: "Rep",
                newName: "IX_Rep_SetId");

            migrationBuilder.RenameColumn(
                name: "SessionsSessionId",
                table: "Excercise",
                newName: "SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_Excercise_SessionsSessionId",
                table: "Excercise",
                newName: "IX_Excercise_SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Excercise_Session_SessionId",
                table: "Excercise",
                column: "SessionId",
                principalTable: "Session",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rep_Set_SetId",
                table: "Rep",
                column: "SetId",
                principalTable: "Set",
                principalColumn: "SetId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Set_Excercise_ExcerciseId",
                table: "Set",
                column: "ExcerciseId",
                principalTable: "Excercise",
                principalColumn: "ExcerciseId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
