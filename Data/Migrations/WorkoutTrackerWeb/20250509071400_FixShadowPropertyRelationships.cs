using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Data.Migrations.WorkoutTrackerWeb
{
    /// <inheritdoc />
    public partial class FixShadowPropertyRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing non-unique index
            migrationBuilder.DropIndex(
                name: "IX_WorkoutFeedbacks_WorkoutSessionId",
                table: "WorkoutFeedbacks");

            // Drop the shadow property column if it exists
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql(@"
                    IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'WorkoutSessionId1' AND Object_ID = Object_ID('WorkoutFeedbacks'))
                    BEGIN
                        ALTER TABLE WorkoutFeedbacks DROP COLUMN WorkoutSessionId1
                    END");
            }

            // Create new unique index for one-to-one relationship
            migrationBuilder.CreateIndex(
                name: "IX_WorkoutFeedbacks_WorkoutSessionId",
                table: "WorkoutFeedbacks",
                column: "WorkoutSessionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkoutFeedbacks_WorkoutSessionId",
                table: "WorkoutFeedbacks");

            // Add back the shadow property column
            migrationBuilder.AddColumn<int>(
                name: "WorkoutSessionId1",
                table: "WorkoutFeedbacks",
                type: "int",
                nullable: true);

            // Recreate the non-unique index
            migrationBuilder.CreateIndex(
                name: "IX_WorkoutFeedbacks_WorkoutSessionId",
                table: "WorkoutFeedbacks",
                column: "WorkoutSessionId");
        }
    }
}
