using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace WorkoutTrackerWeb.Data
{
    /// <summary>
    /// Design-time services for Entity Framework Core to enhance the migration creation process.
    /// This class is automatically discovered by EF Core when running migrations from command-line.
    /// </summary>
    public class DbContextDesignTimeServices : IDesignTimeServices
    {
        /// <summary>
        /// Configure design-time services
        /// </summary>
        /// <param name="serviceCollection">The service collection to configure.</param>
        public void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
        {
            // Register any design-time services here
            // We've removed the custom validator to fix build issues
        }
    }
}