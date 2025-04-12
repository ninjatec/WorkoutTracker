using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSetRepCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rep_Set_SetsSetId",
                table: "Rep");

            migrationBuilder.AddForeignKey(
                name: "FK_Rep_Set_SetsSetId",
                table: "Rep",
                column: "SetsSetId",
                principalTable: "Set",
                principalColumn: "SetId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rep_Set_SetsSetId",
                table: "Rep");

            migrationBuilder.AddForeignKey(
                name: "FK_Rep_Set_SetsSetId",
                table: "Rep",
                column: "SetsSetId",
                principalTable: "Set",
                principalColumn: "SetId");
        }
    }
}
