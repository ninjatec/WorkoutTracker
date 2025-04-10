using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class CompleteExerciseTypeRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Set_Excercise_ExcerciseId",
                table: "Set");

            migrationBuilder.DropTable(
                name: "Excercise");

            migrationBuilder.DropIndex(
                name: "IX_Set_ExcerciseId",
                table: "Set");

            migrationBuilder.DropColumn(
                name: "ExcerciseId",
                table: "Set");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExcerciseId",
                table: "Set",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Excercise",
                columns: table => new
                {
                    ExcerciseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    ExcerciseName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Excercise", x => x.ExcerciseId);
                    table.ForeignKey(
                        name: "FK_Excercise_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Set_ExcerciseId",
                table: "Set",
                column: "ExcerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_Excercise_SessionId",
                table: "Excercise",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Set_Excercise_ExcerciseId",
                table: "Set",
                column: "ExcerciseId",
                principalTable: "Excercise",
                principalColumn: "ExcerciseId");
        }
    }
}
