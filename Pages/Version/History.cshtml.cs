using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services.VersionManagement;

namespace WorkoutTrackerWeb.Pages.Version
{
    public class HistoryModel : PageModel
    {
        private readonly IVersionService _versionService;

        public HistoryModel(IVersionService versionService)
        {
            _versionService = versionService;
        }

        public IEnumerable<AppVersion> VersionHistory { get; set; }
        public AppVersion CurrentVersion { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Get current version
            CurrentVersion = await _versionService.GetCurrentVersionAsync();
            
            // Get version history
            VersionHistory = await _versionService.GetVersionHistoryAsync();
            
            return Page();
        }
    }
}