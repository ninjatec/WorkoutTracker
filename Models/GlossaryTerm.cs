using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkoutTrackerWeb.Models
{
    public class GlossaryTerm
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Term")]
        public string Term { get; set; } = "";

        [Required]
        [Display(Name = "Definition")]
        public string Definition { get; set; } = "";

        [StringLength(500)]
        [Display(Name = "Example")]
        public string Example { get; set; } = "";

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;

        [Display(Name = "Related Terms")]
        public ICollection<GlossaryTerm> RelatedTerms { get; set; } = new List<GlossaryTerm>();

        public string Slug => Term?.ToLower().Replace(" ", "-");

        [Display(Name = "Category")]
        public string Category { get; set; } = "";
    }
}