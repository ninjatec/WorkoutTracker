using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutScheduleStatusTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastGeneratedSessionId",
                table: "WorkoutSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastGeneratedWorkoutDate",
                table: "WorkoutSchedules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastGenerationStatus",
                table: "WorkoutSchedules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalWorkoutsGenerated",
                table: "WorkoutSchedules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ClientActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CoachId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ActivityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ActivityDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsViewedByCoach = table.Column<bool>(type: "bit", nullable: false),
                    ViewedByCoachDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RelatedEntityId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientActivities_AppUser_ClientId",
                        column: x => x.ClientId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientActivities_AppUser_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GoalFeedback",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoalId = table.Column<int>(type: "int", nullable: false),
                    CoachId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FeedbackType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalFeedback_AppUser_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoalFeedback_ClientGoals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "ClientGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_LastGeneratedSessionId",
                table: "WorkoutSchedules",
                column: "LastGeneratedSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientActivities_ActivityDate",
                table: "ClientActivities",
                column: "ActivityDate");

            migrationBuilder.CreateIndex(
                name: "IX_ClientActivities_ClientId",
                table: "ClientActivities",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientActivities_CoachId",
                table: "ClientActivities",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientActivities_IsViewedByCoach",
                table: "ClientActivities",
                column: "IsViewedByCoach");

            migrationBuilder.CreateIndex(
                name: "IX_GoalFeedback_CoachId",
                table: "GoalFeedback",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalFeedback_GoalId",
                table: "GoalFeedback",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalFeedback_IsRead",
                table: "GoalFeedback",
                column: "IsRead");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSchedules_Session_LastGeneratedSessionId",
                table: "WorkoutSchedules",
                column: "LastGeneratedSessionId",
                principalTable: "Session",
                principalColumn: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSchedules_Session_LastGeneratedSessionId",
                table: "WorkoutSchedules");

            migrationBuilder.DropTable(
                name: "ClientActivities");

            migrationBuilder.DropTable(
                name: "GoalFeedback");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSchedules_LastGeneratedSessionId",
                table: "WorkoutSchedules");

            migrationBuilder.DropColumn(
                name: "LastGeneratedSessionId",
                table: "WorkoutSchedules");

            migrationBuilder.DropColumn(
                name: "LastGeneratedWorkoutDate",
                table: "WorkoutSchedules");

            migrationBuilder.DropColumn(
                name: "LastGenerationStatus",
                table: "WorkoutSchedules");

            migrationBuilder.DropColumn(
                name: "TotalWorkoutsGenerated",
                table: "WorkoutSchedules");
        }
    }
}
