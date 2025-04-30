using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCoachClientRelationshipsFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoachClientRelationships_AspNetUsers_ClientId",
                table: "CoachClientRelationships");

            migrationBuilder.DropForeignKey(
                name: "FK_CoachClientRelationships_AspNetUsers_CoachId",
                table: "CoachClientRelationships");

            migrationBuilder.DropIndex(
                name: "IX_CoachClientRelationships_CoachId",
                table: "CoachClientRelationships");

            migrationBuilder.AlterColumn<string>(
                name: "ClientId",
                table: "CoachClientRelationships",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "InvitedEmail",
                table: "CoachClientRelationships",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_CoachId_ClientId",
                table: "CoachClientRelationships",
                columns: new[] { "CoachId", "ClientId" },
                unique: true,
                filter: "[ClientId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_UserName",
                table: "AspNetUsers",
                column: "UserName",
                unique: true,
                filter: "[UserName] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CoachClientRelationships_AspNetUsers_ClientId",
                table: "CoachClientRelationships",
                column: "ClientId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CoachClientRelationships_AspNetUsers_CoachId",
                table: "CoachClientRelationships",
                column: "CoachId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoachClientRelationships_AspNetUsers_ClientId",
                table: "CoachClientRelationships");

            migrationBuilder.DropForeignKey(
                name: "FK_CoachClientRelationships_AspNetUsers_CoachId",
                table: "CoachClientRelationships");

            migrationBuilder.DropIndex(
                name: "IX_CoachClientRelationships_CoachId_ClientId",
                table: "CoachClientRelationships");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_UserName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "InvitedEmail",
                table: "CoachClientRelationships");

            migrationBuilder.AlterColumn<string>(
                name: "ClientId",
                table: "CoachClientRelationships",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_CoachId",
                table: "CoachClientRelationships",
                column: "CoachId");

            migrationBuilder.AddForeignKey(
                name: "FK_CoachClientRelationships_AspNetUsers_ClientId",
                table: "CoachClientRelationships",
                column: "ClientId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CoachClientRelationships_AspNetUsers_CoachId",
                table: "CoachClientRelationships",
                column: "CoachId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
