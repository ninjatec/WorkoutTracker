using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog.Events;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Models.Logging;
using WorkoutTrackerWeb.Services.Logging;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Logging
{
    [Authorize(Roles = "Admin")]
    public class ConfigureModel : PageModel
    {
        private readonly ILoggingService _loggingService;

        public ConfigureModel(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        [TempData]
        public string SuccessMessage { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public LogLevelSettings CurrentSettings { get; set; }
        
        public IEnumerable<string> CommonSourceContexts { get; set; }
        
        public IEnumerable<KeyValuePair<string, int>> LogLevelOptions { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateDefaultLevelAsync(int defaultLogLevel)
        {
            try
            {
                var level = (LogEventLevel)defaultLogLevel;
                await _loggingService.SetDefaultLogLevelAsync(level, User.Identity.Name);
                
                SuccessMessage = $"Default log level updated to {level}";
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Error updating default log level: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddOverrideAsync(string sourceContext, int logLevel)
        {
            try
            {
                var level = (LogEventLevel)logLevel;
                await _loggingService.SetLogLevelOverrideAsync(sourceContext, level, User.Identity.Name);
                
                SuccessMessage = $"Log level for {sourceContext} set to {level}";
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Error setting log level override: {ex.Message}";
            }

            return RedirectToPage(new { updated = sourceContext });
        }

        public async Task<IActionResult> OnPostRemoveOverrideAsync(string sourceContext)
        {
            try
            {
                await _loggingService.RemoveLogLevelOverrideAsync(sourceContext, User.Identity.Name);
                
                SuccessMessage = $"Log level override for {sourceContext} removed";
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Error removing log level override: {ex.Message}";
            }

            return RedirectToPage();
        }
        
        // Helper method to get a Bootstrap badge class for a log level
        public string GetLogLevelBadgeClass(LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Verbose => "bg-light text-dark",
                LogEventLevel.Debug => "bg-info",
                LogEventLevel.Information => "bg-success",
                LogEventLevel.Warning => "bg-warning text-dark",
                LogEventLevel.Error => "bg-danger",
                LogEventLevel.Fatal => "bg-dark",
                _ => "bg-secondary"
            };
        }

        private async Task LoadDataAsync()
        {
            CurrentSettings = await _loggingService.GetCurrentSettingsAsync();
            CommonSourceContexts = _loggingService.GetCommonSourceContexts();
            LogLevelOptions = _loggingService.GetLogLevelOptions();
        }
    }
}