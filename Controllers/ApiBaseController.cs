using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Controllers
{
    /// <summary>
    /// Base controller for API endpoints with standardized response handling
    /// </summary>
    [ApiController]
    public abstract class ApiBaseController : ControllerBase
    {
        /// <summary>
        /// Creates a successful standard API response with optional metadata
        /// </summary>
        /// <typeparam name="T">Type of response data</typeparam>
        /// <param name="data">The data to return</param>
        /// <param name="metadata">Optional metadata to include</param>
        /// <returns>An API response with standardized format</returns>
        protected IActionResult SuccessResponse<T>(T data, Dictionary<string, object> metadata = null)
        {
            var response = ApiResponseEnvelope<T>.CreateSuccessResponse(data, metadata);
            return Ok(response);
        }

        /// <summary>
        /// Creates an error response with optional metadata
        /// </summary>
        /// <typeparam name="T">Type of response data</typeparam>
        /// <param name="errorMessage">The error message</param>
        /// <param name="statusCode">The HTTP status code</param>
        /// <param name="metadata">Optional metadata to include</param>
        /// <returns>An error response with the specified status code</returns>
        protected IActionResult ErrorResponse<T>(string errorMessage, int statusCode = 500, Dictionary<string, object> metadata = null)
        {
            var response = ApiResponseEnvelope<T>.Error(errorMessage, metadata);
            return StatusCode(statusCode, response);
        }

        /// <summary>
        /// Creates a "Not Found" error response
        /// </summary>
        /// <typeparam name="T">Type of response data</typeparam>
        /// <param name="errorMessage">The error message</param>
        /// <param name="metadata">Optional metadata to include</param>
        /// <returns>A 404 Not Found response</returns>
        protected IActionResult NotFoundResponse<T>(string errorMessage = "Resource not found", Dictionary<string, object> metadata = null)
        {
            return ErrorResponse<T>(errorMessage, 404, metadata);
        }

        /// <summary>
        /// Creates a "Bad Request" error response
        /// </summary>
        /// <typeparam name="T">Type of response data</typeparam>
        /// <param name="errorMessage">The error message</param>
        /// <param name="metadata">Optional metadata to include</param>
        /// <returns>A 400 Bad Request response</returns>
        protected IActionResult BadRequestResponse<T>(string errorMessage, Dictionary<string, object> metadata = null)
        {
            return ErrorResponse<T>(errorMessage, 400, metadata);
        }

        /// <summary>
        /// Creates an "Unauthorized" error response
        /// </summary>
        /// <typeparam name="T">Type of response data</typeparam>
        /// <param name="errorMessage">The error message</param>
        /// <param name="metadata">Optional metadata to include</param>
        /// <returns>A 401 Unauthorized response</returns>
        protected IActionResult UnauthorizedResponse<T>(string errorMessage = "Unauthorized", Dictionary<string, object> metadata = null)
        {
            return ErrorResponse<T>(errorMessage, 401, metadata);
        }

        /// <summary>
        /// Creates a "No Content" response for successful operations that don't return data
        /// </summary>
        /// <returns>A 204 No Content response</returns>
        protected IActionResult NoContentResponse()
        {
            return NoContent();
        }
    }
}