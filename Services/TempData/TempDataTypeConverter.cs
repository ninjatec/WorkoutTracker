using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace WorkoutTrackerWeb.Services.TempData
{
    /// <summary>
    /// Extension method to register type-aware TempData services
    /// </summary>
    public static class TempDataTypeConverterExtensions
    {
        /// <summary>
        /// Register TempData processor to fix Guid conversion issues
        /// </summary>
        public static IServiceCollection AddTempDataTypeConverter(this IServiceCollection services)
        {
            // Register our TempData filter to process values before they're stored
            services.AddScoped<GuidTempDataFilter>();
            services.AddMvc(options => {
                // Add our filter to the MVC pipeline
                options.Filters.AddService<GuidTempDataFilter>();
            });
            
            return services;
        }
    }

    /// <summary>
    /// Filter that processes TempData values to handle type conversion issues
    /// </summary>
    public class GuidTempDataFilter : IPageFilter
    {
        private readonly ITempDataDictionary _tempData;

        public GuidTempDataFilter(ITempDataDictionaryFactory tempDataFactory, IHttpContextAccessor httpContextAccessor)
        {
            _tempData = tempDataFactory.GetTempData(httpContextAccessor.HttpContext);
        }

        public void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
            // Process TempData before it's used
            SanitizeTempData();
        }

        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            // No action needed
        }

        public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
        {
            // Process TempData after it's updated
            SanitizeTempData();
        }

        /// <summary>
        /// Convert any Guid values in TempData to strings to prevent type conversion errors
        /// </summary>
        private void SanitizeTempData()
        {
            if (_tempData == null) return;

            // Make a copy of the keys to avoid modifying during enumeration
            var keys = new List<string>(_tempData.Keys);
            
            // Process each key
            foreach (var key in keys)
            {
                if (_tempData[key] is Guid guidValue)
                {
                    // Replace the Guid with its string representation
                    _tempData[key] = guidValue.ToString();
                }
            }
        }
    }
}