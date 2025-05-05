using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.IO;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Services.Metrics;

namespace WorkoutTrackerWeb.Middleware
{
    /// <summary>
    /// Middleware to track compression analytics for response compression
    /// </summary>
    public class CompressionAnalyticsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CompressionAnalyticsMiddleware> _logger;
        private readonly CompressionMetricsService _metricsService;

        public CompressionAnalyticsMiddleware(
            RequestDelegate next,
            ILogger<CompressionAnalyticsMiddleware> logger,
            CompressionMetricsService metricsService)
        {
            _next = next;
            _logger = logger;
            _metricsService = metricsService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Create a buffer to hold the original response
            var originalBodyStream = context.Response.Body;
            
            using var responseBuffer = new MemoryStream();
            context.Response.Body = responseBuffer;

            try
            {
                // Continue down the middleware pipeline
                await _next(context);

                // After the request has been processed
                var contentEncoding = context.Response.Headers["Content-Encoding"].ToString();
                var contentLength = context.Response.Headers["Content-Length"].ToString();
                var contentType = context.Response.Headers["Content-Type"].ToString();

                // Only measure if compression was actually applied
                if (!string.IsNullOrEmpty(contentEncoding) && contentEncoding != "identity")
                {
                    responseBuffer.Position = 0;
                    long compressedSize = responseBuffer.Length;
                    
                    // If Content-Length exists, use it as the uncompressed size
                    // Otherwise, make a reasonable estimate based on content type
                    long uncompressedSize;
                    if (long.TryParse(contentLength, out uncompressedSize) && uncompressedSize > 0)
                    {
                        _metricsService.RecordCompression(
                            uncompressedSize, 
                            compressedSize,
                            contentType,
                            contentEncoding);
                        
                        _logger.LogDebug(
                            "Compression applied: {ContentType}, Method: {Method}, Before: {Before} bytes, After: {After} bytes, Ratio: {Ratio:F2}x", 
                            contentType,
                            contentEncoding,
                            uncompressedSize,
                            compressedSize,
                            uncompressedSize / (double)compressedSize);
                    }
                }

                // Copy the response to the original stream and close 
                responseBuffer.Position = 0;
                await responseBuffer.CopyToAsync(originalBodyStream);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
    }
}