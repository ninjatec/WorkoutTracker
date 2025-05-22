using Microsoft.AspNetCore.Builder;

namespace WorkoutTrackerWeb.Middleware
{
    /// <summary>
    /// Extension methods for the invitation redirect middleware
    /// </summary>
    public static class InvitationRedirectMiddlewareExtensions
    {
        /// <summary>
        /// Adds the InvitationRedirectMiddleware to the request pipeline
        /// </summary>
        /// <param name="builder">The application builder</param>
        /// <returns>The application builder with the middleware added</returns>
        public static IApplicationBuilder UseInvitationRedirect(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<InvitationRedirectMiddleware>();
        }
    }
}