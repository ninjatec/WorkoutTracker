using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.RateLimiting
{
    [Authorize(Roles = "Admin")]
    [OutputCache(NoStore = true)]
    public class IpWhitelistModel : PageModel
    {
        private readonly ITokenRateLimiter _rateLimiter;
        private readonly ILogger<IpWhitelistModel> _logger;
        private readonly WorkoutTrackerWebContext _dbContext;

        public IpWhitelistModel(
            ITokenRateLimiter rateLimiter, 
            ILogger<IpWhitelistModel> logger,
            WorkoutTrackerWebContext dbContext)
        {
            _rateLimiter = rateLimiter;
            _logger = logger;
            _dbContext = dbContext;
        }

        [BindProperty]
        [Required(ErrorMessage = "IP address is required")]
        [RegularExpression(@"^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", 
            ErrorMessage = "Please enter a valid IPv4 address")]
        public string NewIpAddress { get; set; }

        [BindProperty]
        [MaxLength(255)]
        public string Description { get; set; }

        public List<WhitelistedIp> WhitelistedIps { get; private set; } = new List<WhitelistedIp>();
        
        public bool HasLoadError { get; private set; }
        public string LoadErrorMessage { get; private set; }

        public async Task OnGetAsync()
        {
            await LoadWhitelistedIpsAsync();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadWhitelistedIpsAsync();
                return Page();
            }

            // Validate IP format
            if (!IPAddress.TryParse(NewIpAddress, out _))
            {
                ModelState.AddModelError("NewIpAddress", "Invalid IP address format");
                await LoadWhitelistedIpsAsync();
                return Page();
            }

            try
            {
                // Try to add the IP directly to the database first as a fallback
                if (await DirectDatabaseAddIpAsync(NewIpAddress, Description))
                {
                    // If successfully added to db, now add to rate limiter cache
                    var added = await _rateLimiter.AddIpToWhitelistAsync(NewIpAddress, Description, User.Identity?.Name);
                    _logger.LogInformation("Admin user {User} added IP {IP} to rate limit whitelist (DB direct: true, Cache update: {CacheResult})", 
                        User.Identity?.Name, NewIpAddress, added);
                    TempData["SuccessMessage"] = $"IP address {NewIpAddress} has been added to the whitelist.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Failed to add IP address {NewIpAddress} to the whitelist. The IP may already be whitelisted.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding IP {IP} to whitelist", NewIpAddress);
                TempData["ErrorMessage"] = $"Error adding IP: {ex.Message}";
            }
            
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveAsync(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return BadRequest("IP address is required");
            }

            try
            {
                // First try to remove from database directly as a fallback
                if (await DirectDatabaseRemoveIpAsync(ipAddress))
                {
                    // If successfully removed from db, now remove from rate limiter cache
                    var removed = await _rateLimiter.RemoveIpFromWhitelistAsync(ipAddress);
                    _logger.LogInformation("Admin user {User} removed IP {IP} from rate limit whitelist (DB direct: true, Cache update: {CacheResult})", 
                        User.Identity?.Name, ipAddress, removed);
                    TempData["SuccessMessage"] = $"IP address {ipAddress} has been removed from the whitelist.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Failed to remove IP address {ipAddress} from the whitelist. The IP may not be in the whitelist.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing IP {IP} from whitelist", ipAddress);
                TempData["ErrorMessage"] = $"Error removing IP: {ex.Message}";
            }
            
            return RedirectToPage();
        }

        private async Task LoadWhitelistedIpsAsync()
        {
            try
            {
                // First try to get IPs through the rate limiter service
                try
                {
                    WhitelistedIps = (await _rateLimiter.GetWhitelistedIpsAsync()).ToList();
                    HasLoadError = false;
                    LoadErrorMessage = null;
                }
                catch (Exception ex)
                {
                    // If the rate limiter service fails, try to get directly from the database
                    _logger.LogWarning(ex, "Error loading IP whitelist from rate limiter service, falling back to direct database access");
                    
                    try
                    {
                        WhitelistedIps = await _dbContext.WhitelistedIps.ToListAsync();
                        HasLoadError = false;
                        LoadErrorMessage = null;
                    }
                    catch (Exception dbEx)
                    {
                        _logger.LogError(dbEx, "Error loading IP whitelist from database");
                        HasLoadError = true;
                        LoadErrorMessage = $"Database error: {dbEx.Message}";
                        WhitelistedIps = new List<WhitelistedIp>();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error loading IP whitelist");
                HasLoadError = true;
                LoadErrorMessage = $"Unhandled error: {ex.Message}";
                WhitelistedIps = new List<WhitelistedIp>();
            }
        }

        // Direct database operations as fallback when service layer has issues
        private async Task<bool> DirectDatabaseAddIpAsync(string ipAddress, string description)
        {
            try
            {
                // Check if IP already exists
                var existingIp = await _dbContext.WhitelistedIps
                    .FirstOrDefaultAsync(w => w.IpAddress == ipAddress);

                if (existingIp != null)
                {
                    return false; // Already exists
                }

                // Add to database
                var whitelistedIp = new WhitelistedIp
                {
                    IpAddress = ipAddress,
                    Description = description,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name
                };

                _dbContext.WhitelistedIps.Add(whitelistedIp);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during direct database add of IP {IP}", ipAddress);
                throw;
            }
        }

        private async Task<bool> DirectDatabaseRemoveIpAsync(string ipAddress)
        {
            try
            {
                // Find the IP in the database
                var existingIp = await _dbContext.WhitelistedIps
                    .FirstOrDefaultAsync(w => w.IpAddress == ipAddress);

                if (existingIp == null)
                {
                    return false; // Doesn't exist
                }

                // Remove from database
                _dbContext.WhitelistedIps.Remove(existingIp);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during direct database removal of IP {IP}", ipAddress);
                throw;
            }
        }
    }
}