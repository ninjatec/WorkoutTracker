// Extensions/ServiceCollectionExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using WorkoutTrackerWeb.Services.TempData;
using WorkoutTrackerWeb.Utilities;

namespace WorkoutTrackerWeb.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTempDataTypeConverter(this IServiceCollection services)
    {
        services.AddScoped<GuidTempDataFilter>();
        services.AddMvc(options => 
        {
            options.Filters.AddService<GuidTempDataFilter>();
        });
        return services;
    }
}
