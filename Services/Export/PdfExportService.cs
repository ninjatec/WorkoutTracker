using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WorkoutTrackerWeb.Services.Export;
using WorkoutTrackerWeb.ViewModels.Dashboard;

namespace WorkoutTrackerWeb.Services.Export
{
    public class PdfExportService : IPdfExportService
    {
        private readonly ILogger<PdfExportService> _logger;

        public PdfExportService(ILogger<PdfExportService> logger)
        {
            _logger = logger;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateDashboardPdfAsync(
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
                // Use a separate thread for PDF generation to avoid blocking the main thread
                return await Task.Run(() =>
                {
                    var document = Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(1, Unit.Centimetre);
                            page.PageColor(Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(11));

                            page.Header().Element(ComposeHeader);
                            page.Content().Element(content =>
                            {
                                ComposeContent(content, userName, startDate, endDate, metrics, volumeProgress, workoutFrequency, personalBests);
                            });

                            page.Footer().AlignCenter().Text(text =>
                            {
                                text.Span("Generated on ");
                                text.Span(DateTime.Now.ToString("F", CultureInfo.InvariantCulture));
                                text.Span(" | Page ");
                                text.CurrentPageNumber();
                                text.Span(" of ");
                                text.TotalPages();
                            });
                        });
                    });

                    using (var stream = new MemoryStream())
                    {
                        document.GeneratePdf(stream);
                        return stream.ToArray();
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dashboard PDF export");
                throw;
            }
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Workout Progress Dashboard")
                        .Bold().FontSize(20).FontColor(Colors.Blue.Medium);
                });
            });
        }

        private void ComposeContent(IContainer container, string userName, DateTime startDate, DateTime endDate, 
            DashboardMetrics metrics, List<ChartData> volumeProgress, List<ChartData> workoutFrequency, 
            List<PersonalBest> personalBests)
        {
            container.Column(column =>
            {
                column.Item().Element(c => ReportInfoSection(c, userName, startDate, endDate));
                column.Item().PaddingVertical(10).Element(c => MetricsSection(c, metrics));
                
                if (personalBests.Any())
                {
                    column.Item().PaddingVertical(10).Element(c => PersonalBestsSection(c, personalBests));
                }
            });
        }

        private static void ReportInfoSection(IContainer container, string userName, DateTime startDate, DateTime endDate)
        {
            container.Column(column =>
            {
                column.Item().PaddingBottom(5).Text(text =>
                {
                    text.Span("User: ").SemiBold();
                    text.Span(userName);
                });
                column.Item().PaddingBottom(5).Text(text =>
                {
                    text.Span("Report Period: ").SemiBold();
                    text.Span($"{startDate:d} to {endDate:d}");
                });
                column.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            });
        }

        private static void MetricsSection(IContainer container, DashboardMetrics metrics)
        {
            container.Column(column =>
            {
                column.Item().PaddingBottom(10).Text("Workout Summary").FontSize(14).SemiBold();

                column.Item().PaddingBottom(5).Grid(grid =>
                {
                    grid.Columns(4);
                    grid.Item(2).Text(text =>
                    {
                        text.Span("Total Workouts: ").SemiBold();
                        text.Span($"{metrics.TotalWorkouts}");
                    });
                    grid.Item(2).Text(text =>
                    {
                        text.Span("Total Volume: ").SemiBold();
                        text.Span($"{metrics.TotalVolume:N0} kg");
                    });
                    grid.Item(2).Text(text =>
                    {
                        text.Span("Total Calories: ").SemiBold();
                        text.Span($"{metrics.TotalCalories:N0}");
                    });
                    grid.Item(2).Text(text =>
                    {
                        text.Span("Avg. Duration: ").SemiBold();
                        text.Span($"{metrics.AverageDuration.TotalMinutes:N0} min");
                    });
                });

                column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            });
        }

        private static void PersonalBestsSection(IContainer container, List<PersonalBest> personalBests)
        {
            container.Column(column =>
            {
                column.Item().PaddingBottom(10).Text("Personal Bests").FontSize(14).SemiBold();

                column.Item().Table(table =>
                {
                    // Define columns
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(150);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(100);
                        columns.RelativeColumn();
                    });

                    // Add header
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Exercise").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Weight").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Reps").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Est. 1RM").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Date Achieved").SemiBold();
                    });

                    // Add data rows
                    foreach (var pb in personalBests)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(2).Text(pb.ExerciseName);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(2).Text($"{pb.Weight} kg");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(2).Text($"{pb.Reps}");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(2).Text($"{pb.EstimatedOneRM:N1} kg");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(2).Text(pb.AchievedDate.ToShortDateString());
                    }
                });
            });
        }
    }
}
