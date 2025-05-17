using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Blog;

namespace WorkoutTrackerWeb.Services.Blog
{
    public class BlogRepository : IBlogRepository
    {
        private readonly WorkoutTrackerWebContext _context;

        public BlogRepository(WorkoutTrackerWebContext context)
        {
            _context = context;
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
            _context.Entry(blogPost).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return blogPost;
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
            }
        }

        public async Task RemovePostFromCategoryAsync(int postId, int categoryId)
        {
            var mapping = await _context.BlogPostCategory
                .FirstOrDefaultAsync(pc => pc.BlogPostId == postId && pc.BlogCategoryId == categoryId);

            if (mapping != null)
            {
                _context.BlogPostCategory.Remove(mapping);
                await _context.SaveChangesAsync();
            }
        }

        // Post-Tag mapping methods
        public async Task AddPostTagAsync(int postId, int tagId)
        {
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
            }
        }

        public async Task RemovePostTagAsync(int postId, int tagId)
        {
            var mapping = await _context.BlogPostTag
                .FirstOrDefaultAsync(pt => pt.BlogPostId == postId && pt.BlogTagId == tagId);

            if (mapping != null)
            {
                _context.BlogPostTag.Remove(mapping);
                await _context.SaveChangesAsync();
            }
        }
    }
}
