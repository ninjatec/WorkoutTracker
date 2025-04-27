using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutProgrammingModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientEquipments",
                columns: table => new
                {
                    ClientEquipmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientUserId = table.Column<int>(type: "int", nullable: false),
                    EquipmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientEquipments", x => x.ClientEquipmentId);
                    table.ForeignKey(
                        name: "FK_ClientEquipments_User_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientExerciseExclusions",
                columns: table => new
                {
                    ClientExerciseExclusionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientUserId = table.Column<int>(type: "int", nullable: false),
                    ExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByCoachId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientExerciseExclusions", x => x.ClientExerciseExclusionId);
                    table.ForeignKey(
                        name: "FK_ClientExerciseExclusions_ExerciseType_ExerciseTypeId",
                        column: x => x.ExerciseTypeId,
                        principalTable: "ExerciseType",
                        principalColumn: "ExerciseTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientExerciseExclusions_User_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientExerciseExclusions_User_CreatedByCoachId",
                        column: x => x.CreatedByCoachId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ExerciseSubstitutions",
                columns: table => new
                {
                    ExerciseSubstitutionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrimaryExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    SubstituteExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    EquivalencePercentage = table.Column<int>(type: "int", nullable: false),
                    MovementPattern = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EquipmentRequired = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MusclesTargeted = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByCoachId = table.Column<int>(type: "int", nullable: false),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseSubstitutions", x => x.ExerciseSubstitutionId);
                    table.ForeignKey(
                        name: "FK_ExerciseSubstitutions_ExerciseType_PrimaryExerciseTypeId",
                        column: x => x.PrimaryExerciseTypeId,
                        principalTable: "ExerciseType",
                        principalColumn: "ExerciseTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExerciseSubstitutions_ExerciseType_SubstituteExerciseTypeId",
                        column: x => x.SubstituteExerciseTypeId,
                        principalTable: "ExerciseType",
                        principalColumn: "ExerciseTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExerciseSubstitutions_User_CreatedByCoachId",
                        column: x => x.CreatedByCoachId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProgressionRules",
                columns: table => new
                {
                    ProgressionRuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkoutTemplateExerciseId = table.Column<int>(type: "int", nullable: true),
                    WorkoutTemplateSetId = table.Column<int>(type: "int", nullable: true),
                    ClientUserId = table.Column<int>(type: "int", nullable: true),
                    CoachUserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RuleType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Parameter = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IncrementValue = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ConsecutiveSuccessesRequired = table.Column<int>(type: "int", nullable: false),
                    SuccessThreshold = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaximumValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ApplyDeload = table.Column<bool>(type: "bit", nullable: false),
                    DeloadPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ConsecutiveFailuresForDeload = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressionRules", x => x.ProgressionRuleId);
                    table.ForeignKey(
                        name: "FK_ProgressionRules_User_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_ProgressionRules_User_CoachUserId",
                        column: x => x.CoachUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgressionRules_WorkoutTemplateExercise_WorkoutTemplateExerciseId",
                        column: x => x.WorkoutTemplateExerciseId,
                        principalTable: "WorkoutTemplateExercise",
                        principalColumn: "WorkoutTemplateExerciseId");
                    table.ForeignKey(
                        name: "FK_ProgressionRules_WorkoutTemplateSet_WorkoutTemplateSetId",
                        column: x => x.WorkoutTemplateSetId,
                        principalTable: "WorkoutTemplateSet",
                        principalColumn: "WorkoutTemplateSetId");
                });

            migrationBuilder.CreateTable(
                name: "TemplateAssignments",
                columns: table => new
                {
                    TemplateAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkoutTemplateId = table.Column<int>(type: "int", nullable: false),
                    ClientUserId = table.Column<int>(type: "int", nullable: false),
                    CoachUserId = table.Column<int>(type: "int", nullable: false),
                    ClientGroupName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CoachNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ClientNotified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateAssignments", x => x.TemplateAssignmentId);
                    table.ForeignKey(
                        name: "FK_TemplateAssignments_User_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateAssignments_User_CoachUserId",
                        column: x => x.CoachUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateAssignments_WorkoutTemplate_WorkoutTemplateId",
                        column: x => x.WorkoutTemplateId,
                        principalTable: "WorkoutTemplate",
                        principalColumn: "WorkoutTemplateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutFeedbacks",
                columns: table => new
                {
                    WorkoutFeedbackId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    ClientUserId = table.Column<int>(type: "int", nullable: false),
                    FeedbackDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OverallRating = table.Column<int>(type: "int", nullable: false),
                    DifficultyRating = table.Column<int>(type: "int", nullable: false),
                    EnergyLevel = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompletedAllExercises = table.Column<bool>(type: "bit", nullable: false),
                    IncompleteReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CoachNotified = table.Column<bool>(type: "bit", nullable: false),
                    CoachViewed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutFeedbacks", x => x.WorkoutFeedbackId);
                    table.ForeignKey(
                        name: "FK_WorkoutFeedbacks_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutFeedbacks_User_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProgressionHistories",
                columns: table => new
                {
                    ProgressionHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgressionRuleId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: true),
                    ApplicationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActionTaken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreviousValue = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    NewValue = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AppliedAutomatically = table.Column<bool>(type: "bit", nullable: false),
                    CoachOverride = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressionHistories", x => x.ProgressionHistoryId);
                    table.ForeignKey(
                        name: "FK_ProgressionHistories_ProgressionRules_ProgressionRuleId",
                        column: x => x.ProgressionRuleId,
                        principalTable: "ProgressionRules",
                        principalColumn: "ProgressionRuleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgressionHistories_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "SessionId");
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSchedules",
                columns: table => new
                {
                    WorkoutScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateAssignmentId = table.Column<int>(type: "int", nullable: true),
                    ClientUserId = table.Column<int>(type: "int", nullable: false),
                    CoachUserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduledDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    RecurrencePattern = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RecurrenceDayOfWeek = table.Column<int>(type: "int", nullable: true),
                    RecurrenceDayOfMonth = table.Column<int>(type: "int", nullable: true),
                    SendReminder = table.Column<bool>(type: "bit", nullable: false),
                    ReminderHoursBefore = table.Column<int>(type: "int", nullable: false),
                    LastReminderSent = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSchedules", x => x.WorkoutScheduleId);
                    table.ForeignKey(
                        name: "FK_WorkoutSchedules_TemplateAssignments_TemplateAssignmentId",
                        column: x => x.TemplateAssignmentId,
                        principalTable: "TemplateAssignments",
                        principalColumn: "TemplateAssignmentId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkoutSchedules_User_ClientUserId",
                        column: x => x.ClientUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutSchedules_User_CoachUserId",
                        column: x => x.CoachUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseFeedbacks",
                columns: table => new
                {
                    ExerciseFeedbackId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkoutFeedbackId = table.Column<int>(type: "int", nullable: false),
                    SetId = table.Column<int>(type: "int", nullable: false),
                    RPE = table.Column<int>(type: "int", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    TooHeavy = table.Column<bool>(type: "bit", nullable: false),
                    TooLight = table.Column<bool>(type: "bit", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseFeedbacks", x => x.ExerciseFeedbackId);
                    table.ForeignKey(
                        name: "FK_ExerciseFeedbacks_Set_SetId",
                        column: x => x.SetId,
                        principalTable: "Set",
                        principalColumn: "SetId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExerciseFeedbacks_WorkoutFeedbacks_WorkoutFeedbackId",
                        column: x => x.WorkoutFeedbackId,
                        principalTable: "WorkoutFeedbacks",
                        principalColumn: "WorkoutFeedbackId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientEquipments_ClientUserId",
                table: "ClientEquipments",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientEquipments_IsAvailable",
                table: "ClientEquipments",
                column: "IsAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_ClientExerciseExclusions_ClientUserId",
                table: "ClientExerciseExclusions",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientExerciseExclusions_CreatedByCoachId",
                table: "ClientExerciseExclusions",
                column: "CreatedByCoachId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientExerciseExclusions_ExerciseTypeId",
                table: "ClientExerciseExclusions",
                column: "ExerciseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientExerciseExclusions_IsActive",
                table: "ClientExerciseExclusions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseFeedbacks_SetId",
                table: "ExerciseFeedbacks",
                column: "SetId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseFeedbacks_WorkoutFeedbackId",
                table: "ExerciseFeedbacks",
                column: "WorkoutFeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSubstitutions_CreatedByCoachId",
                table: "ExerciseSubstitutions",
                column: "CreatedByCoachId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSubstitutions_IsGlobal",
                table: "ExerciseSubstitutions",
                column: "IsGlobal");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSubstitutions_MovementPattern",
                table: "ExerciseSubstitutions",
                column: "MovementPattern");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSubstitutions_PrimaryExerciseTypeId",
                table: "ExerciseSubstitutions",
                column: "PrimaryExerciseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSubstitutions_SubstituteExerciseTypeId",
                table: "ExerciseSubstitutions",
                column: "SubstituteExerciseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionHistories_ApplicationDate",
                table: "ProgressionHistories",
                column: "ApplicationDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionHistories_ProgressionRuleId",
                table: "ProgressionHistories",
                column: "ProgressionRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionHistories_SessionId",
                table: "ProgressionHistories",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionRules_ClientUserId",
                table: "ProgressionRules",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionRules_CoachUserId",
                table: "ProgressionRules",
                column: "CoachUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionRules_IsActive",
                table: "ProgressionRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionRules_WorkoutTemplateExerciseId",
                table: "ProgressionRules",
                column: "WorkoutTemplateExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionRules_WorkoutTemplateSetId",
                table: "ProgressionRules",
                column: "WorkoutTemplateSetId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAssignments_ClientUserId",
                table: "TemplateAssignments",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAssignments_CoachUserId",
                table: "TemplateAssignments",
                column: "CoachUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAssignments_IsActive",
                table: "TemplateAssignments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAssignments_WorkoutTemplateId",
                table: "TemplateAssignments",
                column: "WorkoutTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutFeedbacks_ClientUserId",
                table: "WorkoutFeedbacks",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutFeedbacks_CoachNotified",
                table: "WorkoutFeedbacks",
                column: "CoachNotified");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutFeedbacks_CoachViewed",
                table: "WorkoutFeedbacks",
                column: "CoachViewed");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutFeedbacks_SessionId",
                table: "WorkoutFeedbacks",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_ClientUserId",
                table: "WorkoutSchedules",
                column: "ClientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_CoachUserId",
                table: "WorkoutSchedules",
                column: "CoachUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_IsActive",
                table: "WorkoutSchedules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_TemplateAssignmentId",
                table: "WorkoutSchedules",
                column: "TemplateAssignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientEquipments");

            migrationBuilder.DropTable(
                name: "ClientExerciseExclusions");

            migrationBuilder.DropTable(
                name: "ExerciseFeedbacks");

            migrationBuilder.DropTable(
                name: "ExerciseSubstitutions");

            migrationBuilder.DropTable(
                name: "ProgressionHistories");

            migrationBuilder.DropTable(
                name: "WorkoutSchedules");

            migrationBuilder.DropTable(
                name: "WorkoutFeedbacks");

            migrationBuilder.DropTable(
                name: "ProgressionRules");

            migrationBuilder.DropTable(
                name: "TemplateAssignments");
        }
    }
}
