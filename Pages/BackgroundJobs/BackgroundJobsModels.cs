using System;
using System.Collections.Generic;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.BackgroundJobs
{
    // View Models for Server Status
    public class ServerStatusViewModel
    {
        public string Id { get; set; }
        public string ServerType { get; set; }
        public string NodeName { get; set; }
        public string PodName { get; set; }
        public int WorkersCount { get; set; }
        public List<QueueViewModel> Queues { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? Heartbeat { get; set; }
        public bool IsActive { get; set; }
    }

    public class QueueViewModel
    {
        public string Name { get; set; }
        public int Length { get; set; }
        public bool IsPaused { get; set; }
    }
    
    // Parameter model for TrainAI import
    public class TrainAIImportData
    {
        public int UserId { get; set; }
        public List<TrainAIWorkout> Workouts { get; set; }
        public string ConnectionId { get; set; }
    }
    
    // Parameter model for Delete All Data
    public class DeleteAllDataParams
    {
        public string UserId { get; set; }
        public string ConnectionId { get; set; }
    }
    
    // New parameter model for JSON import
    public class JsonImportData
    {
        public int UserId { get; set; }
        public string JsonContent { get; set; }
        public bool SkipExisting { get; set; } = true;
        public string ConnectionId { get; set; }
    }
}