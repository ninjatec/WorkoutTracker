using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class SessionMigrationPhase5Complete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create an empty migration that only updates the EF model
            // This is because we've already applied the database changes manually via SQL script
            migrationBuilder.Sql("SELECT 1"); // Dummy command that does nothing
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No down migration needed as we manually updated the database
            migrationBuilder.Sql("SELECT 1"); // Dummy command that does nothing
        }
    }
}
