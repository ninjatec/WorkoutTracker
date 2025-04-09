using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace WorkoutTrackerweb.Data
{
    public class WorkoutTrackerWebContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _currentUserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public WorkoutTrackerWebContext(DbContextOptions<WorkoutTrackerWebContext> options, 
                                       IHttpContextAccessor httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<WorkoutTrackerWeb.Models.User> User { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Session> Session { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Excercise> Excercise { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Rep> Rep { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Set> Set { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Settype> Settype { get; set; } = default!;

        // Helper method to get the current user's own User record
        public async Task<User> GetCurrentUserAsync()
        {
            if (string.IsNullOrEmpty(_currentUserId))
                return null;

            return await User.FirstOrDefaultAsync(u => u.IdentityUserId == _currentUserId);
        }

        // Filter data based on current user
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // No global query filters on User table - we need to see all users for admin purposes
            // But we'll add filters to child entities

            // Sessions are filtered by the current user
            modelBuilder.Entity<Session>()
                .HasQueryFilter(s => _currentUserId == null || s.User.IdentityUserId == _currentUserId);

            // Exercises are filtered by the current user (via Session)
            modelBuilder.Entity<Excercise>()
                .HasQueryFilter(e => _currentUserId == null || e.Session.User.IdentityUserId == _currentUserId);

            // Sets are filtered by the current user (via Exercise -> Session)
            modelBuilder.Entity<Set>()
                .HasQueryFilter(s => _currentUserId == null || s.Exercise.Session.User.IdentityUserId == _currentUserId);

            // Reps are filtered by the current user (via Set -> Exercise -> Session)
            modelBuilder.Entity<Rep>()
                .HasQueryFilter(r => _currentUserId == null || r.Sets.Exercise.Session.User.IdentityUserId == _currentUserId);

            // Configure cascade delete for Rep-Set relationship
            modelBuilder.Entity<Set>()
                .HasMany(s => s.Reps)
                .WithOne(r => r.Sets)
                .HasForeignKey(r => r.SetsSetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
