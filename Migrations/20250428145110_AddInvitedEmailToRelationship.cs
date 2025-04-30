using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitedEmailToRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CoachClientRelationships_CoachId_ClientId",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CoachClientRelationships_CoachId_ClientId",
                table: "CoachClientRelationships");

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
                name: "IX_CoachClientRelationships_CoachId_ClientId",
                table: "CoachClientRelationships",
                columns: new[] { "CoachId", "ClientId" },
                unique: true);
        }
    }
}
