using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerweb.Data
{
    public class WorkoutTrackerWebContext : DbContext
    {
        public WorkoutTrackerWebContext (DbContextOptions<WorkoutTrackerWebContext> options)
            : base(options)
        {
        }

        public DbSet<WorkoutTrackerWeb.Models.Session> Session { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Excercise> Excercise { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Rep> Rep { get; set; } = default!;
        public DbSet<WorkoutTrackerWeb.Models.Set> Set { get; set; } = default!;
    }
}
