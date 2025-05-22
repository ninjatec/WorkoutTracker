using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkoutTrackerWeb.Models
{
    public class HelpArticle
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Title")]
        public string Title { get; set; } = "";

        [Required]
        [StringLength(200)]
        [Display(Name = "Short Description")]
        public string ShortDescription { get; set; } = "";

        [Required]
        [Display(Name = "Content")]
        public string Content { get; set; } = "";

        [Required]
        [Display(Name = "Category")]
        public int HelpCategoryId { get; set; }
        public HelpCategory Category { get; set; }

        [Display(Name = "Related Articles")]
        public ICollection<HelpArticle> RelatedArticles { get; set; } = new List<HelpArticle>();

        [Display(Name = "Tags")]
        public string Tags { get; set; } = "";

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Last Modified Date")]
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;

        [Display(Name = "Version")]
        [StringLength(20)]
        public string Version { get; set; } = "";

        [Display(Name = "Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Is Featured")]
        public bool IsFeatured { get; set; }

        [Display(Name = "View Count")]
        public int ViewCount { get; set; }

        public string Slug => Title?.ToLower().Replace(" ", "-").Replace("?", "").Replace("&", "and");

        public bool HasVideo { get; set; }
        public string VideoUrl { get; set; } = "";

        public bool IsPrintable { get; set; } = true;
    }
}