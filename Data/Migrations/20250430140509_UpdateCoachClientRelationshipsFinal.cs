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

            // Check if the column exists before trying to add it
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[dbo].[CoachClientRelationships]') 
                    AND name = 'InvitedEmail'
                )
                BEGIN
                    ALTER TABLE [CoachClientRelationships] 
                    ADD [InvitedEmail] nvarchar(256) NULL
                END
            ");

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

            // Check if the column exists before dropping it in the down migration
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[dbo].[CoachClientRelationships]') 
                    AND name = 'InvitedEmail'
                ) 
                BEGIN
                    ALTER TABLE [CoachClientRelationships] 
                    DROP COLUMN [InvitedEmail]
                END
            ");

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
