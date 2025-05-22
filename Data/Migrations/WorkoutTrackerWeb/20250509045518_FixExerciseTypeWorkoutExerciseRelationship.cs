using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Data.Migrations.WorkoutTrackerWeb
{
    /// <inheritdoc />
    public partial class FixExerciseTypeWorkoutExerciseRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the shadow property index if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkoutExercises_ExerciseTypeId1')
                BEGIN
                    DROP INDEX [IX_WorkoutExercises_ExerciseTypeId1] ON [WorkoutExercises]
                END");

            // Drop the shadow property column if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'ExerciseTypeId1' AND Object_ID = Object_ID(N'WorkoutExercises'))
                BEGIN
                    ALTER TABLE [WorkoutExercises] DROP COLUMN [ExerciseTypeId1]
                END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back the shadow property if needed
            migrationBuilder.AddColumn<int>(
                name: "ExerciseTypeId1",
                table: "WorkoutExercises",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_ExerciseTypeId1",
                table: "WorkoutExercises",
                column: "ExerciseTypeId1");
        }
    }
}
