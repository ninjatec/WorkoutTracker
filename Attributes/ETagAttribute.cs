using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Attributes
{
    /// <summary>
    /// Action filter that implements ETag support for API resources
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class ETagAttribute : Attribute, IAsyncActionFilter
    {
        private readonly int _cacheDurationSeconds;

        /// <summary>
        /// Creates a new ETag attribute with the specified cache duration in seconds
        /// </summary>
        /// <param name="cacheDurationSeconds">Duration in seconds that the ETag should be considered valid</param>
        public ETagAttribute(int cacheDurationSeconds = 300) // Default 5 minutes
        {
            _cacheDurationSeconds = cacheDurationSeconds;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executedContext = await next();

            if (executedContext.Result is ObjectResult objectResult)
            {
                var response = context.HttpContext.Response;
                
                // Generate ETag from response body
                string etag = GenerateETag(objectResult.Value);
                
                // Add ETag and caching headers - using indexer instead of Add to prevent exceptions if headers exist
                response.Headers["ETag"] = $"\"{etag}\"";
                response.Headers["Cache-Control"] = $"max-age={_cacheDurationSeconds}, private";
                
                // Check If-None-Match header
                var requestETag = context.HttpContext.Request.Headers["If-None-Match"]
                    .FirstOrDefault()?.Replace("\"", "");
                
                // If client has a matching ETag, return 304 Not Modified
                if (!string.IsNullOrEmpty(requestETag) && requestETag == etag)
                {
                    executedContext.Result = new StatusCodeResult(304);
                }
            }
        }

        private string GenerateETag(object value)
        {
            if (value == null)
                return "null";

            // Serialize the object to a JSON string
            var jsonString = JsonSerializer.Serialize(value);
            
            // Compute hash of the JSON string
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(jsonString));
            
            // Convert hash to a Base64 string for the ETag
            return Convert.ToBase64String(hashBytes);
        }
    }
}