using System.ComponentModel.DataAnnotations;

namespace WorkoutTrackerWeb.ViewModels.Blog
{
    public class BlogTagViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tag name is required")]
        [StringLength(50, ErrorMessage = "Tag name cannot be longer than 50 characters")]
        public string Name { get; set; }

        [StringLength(50, ErrorMessage = "Slug cannot be longer than 50 characters")]
        public string Slug { get; set; }

        public int PostCount { get; set; }
    }
}
