using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Coaching;

namespace WorkoutTrackerWeb.Areas.Identity.Pages.Account
{
    [Authorize]
    public class AcceptCoachInvitationModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<AcceptCoachInvitationModel> _logger;
        private readonly WorkoutTrackerWebContext _context;
        private readonly ICoachingService _coachingService;

        public AcceptCoachInvitationModel(
            UserManager<AppUser> userManager,
            ILogger<AcceptCoachInvitationModel> logger,
            WorkoutTrackerWebContext context,
            ICoachingService coachingService)
        {
            _userManager = userManager;
            _logger = logger;
            _context = context;
            _coachingService = coachingService;
        }

        [TempData]
        public string StatusMessage { get; set; }
        
        [TempData]
        public string StatusMessageType { get; set; }
        
        public string CoachName { get; set; }
        
        public string InvitationMessage { get; set; }
        
        public int RelationshipId { get; set; }
        
        public string Token { get; set; }

        public async Task<IActionResult> OnGetAsync(int relationshipId, string token)
        {
            if (relationshipId <= 0 || string.IsNullOrEmpty(token))
            {
                StatusMessageType = "error";
                StatusMessage = "Invalid invitation parameters.";
                return RedirectToPage("/Index");
            }
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                StatusMessageType = "error";
                StatusMessage = "Unable to load user account.";
                return RedirectToPage("/Index");
            }
            
            // Check if the relationship exists and is valid
            var relationship = await _context.CoachClientRelationships
                .Include(r => r.Coach)
                .Include(r => r.Notes)
                .FirstOrDefaultAsync(r => r.Id == relationshipId && 
                                          r.InvitationToken == token && 
                                          r.Status == RelationshipStatus.Pending);
                                         
            if (relationship == null)
            {
                StatusMessageType = "error";
                StatusMessage = "The invitation link is invalid or has expired.";
                return RedirectToPage("/Index");
            }
            
            // Check if the invitation has expired
            if (relationship.InvitationExpiryDate.HasValue && relationship.InvitationExpiryDate.Value < DateTime.UtcNow)
            {
                StatusMessageType = "error";
                StatusMessage = "This invitation has expired. Please contact your coach for a new invitation.";
                return RedirectToPage("/Index");
            }
            
            // Check if the email matches the invited email
            if (user.Email != relationship.InvitedEmail)
            {
                StatusMessageType = "error";
                StatusMessage = "This invitation was not sent to your email address.";
                return RedirectToPage("/Index");
            }
            
            // Get coach name for display
            if (relationship.Coach != null)
            {
                CoachName = relationship.Coach.UserName;
            }
            
            // Get invitation message if available
            if (relationship.Notes != null && relationship.Notes.Count > 0)
            {
                var messageNote = relationship.Notes.FirstOrDefault(n => n.Content.StartsWith("Invitation message:"));
                if (messageNote != null)
                {
                    InvitationMessage = messageNote.Content.Replace("Invitation message: ", "");
                }
            }
            
            // Store values for the form
            RelationshipId = relationshipId;
            Token = token;
            
