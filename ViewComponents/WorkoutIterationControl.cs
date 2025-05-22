using Microsoft.AspNetCore.Mvc;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.ViewComponents
{
    public class WorkoutIterationControl : ViewComponent
    {
        public IViewComponentResult Invoke(WorkoutSession workoutSession)
        {
            return View(workoutSession);
        }
    }
}