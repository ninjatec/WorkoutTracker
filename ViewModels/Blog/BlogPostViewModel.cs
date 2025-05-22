using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using WorkoutTrackerWeb.Models.Blog;

namespace WorkoutTrackerWeb.ViewModels.Blog
{
    public class BlogPostViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot be longer than 200 characters")]
        public string Title { get; set; }

        [StringLength(200, ErrorMessage = "Slug cannot be longer than 200 characters")]
        public string Slug { get; set; }

        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; }

        [StringLength(500, ErrorMessage = "Summary cannot be longer than 500 characters")]
        public string Summary { get; set; }

        public string ImageUrl { get; set; }

        public IFormFile ImageFile { get; set; }

        public string AuthorId { get; set; }

        public string AuthorName { get; set; }

        public bool Published { get; set; }

        public DateTime? PublishedOn { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int ViewCount { get; set; }

        public List<int> SelectedCategoryIds { get; set; } = new List<int>();

        public List<int> SelectedTagIds { get; set; } = new List<int>();

        public List<BlogCategoryViewModel> Categories { get; set; } = new List<BlogCategoryViewModel>();

        public List<BlogTagViewModel> Tags { get; set; } = new List<BlogTagViewModel>();

        public List<BlogCategoryViewModel> AvailableCategories { get; set; } = new List<BlogCategoryViewModel>();

        public List<BlogTagViewModel> AvailableTags { get; set; } = new List<BlogTagViewModel>();
    }
}
