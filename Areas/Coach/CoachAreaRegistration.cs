using Microsoft.AspNetCore.Mvc.Razor;

namespace WorkoutTrackerWeb.Areas.Coach
{
    public class CoachAreaRegistration : IViewLocationExpander
    {
        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            return viewLocations.Concat(new[]
            {
                "/Areas/Coach/Pages/{1}/{0}.cshtml",
                "/Areas/Coach/Pages/Shared/{0}.cshtml"
            });
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            // No additional values to populate
        }
    }
}