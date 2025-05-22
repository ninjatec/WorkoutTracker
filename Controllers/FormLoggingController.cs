using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Controllers
{
    [Route("api/log")]
    [ApiController]
    public class FormLoggingController : ControllerBase
    {
        private readonly ILogger<FormLoggingController> _logger;

        public FormLoggingController(ILogger<FormLoggingController> logger)
        {
            _logger = logger;
        }

        [HttpPost("formsubmission")]
        public IActionResult LogFormSubmission([FromBody] FormSubmissionLogRequest request)
        {
            try
            {
                // Log the form submission with detailed form values
                _logger.LogInformation("[FormSubmissionLog] Form type: {FormType}, Values: {@FormValues}", 
                    request.FormType, 
                    request.FormValues);

                // For recurrence patterns specifically, add a targeted log
                if (request.FormValues.TryGetValue("recurrencePattern", out var recurrencePattern))
                {
                    // Check if days of week were included (for weekly patterns)
                    List<string> daysOfWeek = null;
                    if (request.FormValues.TryGetValue("daysOfWeek", out var daysValue) && daysValue is JsonElement element)
                    {
                        if (element.ValueKind == JsonValueKind.Array)
                        {
                            daysOfWeek = new List<string>();
                            for (int i = 0; i < element.GetArrayLength(); i++)
                            {
                                daysOfWeek.Add(element[i].GetString());
                            }
                        }
                        else if (element.ValueKind == JsonValueKind.String)
                        {
                            daysOfWeek = new List<string> { element.GetString() };
                        }
                    }

                    // Log recurrence pattern details
                    _logger.LogInformation("[RecurrencePatternLog] Pattern: {Pattern}, DaysOfWeek: {@DaysOfWeek}, ScheduleWorkouts: {ScheduleWorkouts}", 
                        recurrencePattern, 
                        daysOfWeek,
                        request.FormValues.TryGetValue("scheduleWorkouts", out var scheduleWorkouts) ? scheduleWorkouts : false);
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FormSubmissionLog] Error logging form submission: {ErrorMessage}", ex.Message);
                return StatusCode(500, new { success = false, message = "Error logging form submission" });
            }
        }

        public class FormSubmissionLogRequest
        {
            public string FormType { get; set; }
            public Dictionary<string, object> FormValues { get; set; }
        }
    }
}