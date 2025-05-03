using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Services.Email;
using WorkoutTrackerWeb.Services.Session;
using WorkoutTrackerWeb.Services.VersionManagement;
using WorkoutTrackerWeb.Services.Hangfire;
using WorkoutTrackerWeb.Services.Logging;
using WorkoutTrackerWeb.Services.Alerting;
using WorkoutTrackerWeb.Services.Calculations;
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
using Hangfire.Redis.StackExchange;
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
using WorkoutTrackerWeb.HealthChecks;
using Microsoft.Extensions.Options;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services.Redis;
using WorkoutTrackerWeb.Extensions;
using WorkoutTrackerWeb.Services.Metrics;

namespace WorkoutTrackerWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
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

                // Run as synchronous to avoid issues with top-level statements
                MainAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application startup failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static async Task MainAsync(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            string[] trustedDomains = new[] {
                "workouttracker.online", 
                "www.workouttracker.online",
                "wot.ninjatec.co.uk",
                "localhost",
                "localhost:5001",
                "localhost:5000"
            };

            builder.Host.UseSerilog();

            builder.Services.AddHostFiltering(options => {
                options.AllowedHosts = trustedDomains;
                options.AllowEmptyHosts = true;
                options.IncludeFailureMessage = true;
            });

            Log.Information("Host filtering configured with allowed hosts: {AllowedHosts}", string.Join(", ", trustedDomains));

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
                options.ForwardLimit = null;
            });

            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
                options.Secure = CookieSecurePolicy.Always;
            });

            builder.Services.AddMemoryCache();

            builder.Services.AddSession(options => 
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = builder.Environment.IsProduction() 
                    ? CookieSecurePolicy.Always 
                    : CookieSecurePolicy.SameAsRequest;
            });

            builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
            builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
            builder.Services.AddInMemoryRateLimiting();
            builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddLoggingServices();
            builder.Services.AddHostedService<WorkoutTrackerWeb.Services.Logging.LogLevelConfigurationHostedService>();

            builder.Services.AddHttpClient();

            // Configure Redis
            builder.Services.AddRedisConfiguration(builder.Configuration);

            // Add output caching services
            builder.Services.AddOutputCache(options => {
                options.AddPolicy("Default", builder => builder.Expire(TimeSpan.FromMinutes(10)));
                options.AddPolicy("Short", builder => builder.Expire(TimeSpan.FromMinutes(1)));
                options.AddPolicy("Medium", builder => builder.Expire(TimeSpan.FromMinutes(30)));
                options.AddPolicy("Long", builder => builder.Expire(TimeSpan.FromHours(1)));
            });

            // Configure Hangfire
            builder.Services.AddSingleton<WorkoutTrackerWeb.Services.Hangfire.HangfireServerConfiguration>();

            builder.Services.AddHangfire((sp, config) =>
            {
                var hangfireConfig = sp.GetRequiredService<WorkoutTrackerWeb.Services.Hangfire.HangfireServerConfiguration>();
                hangfireConfig.ConfigureHangfire(config);
                
                // Add explicit SQL Server storage options to fix query hints errors
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                if (connectionString != null)
                {
                    config.UseSqlServerStorage(connectionString, new Hangfire.SqlServer.SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.FromSeconds(15),
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = false,
                        SchemaName = "HangFire",
                        EnableHeavyMigrations = false,
                        DashboardJobListLimit = 1000
                    });
                }
            });

            // Register Hangfire services and servers
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
                    DisableGlobalLocks = true,
                    PrepareSchemaIfNecessary = true
                })
                .UseActivator(new HangfireActivator(builder.Services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>())));

            // Register background job services
            builder.Services.AddTransient<WorkoutTrackerWeb.Services.Hangfire.AlertingJobsService>();
            builder.Services.AddTransient<WorkoutTrackerWeb.Services.Hangfire.WorkoutReminderJobsService>();
            
            // Set the static service provider for Hangfire jobs to handle parameterless constructor scenarios
            WorkoutTrackerWeb.Services.Hangfire.AlertingJobsService.SetServiceProvider(builder.Services.BuildServiceProvider());

            // Register Hangfire jobs registration services
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Hangfire.AlertingJobsRegistration>();
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Hangfire.WorkoutSchedulingJobsRegistration>();
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Hangfire.WorkoutReminderJobsRegistration>();
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Hangfire.AlertingJobsService>();
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Hangfire.WorkoutReminderJobsService>();

            // Register job services
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Scheduling.WorkoutReminderService>();
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Scheduling.ScheduledWorkoutProcessorService>();

            // Add server after everything is configured
            builder.Services.AddHangfireServer((provider, options) => {
                var serverConfig = provider.GetRequiredService<WorkoutTrackerWeb.Services.Hangfire.HangfireServerConfiguration>();
                if (serverConfig.IsProcessingEnabled)
                {
                    Log.Information("Configuring Hangfire server with {WorkerCount} workers", serverConfig.WorkerCount);
                    serverConfig.ConfigureServerOptions(options);
                }
                else 
                {
                    Log.Information("Hangfire processing is disabled for this instance");
                }
            });

            Func<string, Action<SqlServerDbContextOptionsBuilder>, DbContextOptionsBuilder> getSqlOptions = (connString, sqlOptionsAction) =>
            {
                var poolingConfig = builder.Configuration.GetSection("DatabaseConnectionPooling");
                int maxPoolSize = poolingConfig.GetValue<int>("MaxPoolSize", 200);
                int minPoolSize = poolingConfig.GetValue<int>("MinPoolSize", 10);
                int connectionLifetime = poolingConfig.GetValue<int>("ConnectionLifetime", 300);
                bool connectionResetEnabled = poolingConfig.GetValue<bool>("ConnectionResetEnabled", true);
                int loadBalanceTimeout = poolingConfig.GetValue<int>("LoadBalanceTimeout", 30);
                int retryCount = poolingConfig.GetValue<int>("RetryCount", 5);
                int retryInterval = poolingConfig.GetValue<int>("RetryInterval", 10);
                
                var sqlConnectionBuilder = new SqlConnectionStringBuilder(connString)
                {
                    MaxPoolSize = maxPoolSize,
                    MinPoolSize = minPoolSize,
                    ConnectTimeout = loadBalanceTimeout,
                    LoadBalanceTimeout = loadBalanceTimeout,
                    ConnectRetryCount = retryCount,
                    ConnectRetryInterval = retryInterval
                };

                string enhancedConnectionString = sqlConnectionBuilder.ConnectionString;
                if (connectionLifetime > 0)
                {
                    enhancedConnectionString += $";Connection Lifetime={connectionLifetime}";
                }
                
                if (connectionResetEnabled && OperatingSystem.IsWindows())
                {
                    enhancedConnectionString += $";Connection Reset=true";
                }
                
                var optionsBuilder = new DbContextOptionsBuilder<WorkoutTrackerWebContext>();
                optionsBuilder.UseSqlServer(enhancedConnectionString, sqlOptionsAction)
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
                    
                return optionsBuilder;
            };

            builder.Services.AddDbContext<DataProtectionKeysDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDataProtection()
                .PersistKeysToDbContext<DataProtectionKeysDbContext>()
                .SetApplicationName("WorkoutTracker")
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

            builder.Services.AddDefaultIdentity<WorkoutTrackerWeb.Models.Identity.AppUser>(options => 
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddUserValidator<CustomUserValidator>();

            builder.Services.AddScoped<CustomUsernameManager>();

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
                options.AddPolicy("RequireCoachRole", policy => policy.RequireRole("Coach"));
            });

            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.AddTransient<IEmailService, WorkoutTrackerWeb.Services.Email.EmailService>();
            builder.Services.AddTransient<IEmailSender, EmailSenderAdapter>();

            try
            {
                if (builder.Environment.IsDevelopment())
                {
                    builder.Services.AddSignalR(options => {
                        options.EnableDetailedErrors = true;
                        options.MaximumReceiveMessageSize = 102400;
                        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                        options.HandshakeTimeout = TimeSpan.FromSeconds(15);
                    });
                    Log.Information("Configured SignalR with in-memory backplane for development");
                }
                else
                {
                    try
                    {
                        builder.Services.ConfigureRedis(builder.Configuration);

                        // Configure SignalR with Redis backplane if Redis is enabled
                        var redisSettings = builder.Configuration.GetSection("Redis").Get<WorkoutTrackerWeb.Services.Redis.RedisConfiguration>();
                        if (redisSettings?.Enabled == true)
                        {
                            builder.Services.AddSignalR(options =>
                            {
                                options.EnableDetailedErrors = true;
                                options.MaximumReceiveMessageSize = 102400;
                                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                                options.HandshakeTimeout = TimeSpan.FromSeconds(15);
                                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                            }).AddStackExchangeRedis(redisSettings.ConnectionString);
                        }
                        else
                        {
                            builder.Services.AddSignalR(options => {
                                options.EnableDetailedErrors = true;
                                options.MaximumReceiveMessageSize = 102400;
                                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                                options.HandshakeTimeout = TimeSpan.FromSeconds(15);
                            });
                            Log.Warning("Using in-memory SignalR backplane due to Redis being disabled");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to configure SignalR with Redis backplane, falling back to in-memory");
                        builder.Services.AddSignalR(options => {
                            options.EnableDetailedErrors = true;
                            options.MaximumReceiveMessageSize = 102400;
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
                throw;
            }

            // Configure health checks
            var healthChecksBuilder = builder.Services.AddHealthChecks()
                .AddDbContextCheck<ApplicationDbContext>("database_health_check", 
                    tags: new[] { "ready", "db" },
                    customTestQuery: async (db, ct) => await db.Database.CanConnectAsync(ct))
                .AddDbContextCheck<DataProtectionKeysDbContext>("data_protection_health_check", 
                    tags: new[] { "ready", "db" });

            // Add Redis health checks if Redis is enabled
            var redisConfig = builder.Configuration.GetSection("Redis").Get<WorkoutTrackerWeb.Services.Redis.RedisConfiguration>();
            if (redisConfig?.Enabled == true && !string.IsNullOrEmpty(redisConfig.ConnectionString))
            {
                healthChecksBuilder
                    .AddRedis(
                        redisConfig.ConnectionString,
                        name: "redis_connection",
                        tags: new[] { "ready", "redis" },
                        timeout: TimeSpan.FromSeconds(5))
                    .AddCheck<RedisMetricsHealthCheck>(
                        "redis_metrics",
                        tags: new[] { "ready", "redis" },
                        timeout: TimeSpan.FromSeconds(5));
            }

            builder.Services.AddMvc();

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<UserService>();

            builder.Services.AddScoped<WorkoutDataPortabilityService>();

            builder.Services.AddScoped<TrainAIImportService>();

            builder.Services.AddScoped<BackgroundJobService>();

            builder.Services.AddScoped<WorkoutDataService>();
            
            builder.Services.AddScoped<QuickWorkoutService>();

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

            builder.Services.AddScoped<HelpService>();

            builder.Services.AddScoped<LoginHistoryService>();

            builder.Services.AddScoped<IShareTokenService, ShareTokenService>();

            builder.Services.AddSingleton<TokenRateLimiter>();
            builder.Services.AddSingleton<ITokenRateLimiter>(provider => provider.GetRequiredService<TokenRateLimiter>());
            builder.Services.AddHostedService(provider => provider.GetRequiredService<TokenRateLimiter>());
            builder.Services.AddScoped<ITokenValidationService, TokenValidationService>();

            builder.Services.AddScoped<IVersionService, VersionService>();

            builder.Services.AddScoped<IHangfireInitializationService, HangfireInitializationService>();

            builder.Services.AddSingleton<DatabaseResilienceService>();

            // Register RedisSharedStorageService based on whether Redis is enabled
            if (redisConfig?.Enabled == true)
            {
                // Redis is enabled, normal registration happens in AddRedisConfiguration
                builder.Services.AddScoped<ISharedStorageService, RedisSharedStorageService>();
            }
            else
            {
                // Redis is disabled, register a version with null redis connection
                builder.Services.AddScoped<ISharedStorageService>(sp => {
                    var logger = sp.GetRequiredService<ILogger<RedisSharedStorageService>>();
                    return new RedisSharedStorageService(null, logger);
                });
                Log.Information("Redis is disabled. Using local filesystem fallback for shared storage.");
            }

            builder.Services.AddScoped<IAlertingService, AlertingService>();
            
            builder.Services.AddScoped<IVolumeCalculationService, VolumeCalculationService>();
            builder.Services.AddScoped<ICalorieCalculationService, CalorieCalculationService>();
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Reports.IReportsService, WorkoutTrackerWeb.Services.Reports.ReportsService>();
            
            builder.Services.AddScoped<IWorkoutIterationService, WorkoutIterationService>();

            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Coaching.ICoachingService, WorkoutTrackerWeb.Services.Coaching.CoachingService>();
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Coaching.GoalQueryService>();
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Coaching.GoalProgressService>();
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Coaching.GoalOperationsService>();
            
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Validation.CoachingValidationService>();

            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Scheduling.ScheduledWorkoutProcessorService>();
            builder.Services.Configure<WorkoutTrackerWeb.Services.Scheduling.ScheduledWorkoutProcessorOptions>(builder.Configuration.GetSection("ScheduledWorkoutProcessor"));

            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Scheduling.WorkoutReminderService>();
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Hangfire.WorkoutReminderJobsService>();
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Hangfire.WorkoutReminderJobsRegistration>();

            builder.Services.AddTempDataTypeConverter();

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
                          .AllowCredentials();
                });
            });

            builder.Services.AddRedisConfiguration(builder.Configuration);

            builder.Services.AddAntiforgery(options => 
            {
                options.HeaderName = "X-CSRF-TOKEN";
                options.Cookie.Name = "CSRF-TOKEN";
                options.Cookie.HttpOnly = false;
                
                if (builder.Environment.IsProduction())
                {
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

            builder.Services.AddDbContext<WorkoutTrackerWebContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("WorkoutTrackerWebContext") ?? 
                                       throw new InvalidOperationException("Connection string 'WorkoutTrackerWebContext' not found.");
                
                var poolingConfig = builder.Configuration.GetSection("DatabaseConnectionPooling");
                int maxPoolSize = poolingConfig.GetValue<int>("MaxPoolSize", 200);
                int minPoolSize = poolingConfig.GetValue<int>("MinPoolSize", 10);
                int connectionLifetime = poolingConfig.GetValue<int>("ConnectionLifetime", 300);
                bool connectionResetEnabled = poolingConfig.GetValue<bool>("ConnectionResetEnabled", true);
                int loadBalanceTimeout = poolingConfig.GetValue<int>("LoadBalanceTimeout", 30);
                int retryCount = poolingConfig.GetValue<int>("RetryCount", 5);
                int retryInterval = poolingConfig.GetValue<int>("RetryInterval", 10);
                
                var sqlConnectionBuilder = new SqlConnectionStringBuilder(connectionString)
                {
                    MaxPoolSize = maxPoolSize,
                    MinPoolSize = minPoolSize,
                    ConnectTimeout = loadBalanceTimeout,
                    LoadBalanceTimeout = loadBalanceTimeout,
                    ConnectRetryCount = retryCount,
                    ConnectRetryInterval = retryInterval
                };

                string enhancedConnectionString = sqlConnectionBuilder.ConnectionString;
                if (connectionLifetime > 0)
                {
                    enhancedConnectionString += $";Connection Lifetime={connectionLifetime}";
                }
                
                if (connectionResetEnabled && OperatingSystem.IsWindows())
                {
                    enhancedConnectionString += $";Connection Reset=true";
                }
                
                options.UseSqlServer(enhancedConnectionString, sqlOptions => 
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: retryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(retryInterval),
                        errorNumbersToAdd: new[] { 4060, 40197, 40501, 40613, 49918, 4221, 1205, 233, 64, -2 });
                        
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
                    
                    sqlOptions.MinBatchSize(5);
                    sqlOptions.MaxBatchSize(100);
                })
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
            });

            // Register SqlNullSafetyService
            builder.Services.AddScoped<SqlNullSafetyService>();

            // Register DatabaseMigrationService
            builder.Services.AddScoped<DatabaseMigrationService>();

            builder.Services.AddSingleton<IDbContextFactory<WorkoutTrackerWebContext>>(serviceProvider =>
            {
                var connectionString = builder.Configuration.GetConnectionString("WorkoutTrackerWebContext") ?? 
                                       throw new InvalidOperationException("Connection string 'WorkoutTrackerWebContext' not found.");
                
                var poolingConfig = builder.Configuration.GetSection("DatabaseConnectionPooling");
                int maxPoolSize = poolingConfig.GetValue<int>("MaxPoolSize", 200);
                int minPoolSize = poolingConfig.GetValue<int>("MinPoolSize", 10);
                int connectionLifetime = poolingConfig.GetValue<int>("ConnectionLifetime", 300);
                bool connectionResetEnabled = poolingConfig.GetValue<bool>("ConnectionResetEnabled", true);
                int loadBalanceTimeout = poolingConfig.GetValue<int>("LoadBalanceTimeout", 30);
                int retryCount = poolingConfig.GetValue<int>("RetryCount", 5);
                int retryInterval = poolingConfig.GetValue<int>("RetryInterval", 10);
                
                var sqlConnectionBuilder = new SqlConnectionStringBuilder(connectionString)
                {
                    MaxPoolSize = maxPoolSize,
                    MinPoolSize = minPoolSize,
                    ConnectTimeout = loadBalanceTimeout,
                    LoadBalanceTimeout = loadBalanceTimeout,
                    ConnectRetryCount = retryCount,
                    ConnectRetryInterval = retryInterval
                };

                string enhancedConnectionString = sqlConnectionBuilder.ConnectionString;
                if (connectionLifetime > 0)
                {
                    enhancedConnectionString += $";Connection Lifetime={connectionLifetime}";
                }
                
                if (connectionResetEnabled && OperatingSystem.IsWindows())
                {
                    enhancedConnectionString += $";Connection Reset=true";
                }
                
                var optionsBuilder = new DbContextOptionsBuilder<WorkoutTrackerWebContext>();
                optionsBuilder.UseSqlServer(enhancedConnectionString, sqlOptions => 
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: retryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(retryInterval),
                        errorNumbersToAdd: new[] { 4060, 40197, 40501, 40613, 49918, 4221, 1205, 233, 64, -2 });
                        
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
                    
                    sqlOptions.MinBatchSize(5);
                    sqlOptions.MaxBatchSize(100);
                })
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking) 
                .EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
                
                return new DbContextFactory<WorkoutTrackerWebContext>(optionsBuilder.Options);
            });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "WorkoutTracker.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                
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
                
                if (!builder.Environment.IsDevelopment())
                {
                    options.Cookie.Domain = ".workouttracker.online";
                }
                
                options.Events.OnSignedIn = async context =>
                {
                    var userManager = context.HttpContext.RequestServices
                        .GetRequiredService<UserManager<WorkoutTrackerWeb.Models.Identity.AppUser>>();
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

            // Initialize database
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<ApplicationDbContext>();
                context.Database.Migrate();

                var logger = services.GetRequiredService<ILogger<Program>>();
                try
                {
                    // Run database migration service to ensure CaloriesBurned column is added
                    var migrationService = services.GetRequiredService<DatabaseMigrationService>();
                    await migrationService.MigrateAsync();
                    
                    // Run SqlNullSafetyService to fix NULL string values
                    var sqlNullSafetyService = services.GetRequiredService<SqlNullSafetyService>();
                    int rowsFixed = await sqlNullSafetyService.FixNullStringValuesAsync();
                    logger.LogInformation("Fixed {RowCount} NULL string values in database", rowsFixed);
                    
                    await SeedData.InitializeAsync(services);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseForwardedHeaders();

            app.UseMaintenanceMode();

            app.UseInvitationRedirect();

            app.Use(async (context, next) =>
            {
                context.Response.Headers["Content-Security-Policy"] = 
                    "default-src 'self'; " +
                    "script-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net 'unsafe-inline'; " + 
                    "style-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.3/font/bootstrap-icons.css 'unsafe-inline'; " + 
                    "img-src 'self' data: https://cdn.jsdelivr.net; " + 
                    "font-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
                    "connect-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://wot.ninjatec.co.uk https://workouttracker.online https://www.workouttracker.online " +
                                     "wss://wot.ninjatec.co.uk wss://workouttracker.online wss://www.workouttracker.online " +
                                     "ws://wot.ninjatec.co.uk ws://workouttracker.online ws://www.workouttracker.online wss://* ws://*; " +
                    "frame-src 'self'; " +
                    "frame-ancestors 'self' https://wot.ninjatec.co.uk https://workouttracker.online https://www.workouttracker.online; " + 
                    "form-action 'self' https://wot.ninjatec.co.uk https://workouttracker.online https://www.workouttracker.online; " +
                    "base-uri 'self'; " +
                    "object-src 'none'";
                
                context.Response.Headers["Permissions-Policy"] = 
                    "camera=(), microphone=(), geolocation=()";
                
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                
                context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
                
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                
                await next();
            });

            app.UseRedisResilience();

            app.UseDbConnectionResilience();

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
                        
                        bool schemaExists = hangfireInitializationService.VerifyHangfireSchema();
                        Log.Information("Hangfire schema verification: {Result}", schemaExists ? "Success" : "Failed");

                        if (schemaExists)
                        {
                            // Initialize AlertingJobsService static service provider
                            WorkoutTrackerWeb.Services.Hangfire.AlertingJobsService.SetServiceProvider(scope.ServiceProvider);
                            
                            // Register all background jobs only after schema is verified
                            Log.Information("Registering alerting jobs");
                            var alertingJobsRegistration = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWeb.Services.Hangfire.AlertingJobsRegistration>();
                            alertingJobsRegistration.RegisterAlertingJobs();
                            
                            Log.Information("Registering workout scheduling jobs");
                            var workoutSchedulingJobsRegistration = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWeb.Services.Hangfire.WorkoutSchedulingJobsRegistration>();
                            workoutSchedulingJobsRegistration.RegisterWorkoutSchedulingJobs();
                            
                            Log.Information("Registering workout reminder jobs");
                            var workoutReminderJobsRegistration = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWeb.Services.Hangfire.WorkoutReminderJobsRegistration>();
                            workoutReminderJobsRegistration.RegisterWorkoutReminderJobs();
                            
                            Log.Information("All background jobs registered successfully");
                        }
                        else
                        {
                            Log.Error("Failed to verify Hangfire schema after initialization");
                        }
                    }
                    else 
                    {
                        Log.Error("Hangfire schema initialization failed");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing Hangfire schema and registering jobs");
            }

            app.UseHttpMetrics();

            app.UseHttpsRedirection();

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

            app.UseIpRateLimiting();
            
            app.UseRateLimitBypass();

            app.UseCors("ProductionDomainPolicy");

            app.UseRouting();

            app.UseOutputCache();

            app.UseSession();

            app.UseVersionLogging();

            app.UseRequestLogging(app.Environment.IsProduction());

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = async (context, report) =>
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    await UIResponseWriter.WriteHealthCheckUIResponse(context, report);
                }
            }).AllowAnonymous()
              .WithMetadata(new EndpointNameMetadata("Health_Root"));

            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            }).AllowAnonymous()
              .WithMetadata(new EndpointNameMetadata("Health_Live"));

            app.MapGet("/health/ready", async context =>
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsync("OK");
            }).AllowAnonymous();

            app.MapHealthChecks("/health/database", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("db"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            }).AllowAnonymous();

            app.MapHealthChecks("/health/redis", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("redis")
            });

            app.MapHealthChecks("/health/email", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("email"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            }).AllowAnonymous();

            app.UseHangfireDashboard("/hangfire", new DashboardOptions {
                Authorization = new[] { new HangfireAuthorizationFilter() }
            });

            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("Shared/{action=Index}/{id?}", context =>
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

                    if (!string.IsNullOrEmpty(token))
                    {
                        redirectUrl += (redirectUrl.Contains('?') ? "&" : "?") + $"token={token}";
                    }

                    context.Response.Redirect(redirectUrl);
                    return Task.CompletedTask;
                });
            });

            app.MapMetrics();

            app.MapHub<ImportProgressHub>("/importProgressHub");

            app.MapRazorPages();

            app.Run();
        }
    }
}
