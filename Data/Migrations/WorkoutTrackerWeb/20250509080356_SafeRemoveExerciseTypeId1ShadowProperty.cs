using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Data.Migrations.WorkoutTrackerWeb
{
    /// <inheritdoc />
    public partial class SafeRemoveExerciseTypeId1ShadowProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if FK exists before trying to drop it
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * 
                    FROM sys.foreign_keys 
                    WHERE name = 'FK_WorkoutExercises_ExerciseType_ExerciseTypeId1'
                )
                BEGIN
                    ALTER TABLE [WorkoutExercises] DROP CONSTRAINT [FK_WorkoutExercises_ExerciseType_ExerciseTypeId1]
                END");

            // Check if index exists before trying to drop it
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * 
                    FROM sys.indexes 
                    WHERE name = 'IX_WorkoutExercises_ExerciseTypeId1'
                )
                BEGIN
                    DROP INDEX [IX_WorkoutExercises_ExerciseTypeId1] ON [WorkoutExercises]
                END");

            // Drop the column if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 
                    FROM sys.columns 
                    WHERE Name = N'ExerciseTypeId1'
                    AND Object_ID = Object_ID(N'[dbo].[WorkoutExercises]')
                )
                BEGIN
                    ALTER TABLE [WorkoutExercises] DROP COLUMN [ExerciseTypeId1]
                END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExerciseTypeId1",
                table: "WorkoutExercises",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_ExerciseTypeId1",
                table: "WorkoutExercises",
                column: "ExerciseTypeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutExercises_ExerciseType_ExerciseTypeId1",
                table: "WorkoutExercises",
                column: "ExerciseTypeId1",
                principalTable: "ExerciseType",
                principalColumn: "ExerciseTypeId");
        }
    }
}
