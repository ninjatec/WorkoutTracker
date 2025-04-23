using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace WorkoutTrackerWeb.Services
{
    public class WorkoutDataPortabilityService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly UserService _userService;

        public WorkoutDataPortabilityService(
            WorkoutTrackerWebContext context,
            UserManager<IdentityUser> userManager,
            UserService userService)
        {
            _context = context;
            _userManager = userManager;
            _userService = userService;
        }

        public async Task<WorkoutExport> ExportUserDataAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var user = await _context.User
                .FirstOrDefaultAsync(u => u.UserId == userId);
            
            if (user == null)
                throw new ArgumentException("User not found", nameof(userId));

            var identityUser = await _userManager.FindByIdAsync(user.IdentityUserId);
            
            var sessionsQuery = _context.Session
                .Include(s => s.Sets)
                    .ThenInclude(set => set.ExerciseType)
                .Include(s => s.Sets)
                    .ThenInclude(set => set.Settype)
                .Include(s => s.Sets)
                    .ThenInclude(set => set.Reps)
                .Where(s => s.UserId == userId);

            // Apply date filtering if specified
            if (startDate.HasValue)
                sessionsQuery = sessionsQuery.Where(s => s.datetime >= startDate.Value);
            if (endDate.HasValue)
                sessionsQuery = sessionsQuery.Where(s => s.datetime <= endDate.Value);

            var sessions = await sessionsQuery.OrderBy(s => s.datetime).ToListAsync();

            var exerciseTypes = await _context.ExerciseType.OrderBy(e => e.Name).ToListAsync();
            var setTypes = await _context.Settype.OrderBy(s => s.Name).ToListAsync();

            var export = new WorkoutExport
            {
                ExportDate = DateTime.UtcNow,
                User = new UserExport
                {
                    Name = user.Name,
                    Email = identityUser?.Email
                },
                Sessions = sessions.Select(s => new SessionExport
                {
                    Name = s.Name,
                    DateTime = s.datetime,
                    Sets = s.Sets?.Select(set => new SetExport
                    {
                        Description = set.Description,
                        Notes = set.Notes,
                        ExerciseTypeName = set.ExerciseType.Name,
                        SetTypeName = set.Settype.Name,
                        NumberReps = set.NumberReps,
                        Weight = set.Weight,
                        Reps = set.Reps?.Select(r => new RepExport
                        {
                            Weight = r.weight,
                            RepNumber = r.repnumber,
                            Success = r.success
                        }).ToList() ?? new List<RepExport>()
                    }).ToList() ?? new List<SetExport>()
                }).ToList(),
                ExerciseTypes = exerciseTypes.Select(e => new ExerciseTypeExport
                {
                    Name = e.Name,
                    Description = e.Description
                }).ToList(),
                SetTypes = setTypes.Select(s => new SetTypeExport
                {
                    Name = s.Name,
                    Description = s.Description
                }).ToList()
            };

            return export;
        }

        public async Task<string> ExportUserDataToJsonAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var export = await ExportUserDataAsync(userId, startDate, endDate);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            return JsonSerializer.Serialize(export, options);
        }

        public async Task<(bool success, string message, List<string> importedItems)> ImportUserDataAsync(
            int userId, 
            string jsonData, 
            bool skipExisting = true)
        {
            try
            {
                var import = JsonSerializer.Deserialize<WorkoutExport>(jsonData);
                var importedItems = new List<string>();

                // Import exercise types if they don't exist
                foreach (var exerciseType in import.ExerciseTypes)
                {
                    if (!await _context.ExerciseType.AnyAsync(e => e.Name == exerciseType.Name))
                    {
                        _context.ExerciseType.Add(new ExerciseType
                        {
                            Name = exerciseType.Name,
                            Description = exerciseType.Description
                        });
                        importedItems.Add($"Exercise Type: {exerciseType.Name}");
                    }
                }

                // Import set types if they don't exist
                foreach (var setType in import.SetTypes)
                {
                    if (!await _context.Settype.AnyAsync(s => s.Name == setType.Name))
                    {
                        _context.Settype.Add(new Settype
                        {
                            Name = setType.Name,
                            Description = setType.Description
                        });
                        importedItems.Add($"Set Type: {setType.Name}");
                    }
                }

                await _context.SaveChangesAsync();

                // Import sessions and their related data
                foreach (var sessionExport in import.Sessions)
                {
                    if (skipExisting && await _context.Session.AnyAsync(s => 
                        s.UserId == userId && 
                        s.Name == sessionExport.Name && 
                        s.datetime == sessionExport.DateTime))
                        continue;

                    var session = new Models.Session
                    {
                        Name = sessionExport.Name,
                        datetime = sessionExport.DateTime,
                        UserId = userId
                    };

                    _context.Session.Add(session);
                    await _context.SaveChangesAsync(); // Save to get SessionId

                    importedItems.Add($"Session: {session.Name} ({session.datetime})");

                    foreach (var setExport in sessionExport.Sets)
                    {
                        var exerciseType = await _context.ExerciseType
                            .FirstAsync(e => e.Name == setExport.ExerciseTypeName);
                        var setType = await _context.Settype
                            .FirstAsync(s => s.Name == setExport.SetTypeName);

                        var set = new Set
                        {
                            Description = setExport.Description,
                            Notes = setExport.Notes,
                            ExerciseTypeId = exerciseType.ExerciseTypeId,
                            SettypeId = setType.SettypeId,
                            NumberReps = setExport.NumberReps,
                            Weight = setExport.Weight,
                            SessionId = session.SessionId
                        };

                        _context.Set.Add(set);
                        await _context.SaveChangesAsync(); // Save to get SetId

                        foreach (var repExport in setExport.Reps)
                        {
                            var rep = new Rep
                            {
                                weight = repExport.Weight,
                                repnumber = repExport.RepNumber,
                                success = repExport.Success,
                                SetsSetId = set.SetId
                            };

                            _context.Rep.Add(rep);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return (true, "Import completed successfully", importedItems);
            }
            catch (Exception ex)
            {
                return (false, $"Import failed: {ex.Message}", new List<string>());
            }
        }
    }
}