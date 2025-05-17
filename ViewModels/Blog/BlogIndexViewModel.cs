using System.Collections.Generic;
using WorkoutTrackerWeb.Models.Blog;

namespace WorkoutTrackerWeb.ViewModels.Blog
{
    public class BlogIndexViewModel
    {
        public List<BlogPostViewModel> Posts { get; set; } = new List<BlogPostViewModel>();
        public int TotalPosts { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (TotalPosts + PageSize - 1) / PageSize;
        public string CategorySlug { get; set; }
        public string CategoryName { get; set; }
        public string TagSlug { get; set; }
        public string TagName { get; set; }
        public string SearchTerm { get; set; }
        public List<BlogCategoryViewModel> Categories { get; set; } = new List<BlogCategoryViewModel>();
        public List<BlogTagViewModel> Tags { get; set; } = new List<BlogTagViewModel>();
    }
}
