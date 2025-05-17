using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Models.Blog
{
    public class BlogPost
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(200)]
        public string Slug { get; set; }

        [Required]
        public string Content { get; set; }

        [StringLength(500)]
        public string Summary { get; set; }

        [StringLength(255)]
        public string ImageUrl { get; set; }

        [Required]
        public string AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        public AppUser Author { get; set; }

        public bool Published { get; set; }

        public DateTime? PublishedOn { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int ViewCount { get; set; }

        public virtual ICollection<BlogPostCategory> BlogPostCategories { get; set; } = new List<BlogPostCategory>();
        
        public virtual ICollection<BlogPostTag> BlogPostTags { get; set; } = new List<BlogPostTag>();
    }
}
