using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class RefactorExerciseToExerciseType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Set_Excercise_ExerciseId",
                table: "Set");

            migrationBuilder.RenameColumn(
                name: "ExerciseId",
                table: "Set",
                newName: "SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_Set_ExerciseId",
                table: "Set",
                newName: "IX_Set_SessionId");

            migrationBuilder.AddColumn<int>(
                name: "ExcerciseId",
                table: "Set",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExerciseTypeId",
                table: "Set",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "Set",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "weight",
                table: "Rep",
                type: "decimal(6,2)",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.CreateTable(
                name: "ExerciseType",
                columns: table => new
                {
                    ExerciseTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseType", x => x.ExerciseTypeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Set_ExcerciseId",
                table: "Set",
                column: "ExcerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_Set_ExerciseTypeId",
                table: "Set",
                column: "ExerciseTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Set_Excercise_ExcerciseId",
                table: "Set",
                column: "ExcerciseId",
                principalTable: "Excercise",
                principalColumn: "ExcerciseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Set_ExerciseType_ExerciseTypeId",
                table: "Set",
                column: "ExerciseTypeId",
                principalTable: "ExerciseType",
                principalColumn: "ExerciseTypeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Set_Session_SessionId",
                table: "Set",
                column: "SessionId",
                principalTable: "Session",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Set_Excercise_ExcerciseId",
                table: "Set");

            migrationBuilder.DropForeignKey(
                name: "FK_Set_ExerciseType_ExerciseTypeId",
                table: "Set");

            migrationBuilder.DropForeignKey(
                name: "FK_Set_Session_SessionId",
                table: "Set");

            migrationBuilder.DropTable(
                name: "ExerciseType");

            migrationBuilder.DropIndex(
                name: "IX_Set_ExcerciseId",
                table: "Set");

            migrationBuilder.DropIndex(
                name: "IX_Set_ExerciseTypeId",
                table: "Set");

            migrationBuilder.DropColumn(
                name: "ExcerciseId",
                table: "Set");

            migrationBuilder.DropColumn(
                name: "ExerciseTypeId",
                table: "Set");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Set");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "Set",
                newName: "ExerciseId");

            migrationBuilder.RenameIndex(
                name: "IX_Set_SessionId",
                table: "Set",
                newName: "IX_Set_ExerciseId");

            migrationBuilder.AlterColumn<float>(
                name: "weight",
                table: "Rep",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)");

            migrationBuilder.AddForeignKey(
                name: "FK_Set_Excercise_ExerciseId",
                table: "Set",
                column: "ExerciseId",
                principalTable: "Excercise",
                principalColumn: "ExcerciseId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
