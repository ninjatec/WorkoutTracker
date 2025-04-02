using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class ModeChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Settype",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Settype",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Set",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "ExcercisesExcerciseId",
                table: "Set",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Set",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "SetsSetId",
                table: "Rep",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "SessionsSessionId",
                table: "Excercise",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ExcerciseName",
                table: "Excercise",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddForeignKey(
                name: "FK_Excercise_Session_SessionsSessionId",
                table: "Excercise",
                column: "SessionsSessionId",
                principalTable: "Session",
                principalColumn: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rep_Set_SetsSetId",
                table: "Rep",
                column: "SetsSetId",
                principalTable: "Set",
                principalColumn: "SetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Set_Excercise_ExcercisesExcerciseId",
                table: "Set",
                column: "ExcercisesExcerciseId",
                principalTable: "Excercise",
                principalColumn: "ExcerciseId");
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

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Settype",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Settype",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Set",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ExcercisesExcerciseId",
                table: "Set",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Set",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SetsSetId",
                table: "Rep",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SessionsSessionId",
                table: "Excercise",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExcerciseName",
                table: "Excercise",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

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
    }
}
