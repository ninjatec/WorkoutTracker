using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace WorkoutTrackerWeb
{
    public class CleanupShadowProperties
    {
        public static async Task Main(string[] args)
        {
            string connectionString = "Server=192.168.0.172;Database=WorkoutTrackerWeb;TrustServerCertificate=True;integrated security=False;User ID=marc.coxall;Password=Donald640060!";
            
            try
            {
                // Read the SQL script
                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shadow_property_cleanup.sql");
                string sqlScript = await File.ReadAllTextAsync("shadow_property_cleanup.sql");
                
                // Split the script by GO statements if present
                string[] sqlCommands = sqlScript.Split(new[] { "GO", "go" }, StringSplitOptions.RemoveEmptyEntries);
                
                // Execute each SQL command
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    Console.WriteLine("Connected to database successfully");
                    
                    foreach (string sqlCommand in sqlCommands)
                    {
                        if (!string.IsNullOrWhiteSpace(sqlCommand))
                        {
                            using (SqlCommand command = new SqlCommand(sqlCommand, connection))
                            {
                                try
                                {
                                    await command.ExecuteNonQueryAsync();
                                    Console.WriteLine("Executed SQL command successfully");
                                }
                                catch (Exception ex)
                                {
                                    // Only print error if it's not about a constraint not existing
                                    if (!ex.Message.Contains("does not exist") && !ex.Message.Contains("not found"))
                                    {
                                        Console.WriteLine($"Error executing SQL command: {ex.Message}");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Note: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                    
                    // Check if ClientActivities table has any shadow properties
                    string checkShadowPropertiesQuery = @"
                        SELECT c.name AS ColumnName
                        FROM sys.columns c
                        JOIN sys.tables t ON c.object_id = t.object_id
                        WHERE t.name = 'ClientActivities' 
                        AND (c.name LIKE '%1' OR c.name LIKE '%Id1' OR c.name LIKE 'AppUserId%')
                        AND c.name NOT IN ('ClientId', 'CoachId');
                    ";
                    
                    using (SqlCommand command = new SqlCommand(checkShadowPropertiesQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            bool hasShadowProperties = false;
                            while (await reader.ReadAsync())
                            {
                                hasShadowProperties = true;
                                string columnName = reader.GetString(0);
                                Console.WriteLine($"Found shadow property column: {columnName}");
                            }
                            
                            if (!hasShadowProperties)
                            {
                                Console.WriteLine("No shadow properties found in ClientActivities table");
                            }
                        }
                    }
                }
                
                Console.WriteLine("Shadow property cleanup completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}