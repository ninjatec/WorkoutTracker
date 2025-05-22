using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Models.Blog;

namespace WorkoutTrackerWeb.Services.Blog
{
    public interface IBlogService
    {
        // Blog Post methods
        Task<BlogPost> GetBlogPostByIdAsync(int id);
        Task<BlogPost> GetBlogPostBySlugAsync(string slug);
        Task<List<BlogPost>> GetAllBlogPostsAsync(bool includeUnpublished = false);
        Task<List<BlogPost>> GetPublishedBlogPostsAsync(int pageNumber = 1, int pageSize = 10);
        Task<int> GetTotalPublishedBlogPostCountAsync();
        Task<List<BlogPost>> GetBlogPostsByCategoryAsync(string categorySlug, int pageNumber = 1, int pageSize = 10);
        Task<List<BlogPost>> GetBlogPostsByTagAsync(string tagSlug, int pageNumber = 1, int pageSize = 10);
        Task<List<BlogPost>> SearchBlogPostsAsync(string searchTerm, int pageNumber = 1, int pageSize = 10);
        Task<BlogPost> CreateBlogPostAsync(BlogPost blogPost, List<int> categoryIds, List<int> tagIds);
        Task<BlogPost> UpdateBlogPostAsync(BlogPost blogPost, List<int> categoryIds, List<int> tagIds);
        Task DeleteBlogPostAsync(int id);
        Task IncrementPostViewCountAsync(int id);
        Task<string> GenerateUniqueSlugAsync(string title);
        
        // Category methods
        Task<List<BlogCategory>> GetAllCategoriesAsync();
        Task<BlogCategory> GetCategoryByIdAsync(int id);
        Task<BlogCategory> GetCategoryBySlugAsync(string slug);
        Task<BlogCategory> CreateCategoryAsync(BlogCategory category);
        Task<BlogCategory> UpdateCategoryAsync(BlogCategory category);
        Task DeleteCategoryAsync(int id);
        Task<string> GenerateUniqueCategorySlugAsync(string name);
        
        // Tag methods
        Task<List<BlogTag>> GetAllTagsAsync();
        Task<BlogTag> GetTagByIdAsync(int id);
        Task<BlogTag> GetTagBySlugAsync(string slug);
        Task<BlogTag> CreateTagAsync(BlogTag tag);
        Task<BlogTag> UpdateTagAsync(BlogTag tag);
        Task DeleteTagAsync(int id);
        Task<string> GenerateUniqueTagSlugAsync(string name);
    }
}
