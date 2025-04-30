using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.AspNetCore.WebUtilities;

namespace WorkoutTrackerWeb.Middleware
{
    /// <summary>
    /// Middleware to redirect old invitation URL format to the new Razor Pages
    /// </summary>
    /// <remarks>
    /// This middleware intercepts and redirects specially formatted invitation links
    /// to the appropriate RegisterFromInvite or AcceptCoachInvitation pages
    /// </remarks>
    public class InvitationRedirectMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<InvitationRedirectMiddleware> _logger;

        public InvitationRedirectMiddleware(RequestDelegate next, ILogger<InvitationRedirectMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;
            var query = context.Request.QueryString.Value;

            // Check if this is an Identity invitation redirect URL
            if (path != null && path.Equals("/Identity", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(query))
            {
                try
                {
                    // Parse the query string
                    var queryDictionary = QueryHelpers.ParseQuery(query);
                    
                    if (queryDictionary.TryGetValue("page", out var pageValues) && 
                        pageValues.Count > 0 && 
                        pageValues[0].Equals("/Account/RegisterFromInvite", StringComparison.OrdinalIgnoreCase))
                    {
                        // Extract the necessary parameters
                        queryDictionary.TryGetValue("email", out var email);
                        queryDictionary.TryGetValue("token", out var token);
                        queryDictionary.TryGetValue("relationshipId", out var relationshipId);

                        // Build the new URL
                        var redirectUrl = $"/Identity/Account/RegisterFromInvite?email={email}&token={token}&relationshipId={relationshipId}";
                        
                        _logger.LogInformation("Redirecting invitation URL to: {RedirectUrl}", redirectUrl);
                        
                        // Redirect to the correct URL
                        context.Response.Redirect(redirectUrl);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing invitation redirect URL");
                }
            }

            // If not a matching URL or an error occurred, continue with the pipeline
            await _next(context);
        }
    }
}