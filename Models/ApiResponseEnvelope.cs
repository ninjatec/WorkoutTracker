using System;
using System.Collections.Generic;

namespace WorkoutTrackerWeb.Models
{
    /// <summary>
    /// Standard response envelope for API responses
    /// </summary>
    /// <typeparam name="T">The type of data in the response</typeparam>
    public class ApiResponseEnvelope<T>
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// The data payload of the response
        /// </summary>
        public T Data { get; set; }
        
        /// <summary>
        /// Optional error message if Success is false
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Metadata for the response (pagination info, performance stats, etc.)
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }
        
        /// <summary>
        /// When the response was generated (UTC)
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Creates a successful response with data
        /// </summary>
        /// <param name="data">The data payload</param>
        /// <param name="metadata">Optional metadata</param>
        /// <returns>A success response envelope</returns>
        public static ApiResponseEnvelope<T> CreateSuccessResponse(T data, Dictionary<string, object> metadata = null)
        {
            return new ApiResponseEnvelope<T>
            {
                Success = true,
                Data = data,
                Metadata = metadata ?? new Dictionary<string, object>()
            };
        }
        
        /// <summary>
        /// Creates an error response
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <param name="metadata">Optional metadata</param>
        /// <returns>An error response envelope</returns>
        public static ApiResponseEnvelope<T> Error(string errorMessage, Dictionary<string, object> metadata = null)
        {
            return new ApiResponseEnvelope<T>
            {
                Success = false,
                ErrorMessage = errorMessage,
                Metadata = metadata ?? new Dictionary<string, object>()
            };
        }
    }
}