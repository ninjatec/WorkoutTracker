using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.OutputCaching;

namespace WorkoutTrackerWeb.Services.Dashboard
{
    /// <summary>
    /// Output cache policy for dashboard data
    /// </summary>
    public static class DashboardCachePolicy
    {
        public const string PolicyName = "DashboardCache";
        public const int CacheDurationMinutes = 5;
        
        public static void Configure(OutputCacheOptions options)
        {
            options.AddPolicy(PolicyName, builder =>
            {
                builder.Expire(TimeSpan.FromMinutes(CacheDurationMinutes))
                       .SetVaryByQuery("startDate", "endDate")
                       .Tag("dashboard")
                       .Tag("user")
                       .Tag("content:dashboard"); // Add proper content type tag
            });
        }
    }
}
