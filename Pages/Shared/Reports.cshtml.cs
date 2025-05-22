using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Services.Reports;
using WorkoutTrackerWeb.ViewModels;
using Microsoft.AspNetCore.OutputCaching;

namespace WorkoutTrackerWeb.Pages.Shared
{
    [OutputCache(PolicyName = "SharedWorkoutReports")]
    public class ReportsModel : SharedPageModel
    {
        private readonly IReportsService _reportsService;
        private new readonly ILogger<ReportsModel> _logger;

        public ReportsModel(
            IReportsService reportsService,
            ITokenValidationService tokenValidationService,
            ILogger<ReportsModel> logger)
            : base(tokenValidationService, logger)
        {
            _reportsService = reportsService;
            _logger = logger;
        }

        // Only load personal records on initial page load
        // Chart data will be loaded asynchronously via API calls
        public List<PersonalRecord> PersonalRecords { get; set; } = new List<PersonalRecord>();
        
        // Pagination and filtering
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int ReportPeriod { get; set; } = 90;
        
        // Share token access
        public string ShareToken { get; set; }

        public async Task<IActionResult> OnGetAsync(string token = null, int? pageNumber = 1, int? period = 90)
        {
            // Set token for validation
            Token = token;
            ShareToken = token;
            var isValid = await ValidateShareTokenAsync();
            if (!isValid)
            {
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToPage("./TokenRequired");
                }
                return RedirectToPage("./InvalidToken", new { Message = "Invalid or expired token" });
            }

            // Check if token allows report access
            if (!SharedTokenData.IsValid)
            {
                _logger.LogWarning("Token is not valid");
                return RedirectToPage("./InvalidToken", new { Message = "Invalid or expired token" });
            }
            
            // Check if the token has permission to access reports
            if (!SharedTokenData.AllowReportAccess)
            {
                _logger.LogWarning("Token does not have permission to access reports");
                return RedirectToPage("./AccessDenied", new { Message = "Your share token does not have permission to view reports." });
            }
            
            // Set the page number and period with validation
            CurrentPage = Math.Max(1, pageNumber ?? 1);
            ReportPeriod = period ?? 90;
            
            // Only allow certain period values
            if (ReportPeriod != 30 && ReportPeriod != 60 && ReportPeriod != 90 && ReportPeriod != 120)
            {
                ReportPeriod = 90;
            }

            // Load only personal records data for initial page load
            // Charts will load via API calls
            await LoadPersonalRecordsAsync(SharedTokenData.UserId, CurrentPage);
            
            return Page();
        }

        private async Task LoadPersonalRecordsAsync(int userId, int page)
        {
            try
            {
                // Get personal records with pagination
                var prData = await _reportsService.GetPersonalRecordsAsync(userId, page, 10);
                PersonalRecords = prData.Records;
                TotalPages = prData.TotalPages;
                
                // We no longer preload chart data here - it will be loaded asynchronously via API calls
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading personal records for user {UserId}", userId);
                
                // Initialize empty personal records collection
                PersonalRecords = new List<PersonalRecord>();
                TotalPages = 1;
            }
        }
    }
}