using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Areas.Admin.ViewModels;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public IndexModel(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public PaginatedList<UserViewModel> Users { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string CurrentFilter { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string RoleFilter { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; }
        
        public string CurrentSort { get; set; }
        public string EmailSort { get; set; }
        public string UsernameSort { get; set; }
        
        public SelectList RoleFilterOptions { get; set; }

        public async Task OnGetAsync(
            string sortOrder,
            string searchString,
            string roleFilter,
            string statusFilter,
            int? pageIndex)
        {
            // Set up sort headers
            CurrentSort = sortOrder;
            EmailSort = string.IsNullOrEmpty(sortOrder) ? "email_desc" : "";
            UsernameSort = sortOrder == "username" ? "username_desc" : "username";
            
            // Handle filtering
            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = CurrentFilter;
            }
            
            CurrentFilter = searchString;
            RoleFilter = roleFilter;
            StatusFilter = statusFilter;
            
            // Set up role filter dropdown
            await SetupRoleFilterOptionsAsync();
            
            // Query users
            var users = _userManager.Users.AsQueryable();
            
            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u => u.Email.Contains(searchString) || 
                                        u.UserName.Contains(searchString));
            }
            
            // Apply role filter
            if (!string.IsNullOrEmpty(roleFilter))
            {
                // We need to filter in-memory since role relationships are not directly queryable
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleFilter);
                var userIdsInRole = usersInRole.Select(u => u.Id);
                users = users.Where(u => userIdsInRole.Contains(u.Id));
            }
            
            // Apply status filter
            if (!string.IsNullOrEmpty(statusFilter))
            {
                switch (statusFilter)
                {
                    case "Active":
                        users = users.Where(u => u.EmailConfirmed && !u.LockoutEnabled);
                        break;
                    case "Locked":
                        users = users.Where(u => u.LockoutEnabled && u.LockoutEnd > DateTimeOffset.UtcNow);
                        break;
                    case "Unconfirmed":
                        users = users.Where(u => !u.EmailConfirmed);
                        break;
                }
            }
            
            // Apply sorting
            users = sortOrder switch
            {
                "email_desc" => users.OrderByDescending(u => u.Email),
                "username" => users.OrderBy(u => u.UserName),
                "username_desc" => users.OrderByDescending(u => u.UserName),
                _ => users.OrderBy(u => u.Email),
            };
            
            // Paginate results
            var pageSize = 10;
            var usersList = await users.ToListAsync();
            
            // Convert to view models
            var userViewModels = new List<UserViewModel>();
            
            foreach (var user in usersList)
            {
                var roles = await _userManager.GetRolesAsync(user);
                
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumber = user.PhoneNumber,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    AccessFailedCount = user.AccessFailedCount,
                    Roles = roles.ToList()
                });
            }
            
            Users = PaginatedList<UserViewModel>.Create(
                userViewModels, 
                pageIndex ?? 1, 
                pageSize);
        }
        
        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            
            var user = await _userManager.FindByIdAsync(id);
            
            if (user == null)
            {
                return NotFound();
            }
            
            // Don't allow deleting the current user
            if (User.Identity.Name == user.UserName)
            {
                ModelState.AddModelError(string.Empty, "You cannot delete your own account.");
                return Page();
            }
            
            // Delete the user
            var result = await _userManager.DeleteAsync(user);
            
            if (result.Succeeded)
            {
                return RedirectToPage("./Index");
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            
            return Page();
        }
        
        private async Task SetupRoleFilterOptionsAsync()
        {
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            RoleFilterOptions = new SelectList(roles, "Name", "Name");
        }
    }
    
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            this.AddRange(items);
        }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public static PaginatedList<T> Create(List<T> source, int pageIndex, int pageSize)
        {
            var count = source.Count;
            var items = source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}