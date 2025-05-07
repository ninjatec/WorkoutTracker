using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutMetricsTableOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only add the WorkoutMetrics table without touching any existing relationships
            migrationBuilder.CreateTable(
                name: "WorkoutMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MetricType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AdditionalData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutMetrics_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for the WorkoutMetrics table
            migrationBuilder.CreateIndex(
                name: "IX_WorkoutMetrics_Date",
                table: "WorkoutMetrics",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutMetrics_MetricType",
                table: "WorkoutMetrics",
                column: "MetricType");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutMetrics_UserId",
                table: "WorkoutMetrics",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutMetrics_UserId_Date_MetricType",
                table: "WorkoutMetrics",
                columns: new[] { "UserId", "Date", "MetricType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only remove the WorkoutMetrics table
            migrationBuilder.DropTable(
                name: "WorkoutMetrics");
        }
    }
}
