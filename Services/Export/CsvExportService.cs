using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.ViewModels.Dashboard;

namespace WorkoutTrackerWeb.Services.Export
{
    public class CsvExportService : ICsvExportService
    {
        private readonly ILogger<CsvExportService> _logger;

        public CsvExportService(ILogger<CsvExportService> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> GenerateDashboardCsvAsync(
            string userName,
            DateTime startDate,
            DateTime endDate,
            DashboardMetrics metrics,
            List<ChartData> volumeProgress,
            List<ChartData> workoutFrequency,
            List<PersonalBest> personalBests)
        {
            try
            {
                // Use a separate thread for CSV generation to avoid blocking the main thread
                return await Task.Run(() =>
                {
                    using (var memoryStream = new MemoryStream())
                    using (var writer = new StreamWriter(memoryStream, Encoding.UTF8))
                    {
                        // Write header information
                        writer.WriteLine($"Workout Progress Dashboard");
                        writer.WriteLine($"User: {userName}");
                        writer.WriteLine($"Report Period: {startDate:d} to {endDate:d}");
                        writer.WriteLine($"Generated on: {DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}");
                        writer.WriteLine();

                        // Write summary metrics
                        writer.WriteLine("WORKOUT SUMMARY");
                        writer.WriteLine($"Total Workouts,{metrics.TotalWorkouts}");
                        writer.WriteLine($"Total Volume,{metrics.TotalVolume:N0} kg");
                        writer.WriteLine($"Total Calories,{metrics.TotalCalories:N0}");
                        writer.WriteLine($"Average Duration,{metrics.AverageDuration.TotalMinutes:N0} min");
                        writer.WriteLine();

                        // Write personal bests if any
                        if (personalBests?.Count > 0)
                        {
                            writer.WriteLine("PERSONAL BESTS");
                            writer.WriteLine("Exercise,Weight,Reps,Estimated 1RM,Date Achieved");

                            foreach (var pb in personalBests)
                            {
                                writer.WriteLine($"{EscapeCsvField(pb.ExerciseName)},{pb.Weight} kg,{pb.Reps},{pb.EstimatedOneRM:N1} kg,{pb.AchievedDate:d}");
                            }
                            writer.WriteLine();
                        }

                        // Write volume progress if any
                        if (volumeProgress?.Count > 0)
                        {
                            writer.WriteLine("VOLUME PROGRESS");
                            writer.WriteLine("Date,Volume,Exercise");

                            foreach (var data in volumeProgress)
                            {
                                writer.WriteLine($"{data.Date:d},{data.Value},{EscapeCsvField(data.Label)}");
                            }
                            writer.WriteLine();
                        }

                        // Write workout frequency if any
                        if (workoutFrequency?.Count > 0)
                        {
                            writer.WriteLine("WORKOUT FREQUENCY");
                            writer.WriteLine("Date,Workouts");

                            foreach (var data in workoutFrequency)
                            {
                                writer.WriteLine($"{data.Date:d},{data.Value}");
                            }
                        }

                        writer.Flush();
                        return memoryStream.ToArray();
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dashboard CSV export");
                throw;
            }
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            // If field contains comma, double quote, or newline, wrap in quotes and escape quotes
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }
    }
}
