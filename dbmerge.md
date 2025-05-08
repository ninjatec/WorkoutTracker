# Database Context Merge Plan for WorkoutTracker

## Overview
This document outlines the steps required to merge ApplicationDbContext into WorkoutTrackerWebContext and streamline Entity Framework usage in the WorkoutTracker application. This will simplify database management, reduce code complexity, and improve maintainability.

## Current Database Context Structure
- **ApplicationDbContext**: Handles ASP.NET Identity-related data (users, roles, claims)
- **WorkoutTrackerWebContext**: Contains all application-specific data
- **DataProtectionKeysDbContext**: Manages ASP.NET Core data protection keys

## Backup Strategy

### 1. Create Database Backups
```bash
# Create a full backup of the current database
# Replace {ConnectionString} with actual connection parameters
sqlcmd -S {ServerName} -d {DatabaseName} -U {Username} -P {Password} -Q "BACKUP DATABASE [WorkoutTrackerWeb] TO DISK='C:\Backups\WorkoutTrackerWeb_Full_$(date +%Y%m%d_%H%M%S).bak' WITH INIT, COMPRESSION"

# Back up the schema separately
# Using SQL Server Management Studio or similar tool, script the database schema to a file
```

### 2. Export Current Data (If Needed)
```bash
# Using a tool like SqlPackage or SQL Server Management Studio
# Export the data to .bacpac or SQL files
```

## Step 1: Identify and Copy Identity Tables to WorkoutTrackerWebContext

### 1.1 Identity Tables to Migrate
- AspNetUsers
- AspNetRoles
- AspNetUserRoles
- AspNetUserClaims
- AspNetUserLogins
- AspNetUserTokens
- AspNetRoleClaims
- Versions table (if present in ApplicationDbContext)

### 1.2 Create Migration to Add Identity Tables to WorkoutTrackerWebContext
```bash
# Create migration that will add Identity tables to WorkoutTrackerWebContext
dotnet ef migrations add AddIdentityTablesToMainContext --context WorkoutTrackerWebContext
```

## Step 2: Modify WorkoutTrackerWebContext to Inherit from IdentityDbContext

### 2.1 Update WorkoutTrackerWebContext Class
```csharp
// Change from
public class WorkoutTrackerWebContext : DbContext

// To
public class WorkoutTrackerWebContext : IdentityDbContext<AppUser>
```

### 2.2 Add Necessary Using Statements
```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using WorkoutTrackerWeb.Models.Identity;
```

### 2.3 Ensure Base Constructor Call Passes Options
```csharp
public WorkoutTrackerWebContext(DbContextOptions<WorkoutTrackerWebContext> options, 
                               IHttpContextAccessor httpContextAccessor = null)
    : base(options)
{
    _httpContextAccessor = httpContextAccessor;
}
```

## Step 3: Move Custom Models/Properties from ApplicationDbContext

### 3.1 Identify Custom Models in ApplicationDbContext
- Versions (AppVersion)
- Any other custom DbSets or configurations

### 3.2 Add to WorkoutTrackerWebContext
```csharp
// Add any custom DbSets from ApplicationDbContext
public DbSet<AppVersion> Versions { get; set; } = default!;
// ... any other custom models
```

## Step 4: Update Program.cs References

### 4.1 Modify DbContext Registration
```csharp
// Replace existing ApplicationDbContext registration
// FROM:
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    // configuration
);

// TO:
// Remove the ApplicationDbContext registration completely
```

### 4.2 Update Identity Configuration
```csharp
// FROM:
builder.Services
    .AddDefaultIdentity<AppUser>(options => {
        // options config
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddUserValidator<CustomUserValidator>();

// TO:
builder.Services
    .AddDefaultIdentity<AppUser>(options => {
        // options config
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<WorkoutTrackerWebContext>()
    .AddUserValidator<CustomUserValidator>();
```

### 4.3 Update Health Checks
```csharp
// FROM:
.AddDbContextCheck<ApplicationDbContext>("database_health_check", 
    // options
);

// TO:
// Remove the ApplicationDbContext health check or modify it to use WorkoutTrackerWebContext instead
```

### 4.4 Update Seeding Logic
```csharp
// Update any code that uses ApplicationDbContext for seeding:
// FROM:
var context = services.GetRequiredService<ApplicationDbContext>();

// TO:
var context = services.GetRequiredService<WorkoutTrackerWebContext>();
```

## Step 5: Update Data Initialization and Seeding

