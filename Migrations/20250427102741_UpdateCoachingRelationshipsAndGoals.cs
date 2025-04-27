using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCoachingRelationshipsAndGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachPermissions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "CoachClientRelationships");

            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "CoachClientRelationships",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppUserId1",
                table: "CoachClientRelationships",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClientGroupId",
                table: "CoachClientRelationships",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvitationExpiryDate",
                table: "CoachClientRelationships",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvitationToken",
                table: "CoachClientRelationships",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClientGoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachClientRelationshipId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MeasurementType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StartValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CurrentValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientGoals_CoachClientRelationships_CoachClientRelationshipId",
                        column: x => x.CoachClientRelationshipId,
                        principalTable: "CoachClientRelationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CoachId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ColorCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientGroups_AppUser_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachClientMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachClientRelationshipId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsFromCoach = table.Column<bool>(type: "bit", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachClientMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachClientMessages_CoachClientRelationships_CoachClientRelationshipId",
                        column: x => x.CoachClientRelationshipId,
                        principalTable: "CoachClientRelationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachClientPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachClientRelationshipId = table.Column<int>(type: "int", nullable: false),
                    CanViewWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CanCreateWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CanModifyWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CanEditWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CanDeleteWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CanViewPersonalInfo = table.Column<bool>(type: "bit", nullable: false),
                    CanCreateGoals = table.Column<bool>(type: "bit", nullable: false),
                    CanViewReports = table.Column<bool>(type: "bit", nullable: false),
                    CanMessage = table.Column<bool>(type: "bit", nullable: false),
                    CanCreateTemplates = table.Column<bool>(type: "bit", nullable: false),
                    CanAssignTemplates = table.Column<bool>(type: "bit", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachClientPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachClientPermissions_CoachClientRelationships_CoachClientRelationshipId",
                        column: x => x.CoachClientRelationshipId,
                        principalTable: "CoachClientRelationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachClientRelationshipId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsVisibleToClient = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachNotes_CoachClientRelationships_CoachClientRelationshipId",
                        column: x => x.CoachClientRelationshipId,
                        principalTable: "CoachClientRelationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_AppUserId",
                table: "CoachClientRelationships",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_AppUserId1",
                table: "CoachClientRelationships",
                column: "AppUserId1");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_ClientGroupId",
                table: "CoachClientRelationships",
                column: "ClientGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientGoals_CoachClientRelationshipId",
                table: "ClientGoals",
                column: "CoachClientRelationshipId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientGoals_IsActive",
                table: "ClientGoals",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ClientGroups_CoachId",
                table: "ClientGroups",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientMessages_CoachClientRelationshipId",
                table: "CoachClientMessages",
                column: "CoachClientRelationshipId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientMessages_IsRead",
                table: "CoachClientMessages",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientPermissions_CoachClientRelationshipId",
                table: "CoachClientPermissions",
                column: "CoachClientRelationshipId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachNotes_CoachClientRelationshipId",
                table: "CoachNotes",
                column: "CoachClientRelationshipId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachNotes_IsVisibleToClient",
                table: "CoachNotes",
                column: "IsVisibleToClient");

            migrationBuilder.AddForeignKey(
                name: "FK_CoachClientRelationships_AppUser_AppUserId",
                table: "CoachClientRelationships",
                column: "AppUserId",
                principalTable: "AppUser",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CoachClientRelationships_AppUser_AppUserId1",
                table: "CoachClientRelationships",
                column: "AppUserId1",
                principalTable: "AppUser",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CoachClientRelationships_ClientGroups_ClientGroupId",
                table: "CoachClientRelationships",
                column: "ClientGroupId",
                principalTable: "ClientGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoachClientRelationships_AppUser_AppUserId",
                table: "CoachClientRelationships");

            migrationBuilder.DropForeignKey(
                name: "FK_CoachClientRelationships_AppUser_AppUserId1",
                table: "CoachClientRelationships");

            migrationBuilder.DropForeignKey(
                name: "FK_CoachClientRelationships_ClientGroups_ClientGroupId",
                table: "CoachClientRelationships");

            migrationBuilder.DropTable(
                name: "ClientGoals");

            migrationBuilder.DropTable(
                name: "ClientGroups");

            migrationBuilder.DropTable(
                name: "CoachClientMessages");

            migrationBuilder.DropTable(
                name: "CoachClientPermissions");

            migrationBuilder.DropTable(
                name: "CoachNotes");

            migrationBuilder.DropIndex(
                name: "IX_CoachClientRelationships_AppUserId",
                table: "CoachClientRelationships");

            migrationBuilder.DropIndex(
                name: "IX_CoachClientRelationships_AppUserId1",
                table: "CoachClientRelationships");

            migrationBuilder.DropIndex(
                name: "IX_CoachClientRelationships_ClientGroupId",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "AppUserId1",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "ClientGroupId",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "InvitationExpiryDate",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "InvitationToken",
                table: "CoachClientRelationships");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "CoachClientRelationships",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CoachPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachClientRelationshipId = table.Column<int>(type: "int", nullable: false),
                    CanAssignTemplates = table.Column<bool>(type: "bit", nullable: false),
                    CanCreateGoals = table.Column<bool>(type: "bit", nullable: false),
                    CanCreateTemplates = table.Column<bool>(type: "bit", nullable: false),
                    CanCreateWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CanDeleteWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CanEditWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CanViewPersonalInfo = table.Column<bool>(type: "bit", nullable: false),
                    CanViewReports = table.Column<bool>(type: "bit", nullable: false),
                    CanViewWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachPermissions_CoachClientRelationships_CoachClientRelationshipId",
                        column: x => x.CoachClientRelationshipId,
                        principalTable: "CoachClientRelationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachPermissions_CoachClientRelationshipId",
                table: "CoachPermissions",
                column: "CoachClientRelationshipId",
                unique: true);
        }
    }
}
