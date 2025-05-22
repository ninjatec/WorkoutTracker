using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLogLevelSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogLevelSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DefaultLogLevel = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogLevelSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogLevelOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceContext = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LogLevel = table.Column<int>(type: "int", nullable: false),
                    LogLevelSettingsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogLevelOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogLevelOverrides_LogLevelSettings_LogLevelSettingsId",
                        column: x => x.LogLevelSettingsId,
                        principalTable: "LogLevelSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogLevelOverrides_LogLevelSettingsId",
                table: "LogLevelOverrides",
                column: "LogLevelSettingsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogLevelOverrides");

            migrationBuilder.DropTable(
                name: "LogLevelSettings");
        }
    }
}
