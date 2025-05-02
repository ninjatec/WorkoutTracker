using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Dtos;

namespace WorkoutTrackerWeb.Controllers
{
    [AllowAnonymous]
    public class SharedController : Controller
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IShareTokenService _shareTokenService;

        public SharedController(WorkoutTrackerWebContext context, IShareTokenService shareTokenService)
        {
            _context = context;
            _shareTokenService = shareTokenService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Session(int id, string token = null)
        {
            var validationResponse = await ValidateTokenFromRequest(token);
            if (!validationResponse.IsValid)
            {
                if (validationResponse.Message == "No token provided")
                {
                    return View("TokenRequired");
                }
                return View("InvalidToken", validationResponse);
            }

            if (!validationResponse.ShareToken.AllowSessionAccess)
            {
                return View("AccessDenied", new { Message = "Your share token does not have permission to view workout sessions." });
            }

            var workoutSession = await _context.WorkoutSessions
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.WorkoutSets)
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.ExerciseType)
                .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == id && 
                    ws.UserId == validationResponse.ShareToken.UserId);

            if (workoutSession == null)
            {
                return NotFound();
            }

            var user = await _context.User
                .FirstOrDefaultAsync(u => u.UserId == validationResponse.ShareToken.UserId);

            ViewBag.UserName = user?.Name ?? "this user";
            ViewBag.ShareToken = validationResponse.ShareToken;

            // Calculate session statistics
            var exerciseSets = new Dictionary<string, List<WorkoutSet>>();
            int totalSets = 0;
            int totalReps = 0;
            decimal totalVolume = 0;

            foreach (var exercise in workoutSession.WorkoutExercises.OrderBy(we => we.SequenceNum))
            {
                string exerciseName = exercise.ExerciseType?.Name ?? "Unknown Exercise";
                
                if (!exerciseSets.ContainsKey(exerciseName))
                {
                    exerciseSets[exerciseName] = new List<WorkoutSet>();
                }
                
                var sets = exercise.WorkoutSets.OrderBy(ws => ws.SequenceNum).ToList();
                exerciseSets[exerciseName].AddRange(sets);
                
                totalSets += sets.Count;
                totalReps += sets.Sum(s => s.Reps ?? 0);
                totalVolume += sets.Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0));
            }

            ViewBag.ExerciseSets = exerciseSets;
            ViewBag.TotalSets = totalSets;
            ViewBag.TotalReps = totalReps;
            ViewBag.TotalVolume = totalVolume;
            ViewBag.UniqueExercises = exerciseSets.Count;

            return View(workoutSession);
        }

        private async Task<ShareTokenValidationResponse> ValidateTokenFromRequest(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return new ShareTokenValidationResponse 
                { 
                    IsValid = false,
                    Message = "No token provided"
                };
            }

            var shareToken = await _context.ShareToken
                .Include(st => st.User)
                .FirstOrDefaultAsync(st => st.Token == token);

            if (shareToken == null)
            {
                return new ShareTokenValidationResponse
                {
                    IsValid = false,
                    Message = "Invalid token"
                };
            }

            if (!shareToken.IsActive)
            {
                return new ShareTokenValidationResponse
                {
                    IsValid = false,
                    Message = "Token has been revoked"
                };
            }

            if (shareToken.ExpiresAt <= DateTime.UtcNow)
            {
                return new ShareTokenValidationResponse
                {
                    IsValid = false,
                    Message = "Token has expired"
                };
            }

            // Check access count if limit is set
            if (shareToken.MaxAccessCount.HasValue)
            {
                if (shareToken.AccessCount >= shareToken.MaxAccessCount.Value)
                {
                    return new ShareTokenValidationResponse
                    {
                        IsValid = false,
                        Message = "Token has reached maximum usage limit"
                    };
                }

                // Increment access count
                shareToken.AccessCount++;
                await _context.SaveChangesAsync();
            }

            return new ShareTokenValidationResponse
            {
                IsValid = true,
                ShareToken = shareToken
            };
        }
    }

    public class ShareTokenValidationResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public ShareToken ShareToken { get; set; }
    }
}