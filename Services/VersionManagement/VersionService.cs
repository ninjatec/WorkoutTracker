using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Services.VersionManagement
{
    public class VersionService : IVersionService
    {
        private readonly ApplicationDbContext _context;

        public VersionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AppVersion> GetCurrentVersionAsync()
        {
            var currentVersion = await _context.Versions
                .FirstOrDefaultAsync(v => v.IsCurrent);

            // If no version is set as current, return the latest version
            if (currentVersion == null)
            {
                currentVersion = await _context.Versions
                    .OrderByDescending(v => v.Major)
                    .ThenByDescending(v => v.Minor)
                    .ThenByDescending(v => v.Patch)
                    .ThenByDescending(v => v.BuildNumber)
                    .FirstOrDefaultAsync();

                // If there are any versions, set the latest as current
                if (currentVersion != null)
                {
                    currentVersion.IsCurrent = true;
                    await _context.SaveChangesAsync();
                }
            }

            return currentVersion;
        }

        public async Task<IEnumerable<AppVersion>> GetVersionHistoryAsync()
        {
            return await _context.Versions
                .OrderByDescending(v => v.Major)
                .ThenByDescending(v => v.Minor)
                .ThenByDescending(v => v.Patch)
                .ThenByDescending(v => v.BuildNumber)
                .ToListAsync();
        }

        public async Task<AppVersion> UpdateVersionAsync(int major, int minor, int patch, int build, string description, string gitCommitHash = null)
        {
            // First, remove current flag from all versions
            var currentVersions = await _context.Versions
                .Where(v => v.IsCurrent)
                .ToListAsync();

            foreach (var version in currentVersions)
            {
                version.IsCurrent = false;
            }

            // Create new version as current
            var newVersion = new AppVersion
            {
                Major = major,
                Minor = minor,
                Patch = patch,
                BuildNumber = build,
                Description = description,
                GitCommitHash = gitCommitHash,
                ReleaseDate = DateTime.UtcNow,
                IsCurrent = true
            };

            _context.Versions.Add(newVersion);
            await _context.SaveChangesAsync();

            return newVersion;
        }

        public async Task<AppVersion> AddVersionHistoryAsync(int major, int minor, int patch, int build, 
            string description, string gitCommitHash = null, string releaseNotes = null)
        {
            var newVersion = new AppVersion
            {
                Major = major,
                Minor = minor,
                Patch = patch,
                BuildNumber = build,
                Description = description,
                GitCommitHash = gitCommitHash,
                ReleaseNotes = releaseNotes,
                ReleaseDate = DateTime.UtcNow,
                IsCurrent = false
            };

            _context.Versions.Add(newVersion);
            await _context.SaveChangesAsync();

            return newVersion;
        }

        public string GetVersionDisplayString()
        {
            var version = GetCurrentVersionAsync().Result;
            if (version != null)
            {
                return version.GetVersionString();
            }
            return "0.0.0.0"; // Default version if none exists
        }
    }
}