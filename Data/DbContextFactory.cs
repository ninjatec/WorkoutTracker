using Microsoft.EntityFrameworkCore;

namespace WorkoutTrackerWeb.Data
{
    public class DbContextFactory<TContext> : IDbContextFactory<TContext> where TContext : DbContext
    {
        private readonly DbContextOptions<TContext> _options;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DbContextFactory(DbContextOptions<TContext> options, IHttpContextAccessor httpContextAccessor = null)
        {
            _options = options;
            _httpContextAccessor = httpContextAccessor;
        }

        public TContext CreateDbContext()
        {
            // Try to use the constructor with IHttpContextAccessor if available
            var ctor = typeof(TContext).GetConstructor(new[] { typeof(DbContextOptions<TContext>), typeof(IHttpContextAccessor) });
            if (ctor != null)
            {
                return (TContext)ctor.Invoke(new object[] { _options, _httpContextAccessor });
            }
            // Fallback to options-only constructor
            return (TContext)Activator.CreateInstance(typeof(TContext), _options);
        }
    }
}