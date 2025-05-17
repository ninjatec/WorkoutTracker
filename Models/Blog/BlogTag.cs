using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkoutTrackerWeb.Models.Blog
{
    public class BlogTag
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Slug { get; set; }

        public virtual ICollection<BlogPostTag> BlogPostTags { get; set; } = new List<BlogPostTag>();
    }
}
