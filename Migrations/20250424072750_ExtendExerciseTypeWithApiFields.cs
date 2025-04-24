using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class ExtendExerciseTypeWithApiFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "ExerciseType",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Equipment",
                table: "ExerciseType",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "ExerciseType",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFromApi",
                table: "ExerciseType",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "ExerciseType",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Muscle",
                table: "ExerciseType",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ExerciseType",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "ExerciseType");

            migrationBuilder.DropColumn(
                name: "Equipment",
                table: "ExerciseType");

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "ExerciseType");

            migrationBuilder.DropColumn(
                name: "IsFromApi",
                table: "ExerciseType");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "ExerciseType");

            migrationBuilder.DropColumn(
                name: "Muscle",
                table: "ExerciseType");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ExerciseType");
        }
    }
}
