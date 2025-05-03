using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;

namespace WorkoutTrackerWeb.Services
{
    public class DatabaseMigrationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseMigrationService> _logger;
        private readonly IConfiguration _configuration;

        public DatabaseMigrationService(
            IServiceProvider serviceProvider,
            ILogger<DatabaseMigrationService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task MigrateAsync()
        {
            _logger.LogInformation("Running database migrations");
            
            try
            {
                // Apply EF Core migrations
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
                    await dbContext.Database.MigrateAsync();
                }
                
                // Apply custom migrations (like adding the CaloriesBurned column)
                await ApplyCaloriesBurnedMigrationAsync();
                
                _logger.LogInformation("Database migrations completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during database migration");
                throw;
            }
        }
        
        private async Task ApplyCaloriesBurnedMigrationAsync()
        {
            try
            {
                _logger.LogInformation("Checking if CaloriesBurned column needs to be added");
                
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Check if the column exists
                    bool columnExists = false;
                    string checkSql = @"
                        SELECT COUNT(1) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'WorkoutSession' 
                        AND COLUMN_NAME = 'CaloriesBurned'";
                    
                    using (var command = new SqlCommand(checkSql, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        columnExists = Convert.ToInt32(result) > 0;
                    }
                    
                    if (!columnExists)
                    {
                        _logger.LogInformation("Adding CaloriesBurned column to WorkoutSession table");
                        
                        string migrationSql = @"
                            ALTER TABLE WorkoutSession
                            ADD CaloriesBurned DECIMAL(18, 2) NULL;";
                        
                        using (var command = new SqlCommand(migrationSql, connection))
                        {
                            await command.ExecuteNonQueryAsync();
                        }
                        
                        _logger.LogInformation("CaloriesBurned column added successfully");
                    }
                    else
                    {
                        _logger.LogInformation("CaloriesBurned column already exists");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying CaloriesBurned column migration");
                throw;
            }
        }
    }
}