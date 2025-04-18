using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackEnhancedManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedToAdminId",
                table: "Feedback",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrowserInfo",
                table: "Feedback",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Feedback",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceInfo",
                table: "Feedback",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedCompletionDate",
                table: "Feedback",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Feedback",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "Feedback",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Feedback",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicResponse",
                table: "Feedback",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedToAdminId",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "BrowserInfo",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "DeviceInfo",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "EstimatedCompletionDate",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "PublicResponse",
                table: "Feedback");
        }
    }
}
