using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Models.Blog;

namespace WorkoutTrackerWeb.Services.Blog
{
    public class BlogService : IBlogService
    {
        private readonly IBlogRepository _blogRepository;

        public BlogService(IBlogRepository blogRepository)
        {
            _blogRepository = blogRepository;
        }

        // Blog Post methods
        public async Task<BlogPost> GetBlogPostByIdAsync(int id)
        {
            return await _blogRepository.GetBlogPostByIdAsync(id);
        }

        public async Task<BlogPost> GetBlogPostBySlugAsync(string slug)
        {
            return await _blogRepository.GetBlogPostBySlugAsync(slug);
        }

        public async Task<List<BlogPost>> GetAllBlogPostsAsync(bool includeUnpublished = false)
        {
            return await _blogRepository.GetAllBlogPostsAsync(includeUnpublished);
        }

        public async Task<List<BlogPost>> GetPublishedBlogPostsAsync(int pageNumber = 1, int pageSize = 10)
        {
            return await _blogRepository.GetPublishedBlogPostsAsync(pageNumber, pageSize);
        }

        public async Task<int> GetTotalPublishedBlogPostCountAsync()
        {
            return await _blogRepository.GetTotalPublishedBlogPostCountAsync();
        }

        public async Task<List<BlogPost>> GetBlogPostsByCategoryAsync(string categorySlug, int pageNumber = 1, int pageSize = 10)
        {
            return await _blogRepository.GetBlogPostsByCategoryAsync(categorySlug, pageNumber, pageSize);
        }

        public async Task<List<BlogPost>> GetBlogPostsByTagAsync(string tagSlug, int pageNumber = 1, int pageSize = 10)
        {
            return await _blogRepository.GetBlogPostsByTagAsync(tagSlug, pageNumber, pageSize);
        }

        public async Task<List<BlogPost>> SearchBlogPostsAsync(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            return await _blogRepository.SearchBlogPostsAsync(searchTerm, pageNumber, pageSize);
        }

        public async Task<BlogPost> CreateBlogPostAsync(BlogPost blogPost, List<int> categoryIds, List<int> tagIds)
        {
            // Generate slug if not provided
            if (string.IsNullOrEmpty(blogPost.Slug))
            {
                blogPost.Slug = await GenerateUniqueSlugAsync(blogPost.Title);
            }

            // Set creation date
            blogPost.CreatedOn = DateTime.UtcNow;
            
            // Set published date if publishing
            if (blogPost.Published && !blogPost.PublishedOn.HasValue)
            {
                blogPost.PublishedOn = DateTime.UtcNow;
            }

            // Create the blog post
            var createdPost = await _blogRepository.CreateBlogPostAsync(blogPost);

            // Add categories
            if (categoryIds != null && categoryIds.Count > 0)
            {
                foreach (var categoryId in categoryIds)
                {
                    await _blogRepository.AddPostToCategoryAsync(createdPost.Id, categoryId);
                }
            }

            // Add tags
            if (tagIds != null && tagIds.Count > 0)
            {
                foreach (var tagId in tagIds)
                {
                    await _blogRepository.AddPostTagAsync(createdPost.Id, tagId);
                }
            }

            // Get the full post with relationships
            return await _blogRepository.GetBlogPostByIdAsync(createdPost.Id);
        }

        public async Task<BlogPost> UpdateBlogPostAsync(BlogPost blogPost, List<int> categoryIds, List<int> tagIds)
        {
            // Set update date
            blogPost.UpdatedOn = DateTime.UtcNow;
            
            // Set published date if publishing for the first time
            if (blogPost.Published && !blogPost.PublishedOn.HasValue)
            {
                blogPost.PublishedOn = DateTime.UtcNow;
            }

            // Update the blog post
            await _blogRepository.UpdateBlogPostAsync(blogPost);

            // Get full post with current relationships
            var updatedPost = await _blogRepository.GetBlogPostByIdAsync(blogPost.Id);

            // Update categories - first remove all existing categories
            var existingCategories = updatedPost.BlogPostCategories.ToList();
            foreach (var existingCategory in existingCategories)
            {
                await _blogRepository.RemovePostFromCategoryAsync(updatedPost.Id, existingCategory.BlogCategoryId);
            }

            // Add new categories
            if (categoryIds != null && categoryIds.Count > 0)
            {
                foreach (var categoryId in categoryIds)
                {
                    await _blogRepository.AddPostToCategoryAsync(updatedPost.Id, categoryId);
                }
            }

            // Update tags - first remove all existing tags
            var existingTags = updatedPost.BlogPostTags.ToList();
            foreach (var existingTag in existingTags)
            {
                await _blogRepository.RemovePostTagAsync(updatedPost.Id, existingTag.BlogTagId);
            }

            // Add new tags
            if (tagIds != null && tagIds.Count > 0)
            {
                foreach (var tagId in tagIds)
                {
                    await _blogRepository.AddPostTagAsync(updatedPost.Id, tagId);
                }
            }

            // Get the updated post with new relationships
            return await _blogRepository.GetBlogPostByIdAsync(updatedPost.Id);
        }

