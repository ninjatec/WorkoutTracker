using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Coaching;

namespace WorkoutTrackerWeb.Attributes
{
    /// <summary>
    /// Authorization attribute for coach-specific features.
    /// Can be used to require coach role, and optionally, specific permissions when accessing client data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CoachAuthorizeAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
    {
        private readonly string _permission;
        
        /// <summary>
        /// Requires the user to have the Coach role.
        /// </summary>
        public CoachAuthorizeAttribute() : base("RequireCoachRole")
        {
            _permission = null;
        }
        
        /// <summary>
        /// Requires the user to have the Coach role and the specified permission when accessing client data.
        /// </summary>
        /// <param name="permission">The permission property name on CoachPermission model (e.g., "CanViewWorkouts")</param>
        public CoachAuthorizeAttribute(string permission) : base("RequireCoachRole")
        {
            _permission = permission;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Skip if no permission check is needed
            if (string.IsNullOrEmpty(_permission))
                return;
                
            // Get client ID from route data
            if (!context.RouteData.Values.TryGetValue("clientId", out var clientIdValue) && 
                !context.HttpContext.Request.Query.TryGetValue("clientId", out var clientIdQueryValue))
            {
                context.Result = new ForbidResult();
                return;
            }
            
            string clientId = (clientIdValue ?? clientIdQueryValue.ToString()).ToString();
            if (string.IsNullOrEmpty(clientId))
            {
                context.Result = new ForbidResult();
                return;
            }
            
            // Get current user ID
            string coachId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(coachId))
            {
                context.Result = new ForbidResult();
                return;
            }
            
            // Get db context
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<WorkoutTrackerWebContext>();
            
            // Get the relationship between coach and client
            var relationship = await dbContext.CoachClientRelationships
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.CoachId == coachId && 
                                         r.ClientId == clientId && 
                                         r.Status == RelationshipStatus.Active);
                                         
            if (relationship == null)
            {
                context.Result = new ForbidResult();
                return;
            }
            
            // Check if coach has the required permission
            bool hasPermission = false;
            
            // Use reflection to get the value of the permission property
            var permissionProperty = typeof(CoachPermission).GetProperty(_permission);
            if (permissionProperty != null && permissionProperty.PropertyType == typeof(bool))
            {
                hasPermission = (bool)permissionProperty.GetValue(relationship.Permissions);
            }
            
            if (!hasPermission)
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}