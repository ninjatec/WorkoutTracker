using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services;
using Microsoft.AspNetCore.SignalR;
using WorkoutTrackerWeb.Hubs;
using System.Threading;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Pages.ExerciseTypes
{
    [Authorize(Roles = "Admin")]
    public class EnrichExercisesModel : PageModel
    {
        private readonly ExerciseTypeService _exerciseService;
        private readonly ILogger<EnrichExercisesModel> _logger;
        private readonly IHubContext<ImportProgressHub> _hubContext;
        private static CancellationTokenSource _cancellationTokenSource;

        public EnrichExercisesModel(
            ExerciseTypeService exerciseService,
            ILogger<EnrichExercisesModel> logger,
            IHubContext<ImportProgressHub> hubContext)
        {
            _exerciseService = exerciseService;
            _logger = logger;
            _hubContext = hubContext;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string JobId { get; set; }

        public (int found, int enriched, int failed)? EnrichmentResult { get; private set; }

        public bool IsJobInProgress => !string.IsNullOrEmpty(JobId);
        
        [BindProperty(SupportsGet = true)]
        public bool AutoSelectMatches { get; set; }
        
        public int PendingSelectionsCount { get; private set; }
        
        public List<PendingExerciseSelection> PendingSelections { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (IsJobInProgress)
            {
                // Check if we have pending selections for this job
                PendingSelectionsCount = await _exerciseService.GetPendingSelectionsCountAsync(JobId);
                
                // If we have pending selections and they're requested in the UI, load them
                if (PendingSelectionsCount > 0 && Request.Query.ContainsKey("showPending"))
                {
                    PendingSelections = await _exerciseService.GetPendingSelectionsAsync(JobId);
                }
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Check if a job is already running
                if (IsJobInProgress && _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                {
                    StatusMessage = "A job is already in progress. Please wait for it to complete.";
                    return Page();
                }

                // Generate a new job ID
                JobId = Guid.NewGuid().ToString();
                _logger.LogInformation("Starting bulk exercise enrichment process with job ID {JobId}", JobId);
                
                // Create a new cancellation token source for this job
                _cancellationTokenSource = new CancellationTokenSource();
                
                // Start the background task
                _ = Task.Run(async () => 
                {
                    try
                    {
                        await _exerciseService.EnrichExercisesBackgroundAsync(JobId, AutoSelectMatches, _cancellationTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in background enrichment task for job {JobId}", JobId);
                    }
                }, _cancellationTokenSource.Token);
                
                StatusMessage = "Enrichment process started. You can monitor progress on this page.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting exercise enrichment job");
                StatusMessage = $"Error: {ex.Message}";
                JobId = null;
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostCancelAsync()
        {
            if (IsJobInProgress && _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _logger.LogInformation("Cancelling exercise enrichment job {JobId}", JobId);
                
                // Request cancellation
                _cancellationTokenSource.Cancel();
                
                // Send cancellation notification via SignalR
                string groupName = $"job_{JobId}";
                var progress = new JobProgress
                {
                    Status = "Cancellation requested. Waiting for current operations to complete...",
                    PercentComplete = 0,
                    ErrorMessage = null
                };
                
                await _hubContext.Clients.Group(groupName).SendAsync("receiveProgress", progress);
                
                StatusMessage = "Cancellation requested. The job will stop after the current operation completes.";
            }
            else
            {
                StatusMessage = "No active job to cancel.";
                JobId = null;
            }
            
            return Page();
        }
        
        public async Task<IActionResult> OnPostResolveSelectionAsync(int pendingSelectionId, int selectedApiExerciseIndex)
        {
            try
            {
                if (!IsJobInProgress)
                {
                    // Instead of returning an error, redirect to the dedicated pending selections page
                    TempData["StatusMessage"] = "The enrichment job is no longer active. You'll be redirected to the pending selections page.";
                    return RedirectToPage("./PendingSelections");
                }
                
                var updatedExercise = await _exerciseService.ResolvePendingSelectionAsync(pendingSelectionId, selectedApiExerciseIndex);
                
                if (updatedExercise != null)
                {
                    StatusMessage = $"Successfully updated exercise '{updatedExercise.Name}' with selected data.";
                    
                    // Check if we still have more pending selections
                    PendingSelectionsCount = await _exerciseService.GetPendingSelectionsCountAsync(JobId);
                    
                    if (PendingSelectionsCount > 0)
                    {
                        // Reload the remaining pending selections
                        PendingSelections = await _exerciseService.GetPendingSelectionsAsync(JobId);
                    }
                    else
                    {
                        StatusMessage += " All selections have been resolved.";
                    }
                }
                else
                {
                    StatusMessage = "Failed to update exercise with selected data.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving pending selection {PendingSelectionId}", pendingSelectionId);
                StatusMessage = $"Error: {ex.Message}";
            }
            
            return Page();
        }

        /// <summary>
        /// Gets the count of all pending selections across all jobs
        /// This helper method allows the view to access pending selections without
        /// directly accessing services
        /// </summary>
        public Task<int> GetAllPendingSelectionsCountAsync()
        {
            return _exerciseService.GetAllPendingSelectionsCountAsync();
        }
    }
}