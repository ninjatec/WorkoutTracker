using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class FixRecurringWorkoutSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update all workout schedules with a recurrence pattern that isn't "Once" or null to have IsRecurring = true
            migrationBuilder.Sql(@"
                UPDATE WorkoutSchedules 
                SET IsRecurring = 1 
                WHERE RecurrencePattern IS NOT NULL 
                AND RecurrencePattern != 'Once'
                AND IsRecurring = 0
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No down migration needed since we're fixing incorrect data
        }
    }
}
