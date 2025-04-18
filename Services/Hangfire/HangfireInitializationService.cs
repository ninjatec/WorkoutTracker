using System;
using System.Data.SqlClient;
using System.IO;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Services.Hangfire
{
    public class HangfireInitializationService : IHangfireInitializationService
    {
        private readonly string _connectionString;
        private readonly string _scriptPath;
        private readonly ILogger<HangfireInitializationService> _logger;
        private readonly IHostEnvironment _environment;

        public HangfireInitializationService(
            IConfiguration configuration,
            ILogger<HangfireInitializationService> logger,
            IHostEnvironment environment)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _environment = environment;
            _scriptPath = Path.Combine(_environment.ContentRootPath, "hangfire_schema.sql");
            _logger = logger;
        }

        public bool InitializeHangfireSchema()
        {
            _logger.LogInformation("Attempting to initialize Hangfire schema");

            // First check if we can connect to the database at all
            if (!CanConnectToDatabase())
            {
                _logger.LogError("Cannot connect to database. Check connection string and SQL Server availability.");
                return false;
            }

            // Try multiple initialization approaches
            return InitializeUsingStorageApi() || InitializeUsingSqlScript();
        }
        
        private bool CanConnectToDatabase()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    _logger.LogInformation("Testing database connection");
                    connection.Open();
                    
                    // Execute a simple query to verify connection works
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT 1";
                        var result = command.ExecuteScalar();
                        
                        _logger.LogInformation("Database connection successful");
                        return true;
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error when connecting to database: Error {ErrorCode}", sqlEx.Number);
                
                // Log specific SQL server errors
                switch (sqlEx.Number)
                {
                    case 4060:
                        _logger.LogError("SQL Error 4060: Cannot open database. Database might not exist.");
                        break;
                    case 18456:
                        _logger.LogError("SQL Error 18456: Login failed. Check credentials.");
                        break;
                    case 40:
                        _logger.LogError("SQL Error 40: Could not open connection to the server.");
                        break;
                    case 53:
                        _logger.LogError("SQL Error 53: Server not found or not accessible.");
                        break;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to database");
                return false;
            }
        }

        private bool InitializeUsingStorageApi()
        {
            try
            {
                _logger.LogInformation("Initializing Hangfire schema using Storage API");
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    // First try to connect
                    connection.Open();
                    _logger.LogInformation("Successfully connected to database for Hangfire schema initialization");
                    
                    // Create storage with schema creation enabled
                    var storage = new SqlServerStorage(_connectionString, new SqlServerStorageOptions
                    {
                        PrepareSchemaIfNecessary = true,
                        SchemaName = "HangFire",
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.FromSeconds(15),
                        UseRecommendedIsolationLevel = true, 
                        DisableGlobalLocks = false, // Enable global locks for distributed environment
                    });
                    
                    // This actively triggers schema creation
                    var api = storage.GetMonitoringApi();
                    var stats = api.GetStatistics();
                    _logger.LogInformation("Hangfire schema initialization verified with stats: Servers={Servers}, Succeeded={Succeeded}, Failed={Failed}", 
                        stats.Servers, stats.Succeeded, stats.Failed);
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Hangfire schema using Storage API");
                return false;
            }
        }

        private bool InitializeUsingSqlScript()
        {
            try
            {
                _logger.LogInformation("Attempting to initialize Hangfire schema using SQL script at {ScriptPath}", _scriptPath);
                
                if (!File.Exists(_scriptPath))
                {
                    _logger.LogWarning("Hangfire schema SQL script not found at: {ScriptPath}", _scriptPath);
                    return false;
                }
                
                string script = File.ReadAllText(_scriptPath);
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    // Create schema if it doesn't exist
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "IF NOT EXISTS (SELECT schema_name FROM information_schema.schemata WHERE schema_name = 'HangFire') BEGIN EXEC ('CREATE SCHEMA [HangFire]') END";
                        command.ExecuteNonQuery();
                    }
                    
                    // Properly split the script on GO statements to execute each batch separately
                    // GO must be on a line by itself
                    string[] commandTexts = script.Split(new[] { "\r\nGO\r\n", "\nGO\n", "\r\nGO", "\nGO" }, 
                        StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (string commandText in commandTexts)
                    {
                        if (string.IsNullOrWhiteSpace(commandText))
                            continue;
                            
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = commandText;
                            command.CommandTimeout = 120; // Increase timeout to 2 minutes
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (SqlException ex)
                            {
                                // If the error is that an object already exists, we can continue
                                if (ex.Message.Contains("There is already an object named"))
                                {
                                    _logger.LogWarning("Object already exists: {Message}", ex.Message);
                                    continue;
                                }
                                throw; // Rethrow if it's not an "object already exists" error
                            }
                        }
                    }
                    
                    // Verify that Schema table exists
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "IF NOT EXISTS(SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[HangFire].[Schema]') AND type = N'U') " +
                                             "BEGIN " +
                                             "  CREATE TABLE [HangFire].[Schema]([Version] [int] NOT NULL, CONSTRAINT [PK_HangFire_Schema] PRIMARY KEY CLUSTERED ([Version] ASC)); " +
                                             "  INSERT INTO [HangFire].[Schema] ([Version]) VALUES (7); " +
                                             "END";
                        command.ExecuteNonQuery();
                    }
                    
                    _logger.LogInformation("Executed Hangfire schema SQL script successfully");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Hangfire schema using SQL script");
                return false;
            }
        }

        public bool VerifyHangfireSchema()
        {
            try
            {
                _logger.LogInformation("Verifying Hangfire schema");
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    // Check for Schema table
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'HangFire' AND TABLE_NAME = 'Schema'";
                        var schemaTableExists = Convert.ToInt32(command.ExecuteScalar()) > 0;
                        
                        if (!schemaTableExists)
                        {
                            _logger.LogWarning("Hangfire Schema table not found");
                            return false;
                        }
                    }
                    
                    // Check for critical tables
                    string[] requiredTables = new[]
                    {
                        "Job", "JobParameter", "JobQueue", "State", "Hash", "List", "Set", "Counter", "Server"
                    };
                    
                    bool allTablesExist = true;
                    foreach (var table in requiredTables)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'HangFire' AND TABLE_NAME = '{table}'";
                            bool tableExists = Convert.ToInt32(command.ExecuteScalar()) > 0;
                            
                            if (!tableExists)
                            {
                                _logger.LogWarning("Hangfire {TableName} table not found", table);
                                allTablesExist = false;
                            }
                        }
                    }
                    
                    if (allTablesExist)
                    {
                        _logger.LogInformation("Hangfire schema verification: All required tables exist");
                    }
                    else
                    {
                        _logger.LogWarning("Hangfire schema verification: Some required tables are missing");
                        // If tables are missing, try to force schema creation
                        InitializeHangfireSchema();
                        return false;
                    }
                    
                    // Check for proper schema version
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT TOP 1 [Version] FROM [HangFire].[Schema] ORDER BY [Version] DESC";
                        var version = Convert.ToInt32(command.ExecuteScalar());
                        
                        _logger.LogInformation("Hangfire schema version: {SchemaVersion}", version);
                        return version >= 7; // Hangfire 1.8+ uses schema version 7
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Hangfire schema");
                return false;
            }
        }

        public string GetDiagnosticInfo()
        {
            try
            {
                var diagnostics = new System.Text.StringBuilder();
                diagnostics.AppendLine("## Hangfire Diagnostic Information ##");
                
                // Check database connectivity
                bool canConnect = false;
                using (var connection = new SqlConnection(_connectionString))
                {
                    try
                    {
                        connection.Open();
                        canConnect = true;
                        diagnostics.AppendLine("✅ Database connection: SUCCESS");
                        
                        // Get database name
                        diagnostics.AppendLine($"Database: {connection.Database}");
                        
                        // Check if Hangfire schema exists
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = 'HangFire'";
                            bool schemaExists = Convert.ToInt32(command.ExecuteScalar()) > 0;
                            diagnostics.AppendLine($"HangFire schema exists: {(schemaExists ? "YES ✅" : "NO ❌")}");
                        }
                        
                        // Check if tables exist
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'HangFire'";
                            using (var reader = command.ExecuteReader())
                            {
                                diagnostics.AppendLine("HangFire tables:");
                                bool hasTables = false;
                                
                                while (reader.Read())
                                {
                                    hasTables = true;
                                    diagnostics.AppendLine($"  - {reader.GetString(0)}");
                                }
                                
                                if (!hasTables)
                                {
                                    diagnostics.AppendLine("  No tables found in HangFire schema ❌");
                                }
                            }
                        }
                        
                        // Check job count
                        try
                        {
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = "IF OBJECT_ID('HangFire.Job', 'U') IS NOT NULL " +
                                                      "SELECT COUNT(*) FROM HangFire.Job ELSE SELECT -1";
                                int jobCount = Convert.ToInt32(command.ExecuteScalar());
                                if (jobCount >= 0)
                                {
                                    diagnostics.AppendLine($"Job count: {jobCount}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            diagnostics.AppendLine($"Error checking job count: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        diagnostics.AppendLine($"❌ Database connection: FAILED - {ex.Message}");
                    }
                }
                
                // Check Hangfire SQL script
                bool scriptExists = File.Exists(_scriptPath);
                diagnostics.AppendLine($"Hangfire schema script exists: {(scriptExists ? "YES ✅" : "NO ❌")}");
                if (scriptExists)
                {
                    diagnostics.AppendLine($"Script path: {_scriptPath}");
                    diagnostics.AppendLine($"Script size: {new FileInfo(_scriptPath).Length} bytes");
                }
                
                // Environment info
                diagnostics.AppendLine($"Environment: {_environment.EnvironmentName}");
                
                return diagnostics.ToString();
            }
            catch (Exception ex)
            {
                return $"Error collecting diagnostic information: {ex.Message}";
            }
        }

        // Try to repair any issues with the Hangfire schema
        public async Task<bool> RepairHangfireSchemaAsync()
        {
            _logger.LogInformation("Attempting to repair Hangfire schema");
            
            try
            {
                // First check if we can connect at all
                if (!CanConnectToDatabase())
                {
                    _logger.LogError("Cannot repair schema - unable to connect to database");
                    return false;
                }
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // 1. Make sure the HangFire schema exists
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'HangFire') " +
                                             "BEGIN " +
                                             "    EXEC('CREATE SCHEMA [HangFire]'); " +
                                             "    PRINT 'HangFire schema created.'; " +
                                             "END";
                        await command.ExecuteNonQueryAsync();
                    }
                    
                    // 2. Create the Schema version table if missing
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "IF NOT EXISTS(SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[HangFire].[Schema]') AND type = N'U') " +
                                             "BEGIN " +
                                             "  CREATE TABLE [HangFire].[Schema]([Version] [int] NOT NULL, CONSTRAINT [PK_HangFire_Schema] PRIMARY KEY CLUSTERED ([Version] ASC)); " +
                                             "  INSERT INTO [HangFire].[Schema] ([Version]) VALUES (7); " +
                                             "END";
                        await command.ExecuteNonQueryAsync();
                    }
                    
                    // 3. Check and update schema version if needed
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "IF EXISTS(SELECT * FROM [HangFire].[Schema]) " +
                                             "AND NOT EXISTS(SELECT * FROM [HangFire].[Schema] WHERE [Version] >= 7) " +
                                             "BEGIN " +
                                             "  UPDATE [HangFire].[Schema] SET [Version] = 7 " +
                                             "END";
                        await command.ExecuteNonQueryAsync();
                    }
                    
                    // 4. For a complete repair, use the SQL script approach
                    if (File.Exists(_scriptPath))
                    {
                        _logger.LogInformation("Using SQL script for comprehensive schema repair");
                        return InitializeUsingSqlScript();
                    }
                    else
                    {
                        _logger.LogWarning("SQL script not found, attempting a fresh initialization");
                        return InitializeUsingStorageApi();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error repairing Hangfire schema");
                return false;
            }
        }
    }

    public interface IHangfireInitializationService
    {
        bool InitializeHangfireSchema();
        bool VerifyHangfireSchema();
        string GetDiagnosticInfo();
        Task<bool> RepairHangfireSchemaAsync();
    }
}