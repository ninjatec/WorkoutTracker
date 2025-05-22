using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Blog;

namespace WorkoutTrackerWeb.Services.Blog
{
    public class BlogRepository : IBlogRepository
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<BlogRepository> _logger;

        public BlogRepository(WorkoutTrackerWebContext context, ILogger<BlogRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Blog Post methods
        public async Task<BlogPost> GetBlogPostByIdAsync(int id)
        {
            return await _context.BlogPost
                .Include(p => p.Author)
                .Include(p => p.BlogPostCategories)
                    .ThenInclude(pc => pc.BlogCategory)
                .Include(p => p.BlogPostTags)
                    .ThenInclude(pt => pt.BlogTag)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<BlogPost> GetBlogPostBySlugAsync(string slug)
        {
            return await _context.BlogPost
                .Include(p => p.Author)
                .Include(p => p.BlogPostCategories)
                    .ThenInclude(pc => pc.BlogCategory)
                .Include(p => p.BlogPostTags)
                    .ThenInclude(pt => pt.BlogTag)
                .FirstOrDefaultAsync(p => p.Slug == slug);
        }

        public async Task<List<BlogPost>> GetAllBlogPostsAsync(bool includeUnpublished = false)
        {
            var query = _context.BlogPost
                .Include(p => p.Author)
                .Include(p => p.BlogPostCategories)
                    .ThenInclude(pc => pc.BlogCategory)
                .Include(p => p.BlogPostTags)
                    .ThenInclude(pt => pt.BlogTag)
                .AsQueryable();

            if (!includeUnpublished)
            {
                query = query.Where(p => p.Published);
            }

            return await query
                .OrderByDescending(p => p.PublishedOn)
                .ToListAsync();
        }

        public async Task<List<BlogPost>> GetPublishedBlogPostsAsync(int pageNumber = 1, int pageSize = 10)
        {
            return await _context.BlogPost
                .Include(p => p.Author)
                .Include(p => p.BlogPostCategories)
                    .ThenInclude(pc => pc.BlogCategory)
                .Include(p => p.BlogPostTags)
                    .ThenInclude(pt => pt.BlogTag)
                .Where(p => p.Published)
                .OrderByDescending(p => p.PublishedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalPublishedBlogPostCountAsync()
        {
            return await _context.BlogPost
                .Where(p => p.Published)
                .CountAsync();
        }

        public async Task<List<BlogPost>> GetBlogPostsByCategoryAsync(string categorySlug, int pageNumber = 1, int pageSize = 10)
        {
            return await _context.BlogPost
                .Include(p => p.Author)
                .Include(p => p.BlogPostCategories)
                    .ThenInclude(pc => pc.BlogCategory)
                .Include(p => p.BlogPostTags)
                    .ThenInclude(pt => pt.BlogTag)
                .Where(p => p.Published && p.BlogPostCategories.Any(pc => pc.BlogCategory.Slug == categorySlug))
                .OrderByDescending(p => p.PublishedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<BlogPost>> GetBlogPostsByTagAsync(string tagSlug, int pageNumber = 1, int pageSize = 10)
        {
            return await _context.BlogPost
                .Include(p => p.Author)
                .Include(p => p.BlogPostCategories)
                    .ThenInclude(pc => pc.BlogCategory)
                .Include(p => p.BlogPostTags)
                    .ThenInclude(pt => pt.BlogTag)
                .Where(p => p.Published && p.BlogPostTags.Any(pt => pt.BlogTag.Slug == tagSlug))
                .OrderByDescending(p => p.PublishedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<BlogPost>> SearchBlogPostsAsync(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            return await _context.BlogPost
                .Include(p => p.Author)
                .Include(p => p.BlogPostCategories)
                    .ThenInclude(pc => pc.BlogCategory)
                .Include(p => p.BlogPostTags)
                    .ThenInclude(pt => pt.BlogTag)
                .Where(p => p.Published && 
                           (p.Title.Contains(searchTerm) || 
                            p.Content.Contains(searchTerm) || 
                            p.Summary.Contains(searchTerm)))
                .OrderByDescending(p => p.PublishedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<BlogPost> CreateBlogPostAsync(BlogPost blogPost)
        {
            _context.BlogPost.Add(blogPost);
            await _context.SaveChangesAsync();
            return blogPost;
        }

        public async Task<BlogPost> UpdateBlogPostAsync(BlogPost blogPost)
        {
            try
            {
                _logger.LogInformation("BlogRepository.UpdateBlogPostAsync - Starting database update for blog post {id}", blogPost.Id);

                // Update using direct SQL to ensure Published property gets updated correctly
                var sql = @"
                UPDATE BlogPost 
                SET Title = @Title, 
                    Slug = @Slug, 
                    Content = @Content, 
                    Summary = @Summary, 
                    ImageUrl = @ImageUrl, 
                    Published = @Published, 
                    PublishedOn = @PublishedOn, 
                    UpdatedOn = @UpdatedOn, 
                    ViewCount = @ViewCount
                WHERE Id = @Id";

                _logger.LogInformation("BlogRepository.UpdateBlogPostAsync - Executing SQL update for blog post {id}. Published: {isPublished}, PublishedOn: {publishedOn}", 
                    blogPost.Id, blogPost.Published, blogPost.PublishedOn);

                // Define all parameters to ensure proper data types
                var parameters = new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@Title", blogPost.Title ?? string.Empty),
                    new Microsoft.Data.SqlClient.SqlParameter("@Slug", blogPost.Slug ?? string.Empty),
                    new Microsoft.Data.SqlClient.SqlParameter("@Content", blogPost.Content ?? string.Empty),
                    new Microsoft.Data.SqlClient.SqlParameter("@Summary", (object)(blogPost.Summary ?? null) ?? DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@ImageUrl", (object)blogPost.ImageUrl ?? DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@Published", blogPost.Published),
                    new Microsoft.Data.SqlClient.SqlParameter("@PublishedOn", (object)blogPost.PublishedOn ?? DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@UpdatedOn", (object)blogPost.UpdatedOn ?? DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@ViewCount", blogPost.ViewCount),
                    new Microsoft.Data.SqlClient.SqlParameter("@Id", blogPost.Id)
                };

                // Execute the SQL command
                int rowsAffected = await _context.Database.ExecuteSqlRawAsync(sql, parameters);
                
                _logger.LogInformation("BlogRepository.UpdateBlogPostAsync - SQL update completed. Rows affected: {rowsAffected}", rowsAffected);
                
                // Clear the DbContext to ensure we get fresh data
                _context.ChangeTracker.Clear();
                
                // Return the updated blog post
                var updatedPost = await GetBlogPostByIdAsync(blogPost.Id);
                
                _logger.LogInformation("BlogRepository.UpdateBlogPostAsync - Successfully retrieved updated blog post {id}. Published: {isPublished}", 
                    blogPost.Id, updatedPost?.Published);
                
                return updatedPost;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BlogRepository.UpdateBlogPostAsync - Error updating blog post {id} in database", blogPost.Id);
                throw;
            }
        }

        public async Task DeleteBlogPostAsync(int id)
        {
            var blogPost = await _context.BlogPost.FindAsync(id);
            if (blogPost != null)
            {
                _context.BlogPost.Remove(blogPost);
                await _context.SaveChangesAsync();
            }
        }

        public async Task IncrementPostViewCountAsync(int id)
        {
            var post = await _context.BlogPost.FindAsync(id);
            if (post != null)
            {
                post.ViewCount++;
                await _context.SaveChangesAsync();
            }
        }

        // Category methods
        public async Task<List<BlogCategory>> GetAllCategoriesAsync()
        {
            return await _context.BlogCategory
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<BlogCategory> GetCategoryByIdAsync(int id)
        {
            return await _context.BlogCategory.FindAsync(id);
        }

        public async Task<BlogCategory> GetCategoryBySlugAsync(string slug)
        {
            return await _context.BlogCategory.FirstOrDefaultAsync(c => c.Slug == slug);
        }

        public async Task<BlogCategory> CreateCategoryAsync(BlogCategory category)
        {
            _context.BlogCategory.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<BlogCategory> UpdateCategoryAsync(BlogCategory category)
        {
            _context.Entry(category).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await _context.BlogCategory.FindAsync(id);
            if (category != null)
            {
                _context.BlogCategory.Remove(category);
                await _context.SaveChangesAsync();
            }
        }

        // Tag methods
        public async Task<List<BlogTag>> GetAllTagsAsync()
        {
            return await _context.BlogTag
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<BlogTag> GetTagByIdAsync(int id)
        {
            return await _context.BlogTag.FindAsync(id);
        }

        public async Task<BlogTag> GetTagBySlugAsync(string slug)
        {
            return await _context.BlogTag.FirstOrDefaultAsync(t => t.Slug == slug);
        }

        public async Task<BlogTag> CreateTagAsync(BlogTag tag)
        {
            _context.BlogTag.Add(tag);
            await _context.SaveChangesAsync();
            return tag;
        }

        public async Task<BlogTag> UpdateTagAsync(BlogTag tag)
        {
            _context.Entry(tag).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return tag;
        }

        public async Task DeleteTagAsync(int id)
        {
            var tag = await _context.BlogTag.FindAsync(id);
            if (tag != null)
            {
                _context.BlogTag.Remove(tag);
                await _context.SaveChangesAsync();
            }
        }

        // Post-Category mapping methods
        public async Task AddPostToCategoryAsync(int postId, int categoryId)
        {
            try
            {
                _logger.LogInformation("BlogRepository.AddPostToCategoryAsync - Adding category {categoryId} to post {postId}", categoryId, postId);
                
                var mapping = await _context.BlogPostCategory
                    .FirstOrDefaultAsync(pc => pc.BlogPostId == postId && pc.BlogCategoryId == categoryId);

                if (mapping == null)
                {
                    mapping = new BlogPostCategory
                    {
                        BlogPostId = postId,
                        BlogCategoryId = categoryId
                    };
                    _context.BlogPostCategory.Add(mapping);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("BlogRepository.AddPostToCategoryAsync - Successfully added category {categoryId} to post {postId}", categoryId, postId);
                }
                else
                {
                    _logger.LogWarning("BlogRepository.AddPostToCategoryAsync - Category {categoryId} already exists for post {postId}", categoryId, postId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BlogRepository.AddPostToCategoryAsync - Error adding category {categoryId} to post {postId}", categoryId, postId);
                throw;
            }
        }

        public async Task RemovePostFromCategoryAsync(int postId, int categoryId)
        {
            try
            {
                _logger.LogInformation("BlogRepository.RemovePostFromCategoryAsync - Removing category {categoryId} from post {postId}", categoryId, postId);
                
                var mapping = await _context.BlogPostCategory
                    .FirstOrDefaultAsync(pc => pc.BlogPostId == postId && pc.BlogCategoryId == categoryId);

                if (mapping != null)
                {
                    _context.BlogPostCategory.Remove(mapping);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("BlogRepository.RemovePostFromCategoryAsync - Successfully removed category {categoryId} from post {postId}", categoryId, postId);
                }
                else
                {
                    _logger.LogWarning("BlogRepository.RemovePostFromCategoryAsync - Category {categoryId} not found for post {postId}", categoryId, postId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BlogRepository.RemovePostFromCategoryAsync - Error removing category {categoryId} from post {postId}", categoryId, postId);
                throw;
            }
        }

        // Post-Tag mapping methods
        public async Task AddPostTagAsync(int postId, int tagId)
        {
            try 
            {
                _logger.LogInformation("BlogRepository.AddPostTagAsync - Adding tag {tagId} to post {postId}", tagId, postId);
                
                var mapping = await _context.BlogPostTag
                    .FirstOrDefaultAsync(pt => pt.BlogPostId == postId && pt.BlogTagId == tagId);

                if (mapping == null)
                {
                    mapping = new BlogPostTag
                    {
                        BlogPostId = postId,
                        BlogTagId = tagId
                    };
                    _context.BlogPostTag.Add(mapping);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("BlogRepository.AddPostTagAsync - Successfully added tag {tagId} to post {postId}", tagId, postId);
                }
                else
                {
                    _logger.LogWarning("BlogRepository.AddPostTagAsync - Tag {tagId} already exists for post {postId}", tagId, postId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BlogRepository.AddPostTagAsync - Error adding tag {tagId} to post {postId}", tagId, postId);
                throw;
            }
        }

        public async Task RemovePostTagAsync(int postId, int tagId)
        {
            try
            {
                _logger.LogInformation("BlogRepository.RemovePostTagAsync - Removing tag {tagId} from post {postId}", tagId, postId);
                
                var mapping = await _context.BlogPostTag
                    .FirstOrDefaultAsync(pt => pt.BlogPostId == postId && pt.BlogTagId == tagId);

                if (mapping != null)
                {
                    _context.BlogPostTag.Remove(mapping);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("BlogRepository.RemovePostTagAsync - Successfully removed tag {tagId} from post {postId}", tagId, postId);
                }
                else
                {
                    _logger.LogWarning("BlogRepository.RemovePostTagAsync - Tag {tagId} not found for post {postId}", tagId, postId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BlogRepository.RemovePostTagAsync - Error removing tag {tagId} from post {postId}", tagId, postId);
                throw;
            }
        }
    }
}
