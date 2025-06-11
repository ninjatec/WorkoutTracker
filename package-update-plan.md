# WorkoutTracker Package Update Plan

## Current Package Status (as of June 11, 2025)

The project is correctly using .NET 9.0 SDK version 9.0.202, which is up to date. There are several packages that could be updated:

### Immediate Updates

1. **Microsoft ASP.NET Core and Entity Framework Core Packages**: Update from 9.0.5 to 9.0.6
   - These are minor updates that include bug fixes and improvements
   - Command: `dotnet add package [Package-Name] --version 9.0.6`
   - Packages to update:
     - Microsoft.AspNetCore.DataProtection.EntityFrameworkCore
     - Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
     - Microsoft.AspNetCore.Identity.EntityFrameworkCore
     - Microsoft.AspNetCore.Identity.UI
     - Microsoft.AspNetCore.OutputCaching.StackExchangeRedis
     - Microsoft.AspNetCore.SignalR.StackExchangeRedis
     - Microsoft.EntityFrameworkCore.Design
     - Microsoft.EntityFrameworkCore.SQLite
     - Microsoft.EntityFrameworkCore.SqlServer
     - Microsoft.EntityFrameworkCore.Tools
     - Microsoft.Extensions.Caching.SqlServer
     - Microsoft.Extensions.Caching.StackExchangeRedis
     - Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore

2. **Minor Package Updates**:
   - CsvHelper: 33.0.1 → 33.1.0
   - Polly: 8.5.2 → 8.6.0
   - QuestPDF: 2025.5.0 → 2025.5.1
   - SixLabors.ImageSharp: 3.1.8 → 3.1.9
   - StackExchange.Redis: 2.8.37 → 2.8.41

### Updates to Test in Development First

2. **Hangfire.Console.Extensions**: Update from 1.0.5 to 2.0.0
   - This is a major version update that might contain breaking changes
   - Command: `dotnet add package Hangfire.Console.Extensions --version 2.0.0`
   - Review release notes at: https://www.nuget.org/packages/Hangfire.Console.Extensions

### Updates to Consider With Caution

3. **Microsoft.Data.SqlClient**: Update from 5.2.2 to 6.0.2
   - Major version update that may contain breaking changes
   - Since this package is directly used in your code, careful testing is required
   - Command: `dotnet add package Microsoft.Data.SqlClient --version 6.0.2`
   - Review release notes at: https://github.com/dotnet/SqlClient/tree/main/release-notes

### OpenTelemetry Packages with Version Resolution Issues

The following OpenTelemetry packages are currently in beta/RC status for .NET 9.0:

4. **OpenTelemetry.Instrumentation.EntityFrameworkCore**: Currently at 1.0.0-rc9.14, resolving to 1.10.0-beta.1
5. **OpenTelemetry.Instrumentation.SqlClient**: Currently at 1.12.0-beta.1
6. **OpenTelemetry.Instrumentation.StackExchangeRedis**: Currently at 1.12.0-beta.1

**Recommendation**: Keep monitoring for stable releases of these OpenTelemetry packages for .NET 9.0. Once stable versions are available, update to them.

## Additional Observations

- The Hangfire library has multiple methods marked as obsolete that should be updated using the newer recommended methods
- There are several code style and nullable reference type warnings that could be addressed to improve code quality

## Update Execution Plan

1. Create a Git branch for package updates: `git checkout -b package-updates-2025-06-11`
2. Update the Microsoft ASP.NET Core and Entity Framework Core packages to 9.0.6
3. Update the minor version packages (CsvHelper, Polly, QuestPDF, etc.)
4. Build and test the application
5. If successful, commit the changes: `git commit -m "Update packages to latest versions (June 2025)"`
6. Create separate branches for testing the major updates (Hangfire.Console.Extensions and Microsoft.Data.SqlClient)
7. After thorough testing, merge the updates into the main branch

## Future Package Maintenance Plan

- Set up a recurring task to check for package updates every month
- Use `dotnet list package --outdated` to find outdated packages
- Prioritize security updates and bug fixes
- For major version updates, always create a separate branch and test thoroughly before merging
