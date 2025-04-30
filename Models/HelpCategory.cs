using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkoutTrackerWeb.Models
{
    public class HelpCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Category Name")]
        public string Name { get; set; }

        [StringLength(200)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Icon Class")]
        [StringLength(50)]
        public string IconClass { get; set; }

        public string Slug => Name?.ToLower().Replace(" ", "-");

        public ICollection<HelpArticle> Articles { get; set; }

        [Display(Name = "Parent Category")]
        public int? ParentCategoryId { get; set; }
        public HelpCategory ParentCategory { get; set; }

        public ICollection<HelpCategory> ChildCategories { get; set; }

        public bool IsRootCategory => ParentCategoryId == null;
    }

    public static class DefaultHelpCategories
    {
        public static readonly List<HelpCategory> Categories = new List<HelpCategory>
        {
            new HelpCategory
            {
                Name = "Coaching",
                Description = "Learn how to use the coaching features, either as a coach or a client",
                IconClass = "bi bi-person-lines-fill",
                DisplayOrder = 6
            },
            new HelpCategory
            {
                Name = "Sharing",
                Description = "Share your workout data securely with others",
                IconClass = "bi bi-share",
                DisplayOrder = 7
            }
        };
    }
}