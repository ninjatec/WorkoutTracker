using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Data.Migrations.WorkoutTrackerWeb
{
    /// <inheritdoc />
    public partial class AddRestTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "RestTime",
                table: "WorkoutTemplateSet",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightCm",
                table: "User",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKg",
                table: "User",
                type: "decimal(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RestTime",
                table: "WorkoutTemplateSet");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "User");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "User");
        }
    }
}
