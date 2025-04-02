using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace WorkoutTrackerWeb.Pages.Sessions
{
    public class CreateModel : UserNamePageModel //PageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;

        public CreateModel(WorkoutTrackerweb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            //ViewData["UserId"] = new SelectList(_context.User, "UserId", "UserId");
            PopulateUserNameDropDownList(_context);
            return Page();
        }

        [BindProperty]
        public Session Session { get; set; }
        public async Task<IActionResult> OnPostAsync()
        {
            var emptySession = new Session
            {
                Name = string.Empty, // Provide a default or meaningful value
                datetime = DateTime.Now, // Provide a default or meaningful value
                User = new User { Name = "DefaultUserName" }, // Initialize with a new User object and set the required Name property
                UserId = 1 // Set a default UserId or handle it as per your logic
            };

            if (await TryUpdateModelAsync<Session>(
                 emptySession,
                 "Session",   // Prefix for form value.
                 s => s.datetime, s => s.Name, s => s.User, s => s.UserId))
            {
                _context.Session.Add(emptySession);
                await _context.SaveChangesAsync();
                return RedirectToPage("./Index");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Session.Add(Session);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
