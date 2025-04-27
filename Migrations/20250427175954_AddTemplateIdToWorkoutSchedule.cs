using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateIdToWorkoutSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EquipmentId",
                table: "WorkoutTemplateExercise",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxReps",
                table: "WorkoutTemplateExercise",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinReps",
                table: "WorkoutTemplateExercise",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "WorkoutTemplateExercise",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RestSeconds",
                table: "WorkoutTemplateExercise",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Sets",
                table: "WorkoutTemplateExercise",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TemplateAssignmentId1",
                table: "WorkoutSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TemplateId",
                table: "WorkoutSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "TemplateAssignments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "TemplateAssignments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "TemplateAssignments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDateTime",
                table: "Session",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "ExerciseType",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryMuscles",
                table: "ExerciseType",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryMuscles",
                table: "ExerciseType",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    EquipmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.EquipmentId);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSessions",
                columns: table => new
                {
                    WorkoutSessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    TemplatesUsed = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WorkoutTemplateId = table.Column<int>(type: "int", nullable: true),
                    TemplateAssignmentId = table.Column<int>(type: "int", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsFromCoach = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WorkoutFeedbackId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSessions", x => x.WorkoutSessionId);
                    table.ForeignKey(
                        name: "FK_WorkoutSessions_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutSessions_WorkoutFeedbacks_WorkoutFeedbackId",
                        column: x => x.WorkoutFeedbackId,
                        principalTable: "WorkoutFeedbacks",
                        principalColumn: "WorkoutFeedbackId");
                    table.ForeignKey(
                        name: "FK_WorkoutSessions_WorkoutTemplate_WorkoutTemplateId",
                        column: x => x.WorkoutTemplateId,
                        principalTable: "WorkoutTemplate",
                        principalColumn: "WorkoutTemplateId");
                });

            migrationBuilder.CreateTable(
                name: "WorkoutExercises",
                columns: table => new
                {
                    WorkoutExerciseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkoutSessionId = table.Column<int>(type: "int", nullable: false),
                    ExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    EquipmentId = table.Column<int>(type: "int", nullable: true),
                    SequenceNum = table.Column<int>(type: "int", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RestPeriodSeconds = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutExercises", x => x.WorkoutExerciseId);
                    table.ForeignKey(
                        name: "FK_WorkoutExercises_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "EquipmentId");
                    table.ForeignKey(
                        name: "FK_WorkoutExercises_ExerciseType_ExerciseTypeId",
                        column: x => x.ExerciseTypeId,
                        principalTable: "ExerciseType",
                        principalColumn: "ExerciseTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutExercises_WorkoutSessions_WorkoutSessionId",
                        column: x => x.WorkoutSessionId,
                        principalTable: "WorkoutSessions",
                        principalColumn: "WorkoutSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSets",
                columns: table => new
                {
                    WorkoutSetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkoutExerciseId = table.Column<int>(type: "int", nullable: false),
                    SettypeId = table.Column<int>(type: "int", nullable: true),
                    SequenceNum = table.Column<int>(type: "int", nullable: false),
                    SetNumber = table.Column<int>(type: "int", nullable: false),
                    Reps = table.Column<int>(type: "int", nullable: true),
                    TargetMinReps = table.Column<int>(type: "int", nullable: true),
                    TargetMaxReps = table.Column<int>(type: "int", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    DistanceMeters = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    RPE = table.Column<int>(type: "int", nullable: true),
                    RestSeconds = table.Column<int>(type: "int", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSets", x => x.WorkoutSetId);
                    table.ForeignKey(
                        name: "FK_WorkoutSets_Settype_SettypeId",
                        column: x => x.SettypeId,
                        principalTable: "Settype",
                        principalColumn: "SettypeId");
                    table.ForeignKey(
                        name: "FK_WorkoutSets_WorkoutExercises_WorkoutExerciseId",
                        column: x => x.WorkoutExerciseId,
                        principalTable: "WorkoutExercises",
                        principalColumn: "WorkoutExerciseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercise_EquipmentId",
                table: "WorkoutTemplateExercise",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_TemplateAssignmentId1",
                table: "WorkoutSchedules",
                column: "TemplateAssignmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSchedules_TemplateId",
                table: "WorkoutSchedules",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_EquipmentId",
                table: "WorkoutExercises",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_ExerciseTypeId",
                table: "WorkoutExercises",
                column: "ExerciseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_WorkoutSessionId",
                table: "WorkoutExercises",
                column: "WorkoutSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_UserId",
                table: "WorkoutSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_WorkoutFeedbackId",
                table: "WorkoutSessions",
                column: "WorkoutFeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_WorkoutTemplateId",
                table: "WorkoutSessions",
                column: "WorkoutTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSets_SettypeId",
                table: "WorkoutSets",
                column: "SettypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSets_WorkoutExerciseId",
                table: "WorkoutSets",
                column: "WorkoutExerciseId");

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
                name: "FK_WorkoutTemplateExercise_Equipment_EquipmentId",
                table: "WorkoutTemplateExercise",
                column: "EquipmentId",
                principalTable: "Equipment",
                principalColumn: "EquipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSchedules_TemplateAssignments_TemplateAssignmentId1",
                table: "WorkoutSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSchedules_WorkoutTemplate_TemplateId",
                table: "WorkoutSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutTemplateExercise_Equipment_EquipmentId",
                table: "WorkoutTemplateExercise");

            migrationBuilder.DropTable(
                name: "WorkoutSets");

            migrationBuilder.DropTable(
                name: "WorkoutExercises");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutTemplateExercise_EquipmentId",
                table: "WorkoutTemplateExercise");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSchedules_TemplateAssignmentId1",
                table: "WorkoutSchedules");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSchedules_TemplateId",
                table: "WorkoutSchedules");

            migrationBuilder.DropColumn(
                name: "EquipmentId",
                table: "WorkoutTemplateExercise");

            migrationBuilder.DropColumn(
                name: "MaxReps",
                table: "WorkoutTemplateExercise");

            migrationBuilder.DropColumn(
                name: "MinReps",
                table: "WorkoutTemplateExercise");

            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "WorkoutTemplateExercise");

            migrationBuilder.DropColumn(
                name: "RestSeconds",
                table: "WorkoutTemplateExercise");

            migrationBuilder.DropColumn(
                name: "Sets",
                table: "WorkoutTemplateExercise");

            migrationBuilder.DropColumn(
                name: "TemplateAssignmentId1",
                table: "WorkoutSchedules");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "WorkoutSchedules");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "TemplateAssignments");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "TemplateAssignments");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "TemplateAssignments");

            migrationBuilder.DropColumn(
                name: "StartDateTime",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "ExerciseType");

            migrationBuilder.DropColumn(
                name: "PrimaryMuscles",
                table: "ExerciseType");

            migrationBuilder.DropColumn(
                name: "SecondaryMuscles",
                table: "ExerciseType");
        }
    }
}
