using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkoutTrackerWeb.Models.Blog
{
    public class BlogCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string Slug { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public virtual ICollection<BlogPostCategory> BlogPostCategories { get; set; } = new List<BlogPostCategory>();
    }
}
