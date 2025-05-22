using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Data.Migrations.WorkoutTrackerWeb
{
    /// <inheritdoc />
    public partial class RemoveExerciseTypeId1WithoutFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First check if the column exists before trying to drop it
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 
                    FROM sys.columns 
                    WHERE Name = N'ExerciseTypeId1'
                    AND Object_ID = Object_ID(N'[dbo].[WorkoutExercises]'))
                BEGIN
                    ALTER TABLE [WorkoutExercises] DROP COLUMN [ExerciseTypeId1]
                END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add the column back if we need to roll back
            migrationBuilder.AddColumn<int>(
                name: "ExerciseTypeId1",
                table: "WorkoutExercises",
                type: "int",
                nullable: true);
        }
    }
}