        public async Task DeleteBlogPostAsync(int id)
        {
            await _blogRepository.DeleteBlogPostAsync(id);
        }

        public async Task IncrementPostViewCountAsync(int id)
        {
            await _blogRepository.IncrementPostViewCountAsync(id);
        }

        public async Task<string> GenerateUniqueSlugAsync(string title)
        {
            // Convert to lowercase and remove special characters
            var slug = title.ToLowerInvariant();
            
            // Remove diacritics (accents)
            slug = RemoveDiacritics(slug);
            
            // Replace spaces with hyphens and remove invalid characters
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            slug = slug.Trim('-');

            // Check if slug already exists
            var baseSlug = slug;
            var slugExists = true;
            var counter = 2;

            while (slugExists)
            {
                var post = await _blogRepository.GetBlogPostBySlugAsync(slug);
                if (post == null)
                {
                    slugExists = false;
                }
                else
                {
                    slug = $"{baseSlug}-{counter}";
                    counter++;
                }
            }

            return slug;
        }

        // Category methods
        public async Task<List<BlogCategory>> GetAllCategoriesAsync()
        {
            return await _blogRepository.GetAllCategoriesAsync();
        }

        public async Task<BlogCategory> GetCategoryByIdAsync(int id)
        {
            return await _blogRepository.GetCategoryByIdAsync(id);
        }

        public async Task<BlogCategory> GetCategoryBySlugAsync(string slug)
        {
            return await _blogRepository.GetCategoryBySlugAsync(slug);
        }

        public async Task<BlogCategory> CreateCategoryAsync(BlogCategory category)
        {
            // Generate slug if not provided
            if (string.IsNullOrEmpty(category.Slug))
            {
                category.Slug = await GenerateUniqueCategorySlugAsync(category.Name);
            }

            return await _blogRepository.CreateCategoryAsync(category);
        }

        public async Task<BlogCategory> UpdateCategoryAsync(BlogCategory category)
        {
            return await _blogRepository.UpdateCategoryAsync(category);
        }

        public async Task DeleteCategoryAsync(int id)
        {
            await _blogRepository.DeleteCategoryAsync(id);
        }

        public async Task<string> GenerateUniqueCategorySlugAsync(string name)
        {
            // Convert to lowercase and remove special characters
            var slug = name.ToLowerInvariant();
            
            // Remove diacritics (accents)
            slug = RemoveDiacritics(slug);
            
            // Replace spaces with hyphens and remove invalid characters
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            slug = slug.Trim('-');

            // Check if slug already exists
            var baseSlug = slug;
            var slugExists = true;
            var counter = 2;

            while (slugExists)
            {
                var category = await _blogRepository.GetCategoryBySlugAsync(slug);
                if (category == null)
                {
                    slugExists = false;
                }
                else
                {
                    slug = $"{baseSlug}-{counter}";
                    counter++;
                }
            }

            return slug;
        }

        // Tag methods
        public async Task<List<BlogTag>> GetAllTagsAsync()
        {
            return await _blogRepository.GetAllTagsAsync();
        }

        public async Task<BlogTag> GetTagByIdAsync(int id)
        {
            return await _blogRepository.GetTagByIdAsync(id);
        }

        public async Task<BlogTag> GetTagBySlugAsync(string slug)
        {
            return await _blogRepository.GetTagBySlugAsync(slug);
        }

        public async Task<BlogTag> CreateTagAsync(BlogTag tag)
        {
            // Generate slug if not provided
            if (string.IsNullOrEmpty(tag.Slug))
            {
                tag.Slug = await GenerateUniqueTagSlugAsync(tag.Name);
            }

            return await _blogRepository.CreateTagAsync(tag);
        }

        public async Task<BlogTag> UpdateTagAsync(BlogTag tag)
        {
            return await _blogRepository.UpdateTagAsync(tag);
        }

        public async Task DeleteTagAsync(int id)
        {
            await _blogRepository.DeleteTagAsync(id);
        }

        public async Task<string> GenerateUniqueTagSlugAsync(string name)
        {
            // Convert to lowercase and remove special characters
            var slug = name.ToLowerInvariant();
            
            // Remove diacritics (accents)
            slug = RemoveDiacritics(slug);
            
            // Replace spaces with hyphens and remove invalid characters
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            slug = slug.Trim('-');

            // Check if slug already exists
            var baseSlug = slug;
            var slugExists = true;
            var counter = 2;

            while (slugExists)
            {
                var tag = await _blogRepository.GetTagBySlugAsync(slug);
                if (tag == null)
                {
                    slugExists = false;
                }
                else
                {
                    slug = $"{baseSlug}-{counter}";
                    counter++;
                }
            }

            return slug;
        }

        // Helper methods
        private string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }
    }
}
