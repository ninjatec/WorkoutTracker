using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutTemplateModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkoutTemplate",
                columns: table => new
                {
                    WorkoutTemplateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutTemplate", x => x.WorkoutTemplateId);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplate_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutTemplateExercise",
                columns: table => new
                {
                    WorkoutTemplateExerciseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkoutTemplateId = table.Column<int>(type: "int", nullable: false),
                    ExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    SequenceNum = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutTemplateExercise", x => x.WorkoutTemplateExerciseId);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateExercise_ExerciseType_ExerciseTypeId",
                        column: x => x.ExerciseTypeId,
                        principalTable: "ExerciseType",
                        principalColumn: "ExerciseTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateExercise_WorkoutTemplate_WorkoutTemplateId",
                        column: x => x.WorkoutTemplateId,
                        principalTable: "WorkoutTemplate",
                        principalColumn: "WorkoutTemplateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutTemplateSet",
                columns: table => new
                {
                    WorkoutTemplateSetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkoutTemplateExerciseId = table.Column<int>(type: "int", nullable: false),
                    SettypeId = table.Column<int>(type: "int", nullable: false),
                    DefaultReps = table.Column<int>(type: "int", nullable: false),
                    DefaultWeight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SequenceNum = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutTemplateSet", x => x.WorkoutTemplateSetId);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateSet_Settype_SettypeId",
                        column: x => x.SettypeId,
                        principalTable: "Settype",
                        principalColumn: "SettypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateSet_WorkoutTemplateExercise_WorkoutTemplateExerciseId",
                        column: x => x.WorkoutTemplateExerciseId,
                        principalTable: "WorkoutTemplateExercise",
                        principalColumn: "WorkoutTemplateExerciseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplate_Category",
                table: "WorkoutTemplate",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplate_UserId",
                table: "WorkoutTemplate",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercise_ExerciseTypeId",
                table: "WorkoutTemplateExercise",
                column: "ExerciseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercise_SequenceNum",
                table: "WorkoutTemplateExercise",
                column: "SequenceNum");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercise_WorkoutTemplateId",
                table: "WorkoutTemplateExercise",
                column: "WorkoutTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateSet_SequenceNum",
                table: "WorkoutTemplateSet",
                column: "SequenceNum");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateSet_SettypeId",
                table: "WorkoutTemplateSet",
                column: "SettypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateSet_WorkoutTemplateExerciseId",
                table: "WorkoutTemplateSet",
                column: "WorkoutTemplateExerciseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkoutTemplateSet");

            migrationBuilder.DropTable(
                name: "WorkoutTemplateExercise");

            migrationBuilder.DropTable(
                name: "WorkoutTemplate");
        }
    }
}
