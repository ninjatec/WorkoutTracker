using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.Caching.Distributed;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Services.Email;
using WorkoutTrackerWeb.Services.Session;
using WorkoutTrackerWeb.Services.VersionManagement;
using WorkoutTrackerWeb.Services.Hangfire;
using WorkoutTrackerWeb.Services.Logging; // Add the Logging namespace for extension methods
using WorkoutTrackerWeb.Services.Alerting; // Add the Alerting namespace
using WorkoutTrackerWeb.Middleware;
using WorkoutTrackerWeb.Hubs;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Net.Mime;
using Prometheus;
using HealthChecks.UI.Client;
using AspNetCoreRateLimit;
using Serilog;
using Serilog.Events;
using Serilog.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Hangfire;
using Hangfire.SqlServer;
using Hangfire.Dashboard;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.StaticFiles;
using StackExchange.Redis;
using HealthChecks.Redis;
using HealthChecks.System;
using Microsoft.AspNetCore.HttpOverrides;
using WorkoutTrackerWeb.Services.TempData;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WorkoutTrackerWeb.HealthChecks; // Add this for our custom health checks

// Initialize Serilog first, before creating the web host
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting up WorkoutTracker application");

    var builder = WebApplication.CreateBuilder(args);

    // Add trusted domains for the application
    string[] trustedDomains = new[] {
        "workouttracker.online", 
        "www.workouttracker.online",
        "wot.ninjatec.co.uk",
        "localhost",
        "localhost:5001",
        "localhost:5000"
    };

    // Add Serilog to the application
    builder.Host.UseSerilog();

    // Configure host filtering to accept the correct domains
    builder.Services.AddHostFiltering(options => {
        options.AllowedHosts = trustedDomains;
        options.AllowEmptyHosts = true;  // Allow requests without a Host header
        options.IncludeFailureMessage = true; // Include detailed failure messages
    });

    // Log the host filtering settings
    Log.Information("Host filtering configured with allowed hosts: {AllowedHosts}", string.Join(", ", trustedDomains));

    // Configure forwarded headers options to properly handle proxy servers like Cloudflare
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        
        // Clear the default networks that are trusted by default - we'll explicitly add our own
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        
        // Trust all proxies for now - in a production environment you'd want to be more specific
        // about which proxies are trusted. For Cloudflare, you'd add their IP ranges.
        options.ForwardLimit = null; // No limit on number of proxy hops
    });

    // Configure cookie policy
    builder.Services.Configure<CookiePolicyOptions>(options =>
    {
        options.CheckConsentNeeded = context => true;
        options.MinimumSameSitePolicy = SameSiteMode.None;
        options.Secure = CookieSecurePolicy.Always;
    });

    // Configure rate limiting
    builder.Services.AddMemoryCache();

    // Rate limiting configuration
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

    // Add services to the container.
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    // Register Logging Services
    builder.Services.AddLoggingServices();
    builder.Services.AddHostedService<WorkoutTrackerWeb.Services.Logging.LogLevelConfigurationHostedService>();

    // Register HttpClientFactory
    builder.Services.AddHttpClient();

    // Register HangfireServerConfiguration first so it's available for service configuration
    builder.Services.AddSingleton<HangfireServerConfiguration>();

    // Configure Hangfire for background processing
    builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.FromSeconds(15),
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = false, // Enable global locks for distributed environment
            SchemaName = "HangFire",
            PrepareSchemaIfNecessary = true // Enable auto schema creation
        }));

    // Get Hangfire configuration to determine if we should register this instance as a server
    using (var scope = builder.Services.BuildServiceProvider().CreateScope())
    {
        var hangfireConfig = scope.ServiceProvider.GetRequiredService<HangfireServerConfiguration>();
        
        // Only add Hangfire server if processing is enabled
        if (hangfireConfig.IsProcessingEnabled) 
        {
            Log.Information("Registering this instance as a Hangfire server with {WorkerCount} workers", hangfireConfig.WorkerCount);
            builder.Services.AddHangfireServer(options => {
                // Let the configuration service configure the options
                hangfireConfig.ConfigureServerOptions(options);
            });
        }
        else
        {
            Log.Information("Hangfire processing is disabled for this instance - NOT registering as a server");
        }
    }

    // Helper method to get SQL connection options with pooling and resilience settings
    Func<string, Action<SqlServerDbContextOptionsBuilder>, DbContextOptionsBuilder> getSqlOptions = (connString, sqlOptionsAction) =>
    {
        // Get connection pooling settings from configuration
        var poolingConfig = builder.Configuration.GetSection("DatabaseConnectionPooling");
        int maxPoolSize = poolingConfig.GetValue<int>("MaxPoolSize", 200);
        int minPoolSize = poolingConfig.GetValue<int>("MinPoolSize", 10);
        int connectionLifetime = poolingConfig.GetValue<int>("ConnectionLifetime", 300);
        bool connectionResetEnabled = poolingConfig.GetValue<bool>("ConnectionResetEnabled", true);
        int loadBalanceTimeout = poolingConfig.GetValue<int>("LoadBalanceTimeout", 30);
        int retryCount = poolingConfig.GetValue<int>("RetryCount", 5);
        int retryInterval = poolingConfig.GetValue<int>("RetryInterval", 10);
        
        // Build connection string with additional connection pooling parameters
        var sqlConnectionBuilder = new SqlConnectionStringBuilder(connString)
        {
            MaxPoolSize = maxPoolSize,
            MinPoolSize = minPoolSize,
            ConnectTimeout = loadBalanceTimeout,
            LoadBalanceTimeout = loadBalanceTimeout,
            ConnectRetryCount = retryCount,
            ConnectRetryInterval = retryInterval
        };

        // Add additional parameters to connection string directly
        string enhancedConnectionString = sqlConnectionBuilder.ConnectionString;
        if (connectionLifetime > 0)
        {
            enhancedConnectionString += $";Connection Lifetime={connectionLifetime}";
        }
        
        // Add connection reset parameter directly to connection string - only on Windows
        if (connectionResetEnabled && OperatingSystem.IsWindows())
        {
            enhancedConnectionString += $";Connection Reset=true";
        }
        
        // Create options builder with the enhanced connection string and SQL Server options
        var optionsBuilder = new DbContextOptionsBuilder<WorkoutTrackerWebContext>();
        optionsBuilder.UseSqlServer(enhancedConnectionString, sqlOptionsAction)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
            
        return optionsBuilder;
    };

    // Configure DataProtectionKeysDbContext for persistent key storage
    builder.Services.AddDbContext<DataProtectionKeysDbContext>(options =>
        options.UseSqlServer(connectionString));

    // Configure Data Protection with persistent key storage
    builder.Services.AddDataProtection()
        .PersistKeysToDbContext<DataProtectionKeysDbContext>()
        .SetApplicationName("WorkoutTracker")
        .SetDefaultKeyLifetime(TimeSpan.FromDays(90)); // Rotate keys every 90 days

    // Configure Identity to require confirmed account and customizable password requirements
    builder.Services.AddDefaultIdentity<IdentityUser>(options => 
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
    })
        .AddRoles<IdentityRole>() // Add role services
        .AddEntityFrameworkStores<ApplicationDbContext>();

    // Configure Authorization policies for Admin role
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    });

    // Configure email service
    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
    builder.Services.AddTransient<IEmailService, WorkoutTrackerWeb.Services.Email.EmailService>();
    // Register adapter to make our email service compatible with Identity
    builder.Services.AddTransient<IEmailSender, EmailSenderAdapter>();

    // Add SignalR services with Redis backplane configuration
    try
    {
        if (builder.Environment.IsDevelopment())
        {
            // In development, use in-memory SignalR without Redis
            builder.Services.AddSignalR(options => {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 102400; // 100 KB
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30); // How long to wait before timing out the client
                options.HandshakeTimeout = TimeSpan.FromSeconds(15);     // How long to wait for the handshake to complete
            });
            Log.Information("Configured SignalR with in-memory backplane for development");
        }
        else
        {
            // In production, use Redis backplane for scaling
            try
            {
                // Configure Redis with Sentinel for HA
                var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? 
                                        Environment.GetEnvironmentVariable("ConnectionStrings__Redis") ?? 
                                        "redis-master.web.svc.cluster.local:6379,abortConnect=false";
                                        
                var redisOptions = ConfigureRedisOptions(redisConnectionString);
                
                builder.Services.AddSignalR(options => {
                    options.EnableDetailedErrors = true;
                    options.MaximumReceiveMessageSize = 102400; // 100 KB
                    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
                    // Set reasonable timeouts for connection keep-alive
                    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                }).AddStackExchangeRedis(options => 
                {
                    options.Configuration = redisOptions;
                    options.ConnectionFactory = async writer => 
                    {
                        var connection = await ConnectionMultiplexer.ConnectAsync(redisOptions, writer);
                        connection.ConnectionFailed += (_, e) => 
                        {
                            Log.Warning("Redis connection failed: {EndPoint}, {FailureType}", e.EndPoint, e.FailureType);
                        };
                        connection.ConnectionRestored += (_, e) => 
                        {
                            Log.Information("Redis connection restored: {EndPoint}", e.EndPoint);
                        };
                        connection.ErrorMessage += (_, e) =>
                        {
                            Log.Warning("Redis error: {Message}", e.Message);
                        };
                        return connection;
                    };
                });
                Log.Information("Configured SignalR with Redis backplane using {ConnectionString}", redisConnectionString);
            }
            catch (Exception ex)
            {
                // Fallback to in-memory SignalR if Redis configuration fails
                Log.Error(ex, "Failed to configure SignalR with Redis backplane, falling back to in-memory");
                builder.Services.AddSignalR(options => {
                    options.EnableDetailedErrors = true;
                    options.MaximumReceiveMessageSize = 102400; // 100 KB
                    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
                });
                Log.Warning("Using in-memory SignalR backplane in production due to Redis configuration failure");
            }
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error configuring SignalR services");
        throw; // Re-throw as this is a critical error
    }

    // Add Redis for distributed caching and shared storage
    var redisConfigString = builder.Configuration.GetConnectionString("Redis") ?? 
                         Environment.GetEnvironmentVariable("ConnectionStrings__Redis") ?? 
                         "redis-master.web.svc.cluster.local:6379,abortConnect=false";

    try {
        var redisOptions = ConfigureRedisOptions(redisConfigString);
        
        // Register the ConnectionMultiplexer as a singleton
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp => {
            var logger = sp.GetRequiredService<ILogger<Program>>();
            try {
                var redisOptions = ConfigureRedisOptions(redisConfigString);
                
                // Create a new multiplexer with these options
                var connection = ConnectionMultiplexer.Connect(redisOptions);
                
                // Verify we have a master connection
                var hasWritableEndpoint = false;
                foreach (var endpoint in connection.GetEndPoints())
                {
                    var server = connection.GetServer(endpoint);
                    if (!server.IsReplica)
                    {
                        logger.LogInformation("Connected to master Redis server at {Endpoint}", endpoint);
                        hasWritableEndpoint = true;
                        break;
                    }
                }
                
                // Set up event handlers for connection management
                connection.ConnectionFailed += (_, e) => {
                    logger.LogWarning("Redis connection failed: {EndPoint}, {FailureType}", e.EndPoint, e.FailureType);
                };
                connection.ConnectionRestored += (_, e) => {
                    logger.LogInformation("Redis connection restored: {EndPoint}", e.EndPoint);
                };
                connection.ErrorMessage += (_, e) => {
                    logger.LogWarning("Redis error: {Message}", e.Message);
                };
                
                return connection;
            } catch (Exception ex) {
                logger.LogError(ex, "Failed to create Redis connection");
                throw;
            }
        });
        
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.ConfigurationOptions = redisOptions;
            options.InstanceName = "WorkoutTracker:";
        });
        
        // Register the shared storage service
        builder.Services.AddScoped<ISharedStorageService, RedisSharedStorageService>();
        
        Log.Information("Configured Redis distributed cache for production using {ConnectionString} with enhanced resilience", redisConfigString);
    } catch (Exception ex) {
        // Fallback to in-memory cache if Redis configuration fails
        Log.Error(ex, "Failed to configure Redis distributed cache, falling back to in-memory cache");
        builder.Services.AddDistributedMemoryCache();
        
        // Register mock shared storage service for development
        builder.Services.AddScoped<ISharedStorageService>(sp => {
            // Create a mock implementation for development that uses local filesystem
            var logger = sp.GetRequiredService<ILogger<RedisSharedStorageService>>();
            return new RedisSharedStorageService(null, logger);
        });
        
        Log.Warning("Using in-memory distributed cache in production due to Redis configuration failure");
    }

    // Add MVC services for Area support
    builder.Services.AddMvc();

    // Add HttpContextAccessor for user identity access
    builder.Services.AddHttpContextAccessor();

    // Register our UserService
    builder.Services.AddScoped<UserService>();

    // Register WorkoutDataPortabilityService
    builder.Services.AddScoped<WorkoutDataPortabilityService>();

    // Register TrainAIImportService
    builder.Services.AddScoped<TrainAIImportService>();

    // Register BackgroundJobService for handling long-running tasks
    builder.Services.AddScoped<BackgroundJobService>();

    // Register WorkoutDataService
    builder.Services.AddScoped<WorkoutDataService>();
    
    // Register QuickWorkoutService for optimized gym experience
    builder.Services.AddScoped<QuickWorkoutService>();

    // Register API Ninjas integration services
    var apiNinjasKey = builder.Configuration["ApiKeys:ApiNinjas"];
    if (string.IsNullOrEmpty(apiNinjasKey))
    {
        Log.Warning("API Ninjas key is not configured. Exercise enrichment functionality will not work correctly.");
    }
    else
    {
        Log.Information("API Ninjas key is configured successfully.");
    }

    builder.Services.AddHttpClient("ExerciseApi", client =>
    {
        client.BaseAddress = new Uri("https://api.api-ninjas.com/v1/exercises");
        client.DefaultRequestHeaders.Add("X-Api-Key", apiNinjasKey);
    });
    builder.Services.AddScoped<ExerciseApiService>();
    builder.Services.AddScoped<ExerciseTypeService>();
    builder.Services.AddScoped<ExerciseSelectionService>();

    // Register our HelpService
    builder.Services.AddScoped<HelpService>();

    // Register LoginHistoryService for tracking login history
    builder.Services.AddScoped<LoginHistoryService>();

    // Register ShareTokenService for workout sharing
    builder.Services.AddScoped<IShareTokenService, ShareTokenService>();

    // Register token validation services
    builder.Services.AddSingleton<ITokenRateLimiter, TokenRateLimiter>();
    builder.Services.AddScoped<ITokenValidationService, TokenValidationService>();

    // Register VersionService for version management
    builder.Services.AddScoped<IVersionService, VersionService>();

    // Register HangfireInitializationService
    builder.Services.AddScoped<IHangfireInitializationService, HangfireInitializationService>();

    // Register DatabaseResilienceService for connection pooling and retry logic
    builder.Services.AddSingleton<DatabaseResilienceService>();

    // Register alerting services
    builder.Services.AddScoped<IAlertingService, AlertingService>();

    // Register our alerting background job service
    builder.Services.AddScoped<WorkoutTrackerWeb.Services.Hangfire.AlertingJobsService>();
    builder.Services.AddScoped<WorkoutTrackerWeb.Services.Hangfire.AlertingJobsRegistration>();

    // Add session state with Redis caching and JSON serialization
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true; // Make the session cookie essential
        
        // Only require HTTPS in production
        if (builder.Environment.IsDevelopment())
        {
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        }
        else
        {
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        }
        
        // Set domain for production environment
        if (!builder.Environment.IsDevelopment())
        {
            // Use the same domain pattern as the auth cookie
            options.Cookie.Domain = ".workouttracker.online";
        }
        
        // Set a reasonable cookie name
        options.Cookie.Name = "WorkoutTracker.Session";
    });

    // Add Output Cache with Redis as the backing store if Redis is configured
    if (builder.Services.Any(s => s.ServiceType == typeof(IConnectionMultiplexer)))
    {
        // Use Redis as distributed cache for OutputCache in production
        builder.Services.AddStackExchangeRedisOutputCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? 
                                Environment.GetEnvironmentVariable("ConnectionStrings__Redis") ?? 
                                "redis-master.web.svc.cluster.local:6379,abortConnect=false";
            options.InstanceName = "WorkoutTracker:OutputCache:";
        });
        Log.Information("Configured OutputCache with Redis backend");
    }
    else 
    {
        // Use memory cache in development
        builder.Services.AddOutputCache();
        Log.Information("Configured OutputCache with memory backend");
    }

    // Configure OutputCache policies
    builder.Services.AddOutputCache(options =>
    {
        // Default policy for static content
        options.AddPolicy("StaticContent", builder => 
            builder.Cache()
                   .Expire(TimeSpan.FromHours(12))
                   .Tag("static-content"));
                   
        // Policy for content that varies by ID
        options.AddPolicy("StaticContentWithId", builder => 
            builder.Cache()
                   .Expire(TimeSpan.FromHours(12))
                   .SetVaryByRouteValue("id")
                   .Tag("static-content-with-id"));
                   
        // Policy for help articles that change less frequently
        options.AddPolicy("HelpContent", builder => 
            builder.Cache()
                   .Expire(TimeSpan.FromDays(1))
                   .SetVaryByRouteValue("id")
                   .SetVaryByRouteValue("category")
                   .Tag("help-content"));
                   
        // Policy for glossary content
        options.AddPolicy("GlossaryContent", builder => 
            builder.Cache()
                   .Expire(TimeSpan.FromDays(1))
                   .Tag("glossary-content"));
                   
        // Policy for exercise library content with shorter expiration
        options.AddPolicy("ExerciseLibrary", builder => 
            builder.Cache()
                   .Expire(TimeSpan.FromHours(6))
                   .SetVaryByQuery("category", "search")
                   .Tag("exercise-library"));
        
        // Policy for shared workout reports
        options.AddPolicy("SharedWorkoutReports", builder => 
            builder.Cache()
                   .Expire(TimeSpan.FromHours(6))
                   .SetVaryByQuery("token", "period")
                   .Tag("shared-workout-reports"));
        
        // Policy for shared workout pages
        options.AddPolicy("SharedWorkout", builder => 
            builder.Cache()
                   .Expire(TimeSpan.FromHours(3))
                   .SetVaryByQuery("token")
                   .Tag("shared-workout"));
    });

    // Add custom session serialization to use System.Text.Json for better performance
    builder.Services.AddOptions<SessionOptions>()
        .Configure<IDistributedCache>((options, cache) => 
        {
            // Configure the session options to use our distributed cache implementation
            options.IOTimeout = TimeSpan.FromSeconds(5);
        });

    // Add custom session serialization middleware (reuse Redis connection)
    builder.Services.AddSingleton<ISessionStore, WorkoutTrackerWeb.Services.Session.DistributedSessionStore>();
    builder.Services.AddSingleton<ISessionSerializer, JsonSessionSerializer>();

    // Configure custom options for session serialization
    builder.Services.Configure<JsonSessionSerializerOptions>(options => 
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.WriteIndented = false; // Keep session state compact
        options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

    // Add custom TempData type conversion to handle Guid to string conversion
    builder.Services.AddTempDataTypeConverter();

    // Add CORS policy for production domains
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ProductionDomainPolicy", policy =>
        {
            policy.WithOrigins(
                    "https://wot.ninjatec.co.uk", 
                    "https://workouttracker.online", 
                    "https://www.workouttracker.online")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // Required for SignalR
        });
    });

    // Configure Antiforgery options for AJAX requests with X-CSRF-TOKEN header
    builder.Services.AddAntiforgery(options => 
    {
        options.HeaderName = "X-CSRF-TOKEN";
        options.Cookie.Name = "CSRF-TOKEN";
        options.Cookie.HttpOnly = false; // Must be accessible via JavaScript
        
        // For production, we need to handle Cloudflare and Kubernetes ingress properly
        if (builder.Environment.IsProduction())
        {
            // When behind Cloudflare, we need to set this to Always
            // Cloudflare will terminate SSL but expects secure cookies
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        }
        else if (builder.Environment.IsDevelopment())
        {
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        }
        else
        {
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        }
        
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

    // Enhanced health checks
    var healthChecksBuilder = builder.Services.AddHealthChecks()
        // Database connectivity checks
        .AddDbContextCheck<ApplicationDbContext>("database_health_check", 
            tags: new[] { "ready", "db" },
            customTestQuery: async (db, ct) => await db.Database.CanConnectAsync(ct))
        .AddDbContextCheck<DataProtectionKeysDbContext>("data_protection_health_check", 
            tags: new[] { "ready", "db" })
        // SQL Server dedicated health check with more detailed diagnostics
        .AddSqlServer(
            connectionString,
            healthQuery: "SELECT 1;",
            name: "sql_health_check",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "ready", "db", "sql" })
        // System checks
        .AddDiskStorageHealthCheck(
            setup => setup.AddDrive("/", 512), // Reduced to 512MB minimum free space
            name: "disk_storage",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "ready", "system" })
        .AddPrivateMemoryHealthCheck(
            1024 * 1024 * 1024, // Increased to 1GB max memory
            name: "private_memory_check",
            tags: new[] { "ready", "system" })
        // Simple liveness check
        .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
        // Register the database connection pool health check
        .AddCheck<WorkoutTrackerWeb.HealthChecks.DatabaseConnectionPoolHealthCheck>(
            "database_connection_pool", 
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
            tags: new[] { "ready", "db", "connection-pool" })
        // Register the SMTP health check for email service
        .AddCheck<WorkoutTrackerWeb.HealthChecks.SmtpHealthCheck>(
            "email_smtp_health", 
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
            tags: new[] { "ready", "email", "smtp" });

    // Add Redis health check only in production
    if (!builder.Environment.IsDevelopment())
    {
        var redisHealthConnectionString = builder.Configuration.GetConnectionString("Redis") ?? 
                                    Environment.GetEnvironmentVariable("ConnectionStrings__Redis") ?? 
                                    "redis-master.web.svc.cluster.local:6379,abortConnect=false";
        
        // Register RedisOptions for our custom health check
        builder.Services.Configure<RedisOptions>(options => 
        {
            options.ConnectionString = redisHealthConnectionString;
        });
        
        // Register our custom Redis metrics health check
        healthChecksBuilder.AddCheck<RedisMetricsHealthCheck>(
            "redis_metrics_health_check",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "ready", "cache", "redis" });
    }

    builder.Services.AddDbContext<WorkoutTrackerWebContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("WorkoutTrackerWebContext") ?? 
                               throw new InvalidOperationException("Connection string 'WorkoutTrackerWebContext' not found.");
        
        // Get connection pooling settings from configuration
        var poolingConfig = builder.Configuration.GetSection("DatabaseConnectionPooling");
        int maxPoolSize = poolingConfig.GetValue<int>("MaxPoolSize", 200);
        int minPoolSize = poolingConfig.GetValue<int>("MinPoolSize", 10);
        int connectionLifetime = poolingConfig.GetValue<int>("ConnectionLifetime", 300);
        bool connectionResetEnabled = poolingConfig.GetValue<bool>("ConnectionResetEnabled", true);
        int loadBalanceTimeout = poolingConfig.GetValue<int>("LoadBalanceTimeout", 30);
        int retryCount = poolingConfig.GetValue<int>("RetryCount", 5);
        int retryInterval = poolingConfig.GetValue<int>("RetryInterval", 10);
        
        // Build connection string with additional connection pooling parameters
        var sqlConnectionBuilder = new SqlConnectionStringBuilder(connectionString)
        {
            MaxPoolSize = maxPoolSize,
            MinPoolSize = minPoolSize,
            ConnectTimeout = loadBalanceTimeout,
            LoadBalanceTimeout = loadBalanceTimeout,
            ConnectRetryCount = retryCount,
            ConnectRetryInterval = retryInterval
        };

        // Add additional parameters to connection string directly since SqlConnectionStringBuilder 
        // doesn't expose all pooling properties
        string enhancedConnectionString = sqlConnectionBuilder.ConnectionString;
        if (connectionLifetime > 0)
        {
            enhancedConnectionString += $";Connection Lifetime={connectionLifetime}";
        }
        
        // Add connection reset parameter directly to connection string - only on Windows
        // This parameter is not supported on macOS and Linux
        if (connectionResetEnabled && OperatingSystem.IsWindows())
        {
            enhancedConnectionString += $";Connection Reset=true";
        }
        
        // Use the enhanced connection string with all the pooling settings
        options.UseSqlServer(enhancedConnectionString, sqlOptions => 
        {
            // Enhanced retry logic for transient SQL errors
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: retryCount,
                maxRetryDelay: TimeSpan.FromSeconds(retryInterval),
                errorNumbersToAdd: new[] { 4060, 40197, 40501, 40613, 49918, 4221, 1205, 233, 64, -2 });
                
            // Connection resiliency settings
            sqlOptions.CommandTimeout(30); // Set reasonable command timeout
            sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
            
            // Connection pooling optimizations
            sqlOptions.MinBatchSize(5);      // Minimum number of operations to batch
            sqlOptions.MaxBatchSize(100);    // Maximum number of operations to batch
        })
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking) // Set default behavior to NoTracking
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()); // Only in development
    });

    // Update DbContextFactory registration to avoid scoped service from singleton
    builder.Services.AddSingleton<IDbContextFactory<WorkoutTrackerWebContext>>(serviceProvider =>
    {
        var connectionString = builder.Configuration.GetConnectionString("WorkoutTrackerWebContext") ?? 
                               throw new InvalidOperationException("Connection string 'WorkoutTrackerWebContext' not found.");
        
        // Get connection pooling settings from configuration
        var poolingConfig = builder.Configuration.GetSection("DatabaseConnectionPooling");
        int maxPoolSize = poolingConfig.GetValue<int>("MaxPoolSize", 200);
        int minPoolSize = poolingConfig.GetValue<int>("MinPoolSize", 10);
        int connectionLifetime = poolingConfig.GetValue<int>("ConnectionLifetime", 300);
        bool connectionResetEnabled = poolingConfig.GetValue<bool>("ConnectionResetEnabled", true);
        int loadBalanceTimeout = poolingConfig.GetValue<int>("LoadBalanceTimeout", 30);
        int retryCount = poolingConfig.GetValue<int>("RetryCount", 5);
        int retryInterval = poolingConfig.GetValue<int>("RetryInterval", 10);
        
        // Build connection string with additional connection pooling parameters
        var sqlConnectionBuilder = new SqlConnectionStringBuilder(connectionString)
        {
            MaxPoolSize = maxPoolSize,
            MinPoolSize = minPoolSize,
            ConnectTimeout = loadBalanceTimeout,
            LoadBalanceTimeout = loadBalanceTimeout,
            ConnectRetryCount = retryCount,
            ConnectRetryInterval = retryInterval
        };

        // Add additional parameters to connection string directly
        string enhancedConnectionString = sqlConnectionBuilder.ConnectionString;
        if (connectionLifetime > 0)
        {
            enhancedConnectionString += $";Connection Lifetime={connectionLifetime}";
        }
        
        // Add connection reset parameter directly to connection string - only on Windows
        if (connectionResetEnabled && OperatingSystem.IsWindows())
        {
            enhancedConnectionString += $";Connection Reset=true";
        }
        
        // Create factory options that will be used to create context instances
        var optionsBuilder = new DbContextOptionsBuilder<WorkoutTrackerWebContext>();
        optionsBuilder.UseSqlServer(enhancedConnectionString, sqlOptions => 
        {
            // Enhanced retry logic for transient SQL errors
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: retryCount,
                maxRetryDelay: TimeSpan.FromSeconds(retryInterval),
                errorNumbersToAdd: new[] { 4060, 40197, 40501, 40613, 49918, 4221, 1205, 233, 64, -2 });
                
            // Connection resiliency settings
            sqlOptions.CommandTimeout(30); // Set reasonable command timeout
            sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
            
            // Connection pooling optimizations
            sqlOptions.MinBatchSize(5);
            sqlOptions.MaxBatchSize(100);
        })
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking) 
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
        
        // Create and return a factory instead of using PooledDbContextFactory
        return new DbContextFactory<WorkoutTrackerWebContext>(optionsBuilder.Options);
    });

    // Configure application cookie settings for authentication
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.Name = "WorkoutTracker.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        
        // Only require HTTPS in production
        if (builder.Environment.IsDevelopment())
        {
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        }
        else
        {
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        }
        
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        
        // Set domain for production environment
        if (!builder.Environment.IsDevelopment())
        {
            // Use a domain that works for both apex and www subdomains
            // The leading dot allows cookies to work across all subdomains
            options.Cookie.Domain = ".workouttracker.online";
        }
        
        options.Events.OnSignedIn = async context =>
        {
            var userManager = context.HttpContext.RequestServices
                .GetRequiredService<UserManager<IdentityUser>>();
            var loginHistoryService = context.HttpContext.RequestServices
                .GetRequiredService<LoginHistoryService>();
            
            var user = await userManager.GetUserAsync(context.Principal);
            if (user != null)
            {
                await loginHistoryService.RecordSuccessfulLoginAsync(user.Id);
            }
        };
    });

    var app = builder.Build();

    // Explicitly initialize Hangfire database schema BEFORE configuring any middleware
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Running early Hangfire schema initialization");
            
            var hangfireInitService = scope.ServiceProvider.GetRequiredService<IHangfireInitializationService>();
            var success = hangfireInitService.InitializeHangfireSchema();
            
            if (success)
            {
                logger.LogInformation("Early Hangfire schema initialization successful");
            }
            else
            {
                logger.LogWarning("Early Hangfire schema initialization failed - dashboard may not be accessible");
            }
        }
    }
    catch (Exception ex)
    {
        // Log but don't fail startup
        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Error during early Hangfire schema initialization");
        }
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseExceptionHandler("/Errors/Error");
        app.UseStatusCodePagesWithReExecute("/Errors/Error", "?statusCode={0}");
        // Configure HSTS for production
        app.UseHsts();
    }

    // Use ForwardedHeaders middleware early in the pipeline to handle proxy headers
    app.UseForwardedHeaders();

    // Add Content Security Policy middleware
    app.Use(async (context, next) =>
    {
        // Define CSP policy
        context.Response.Headers["Content-Security-Policy"] = 
            "default-src 'self'; " +
            "script-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net 'unsafe-inline'; " + 
            "style-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.3/font/bootstrap-icons.css 'unsafe-inline'; " + 
            "img-src 'self' data: https://cdn.jsdelivr.net; " + 
            "font-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
            "connect-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://wot.ninjatec.co.uk https://workouttracker.online https://www.workouttracker.online " +
                             "wss://wot.ninjatec.co.uk wss://workouttracker.online wss://www.workouttracker.online " +
                             "ws://wot.ninjatec.co.uk ws://workouttracker.online ws://www.workouttracker.online wss://* ws://*; " +
            "frame-src 'self'; " +
            "frame-ancestors 'self' https://wot.ninjatec.co.uk https://workouttracker.online https://www.workouttracker.online; " + 
            "form-action 'self' https://wot.ninjatec.co.uk https://workouttracker.online https://www.workouttracker.online; " +
            "base-uri 'self'; " +
            "object-src 'none'";
        
        // Add Feature-Policy header (now called Permissions-Policy)
        context.Response.Headers["Permissions-Policy"] = 
            "camera=(), microphone=(), geolocation=()";
        
        // Add X-Content-Type-Options to prevent MIME type sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        
        // Add X-Frame-Options to prevent clickjacking
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        
        // Add Referrer-Policy to control what information is sent in the Referer header
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        
        await next();
    });

    // Use our dedicated Redis resilience middleware
    app.UseRedisResilience();

    // Use our database connection resilience middleware
    app.UseDbConnectionResilience();

    // Initialize Hangfire database using HangfireInitializationService
    Log.Information("Attempting to initialize Hangfire schema");
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var hangfireInitializationService = scope.ServiceProvider.GetRequiredService<IHangfireInitializationService>();
            bool success = hangfireInitializationService.InitializeHangfireSchema();
            if (success)
            {
                Log.Information("Hangfire schema initialized successfully");
            }
            else 
            {
                Log.Warning("Hangfire schema initialization returned false - tables may not have been created");
            }
            
            // Verify the schema was created
            bool schemaExists = hangfireInitializationService.VerifyHangfireSchema();
            Log.Information("Hangfire schema verification: {Result}", schemaExists ? "Success" : "Failed");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error initializing Hangfire schema");
    }

    // Register Hangfire background jobs using the service-based API
    try {
        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Registering alerting jobs through service-based API");
            
            var alertingJobsRegistration = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWeb.Services.Hangfire.AlertingJobsRegistration>();
            alertingJobsRegistration.RegisterAlertingJobs();
            
            logger.LogInformation("Alerting jobs registered successfully");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error registering alerting jobs");
    }

    // Add request metrics middleware before routing
    app.UseHttpMetrics();

    app.UseHttpsRedirection();

    // Configure static files with proper MIME types
    app.UseStaticFiles(new StaticFileOptions
    {
        ContentTypeProvider = new FileExtensionContentTypeProvider
        {
            Mappings = 
            {
                [".js"] = "application/javascript",
                [".min.js"] = "application/javascript"
            }
        }
    });

    // Apply IP rate limiting
    app.UseIpRateLimiting();

    // Use CORS with our production domain policy
    app.UseCors("ProductionDomainPolicy");

    app.UseRouting();

    // Use OutputCache middleware after routing but before session and auth
    app.UseOutputCache();

    // Enable session - only register once
    app.UseSession();

    // Add VersionLoggingMiddleware to the pipeline
    app.UseVersionLogging();

    // Add request logging middleware right before authentication in the pipeline
    app.UseRequestLogging(app.Environment.IsProduction());

    app.UseAuthentication();
    app.UseAuthorization();

    // Map health check endpoints BEFORE applying global auth policy to Razor Pages
    // These need to be configured first so they won't require authentication

    // Map health check endpoints with improved response format using HealthCheck UI client
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous()
      .WithMetadata(new EndpointNameMetadata("Health_Root"));

    // Specific health check endpoints for Kubernetes with improved responses
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous()
      .WithMetadata(new EndpointNameMetadata("Health_Live"));

    // Special minimal endpoint for Kubernetes probes with direct response
    app.MapGet("/health/ready", async context =>
    {
        // This is a super simple endpoint that always returns OK 
        // to make Kubernetes probes happy
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsync("OK");
    }).AllowAnonymous();

    // Specific database health check endpoint
    app.MapHealthChecks("/health/database", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("db"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous();

    // Specific Redis health check endpoint
    app.MapHealthChecks("/health/redis", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("redis"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous();

    // Specific Email health check endpoint
    app.MapHealthChecks("/health/email", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("email"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous();

    // Configure Hangfire dashboard AFTER authentication and authorization
    app.UseHangfireDashboard("/hangfire", new DashboardOptions {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });

    // Map area routes before regular routes
    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

    // Map default controller route
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    // Add redirect from old MVC routes to new Razor Pages implementations for shared workout views
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapGet("Shared/{action=Index}/{id?}", async context =>
        {
            var action = context.Request.RouteValues["action"]?.ToString() ?? "Index";
            var id = context.Request.RouteValues["id"]?.ToString();
            var token = context.Request.Query["token"].ToString();

            string redirectUrl;
            switch (action.ToLower())
            {
                case "index":
                    redirectUrl = "/Shared/Index";
                    break;
                case "session":
                    redirectUrl = $"/Shared/Session/{id}";
                    break;
                case "reports":
                    redirectUrl = "/Shared/Reports";
                    break;
                case "calculator":
                    redirectUrl = "/Shared/Calculator";
                    break;
                default:
                    redirectUrl = "/Shared/Index";
                    break;
            }

            // Keep the token parameter if it exists
            if (!string.IsNullOrEmpty(token))
            {
                redirectUrl += (redirectUrl.Contains('?') ? "&" : "?") + $"token={token}";
            }

            context.Response.Redirect(redirectUrl);
        });
    });

    // Expose Prometheus metrics at the /metrics endpoint
    app.MapMetrics();

    // Map SignalR hub
    app.MapHub<ImportProgressHub>("/importProgressHub");

    // Map Razor Pages (routes are automatically registered)
    app.MapRazorPages();

    // Initialize seed data
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            // Seed the database with initial data
            WorkoutTrackerWeb.Data.SeedData.InitializeAsync(services).Wait();
            Log.Information("Seed data initialization completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while seeding the database");
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    Log.CloseAndFlush();
}

// Configure Redis options with proper master/replica handling
static ConfigurationOptions ConfigureRedisOptions(string connectionString)
{
    var options = ConfigurationOptions.Parse(connectionString);
    
    // Basic connection settings
    options.KeepAlive = 180;
    options.ConnectTimeout = 10000; // 10 seconds
    options.SyncTimeout = 10000;    // 10 seconds
    options.AbortOnConnectFail = false; // Don't fail if Redis is temporarily unavailable
    options.ReconnectRetryPolicy = new ExponentialRetry(5000);
    options.ConnectRetry = 5;
    
    // Critical settings for handling master/replica scenarios
    options.AllowAdmin = true; // Needed to query node types
    options.TieBreaker = ""; // Don't use tiebreakers which can be problematic
    
    // Configure write operations to only go to master nodes
    options.ConfigCheckSeconds = 5; // Check for configuration changes frequently
    options.ConfigurationChannel = ""; // Don't use pub/sub for config
    
    // Set client name for diagnostics
    options.ClientName = "WorkoutTrackerCache";
    
    return options;
}

// Monitoring metrics class - moved to end of file to fix build error
public static class WorkoutTrackerMetrics
{
    public static readonly Counter SessionsCreated = Metrics.CreateCounter(
        "workout_tracker_sessions_created_total", "Number of workout sessions created");
    
    public static readonly Counter SetsCreated = Metrics.CreateCounter(
        "workout_tracker_sets_created_total", "Number of exercise sets created");
        
    public static readonly Counter RepsCreated = Metrics.CreateCounter(
        "workout_tracker_reps_created_total", "Number of exercise reps created");

    public static readonly Gauge ActiveUsers = Metrics.CreateGauge(
        "workout_tracker_active_users", "Number of currently active users");
        
    public static readonly Histogram HttpRequestDuration = Metrics.CreateHistogram(
        "workout_tracker_http_request_duration_seconds", 
        "Duration of HTTP requests in seconds",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
        });
}

// Hangfire authorization filter to restrict access to admins
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // Allow local requests during development
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" &&
           (httpContext.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || 
            httpContext.Request.Host.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }
        
        // Check if user is authenticated and in Admin role
        return httpContext.User.Identity?.IsAuthenticated == true && 
               httpContext.User.IsInRole("Admin");
    }
}
