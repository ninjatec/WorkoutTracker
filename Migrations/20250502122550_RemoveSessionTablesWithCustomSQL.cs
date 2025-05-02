using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSessionTablesWithCustomSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use SQL commands with IF EXISTS to check for indexes and drop them if they exist
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Rep_success_SetsSetId' AND object_id = OBJECT_ID('Rep'))
                BEGIN
                    DROP INDEX IX_Rep_success_SetsSetId ON Rep;
                END

                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Rep_SetsSetId' AND object_id = OBJECT_ID('Rep'))
                BEGIN
                    DROP INDEX IX_Rep_SetsSetId ON Rep;
                END
            ");
            
            // First drop foreign keys to prevent constraint violations
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Rep_Set_SetsSetId')
                BEGIN
                    ALTER TABLE Rep DROP CONSTRAINT FK_Rep_Set_SetsSetId;
                END
            ");
            
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseFeedbacks_Set_SetId",
                table: "ExerciseFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgressionHistories_Session_SessionId",
                table: "ProgressionHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Rep_Set_SetsSetId",
                table: "Rep");

            migrationBuilder.DropForeignKey(
                name: "FK_ShareToken_Session_SessionId",
                table: "ShareToken");

            migrationBuilder.DropForeignKey(
                name: "FK_ShareToken_WorkoutSessions_WorkoutSessionId",
                table: "ShareToken");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutExercises_ExerciseType_ExerciseTypeId",
                table: "WorkoutExercises");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutFeedbacks_Session_SessionId",
                table: "WorkoutFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutFeedbacks_User_ClientUserId",
                table: "WorkoutFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSchedules_Session_LastGeneratedSessionId",
                table: "WorkoutSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_Session_SessionId",
                table: "WorkoutSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_User_UserId",
                table: "WorkoutSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_WorkoutFeedbacks_WorkoutFeedbackId",
                table: "WorkoutSessions");

            migrationBuilder.DropTable(
                name: "Set");

            migrationBuilder.DropTable(
                name: "Session");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_SessionId",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutFeedbacks_SessionId",
                table: "WorkoutFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_ShareToken_SessionId",
                table: "ShareToken");

            migrationBuilder.DropIndex(
                name: "IX_Rep_SetsSetId",
                table: "Rep");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "ShareToken");

            migrationBuilder.DropColumn(
                name: "SetsSetId",
                table: "Rep");

            migrationBuilder.RenameColumn(
                name: "WorkoutFeedbackId",
                table: "WorkoutSessions",
                newName: "UserId1");

            migrationBuilder.RenameIndex(
                name: "IX_WorkoutSessions_WorkoutFeedbackId",
                table: "WorkoutSessions",
                newName: "IX_WorkoutSessions_UserId1");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "WorkoutFeedbacks",
                newName: "WorkoutSessionId");

            migrationBuilder.RenameColumn(
                name: "weight",
                table: "Rep",
                newName: "Weight");

            migrationBuilder.RenameColumn(
                name: "success",
                table: "Rep",
                newName: "Success");

            migrationBuilder.RenameColumn(
                name: "repnumber",
                table: "Rep",
                newName: "RepNumber");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "ProgressionHistories",
                newName: "WorkoutSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_ProgressionHistories_SessionId",
                table: "ProgressionHistories",
                newName: "IX_ProgressionHistories_WorkoutSessionId");

            migrationBuilder.RenameColumn(
                name: "SetId",
                table: "ExerciseFeedbacks",
                newName: "WorkoutSetId");

            migrationBuilder.RenameIndex(
                name: "IX_ExerciseFeedbacks_SetId",
                table: "ExerciseFeedbacks",
                newName: "IX_ExerciseFeedbacks_WorkoutSetId");

            migrationBuilder.AddColumn<int>(
                name: "ExerciseTypeId1",
                table: "WorkoutTemplateExercise",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Weight",
                table: "WorkoutSets",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RPE",
                table: "WorkoutSets",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedOneRM",
                table: "WorkoutSets",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsWarmup",
                table: "WorkoutSets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "RestTime",
                table: "WorkoutSets",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetReps",
                table: "WorkoutSets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetWeight",
                table: "WorkoutSets",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkoutSessionId1",
                table: "WorkoutFeedbacks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExerciseTypeId1",
                table: "WorkoutExercises",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Weight",
                table: "Rep",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)");

            migrationBuilder.AddColumn<DateTime>(
                name: "Timestamp",
                table: "Rep",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "WorkoutSetId",
                table: "Rep",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ExerciseType",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "ExerciseType",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CaloriesPerMinute",
                table: "ExerciseType",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryMuscleGroup",
                table: "ExerciseType",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercise_ExerciseTypeId1",
                table: "WorkoutTemplateExercise",
                column: "ExerciseTypeId1");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_StartDateTime",
                table: "WorkoutSessions",
                column: "StartDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutFeedbacks_WorkoutSessionId",
                table: "WorkoutFeedbacks",
                column: "WorkoutSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutFeedbacks_WorkoutSessionId1",
                table: "WorkoutFeedbacks",
                column: "WorkoutSessionId1",
                unique: true,
                filter: "[WorkoutSessionId1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_ExerciseTypeId1",
                table: "WorkoutExercises",
                column: "ExerciseTypeId1");

            migrationBuilder.CreateIndex(
                name: "IX_Rep_WorkoutSetId",
                table: "Rep",
                column: "WorkoutSetId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseFeedbacks_WorkoutSets_WorkoutSetId",
                table: "ExerciseFeedbacks",
                column: "WorkoutSetId",
                principalTable: "WorkoutSets",
                principalColumn: "WorkoutSetId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgressionHistories_WorkoutSessions_WorkoutSessionId",
                table: "ProgressionHistories",
                column: "WorkoutSessionId",
                principalTable: "WorkoutSessions",
                principalColumn: "WorkoutSessionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Rep_WorkoutSets_WorkoutSetId",
                table: "Rep",
                column: "WorkoutSetId",
                principalTable: "WorkoutSets",
                principalColumn: "WorkoutSetId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShareToken_WorkoutSessions_WorkoutSessionId",
                table: "ShareToken",
                column: "WorkoutSessionId",
                principalTable: "WorkoutSessions",
                principalColumn: "WorkoutSessionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutExercises_ExerciseType_ExerciseTypeId",
                table: "WorkoutExercises",
                column: "ExerciseTypeId",
                principalTable: "ExerciseType",
                principalColumn: "ExerciseTypeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutExercises_ExerciseType_ExerciseTypeId1",
                table: "WorkoutExercises",
                column: "ExerciseTypeId1",
                principalTable: "ExerciseType",
                principalColumn: "ExerciseTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutFeedbacks_User_ClientUserId",
                table: "WorkoutFeedbacks",
                column: "ClientUserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutFeedbacks_WorkoutSessions_WorkoutSessionId",
                table: "WorkoutFeedbacks",
                column: "WorkoutSessionId",
                principalTable: "WorkoutSessions",
                principalColumn: "WorkoutSessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutFeedbacks_WorkoutSessions_WorkoutSessionId1",
                table: "WorkoutFeedbacks",
                column: "WorkoutSessionId1",
                principalTable: "WorkoutSessions",
                principalColumn: "WorkoutSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSchedules_WorkoutSessions_LastGeneratedSessionId",
                table: "WorkoutSchedules",
                column: "LastGeneratedSessionId",
                principalTable: "WorkoutSessions",
                principalColumn: "WorkoutSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_User_UserId",
                table: "WorkoutSessions",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_User_UserId1",
                table: "WorkoutSessions",
                column: "UserId1",
                principalTable: "User",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutTemplateExercise_ExerciseType_ExerciseTypeId1",
                table: "WorkoutTemplateExercise",
                column: "ExerciseTypeId1",
                principalTable: "ExerciseType",
                principalColumn: "ExerciseTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseFeedbacks_WorkoutSets_WorkoutSetId",
                table: "ExerciseFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgressionHistories_WorkoutSessions_WorkoutSessionId",
                table: "ProgressionHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Rep_WorkoutSets_WorkoutSetId",
                table: "Rep");

            migrationBuilder.DropForeignKey(
                name: "FK_ShareToken_WorkoutSessions_WorkoutSessionId",
                table: "ShareToken");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutExercises_ExerciseType_ExerciseTypeId",
                table: "WorkoutExercises");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutExercises_ExerciseType_ExerciseTypeId1",
                table: "WorkoutExercises");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutFeedbacks_User_ClientUserId",
                table: "WorkoutFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutFeedbacks_WorkoutSessions_WorkoutSessionId",
                table: "WorkoutFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutFeedbacks_WorkoutSessions_WorkoutSessionId1",
                table: "WorkoutFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSchedules_WorkoutSessions_LastGeneratedSessionId",
                table: "WorkoutSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_User_UserId",
                table: "WorkoutSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_User_UserId1",
                table: "WorkoutSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutTemplateExercise_ExerciseType_ExerciseTypeId1",
                table: "WorkoutTemplateExercise");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutTemplateExercise_ExerciseTypeId1",
                table: "WorkoutTemplateExercise");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_StartDateTime",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutFeedbacks_WorkoutSessionId",
                table: "WorkoutFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutFeedbacks_WorkoutSessionId1",
                table: "WorkoutFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutExercises_ExerciseTypeId1",
                table: "WorkoutExercises");

            migrationBuilder.DropIndex(
                name: "IX_Rep_WorkoutSetId",
                table: "Rep");

            migrationBuilder.DropColumn(
                name: "ExerciseTypeId1",
                table: "WorkoutTemplateExercise");

            migrationBuilder.DropColumn(
                name: "EstimatedOneRM",
                table: "WorkoutSets");

            migrationBuilder.DropColumn(
                name: "IsWarmup",
                table: "WorkoutSets");

            migrationBuilder.DropColumn(
                name: "RestTime",
                table: "WorkoutSets");

            migrationBuilder.DropColumn(
                name: "TargetReps",
                table: "WorkoutSets");

            migrationBuilder.DropColumn(
                name: "TargetWeight",
                table: "WorkoutSets");

            migrationBuilder.DropColumn(
                name: "WorkoutSessionId1",
                table: "WorkoutFeedbacks");

            migrationBuilder.DropColumn(
                name: "ExerciseTypeId1",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "Rep");

            migrationBuilder.DropColumn(
                name: "WorkoutSetId",
                table: "Rep");

            migrationBuilder.DropColumn(
                name: "CaloriesPerMinute",
                table: "ExerciseType");

            migrationBuilder.DropColumn(
                name: "PrimaryMuscleGroup",
                table: "ExerciseType");

            migrationBuilder.RenameColumn(
                name: "UserId1",
                table: "WorkoutSessions",
                newName: "WorkoutFeedbackId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkoutSessions_UserId1",
                table: "WorkoutSessions",
                newName: "IX_WorkoutSessions_WorkoutFeedbackId");

            migrationBuilder.RenameColumn(
                name: "WorkoutSessionId",
                table: "WorkoutFeedbacks",
                newName: "SessionId");

            migrationBuilder.RenameColumn(
                name: "Weight",
                table: "Rep",
                newName: "weight");

            migrationBuilder.RenameColumn(
                name: "Success",
                table: "Rep",
                newName: "success");

            migrationBuilder.RenameColumn(
                name: "RepNumber",
                table: "Rep",
                newName: "repnumber");

            migrationBuilder.RenameColumn(
                name: "WorkoutSessionId",
                table: "ProgressionHistories",
                newName: "SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_ProgressionHistories_WorkoutSessionId",
                table: "ProgressionHistories",
                newName: "IX_ProgressionHistories_SessionId");

            migrationBuilder.RenameColumn(
                name: "WorkoutSetId",
                table: "ExerciseFeedbacks",
                newName: "SetId");

            migrationBuilder.RenameIndex(
                name: "IX_ExerciseFeedbacks_WorkoutSetId",
                table: "ExerciseFeedbacks",
                newName: "IX_ExerciseFeedbacks_SetId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Weight",
                table: "WorkoutSets",
                type: "decimal(10,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RPE",
                table: "WorkoutSets",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "WorkoutSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "ShareToken",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "weight",
                table: "Rep",
                type: "decimal(6,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<int>(
                name: "SetsSetId",
                table: "Rep",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ExerciseType",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "ExerciseType",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateTable(
                name: "Session",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    datetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    endtime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Session", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_Session_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Set",
                columns: table => new
                {
                    SetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    SettypeId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberReps = table.Column<int>(type: "int", nullable: false),
                    SequenceNum = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Set", x => x.SetId);
                    table.ForeignKey(
                        name: "FK_Set_ExerciseType_ExerciseTypeId",
                        column: x => x.ExerciseTypeId,
                        principalTable: "ExerciseType",
                        principalColumn: "ExerciseTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Set_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Set_Settype_SettypeId",
                        column: x => x.SettypeId,
                        principalTable: "Settype",
                        principalColumn: "SettypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_SessionId",
                table: "WorkoutSessions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutFeedbacks_SessionId",
                table: "WorkoutFeedbacks",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareToken_SessionId",
                table: "ShareToken",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Rep_SetsSetId",
                table: "Rep",
                column: "SetsSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Session_UserId",
                table: "Session",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Set_ExerciseTypeId",
                table: "Set",
                column: "ExerciseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Set_SessionId",
                table: "Set",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Set_SettypeId",
                table: "Set",
                column: "SettypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseFeedbacks_Set_SetId",
                table: "ExerciseFeedbacks",
                column: "SetId",
                principalTable: "Set",
                principalColumn: "SetId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgressionHistories_Session_SessionId",
                table: "ProgressionHistories",
                column: "SessionId",
                principalTable: "Session",
                principalColumn: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rep_Set_SetsSetId",
                table: "Rep",
                column: "SetsSetId",
                principalTable: "Set",
                principalColumn: "SetId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShareToken_Session_SessionId",
                table: "ShareToken",
                column: "SessionId",
                principalTable: "Session",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ShareToken_WorkoutSessions_WorkoutSessionId",
                table: "ShareToken",
                column: "WorkoutSessionId",
                principalTable: "WorkoutSessions",
                principalColumn: "WorkoutSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutExercises_ExerciseType_ExerciseTypeId",
                table: "WorkoutExercises",
                column: "ExerciseTypeId",
                principalTable: "ExerciseType",
                principalColumn: "ExerciseTypeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutFeedbacks_Session_SessionId",
                table: "WorkoutFeedbacks",
                column: "SessionId",
                principalTable: "Session",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutFeedbacks_User_ClientUserId",
                table: "WorkoutFeedbacks",
                column: "ClientUserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSchedules_Session_LastGeneratedSessionId",
                table: "WorkoutSchedules",
                column: "LastGeneratedSessionId",
                principalTable: "Session",
                principalColumn: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_Session_SessionId",
                table: "WorkoutSessions",
                column: "SessionId",
                principalTable: "Session",
                principalColumn: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_User_UserId",
                table: "WorkoutSessions",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_WorkoutFeedbacks_WorkoutFeedbackId",
                table: "WorkoutSessions",
                column: "WorkoutFeedbackId",
                principalTable: "WorkoutFeedbacks",
                principalColumn: "WorkoutFeedbackId");
        }
    }
}
