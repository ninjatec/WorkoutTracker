using System.ComponentModel.DataAnnotations;

namespace WorkoutTrackerWeb.ViewModels.Blog
{
    public class BlogCategoryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, ErrorMessage = "Category name cannot be longer than 100 characters")]
        public string Name { get; set; }

        [StringLength(100, ErrorMessage = "Slug cannot be longer than 100 characters")]
        public string Slug { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
        public string Description { get; set; }

        public int PostCount { get; set; }
    }
}