            return Page();
        }
        
        public async Task<IActionResult> OnPostAcceptAsync(int relationshipId, string token)
        {
            if (relationshipId <= 0 || string.IsNullOrEmpty(token))
            {
                StatusMessageType = "error";
                StatusMessage = "Invalid invitation parameters.";
                return RedirectToPage("/Index");
            }
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                StatusMessageType = "error";
                StatusMessage = "Unable to load user account.";
                return RedirectToPage("/Index");
            }
            
            // Check if the relationship exists and is valid
            var relationship = await _context.CoachClientRelationships
                .Include(r => r.Coach)
                .FirstOrDefaultAsync(r => r.Id == relationshipId && 
                                          r.InvitationToken == token && 
                                          r.Status == RelationshipStatus.Pending);
                                         
            if (relationship == null)
            {
                StatusMessageType = "error";
                StatusMessage = "The invitation link is invalid or has expired.";
                return RedirectToPage("/Index");
            }
            
            // Check if the invitation has expired
            if (relationship.InvitationExpiryDate.HasValue && relationship.InvitationExpiryDate.Value < DateTime.UtcNow)
            {
                StatusMessageType = "error";
                StatusMessage = "This invitation has expired. Please contact your coach for a new invitation.";
                return RedirectToPage("/Index");
            }
            
            // Check if the email matches the invited email
            if (user.Email != relationship.InvitedEmail)
            {
                StatusMessageType = "error";
                StatusMessage = "This invitation was not sent to your email address.";
                return RedirectToPage("/Index");
            }
            
            // Verify that a corresponding user profile exists
            var workoutUser = await _context.User
                .FirstOrDefaultAsync(u => u.IdentityUserId == user.Id);
            
            // Use a transaction to ensure data consistency
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // If workoutUser doesn't exist, create it
                if (workoutUser == null)
                {
                    _logger.LogInformation("Creating missing User profile for identity user {UserId}", user.Id);
                    workoutUser = new Models.User
                    {
                        IdentityUserId = user.Id,
                        Name = user.UserName // Use username as fallback name
                    };
                    _context.User.Add(workoutUser);
                    await _context.SaveChangesAsync();
                }
                
                // Update the relationship
                relationship.ClientId = user.Id;
                relationship.Status = RelationshipStatus.Active;
                relationship.StartDate = DateTime.UtcNow;
                relationship.LastModifiedDate = DateTime.UtcNow;
                
                _context.CoachClientRelationships.Update(relationship);
                await _context.SaveChangesAsync();
                
                // Commit the transaction
                await transaction.CommitAsync();
                
                StatusMessageType = "success";
                StatusMessage = $"You are now connected with {relationship.Coach?.UserName ?? "your coach"}.";
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "Error accepting coach invitation for relationship {RelationshipId}", relationshipId);
                
                // Roll back the transaction
                await transaction.RollbackAsync();
                
                StatusMessageType = "error";
                StatusMessage = "An error occurred while accepting the invitation. Please try again or contact support.";
            }
            
            return RedirectToPage("/Index");
        }
        
        public async Task<IActionResult> OnPostRejectAsync(int relationshipId, string token)
        {
            if (relationshipId <= 0 || string.IsNullOrEmpty(token))
            {
                StatusMessageType = "error";
                StatusMessage = "Invalid invitation parameters.";
                return RedirectToPage("/Index");
            }
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                StatusMessageType = "error";
                StatusMessage = "Unable to load user account.";
                return RedirectToPage("/Index");
            }
            
            // Check if the relationship exists and is valid
            var relationship = await _context.CoachClientRelationships
                .Include(r => r.Coach)
                .FirstOrDefaultAsync(r => r.Id == relationshipId && 
                                          r.InvitationToken == token && 
                                          r.Status == RelationshipStatus.Pending);
                                         
            if (relationship == null)
            {
                StatusMessageType = "error";
                StatusMessage = "The invitation link is invalid or has expired.";
                return RedirectToPage("/Index");
            }
            
            // Check if the email matches the invited email
            if (user.Email != relationship.InvitedEmail)
            {
                StatusMessageType = "error";
                StatusMessage = "This invitation was not sent to your email address.";
                return RedirectToPage("/Index");
            }
            
            // Use a transaction to ensure data consistency
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Update the relationship
                relationship.Status = RelationshipStatus.Rejected;
                relationship.LastModifiedDate = DateTime.UtcNow;
                
                _context.CoachClientRelationships.Update(relationship);
                await _context.SaveChangesAsync();
                
                // Commit the transaction
                await transaction.CommitAsync();
                
                StatusMessageType = "info";
                StatusMessage = "You have declined the coaching invitation.";
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "Error rejecting coach invitation for relationship {RelationshipId}", relationshipId);
                
                // Roll back the transaction
                await transaction.RollbackAsync();
                
                StatusMessageType = "error";
                StatusMessage = "An error occurred while declining the invitation. Please try again or contact support.";
            }
            
            return RedirectToPage("/Index");
        }
    }
}