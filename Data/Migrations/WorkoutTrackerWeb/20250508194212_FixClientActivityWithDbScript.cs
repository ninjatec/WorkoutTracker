using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Data.Migrations.WorkoutTrackerWeb
{
    /// <inheritdoc />
    public partial class FixClientActivityWithDbScript : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientActivities_AspNetUsers_ClientId",
                table: "ClientActivities");

            migrationBuilder.DropForeignKey(
                name: "FK_ClientActivities_AspNetUsers_CoachId",
                table: "ClientActivities");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientActivities_AspNetUsers_ClientId",
                table: "ClientActivities",
                column: "ClientId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClientActivities_AspNetUsers_CoachId",
                table: "ClientActivities",
                column: "CoachId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientActivities_AspNetUsers_ClientId",
                table: "ClientActivities");

            migrationBuilder.DropForeignKey(
                name: "FK_ClientActivities_AspNetUsers_CoachId",
                table: "ClientActivities");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientActivities_AspNetUsers_ClientId",
                table: "ClientActivities",
                column: "ClientId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientActivities_AspNetUsers_CoachId",
                table: "ClientActivities",
                column: "CoachId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
