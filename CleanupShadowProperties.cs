using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;

namespace WorkoutTrackerWeb
{
    /// <summary>
    /// Host service that runs at application startup to verify that no shadow properties
    /// are created for WorkoutExercise-ExerciseType and WorkoutFeedback-WorkoutSession relationships
    /// </summary>
    public class CleanupShadowProperties : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CleanupShadowProperties> _logger;

        public CleanupShadowProperties(IServiceScopeFactory scopeFactory, ILogger<CleanupShadowProperties> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();

            // Log startup verification
            _logger.LogInformation("Verifying entity relationship configurations to prevent shadow properties");
            
            try
            {
                // Verify WorkoutExercise-ExerciseType relationship
                var workoutExerciseType = dbContext.Model.FindEntityType(typeof(WorkoutExercise));
                var exerciseTypeProperty = workoutExerciseType?.FindProperty("ExerciseTypeId1");
                
                if (exerciseTypeProperty != null)
                {
                    _logger.LogWarning(
                        "Found shadow property ExerciseTypeId1 in WorkoutExercise model. " +
                        "This should be fixed in the DbContext ModelCreating configuration.");
                }
                else
                {
                    _logger.LogInformation("No ExerciseTypeId1 shadow property found in WorkoutExercise model");
                }
                
                // Verify WorkoutFeedback-WorkoutSession relationship
                var workoutFeedbackType = dbContext.Model.FindEntityType(typeof(WorkoutFeedback));
                var workoutSessionProperty = workoutFeedbackType?.FindProperty("WorkoutSessionId1");
                
                if (workoutSessionProperty != null)
                {
                    _logger.LogWarning(
                        "Found shadow property WorkoutSessionId1 in WorkoutFeedback model. " +
                        "This should be fixed in the DbContext ModelCreating configuration.");
                }
                else
                {
                    _logger.LogInformation("No WorkoutSessionId1 shadow property found in WorkoutFeedback model");
                }
                
                // Verify the model has no validation errors
                var validationErrors = dbContext.Model.GetAnnotations()
                    .Where(a => a.Name.Contains("ValidationError"))
                    .Select(a => a.Value?.ToString())
                    .Where(v => !string.IsNullOrEmpty(v))
                    .ToList();

                if (validationErrors.Any())
                {
                    foreach (var error in validationErrors)
                    {
                        _logger.LogError($"EF Core model validation error: {error}");
                    }
                }
                else
                {
                    _logger.LogInformation("Entity model validation passed with no errors");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during entity relationship configuration verification");
            }
            
            await Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}