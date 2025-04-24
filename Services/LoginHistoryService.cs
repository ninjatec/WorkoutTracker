using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using UAParser;

namespace WorkoutTrackerWeb.Services
{
    public class LoginHistoryService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoginHistoryService(
            WorkoutTrackerWebContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        
        /// <summary>
        /// Records a successful login attempt
        /// </summary>
        public async Task RecordSuccessfulLoginAsync(string userId)
        {
            await RecordLoginAttemptAsync(userId, true);
        }
        
        /// <summary>
        /// Records a failed login attempt
        /// </summary>
        public async Task RecordFailedLoginAsync(string userId)
        {
            await RecordLoginAttemptAsync(userId, false);
        }
        
        /// <summary>
        /// Gets the login history for a specific user
        /// </summary>
        public async Task<List<LoginHistory>> GetUserLoginHistoryAsync(string userId, int limit = 20)
        {
            return await _context.LoginHistory
                .Where(lh => lh.IdentityUserId == userId)
                .OrderByDescending(lh => lh.LoginTime)
                .Take(limit)
                .ToListAsync();
        }
        
        /// <summary>
        /// Gets all login history with pagination
        /// </summary>
        public async Task<(List<LoginHistory> items, int totalCount)> GetAllLoginHistoryAsync(
            int page = 1, 
            int pageSize = 20, 
            string userId = null,
            bool? isSuccessful = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _context.LoginHistory.AsQueryable();
            
            // Apply filters
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(lh => lh.IdentityUserId == userId);
            }
            
            if (isSuccessful.HasValue)
            {
                query = query.Where(lh => lh.IsSuccessful == isSuccessful.Value);
            }
            
            if (fromDate.HasValue)
            {
                query = query.Where(lh => lh.LoginTime >= fromDate.Value);
            }
            
            if (toDate.HasValue)
            {
                query = query.Where(lh => lh.LoginTime <= toDate.Value);
            }
            
            // Get total count for pagination
            var totalCount = await query.CountAsync();
            
            // Apply pagination and ordering
            var items = await query
                .OrderByDescending(lh => lh.LoginTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
                
            return (items, totalCount);
        }
        
        private async Task RecordLoginAttemptAsync(string userId, bool isSuccessful)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                // Can't get IP or user agent without HttpContext
                return;
            }
            
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            
            var uaParser = Parser.GetDefault();
            var clientInfo = uaParser.Parse(userAgent);
            
            var loginHistory = new LoginHistory
            {
                IdentityUserId = userId,
                LoginTime = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccessful = isSuccessful,
                DeviceType = clientInfo.Device.Family,
                Platform = clientInfo.OS.Family + " " + clientInfo.OS.Major
            };
            
            _context.LoginHistory.Add(loginHistory);
            await _context.SaveChangesAsync();
        }
    }
}