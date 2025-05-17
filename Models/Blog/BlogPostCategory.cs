using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models.Blog
{
    public class BlogPostCategory
    {
        public int Id { get; set; }

        public int BlogPostId { get; set; }
        
        public int BlogCategoryId { get; set; }

        [ForeignKey("BlogPostId")]
        public BlogPost BlogPost { get; set; }

        [ForeignKey("BlogCategoryId")]
        public BlogCategory BlogCategory { get; set; }
    }
}
