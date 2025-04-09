using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class FixSetTypeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Set");

            migrationBuilder.AddColumn<int>(
                name: "SettypeId",
                table: "Set",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Set_SettypeId",
                table: "Set",
                column: "SettypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Set_Settype_SettypeId",
                table: "Set",
                column: "SettypeId",
                principalTable: "Settype",
                principalColumn: "SettypeId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Set_Settype_SettypeId",
                table: "Set");

            migrationBuilder.DropIndex(
                name: "IX_Set_SettypeId",
                table: "Set");

            migrationBuilder.DropColumn(
                name: "SettypeId",
                table: "Set");

            migrationBuilder.AddColumn<bool>(
                name: "Type",
                table: "Set",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
