using System;
using System.ComponentModel.DataAnnotations;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Dtos
{
    public class ShareTokenDto
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public int AccessCount { get; set; }
        public int? MaxAccessCount { get; set; }
        public bool AllowSessionAccess { get; set; }
        public bool AllowReportAccess { get; set; }
        public bool AllowCalculatorAccess { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int? WorkoutSessionId { get; set; }
        public string WorkoutSessionName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsValid { get; set; }
        public int DaysUntilExpiration { get; set; }
        public bool HasUsageLimits { get; set; }
        public int? RemainingUses { get; set; }
        public int SessionId { get; set; }
        public string SessionName { get; set; }
    }

    public class CreateShareTokenRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Expiry days must be at least 1")]
        public int ExpiryDays { get; set; } = 7;
        
        public int? SessionId { get; set; }
        public int? WorkoutSessionId { get; set; }
        
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        public int? MaxAccessCount { get; set; }
        
        public bool AllowSessionAccess { get; set; } = true;
        public bool AllowReportAccess { get; set; } = true;
        public bool AllowCalculatorAccess { get; set; } = true;
    }

    public class UpdateShareTokenRequest
    {
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Expiry days must be at least 0")]
        public int ExpiryDays { get; set; }
        
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        public int? MaxAccessCount { get; set; }
        
        public bool AllowSessionAccess { get; set; }
        public bool AllowReportAccess { get; set; }
        public bool AllowCalculatorAccess { get; set; }
    }

    public class ShareTokenValidationRequest
    {
        [Required]
        public string Token { get; set; }
    }

    public class ShareTokenValidationResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public ShareTokenDto ShareToken { get; set; }
    }
}