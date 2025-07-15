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
using WorkoutTrackerWeb.Services.Cache;
using WorkoutTrackerWeb.Services.Redis;
using WorkoutTrackerWeb.Extensions;
using WorkoutTrackerWeb.Middleware;
using WorkoutTrackerWeb.Hubs;
using WorkoutTrackerWeb.HealthChecks;
using WorkoutTrackerWeb.Models.Configuration;
using WorkoutTrackerWeb.Utilities;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using System.IO.Compression;
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
using Serilog.Enrichers.Span;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.HttpOverrides;
using Hangfire;
using Hangfire.SqlServer;
using Hangfire.Dashboard;
using Hangfire.Redis.StackExchange;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.StaticFiles;
using StackExchange.Redis;
using HealthChecks.Redis;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;
using OpenTelemetry;
using System.Diagnostics;

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
                .Enrich.FromLogContext()
                .Enrich.WithSpan()
                .CreateLogger();

            try
            {
                Log.Information("Starting up WorkoutTracker application");
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

            var openTelemetryConfig = builder.Configuration.GetSection("OpenTelemetry").Get<OpenTelemetryConfig>() ?? new OpenTelemetryConfig();

            // Only enable OpenTelemetry in production
            if (!builder.Environment.IsDevelopment() && openTelemetryConfig.Enabled)
            {
                try
                {
                    // Get the endpoint from config or environment variable for better diagnostics
                    var endpoint = openTelemetryConfig.OtlpExporterEndpoint ?? 
                                  Environment.GetEnvironmentVariable("OTLP_ENDPOINT") ?? 
                                  "http://otel-collector.monitoring:4317";
                    
                    // Add diagnostic information about authentication
                    var bearerToken = Environment.GetEnvironmentVariable("OTEL_AUTH_BEARER");
                    bool hasAuthToken = !string.IsNullOrEmpty(bearerToken);
                    
                    Log.Information("Configuring OpenTelemetry with endpoint: {Endpoint}, Service: {ServiceName}, Version: {ServiceVersion}, Auth Configured: {HasAuth}", 
                        endpoint, openTelemetryConfig.ServiceName, openTelemetryConfig.ServiceVersion, hasAuthToken);

                    if (string.IsNullOrEmpty(openTelemetryConfig.OtlpExporterEndpoint))
                    {
                        throw new InvalidOperationException("OpenTelemetry OTLP exporter endpoint is not configured");
                    }

                    if (string.IsNullOrEmpty(openTelemetryConfig.ServiceName))
                    {
                        throw new InvalidOperationException("OpenTelemetry service name is not configured");
                    }

                    builder.Services.AddOpenTelemetry()
                        .ConfigureResource(resource => resource
                            .AddService(openTelemetryConfig.ServiceName,
                                       serviceVersion: openTelemetryConfig.ServiceVersion,
                                       serviceInstanceId: Environment.MachineName))
                        .WithTracing(tracing => {
                            // Configure sampling
                            if (openTelemetryConfig.SamplingProbability > 0 && openTelemetryConfig.SamplingProbability <= 1.0)
                            {
                                tracing.SetSampler(new TraceIdRatioBasedSampler(openTelemetryConfig.SamplingProbability));
                                Log.Information("OpenTelemetry sampling configured with probability: {SamplingProbability}", openTelemetryConfig.SamplingProbability);
                            }

                            // Add instrumentation sources
                            tracing.AddAspNetCoreInstrumentation(options => 
                                {
                                    options.RecordException = true;
                                    options.EnrichWithHttpRequest = (activity, request) =>
                                    {
                                        var userAgent = request.Headers.UserAgent.ToString();
                                        if (!string.IsNullOrEmpty(userAgent))
                                        {
                                            activity.SetTag("http.user_agent", userAgent);
                                        }
                                        
                                        var clientIp = request.Headers["X-Real-IP"].FirstOrDefault() 
                                            ?? request.Headers["X-Forwarded-For"].FirstOrDefault() 
                                            ?? request.HttpContext.Connection.RemoteIpAddress?.ToString();
                                        if (!string.IsNullOrEmpty(clientIp))
                                        {
                                            activity.SetTag("http.client_ip", clientIp);
                                        }
                                    };
                                    options.EnrichWithHttpResponse = (activity, response) =>
                                    {
                                        activity.SetTag("http.response.status_code", response.StatusCode);
                                    };
                                })
                              .AddHttpClientInstrumentation(options =>
                                {
                                    options.RecordException = true;
                                    options.EnrichWithHttpRequestMessage = (activity, request) =>
                                    {
                                        activity.SetTag("http.request.method", request.Method.Method);
                                        activity.SetTag("http.url", request.RequestUri?.ToString());
                                    };
                                    options.EnrichWithHttpResponseMessage = (activity, response) =>
                                    {
                                        activity.SetTag("http.response.status_code", (int)response.StatusCode);
                                    };
                                })
                              .AddSqlClientInstrumentation(options => 
                              {
                                  options.SetDbStatementForText = true;
                                  options.RecordException = true;
                              });

                            // Add Entity Framework Core instrumentation
                            builder.Services.AddOpenTelemetry()
                                .WithTracing(builder => builder.AddEntityFrameworkCoreInstrumentation(options =>
                                {
                                    options.SetDbStatementForText = true;
                                }));

                            // Configure Redis instrumentation if enabled
                            var redisConfig = builder.Configuration.GetSection("Redis").Get<WorkoutTrackerWeb.Services.Redis.RedisConfiguration>();
                            if (redisConfig?.Enabled == true && !string.IsNullOrEmpty(redisConfig.ConnectionString))
                            {
                                try
                                {
                                    tracing.AddRedisInstrumentation();
                                    Log.Information("Redis tracing instrumentation enabled");
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "Failed to configure Redis tracing instrumentation");
                                    throw; // Re-throw to prevent running without Redis tracing
                                }
                            }

                            // Add custom instrumentation sources including Hangfire
                            var sources = (openTelemetryConfig.Sources?.ToList() ?? new List<string> { "WorkoutTracker.CustomInstrumentation" })
                                .Concat(new[] { "Hangfire" })
                                .Distinct()
                                .ToArray();
                            
                            tracing.AddSource(sources);
                            Log.Information("OpenTelemetry sources configured: {Sources}", string.Join(", ", sources));

                            // Configure OpenTelemetry Protocol exporter with error handling
                            try
                            {
                                var endpoint = openTelemetryConfig.OtlpExporterEndpoint ?? "http://tempo:4317";
                                var uri = new Uri(endpoint); // Validate URI format
                                
                                // Always use gRPC protocol for better performance
                                var protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                                
                                // Get the bearer token from environment variable
                                var bearerToken = Environment.GetEnvironmentVariable("OTEL_AUTH_BEARER");
                                
                                tracing.AddOtlpExporter(otlpOptions =>
                                {
                                    otlpOptions.Endpoint = uri;
                                    otlpOptions.Protocol = protocol;
                                    otlpOptions.TimeoutMilliseconds = 30000; // 30 second timeout
                                    
                                    // Set authentication headers if token is available
                                    if (!string.IsNullOrEmpty(bearerToken))
                                    {
                                        otlpOptions.Headers = $"Authorization=Bearer {bearerToken}";
                                        Log.Information("Authentication configured for OTLP exporter");
                                    }
                                });
                                
                                Log.Information("OTLP exporter configured successfully with endpoint: {Endpoint}, protocol: {Protocol}", 
                                    endpoint, protocol);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Failed to configure OTLP exporter with endpoint {Endpoint}. Error details: {ErrorMessage}",
                                    openTelemetryConfig.OtlpExporterEndpoint, 
                                    ex.ToString());
                                    
                                // Log environment information for debugging
                                var bearerToken = Environment.GetEnvironmentVariable("OTEL_AUTH_BEARER");
                                Log.Information("OTEL_AUTH_BEARER present: {HasToken}", !string.IsNullOrEmpty(bearerToken));
                                
                                throw; // Re-throw to prevent application startup
                            }

                            // Add console exporter if enabled
                            if (openTelemetryConfig.ConsoleExporterEnabled)
                            {
                                try
                                {
                                    tracing.AddConsoleExporter();
                                    Log.Information("Console exporter enabled for OpenTelemetry traces");
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "Failed to configure console exporter");
                                }
                            }
                        });

                    Log.Information("OpenTelemetry configuration completed successfully");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Critical failure in OpenTelemetry configuration. Application will not start to prevent running without tracing.");
                    throw; // Re-throw to prevent application startup
                }
            }
            else
            {
                if (builder.Environment.IsDevelopment())
                {
                    Log.Information("OpenTelemetry is disabled in development environment");
                }
                else 
                {
                    Log.Warning("OpenTelemetry is disabled in production environment. This may be unintentional.");
                }
            }

            // Add Telemetry Services
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Telemetry.TelemetryService>();
            
            // Add diagnostic trace service to help troubleshoot OpenTelemetry issues
            builder.Services.AddHostedService<WorkoutTrackerWeb.Services.Telemetry.DiagnosticTraceService>();

            var allowedHosts = builder.Configuration["AllowedHosts"]?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>();

            builder.Host.UseSerilog();

            builder.Services.AddHostFiltering(options => {
                options.AllowedHosts = allowedHosts;
                options.AllowEmptyHosts = true;
            });

            Log.Information("Host filtering configured with allowed hosts: {AllowedHosts}", string.Join(", ", allowedHosts));

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
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddLoggingServices();
            builder.Services.AddHostedService<WorkoutTrackerWeb.Services.Logging.LogLevelConfigurationHostedService>();
            // Add shadow property cleanup service to detect and verify shadow property issues
            builder.Services.AddHostedService<WorkoutTrackerWeb.CleanupShadowProperties>();

            builder.Services.AddHttpClient();

            // Register SignalR message batching service
            builder.Services.AddSingleton<SignalRMessageBatchService>();

            // Configure Redis
            builder.Services.AddRedisConfiguration(builder.Configuration);

            // Add query result caching services
            builder.Services.AddQueryResultCaching(builder.Configuration);

            // Add output caching services
            builder.Services.AddOutputCache(options => {
                options.AddPolicy("Default", builder => builder.Expire(TimeSpan.FromMinutes(10)));
                options.AddPolicy("Short", builder => builder.Expire(TimeSpan.FromMinutes(1)));
                options.AddPolicy("Medium", builder => builder.Expire(TimeSpan.FromMinutes(30)));
                options.AddPolicy("Long", builder => builder.Expire(TimeSpan.FromHours(1)));
                options.AddPolicy("StaticContent", builder => builder.Expire(TimeSpan.FromDays(1)));
                options.AddPolicy("HomePagePolicy", builder => 
                    builder.Expire(TimeSpan.FromMinutes(5))
                           .SetVaryByQuery("none")
                           .Tag("content:home"));
            });

            // Register the custom OutputCache services
            builder.Services.AddSingleton<ITaggedOutputCachePolicyProvider, TaggedOutputCachePolicyProvider>();
            builder.Services.AddSingleton<IOutputCacheInvalidationService, OutputCacheInvalidationService>();
            builder.Services.AddSingleton<AntiDogpileLockManager>();
            
            // Configure and register the background refresher
            builder.Services.Configure<OutputCacheRefresherOptions>(builder.Configuration.GetSection("OutputCacheRefresher"));
            builder.Services.AddSingleton<OutputCacheBackgroundRefresher>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<OutputCacheBackgroundRefresher>());

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
                }));

            // Register HangfireActivator properly
            builder.Services.AddSingleton<JobActivator>(sp => 
                new HangfireActivator(sp.GetRequiredService<IServiceScopeFactory>()));

            // Configure JobFilterAttributeUtils with the service provider
            builder.Services.AddSingleton<IStartupFilter>(sp => new JobFilterAttributeStartupFilter(sp));

            // Register background job services
            builder.Services.AddTransient<WorkoutTrackerWeb.Services.Hangfire.AlertingJobsService>();
            builder.Services.AddTransient<WorkoutTrackerWeb.Services.Hangfire.WorkoutReminderJobsService>();
            
            // Register the HangfireStorageMaintenanceService for job storage cleanup and maintenance
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Hangfire.HangfireStorageMaintenanceService>();
            
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

            // Read environment variable directly instead of creating a temporary service provider
            var hangfireProcessingEnabled = Environment.GetEnvironmentVariable("HANGFIRE_PROCESSING_ENABLED");
            var isHangfireProcessingEnabled = string.IsNullOrEmpty(hangfireProcessingEnabled) || 
                                              (bool.TryParse(hangfireProcessingEnabled, out var result) && result);

            // Only add Hangfire server when processing is enabled
            if (isHangfireProcessingEnabled)
            {
                // Add server after everything is configured
                builder.Services.AddHangfireServer((provider, options) => {
                    var serverConfig = provider.GetRequiredService<WorkoutTrackerWeb.Services.Hangfire.HangfireServerConfiguration>();
                    Log.Information("Configuring Hangfire server with {WorkerCount} workers", serverConfig.WorkerCount);
                    serverConfig.ConfigureServerOptions(options);
                });
                
                Log.Information("Hangfire server registered for this instance");
            }
            else
            {
                Log.Information("Hangfire processing is disabled for this instance. No server registered.");
            }

            Func<string, Action<SqlServerDbContextOptionsBuilder>, DbContextOptionsBuilder> getSqlOptions = (connString, sqlOptionsAction) =>
            {
                var poolingConfig = builder.Configuration.GetSection("DatabaseConnectionPooling");
                
                // Build SQL connection with pooling settings from config
                var sqlConnectionBuilder = new SqlConnectionStringBuilder(connString);
                if (poolingConfig.GetValue<bool>("EnableConnectionPooling", true))
                {
                    sqlConnectionBuilder.Pooling = true;
                    sqlConnectionBuilder.MaxPoolSize = poolingConfig.GetValue<int>("MaxPoolSize", 100);
                    sqlConnectionBuilder.MinPoolSize = poolingConfig.GetValue<int>("MinPoolSize", 5);
                    sqlConnectionBuilder.ConnectTimeout = poolingConfig.GetValue<int>("ConnectTimeout", 30);
                    sqlConnectionBuilder.LoadBalanceTimeout = poolingConfig.GetValue<int>("LoadBalanceTimeout", 30);
                }
                else
                {
                    sqlConnectionBuilder.Pooling = false;
                }

                // Apply additional connection settings
                string enhancedConnectionString = sqlConnectionBuilder.ConnectionString;
                if (poolingConfig.GetValue<int>("ConnectionLifetime", 0) > 0)
                {
                    enhancedConnectionString += $";Connection Lifetime={poolingConfig.GetValue<int>("ConnectionLifetime")}";
                }
                
                if (poolingConfig.GetValue<bool>("ConnectionResetEnabled", true) && OperatingSystem.IsWindows())
                {
                    enhancedConnectionString += ";Connection Reset=true";
                }

                var optionsBuilder = new DbContextOptionsBuilder<WorkoutTrackerWebContext>();
                optionsBuilder.UseSqlServer(enhancedConnectionString, sqlOptionsAction);
                
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
                // Allow email addresses as usernames by including additional characters
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+!#$%&'*/=?^`{|}~";
            })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<WorkoutTrackerWebContext>()
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
                .AddDbContextCheck<WorkoutTrackerWebContext>("database_health_check", 
                    tags: new[] { "ready", "db" },
                    customTestQuery: async (db, ct) => await db.Database.CanConnectAsync(ct))
                .AddDbContextCheck<DataProtectionKeysDbContext>("data_protection_health_check", 
                    tags: new[] { "ready", "db" })
                .AddCheck<DatabaseConnectionPoolHealthCheck>(
                    "connection_pool_health", 
                    tags: new[] { "ready", "db", "pool" },
                    timeout: TimeSpan.FromSeconds(5));

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

            builder.Services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
                    options.JsonSerializerOptions.MaxDepth = 64; // Increase if needed for deep graphs
                });

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<UserService>();
            
            // Register TelemetryService
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Telemetry.TelemetryService>();

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

            // Blog Services
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Blog.IBlogRepository, WorkoutTrackerWeb.Services.Blog.BlogRepository>();
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Blog.IBlogService, WorkoutTrackerWeb.Services.Blog.BlogService>();
            builder.Services.AddSingleton<WorkoutTrackerWeb.Utilities.BlogImageUtility>();

            // Register DatabaseResilienceService with configuration
            builder.Services.AddSingleton<DatabaseResilienceService>(sp => 
                new DatabaseResilienceService(
                    sp.GetRequiredService<ILogger<DatabaseResilienceService>>(),
                    sp.GetRequiredService<IConfiguration>()
                ));

            // Register ConnectionStringBuilderService for centralized connection pooling configuration
            builder.Services.AddSingleton<ConnectionStringBuilderService>();

            // Register RedisSharedStorageService based on whether Redis is enabled
            if (redisConfig?.Enabled == true)
            {
                // Redis is enabled, register with proper dependencies
                builder.Services.AddScoped<ISharedStorageService>(sp => {
                    var logger = sp.GetRequiredService<ILogger<RedisSharedStorageService>>();
                    var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
                    var keyService = sp.GetRequiredService<IRedisKeyService>();
                    return new RedisSharedStorageService(multiplexer, logger, keyService);
                });
            }
            else
            {
                // Redis is disabled, register a version with null redis connection
                builder.Services.AddScoped<ISharedStorageService>(sp => {
                    var logger = sp.GetRequiredService<ILogger<RedisSharedStorageService>>();
                    var keyService = sp.GetRequiredService<IRedisKeyService>();
                    return new RedisSharedStorageService(null, logger, keyService);
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
                var connectionStringBuilder = builder.Services.BuildServiceProvider().GetRequiredService<ConnectionStringBuilderService>();
                var enhancedConnectionString = connectionStringBuilder.BuildConnectionString("WorkoutTrackerWebContext", false);
                var poolingConfig = builder.Configuration.GetSection("DatabaseConnectionPooling");

                // Configure EF Core with resilience settings
                options.UseSqlServer(enhancedConnectionString, sqlOptions => 
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: poolingConfig.GetValue<int>("RetryCount", 3),
                        maxRetryDelay: TimeSpan.FromSeconds(poolingConfig.GetValue<int>("RetryInterval", 10)),
                        errorNumbersToAdd: new[] { 4060, 40197, 40501, 40613, 49918, 4221, 1205, 233, 64, -2 });
                        
                    sqlOptions.CommandTimeout(poolingConfig.GetValue<int>("ConnectTimeout", 30));
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
                })
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
                // Suppress the warning about pending model changes
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            });

            // Register SqlNullSafetyService
            builder.Services.AddScoped<SqlNullSafetyService>();

            // Register DatabaseMigrationService
            builder.Services.AddScoped<DatabaseMigrationService>();

            // Register new database optimization services
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Database.ProjectionService>();

            // Register compression metrics service
            builder.Services.AddSingleton<WorkoutTrackerWeb.Services.Metrics.CompressionMetricsService>();

            builder.Services.AddSingleton<IDbContextFactory<WorkoutTrackerWebContext>>(serviceProvider =>
            {
                var connectionStringBuilder = serviceProvider.GetRequiredService<ConnectionStringBuilderService>();
                var enhancedConnectionString = connectionStringBuilder.BuildConnectionString("WorkoutTrackerWebContext", false);
                var poolingConfig = builder.Configuration.GetSection("DatabaseConnectionPooling");
                int retryCount = poolingConfig.GetValue<int>("RetryCount", 5);
                int retryInterval = poolingConfig.GetValue<int>("RetryInterval", 10);
                
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
                
                var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
                return new DbContextFactory<WorkoutTrackerWebContext>(optionsBuilder.Options, httpContextAccessor);
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

            builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            builder.Services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { 
                        "application/json",
                        "application/javascript", 
                        "text/css",
                        "text/html",
                        "text/javascript",
                        "text/json",
                        "text/plain",
                        "text/xml",
                        "application/xml",
                        "application/x-javascript"
                    });
                // Add exclusion for already compressed content types
                options.ExcludedMimeTypes = new[] {
                    "image/jpeg",
                    "image/png",
                    "image/gif",
                    "image/webp", 
                    "image/svg+xml",
                    "audio/mpeg",
                    "video/mp4",
                    "font/woff",
                    "font/woff2",
                    "application/octet-stream",
                    "application/pdf",
                    "application/zip",
                    "application/x-gzip"
                };
            });

            builder.Services.AddHostedService<VersionCacheInvalidationService>();

            // Remove any built-in asset optimization
            builder.Services.Configure<StaticFileOptions>(options =>
            {
                options.ServeUnknownFileTypes = true;
            });

            // Add proper cache control for static files
            builder.Services.AddResponseCaching();

            builder.Services.AddScoped<UserPreferenceService>(sp => 
                new UserPreferenceService(
                    sp.GetRequiredService<IDbContextFactory<WorkoutTrackerWebContext>>(), 
                    sp.GetRequiredService<ILogger<UserPreferenceService>>()));

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IWorkoutIterationService, WorkoutIterationService>();
            
            // Dashboard services
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Dashboard.IDashboardService, WorkoutTrackerWeb.Services.Dashboard.DashboardService>();
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Export.IPdfExportService, WorkoutTrackerWeb.Services.Export.PdfExportService>();
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Export.ICsvExportService, WorkoutTrackerWeb.Services.Export.CsvExportService>();
            builder.Services.AddScoped<WorkoutTrackerWeb.Services.Dashboard.IDashboardRepository, WorkoutTrackerWeb.Services.Dashboard.DashboardRepository>();

            // Configure output cache for dashboard data
            builder.Services.AddOutputCache(options => 
            {
                // Add the dashboard cache policy with custom configuration
                WorkoutTrackerWeb.Services.Dashboard.DashboardCachePolicy.Configure(options);
            });

            var app = builder.Build();

            // Initialize database
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<WorkoutTrackerWebContext>();
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
                    
                    // Analyze DbContext for shadow property conflicts if in development environment
                    if (app.Environment.IsDevelopment())
                    {
                        logger.LogInformation("Analyzing DbContext for shadow property conflicts...");
                        var analyzer = new ShadowPropertyAnalyzer(logger);
                        var conflicts = analyzer.AnalyzeContext(context).ToList();
                        
                        if (conflicts.Any())
                        {
                            logger.LogWarning("Found {Count} shadow property conflicts in DbContext:", conflicts.Count);
                            foreach (var conflict in conflicts)
                            {
                                logger.LogWarning("Entity {EntityType} has conflicting navigations to {TargetType}: {Navigations}",
                                    conflict.EntityType,
                                    conflict.TargetEntityType,
                                    string.Join(", ", conflict.ConflictingNavigations));
                            }
                        }
                        else
                        {
                            logger.LogInformation("No shadow property conflicts detected in DbContext");
                        }
                    }
                    
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
                // Enable shadow property analyzer middleware in development environment
                app.UseShadowPropertyAnalyzer();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseForwardedHeaders();

            app.UseMaintenanceMode();

            app.UseInvitationRedirect();

            // Add Content Security Policy and other security headers
            app.UseContentSecurityPolicy();

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
                            
                            // Register storage maintenance jobs
                            Log.Information("Registering storage maintenance jobs");
                            var storageMaintenanceService = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWeb.Services.Hangfire.HangfireStorageMaintenanceService>();
                            storageMaintenanceService.RegisterMaintenanceJobs();
                            
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

                    // Initialize JobFilterAttributeUtils with the service provider
                    JobFilterAttributeUtils.SetServiceProvider(scope.ServiceProvider);
                    
                    // Register the retry backoff attribute globally
                    GlobalJobFilters.Filters.Add(new JobRetryBackoffAttribute());
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing Hangfire schema and registering jobs");
            }

            app.UseHttpMetrics();

            app.UseHttpsRedirection();

            // Configure response compression
            app.UseResponseCompression();
            app.UseCompressionAnalytics();

            // Configure static files with proper cache headers
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".webp"] = "image/webp";
            provider.Mappings[".woff"] = "font/woff";
            provider.Mappings[".woff2"] = "font/woff2";
            provider.Mappings[".ttf"] = "font/ttf";
            provider.Mappings[".eot"] = "application/vnd.ms-fontobject";
            provider.Mappings[".otf"] = "font/otf";
            provider.Mappings[".map"] = "application/json";

            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = provider,
                OnPrepareResponse = ctx =>
                {
                    // Disable caching for development
                    if (app.Environment.IsDevelopment())
                    {
                        ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store";
                        return;
                    }

                    // Set cache control headers for production
                    var maxAge = TimeSpan.FromDays(7); // Default to 7 days
                    var filePath = ctx.File.PhysicalPath;

                    if (filePath != null)
                    {
                        if (filePath.EndsWith(".css") || filePath.EndsWith(".js"))
                        {
                            maxAge = TimeSpan.FromDays(7);
                        }
                        else if (filePath.EndsWith(".min.css") || filePath.EndsWith(".min.js"))
                        {
                            maxAge = TimeSpan.FromDays(30);
                        }
                        else if (filePath.EndsWith(".jpg") || filePath.EndsWith(".png") || 
                                 filePath.EndsWith(".gif") || filePath.EndsWith(".ico") ||
                                 filePath.EndsWith(".svg") || filePath.EndsWith(".webp"))
                        {
                            maxAge = TimeSpan.FromDays(30);
                        }
                        else if (filePath.EndsWith(".woff") || filePath.EndsWith(".woff2") || 
                                 filePath.EndsWith(".ttf") || filePath.EndsWith(".eot") ||
                                 filePath.EndsWith(".otf"))
                        {
                            maxAge = TimeSpan.FromDays(365);
                        }
                    }

                    // Add immutable directive for versioned files
                    var path = ctx.Context.Request.Path.Value?.ToLower() ?? "";
                    if (path.Contains(".v") || path.Contains(".hash"))
                    {
                        ctx.Context.Response.Headers["Cache-Control"] = $"public, max-age={maxAge.TotalSeconds}, immutable";
                    }
                    else
                    {
                        ctx.Context.Response.Headers["Cache-Control"] = $"public, max-age={maxAge.TotalSeconds}";
                    }

                    // Add Vary header for proper caching with compression
                    ctx.Context.Response.Headers["Vary"] = "Accept-Encoding";
                }
            });

            app.UseStaticAssetCacheHeaders();

            app.UseIpRateLimiting();
            
            app.UseRateLimitBypass();

            app.UseCors("ProductionDomainPolicy");

            app.UseRouting();

            app.UseOutputCache();

            // Add OutputCache metrics and partitioning middleware
            app.UseOutputCacheMetrics();

            app.UseSession();

            app.UseVersionLogging();

            app.UseRequestLogging(app.Environment.IsProduction());

            app.UseAuthentication();
            app.UseAuthorization();

            // Add cache headers for static assets
            app.UseStaticAssetCacheHeaders();

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

            app.MapHealthChecks("/health/pool", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("pool"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            }).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

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

    /// <summary>
    /// Startup filter to configure job filter attribute utils early in the pipeline
    /// </summary>
    public class JobFilterAttributeStartupFilter : IStartupFilter
    {
        private readonly IServiceProvider _serviceProvider;

        public JobFilterAttributeStartupFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                // Set the service provider to be used by job filter attributes
                JobFilterAttributeUtils.SetServiceProvider(_serviceProvider);
                
                // Call the next configure action
                next(builder);
            };
        }
    }
}
