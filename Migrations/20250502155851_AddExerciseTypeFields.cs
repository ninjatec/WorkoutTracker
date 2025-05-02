using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseTypeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if constraint exists before attempting to drop it
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_WorkoutSessions_User_UserId1')
                BEGIN
                    ALTER TABLE WorkoutSessions DROP CONSTRAINT FK_WorkoutSessions_User_UserId1;
                END
            ");

            // Check if index exists before attempting to drop it
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkoutSessions_UserId1')
                BEGIN
                    DROP INDEX IX_WorkoutSessions_UserId1 ON WorkoutSessions;
                END
            ");

            // Check if column exists before attempting to drop it
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE name = 'UserId1' AND object_id = OBJECT_ID('WorkoutSessions'))
                BEGIN
                    ALTER TABLE WorkoutSessions DROP COLUMN UserId1;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-add column only if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'UserId1' AND object_id = OBJECT_ID('WorkoutSessions'))
                BEGIN
                    ALTER TABLE WorkoutSessions ADD UserId1 int NULL;
                    
                    CREATE INDEX IX_WorkoutSessions_UserId1 ON WorkoutSessions(UserId1);
                    
                    ALTER TABLE WorkoutSessions 
                    ADD CONSTRAINT FK_WorkoutSessions_User_UserId1 
                    FOREIGN KEY (UserId1) REFERENCES [User](UserId);
                END
            ");
        }
    }
}
