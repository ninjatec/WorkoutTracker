using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class ExtendClientGoalUserCreatedAndCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CoachClientRelationshipId",
                table: "ClientGoals",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Category",
                table: "ClientGoals",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionCriteria",
                table: "ClientGoals",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomCategory",
                table: "ClientGoals",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCoachCreated",
                table: "ClientGoals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVisibleToCoach",
                table: "ClientGoals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastProgressUpdate",
                table: "ClientGoals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeasurementUnit",
                table: "ClientGoals",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingFrequency",
                table: "ClientGoals",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ClientGoals",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletionCriteria",
                table: "ClientGoals");

            migrationBuilder.DropColumn(
                name: "CustomCategory",
                table: "ClientGoals");

            migrationBuilder.DropColumn(
                name: "IsCoachCreated",
                table: "ClientGoals");

            migrationBuilder.DropColumn(
                name: "IsVisibleToCoach",
                table: "ClientGoals");

            migrationBuilder.DropColumn(
                name: "LastProgressUpdate",
                table: "ClientGoals");

            migrationBuilder.DropColumn(
                name: "MeasurementUnit",
                table: "ClientGoals");

            migrationBuilder.DropColumn(
                name: "TrackingFrequency",
                table: "ClientGoals");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ClientGoals");

            migrationBuilder.AlterColumn<int>(
                name: "CoachClientRelationshipId",
                table: "ClientGoals",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "ClientGoals",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
