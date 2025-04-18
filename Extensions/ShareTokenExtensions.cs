using Microsoft.AspNetCore.Http;
using System;
using WorkoutTrackerWeb.Dtos;

namespace WorkoutTrackerWeb.Extensions
{
    public static class ShareTokenExtensions
    {
        /// <summary>
        /// Gets the validated ShareToken from the HttpContext
        /// </summary>
        /// <param name="httpContext">The HttpContext</param>
        /// <returns>The validated ShareToken or null if not found</returns>
        public static ShareTokenDto GetShareToken(this HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (httpContext.Items.TryGetValue("ShareToken", out var shareToken) && shareToken is ShareTokenDto token)
            {
                return token;
            }

            return null;
        }
    }
}