### 5.1 Update SeedData.cs
```csharp
// FROM:
private static async Task SeedInitialVersion(ApplicationDbContext context)
{
    // seeding code
}

// TO:
private static async Task SeedInitialVersion(WorkoutTrackerWebContext context)
{
    // seeding code
}
```

### 5.2 Update InitializeAsync Method
```csharp
// Modify to remove references to ApplicationDbContext
public static async Task InitializeAsync(IServiceProvider serviceProvider)
{
    using var context = new WorkoutTrackerWebContext(
        serviceProvider.GetRequiredService<DbContextOptions<WorkoutTrackerWebContext>>(),
        serviceProvider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>());

    // No longer need ApplicationDbContext
    // Remove:
    // using var appDbContext = new ApplicationDbContext(
    //     serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

    // Initialize roles and admin users first
    await InitializeRolesAndAdminUser(serviceProvider);
    
    // Update to use WorkoutTrackerWebContext
    // FROM:
    // await SeedInitialVersion(appDbContext);
    // TO:
    await SeedInitialVersion(context);
    
    // Rest of the method remains unchanged
    await SeedExerciseTypes(context);
    await SeedSetTypes(context);
    await SeedHelpContent(context);
    await SeedGlossaryTerms(context);
}
```

## Step 6: Update Data Migration Service

### 6.1 Update DatabaseMigrationService
```csharp
// Find and update any migration service code to use a single context
```

### 6.2 Remove Code for Managing Multiple Contexts
```csharp
// Remove any helper methods or utilities specifically built to manage multiple contexts
```

## Step 7: Clean Up DbContextFactory Classes

### 7.1 Update or Remove WorkoutTrackerWebContextFactory
```csharp
// Simplify if needed, but this class should still work for design-time migrations
```

### 7.2 Remove ApplicationDbContextFactory (if exists)
```bash
# Delete the file if it exists
```

## Step 8: Update Extensions That Reference ApplicationDbContext

### 8.1 Update DatabaseExtensions.cs GetCurrentUserAsync Method
```csharp
// FROM:
// Using ApplicationDbContext to get user info
using (var scope = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
    .AddEntityFrameworkSqlServer()
    .BuildServiceProvider())
{
    // Get connection info from current context to create a consistent connection
    string connectionString = context.Database.GetDbConnection().ConnectionString;
    
    // Create options for ApplicationDbContext which has the AppUser entities
    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
    // ...
}

// TO:
// Use WorkoutTrackerWebContext directly
// User identity data is now in the same context
```

## Step 9: Update Database and Test

### 9.1 Apply Migrations
```bash
# Apply migrations to update the schema
dotnet ef database update --context WorkoutTrackerWebContext
```

### 9.2 Run Application Tests
```bash
# Run tests to verify the change hasn't broken functionality
```

### 9.3 Manually Test Application
- Verify user login works
- Verify registration works
- Ensure all existing functionality still works properly

## Step 10: Remove Custom EF Code

### 10.1 Identify Custom EF Code
- Check for custom query filters
- Check for manual context configurations
- Identify any performance optimizations

### 10.2 Evaluate Each Custom Implementation
Determine if each custom implementation:
- Is necessary or can be replaced with standard EF Core features
- Should be kept because it solves a specific problem
- Can be simplified

### 10.3 Clean Up OnModelCreating
- Remove unnecessary relationship configurations
- Use data annotations where possible instead of fluent API
- Keep necessary query filters but simplify where possible

### 10.4 Remove or Simplify DbContextFactory
- Simplify if factory pattern is still needed
- Remove if standard dependency injection can handle it

## Step 11: Final Cleanup

### 11.1 Remove Unused Code
```bash
# Delete ApplicationDbContext.cs file
```

### 11.2 Remove Unused Migration Files
```bash
# Clean up old migrations that are no longer needed
```

### 11.3 Update Documentation
- Update README.md
- Update any developer documentation regarding database structure

## Troubleshooting

### Common Issues and Solutions
1. **Migration errors**: If migration fails, check for missing foreign key relationships or model validation errors
2. **Identity issues**: Ensure all identity tables are properly migrated and the user data is preserved
3. **Connection string problems**: Verify the connection string is properly configured for the merged context
4. **Performance issues**: Monitor query performance after changes; may need to reintroduce specific optimizations

### Rollback Plan
If serious issues occur:
1. Stop the application
2. Restore the database from backup
3. Revert code changes
4. Return to using separate contexts until issues are resolved
