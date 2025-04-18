using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.DataPortability
{
    [Authorize]
    public class ImportModel : PageModel
    {
        private readonly WorkoutDataPortabilityService _portabilityService;
        private readonly UserService _userService;

        public ImportModel(
            WorkoutDataPortabilityService portabilityService,
            UserService userService)
        {
            _portabilityService = portabilityService;
            _userService = userService;
        }

        [BindProperty]
        public IFormFile ImportFile { get; set; }

        [BindProperty]
        public bool SkipExisting { get; set; } = true;

        public string Message { get; set; }
        public bool Success { get; set; }
        public List<string> ImportedItems { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ImportFile == null || ImportFile.Length == 0)
            {
                Message = "Please select a file to import";
                Success = false;
                return Page();
            }

            try
            {
                var userId = await _userService.GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    throw new InvalidOperationException("User not found");
                }

                using var reader = new StreamReader(ImportFile.OpenReadStream());
                var jsonData = await reader.ReadToEndAsync();

                var (success, message, importedItems) = await _portabilityService.ImportUserDataAsync(
                    userId.Value, jsonData, SkipExisting);

                Message = message;
                Success = success;
                ImportedItems = importedItems;

                return Page();
            }
            catch (Exception ex)
            {
                Message = $"Import failed: {ex.Message}";
                Success = false;
                return Page();
            }
        }
    }
}