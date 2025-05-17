using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models.Blog
{
    public class BlogPostTag
    {
        public int Id { get; set; }

        public int BlogPostId { get; set; }
        
        public int BlogTagId { get; set; }

        [ForeignKey("BlogPostId")]
        public BlogPost BlogPost { get; set; }

        [ForeignKey("BlogTagId")]
        public BlogTag BlogTag { get; set; }
    }
}
