# WorkoutTracker Package Update Plan

## Current Package Status (as of May 24, 2025)

The project is correctly using .NET 9.0 and most packages are up to date. There are a few packages that could be updated:

### Immediate Updates

1. **Microsoft.AspNetCore.Session**: Update from 2.3.0 to 9.0.5
   - This is a critical update to match the version of other ASP.NET Core packages
   - Command: `dotnet add package Microsoft.AspNetCore.Session --version 9.0.5`

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

1. Create a Git branch for package updates: `git checkout -b package-updates-2025-05-24`
2. Update the Microsoft.AspNetCore.Session package first
3. Build and test the application
4. If successful, commit the change: `git commit -m "Update Microsoft.AspNetCore.Session to 9.0.5"`
5. Create separate branches for testing the other updates in development
6. After thorough testing, merge the updates into the main branch

## Future Package Maintenance Plan

- Set up a recurring task to check for package updates every month
- Use `dotnet list package --outdated` to find outdated packages
- Prioritize security updates and bug fixes
- For major version updates, always create a separate branch and test thoroughly before merging
