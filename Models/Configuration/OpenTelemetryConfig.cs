using System;
using System.Collections.Generic;

namespace WorkoutTrackerWeb.Models.Configuration
{
    public class OpenTelemetryConfig
    {
        public bool Enabled { get; set; } = true;
        public string ServiceName { get; set; } = "WorkoutTracker";
        public string ServiceVersion { get; set; } = "1.0.0";
        public string OtlpExporterEndpoint { get; set; } = "http://tempo:4317";
        public bool ConsoleExporterEnabled { get; set; } = false;
        public double SamplingProbability { get; set; } = 1.0;
        public IList<string> Sources { get; set; } = new List<string>();
    }
}
