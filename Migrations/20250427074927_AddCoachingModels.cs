using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachingModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUser",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CoachClientRelationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachClientRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachClientRelationships_AppUser_ClientId",
                        column: x => x.ClientId,
                        principalTable: "AppUser",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CoachClientRelationships_AppUser_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AppUser",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CoachPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CanViewWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CanCreateWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CanEditWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CanDeleteWorkouts = table.Column<bool>(type: "bit", nullable: false),
                    CanViewReports = table.Column<bool>(type: "bit", nullable: false),
                    CanCreateTemplates = table.Column<bool>(type: "bit", nullable: false),
                    CanAssignTemplates = table.Column<bool>(type: "bit", nullable: false),
                    CanViewPersonalInfo = table.Column<bool>(type: "bit", nullable: false),
                    CanCreateGoals = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CoachClientRelationshipId = table.Column<int>(type: "int", nullable: false)
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
                name: "IX_CoachClientRelationships_ClientId",
                table: "CoachClientRelationships",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_CoachId",
                table: "CoachClientRelationships",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_CoachId_ClientId",
                table: "CoachClientRelationships",
                columns: new[] { "CoachId", "ClientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_Status",
                table: "CoachClientRelationships",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CoachPermissions_CoachClientRelationshipId",
                table: "CoachPermissions",
                column: "CoachClientRelationshipId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachPermissions");

            migrationBuilder.DropTable(
                name: "CoachClientRelationships");

            migrationBuilder.DropTable(
                name: "AppUser");
        }
    }
}
