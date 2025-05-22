using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWorkoutFeedbackShadowProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientActivities_AppUser_ClientId",
                table: "ClientActivities");

            migrationBuilder.DropForeignKey(
                name: "FK_ClientActivities_AppUser_CoachId",
                table: "ClientActivities");

            migrationBuilder.DropForeignKey(
                name: "FK_CoachClientRelationships_AppUser_AppUserId",
                table: "CoachClientRelationships");

            migrationBuilder.DropForeignKey(
                name: "FK_CoachClientRelationships_AppUser_AppUserId1",
                table: "CoachClientRelationships");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseSubstitutions_ExerciseType_PrimaryExerciseTypeId",
                table: "ExerciseSubstitutions");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseSubstitutions_ExerciseType_SubstituteExerciseTypeId",
                table: "ExerciseSubstitutions");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseSubstitutions_User_CreatedByCoachId",
                table: "ExerciseSubstitutions");

            migrationBuilder.DropForeignKey(
                name: "FK_GoalFeedback_AppUser_CoachId",
                table: "GoalFeedback");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgressionHistories_WorkoutSessions_WorkoutSessionId",
                table: "ProgressionHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateAssignments_CoachClientRelationships_ClientRelationshipId",
                table: "TemplateAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateAssignments_User_ClientUserId",
                table: "TemplateAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateAssignments_User_CoachUserId",
                table: "TemplateAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateAssignments_WorkoutTemplate_WorkoutTemplateId",
                table: "TemplateAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSchedules_TemplateAssignments_TemplateAssignmentId1",
                table: "WorkoutSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSchedules_WorkoutTemplate_TemplateId",
                table: "WorkoutSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_User_UserId",
                table: "WorkoutSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_WorkoutSchedules_ScheduleId",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSchedules_LastGeneratedSessionId",
                table: "WorkoutSchedules");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSchedules_TemplateAssignmentId1",
                table: "WorkoutSchedules");

            migrationBuilder.DropIndex(
                name: "IX_CoachClientRelationships_AppUserId",
                table: "CoachClientRelationships");

            migrationBuilder.DropIndex(
                name: "IX_CoachClientRelationships_AppUserId1",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "TemplateAssignmentId1",
                table: "WorkoutSchedules");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "AppUserId1",
                table: "CoachClientRelationships");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_LastGeneratedSessionId",
                table: "WorkoutSchedules",
                column: "LastGeneratedSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientActivities_AppUser_ClientId",
                table: "ClientActivities",
                column: "ClientId",
                principalTable: "AppUser",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientActivities_AppUser_CoachId",
                table: "ClientActivities",
                column: "CoachId",
                principalTable: "AppUser",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseSubstitutions_ExerciseType_PrimaryExerciseTypeId",
                table: "ExerciseSubstitutions",
                column: "PrimaryExerciseTypeId",
                principalTable: "ExerciseType",
                principalColumn: "ExerciseTypeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseSubstitutions_ExerciseType_SubstituteExerciseTypeId",
                table: "ExerciseSubstitutions",
                column: "SubstituteExerciseTypeId",
                principalTable: "ExerciseType",
                principalColumn: "ExerciseTypeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseSubstitutions_User_CreatedByCoachId",
                table: "ExerciseSubstitutions",
                column: "CreatedByCoachId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GoalFeedback_AppUser_CoachId",
                table: "GoalFeedback",
                column: "CoachId",
                principalTable: "AppUser",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgressionHistories_WorkoutSessions_WorkoutSessionId",
                table: "ProgressionHistories",
                column: "WorkoutSessionId",
                principalTable: "WorkoutSessions",
                principalColumn: "WorkoutSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateAssignments_CoachClientRelationships_ClientRelationshipId",
                table: "TemplateAssignments",
                column: "ClientRelationshipId",
                principalTable: "CoachClientRelationships",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateAssignments_User_ClientUserId",
                table: "TemplateAssignments",
                column: "ClientUserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateAssignments_User_CoachUserId",
                table: "TemplateAssignments",
                column: "CoachUserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateAssignments_WorkoutTemplate_WorkoutTemplateId",
                table: "TemplateAssignments",
                column: "WorkoutTemplateId",
                principalTable: "WorkoutTemplate",
                principalColumn: "WorkoutTemplateId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSchedules_WorkoutTemplate_TemplateId",
                table: "WorkoutSchedules",
                column: "TemplateId",
                principalTable: "WorkoutTemplate",
                principalColumn: "WorkoutTemplateId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_User_UserId",
                table: "WorkoutSessions",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_WorkoutSchedules_ScheduleId",
                table: "WorkoutSessions",
                column: "ScheduleId",
                principalTable: "WorkoutSchedules",
                principalColumn: "WorkoutScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientActivities_AppUser_ClientId",
                table: "ClientActivities");

            migrationBuilder.DropForeignKey(
                name: "FK_ClientActivities_AppUser_CoachId",
                table: "ClientActivities");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseSubstitutions_ExerciseType_PrimaryExerciseTypeId",
                table: "ExerciseSubstitutions");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseSubstitutions_ExerciseType_SubstituteExerciseTypeId",
                table: "ExerciseSubstitutions");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseSubstitutions_User_CreatedByCoachId",
                table: "ExerciseSubstitutions");

            migrationBuilder.DropForeignKey(
                name: "FK_GoalFeedback_AppUser_CoachId",
                table: "GoalFeedback");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgressionHistories_WorkoutSessions_WorkoutSessionId",
                table: "ProgressionHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateAssignments_CoachClientRelationships_ClientRelationshipId",
                table: "TemplateAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateAssignments_User_ClientUserId",
                table: "TemplateAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateAssignments_User_CoachUserId",
                table: "TemplateAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateAssignments_WorkoutTemplate_WorkoutTemplateId",
                table: "TemplateAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSchedules_WorkoutTemplate_TemplateId",
                table: "WorkoutSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_User_UserId",
                table: "WorkoutSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_WorkoutSchedules_ScheduleId",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSchedules_LastGeneratedSessionId",
                table: "WorkoutSchedules");

            migrationBuilder.AddColumn<int>(
                name: "TemplateAssignmentId1",
                table: "WorkoutSchedules",
                type: "int",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_LastGeneratedSessionId",
                table: "WorkoutSchedules",
                column: "LastGeneratedSessionId",
                unique: true,
                filter: "[LastGeneratedSessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_TemplateAssignmentId1",
                table: "WorkoutSchedules",
                column: "TemplateAssignmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_AppUserId",
                table: "CoachClientRelationships",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_AppUserId1",
                table: "CoachClientRelationships",
                column: "AppUserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientActivities_AppUser_ClientId",
                table: "ClientActivities",
                column: "ClientId",
                principalTable: "AppUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClientActivities_AppUser_CoachId",
                table: "ClientActivities",
                column: "CoachId",
                principalTable: "AppUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

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
                name: "FK_ExerciseSubstitutions_ExerciseType_PrimaryExerciseTypeId",
                table: "ExerciseSubstitutions",
                column: "PrimaryExerciseTypeId",
                principalTable: "ExerciseType",
                principalColumn: "ExerciseTypeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseSubstitutions_ExerciseType_SubstituteExerciseTypeId",
                table: "ExerciseSubstitutions",
                column: "SubstituteExerciseTypeId",
                principalTable: "ExerciseType",
                principalColumn: "ExerciseTypeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseSubstitutions_User_CreatedByCoachId",
                table: "ExerciseSubstitutions",
                column: "CreatedByCoachId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GoalFeedback_AppUser_CoachId",
                table: "GoalFeedback",
                column: "CoachId",
                principalTable: "AppUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgressionHistories_WorkoutSessions_WorkoutSessionId",
                table: "ProgressionHistories",
                column: "WorkoutSessionId",
                principalTable: "WorkoutSessions",
                principalColumn: "WorkoutSessionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateAssignments_CoachClientRelationships_ClientRelationshipId",
                table: "TemplateAssignments",
                column: "ClientRelationshipId",
                principalTable: "CoachClientRelationships",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateAssignments_User_ClientUserId",
                table: "TemplateAssignments",
                column: "ClientUserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateAssignments_User_CoachUserId",
                table: "TemplateAssignments",
                column: "CoachUserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateAssignments_WorkoutTemplate_WorkoutTemplateId",
                table: "TemplateAssignments",
                column: "WorkoutTemplateId",
                principalTable: "WorkoutTemplate",
                principalColumn: "WorkoutTemplateId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSchedules_TemplateAssignments_TemplateAssignmentId1",
                table: "WorkoutSchedules",
                column: "TemplateAssignmentId1",
                principalTable: "TemplateAssignments",
                principalColumn: "TemplateAssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSchedules_WorkoutTemplate_TemplateId",
                table: "WorkoutSchedules",
                column: "TemplateId",
                principalTable: "WorkoutTemplate",
                principalColumn: "WorkoutTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_User_UserId",
                table: "WorkoutSessions",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_WorkoutSchedules_ScheduleId",
                table: "WorkoutSessions",
                column: "ScheduleId",
                principalTable: "WorkoutSchedules",
                principalColumn: "WorkoutScheduleId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
