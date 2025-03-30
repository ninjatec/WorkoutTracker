using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerweb.Data
{
    public class WorkoutTrackerContext : DbContext
    {
        public WorkoutTrackerContext (DbContextOptions<WorkoutTrackerContext> options)
            : base(options)
        {
        }

        public DbSet<WorkoutTrackerWeb.Models.User> User { get; set; } = default!;
    }
}
