using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Data;

namespace WorkoutTrackerWeb.Pages.Sets
{
    public class SettypeNameModel : PageModel
    {
        public SelectList SettypeNameSL { get; set; }

        public void PopulateSettypeNameDropDownList(WorkoutTrackerWeb.Data.WorkoutTrackerWebContext _context, 
            object selectedSettype = null)
        {
            var settypeQuery = from s in _context.Settype
                            orderby s.Name
                            select s;

            SettypeNameSL = new SelectList(settypeQuery.AsNoTracking(), 
                nameof(Models.Settype.SettypeId), 
                nameof(Models.Settype.Name),
                selectedSettype);
        }
    }
}