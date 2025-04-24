using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Services
{
    public class HelpService
    {
        private readonly WorkoutTrackerWebContext _context;

        public HelpService(WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public async Task<List<HelpCategory>> GetCategoriesAsync()
        {
            return await _context.HelpCategory
                .Include(c => c.ParentCategory)
                .Include(c => c.ChildCategories)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<HelpCategory>> GetRootCategoriesAsync()
        {
            return await _context.HelpCategory
                .Include(c => c.Articles)
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<HelpArticle>> GetFeaturedArticlesAsync()
        {
            return await _context.HelpArticle
                .Include(a => a.Category)
                .Where(a => a.IsFeatured)
                .OrderBy(a => a.DisplayOrder)
                .ToListAsync();
        }

        public async Task<HelpArticle> GetArticleByIdAsync(int id)
        {
            var article = await _context.HelpArticle
                .Include(a => a.Category)
                .Include(a => a.RelatedArticles)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article != null)
            {
                article.ViewCount++;
                await _context.SaveChangesAsync();
            }

            return article;
        }

        public async Task<HelpArticle> GetArticleBySlugAsync(string slug)
        {
            var article = await _context.HelpArticle
                .Include(a => a.Category)
                .Include(a => a.RelatedArticles)
                .FirstOrDefaultAsync(a => a.Slug == slug);

            if (article != null)
            {
                article.ViewCount++;
                await _context.SaveChangesAsync();
            }

            return article;
        }

        public async Task<List<HelpArticle>> GetArticlesByCategoryAsync(int categoryId)
        {
            return await _context.HelpArticle
                .Where(a => a.HelpCategoryId == categoryId)
                .OrderBy(a => a.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<HelpArticle>> SearchArticlesAsync(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return new List<HelpArticle>();

            return await _context.HelpArticle
                .Where(a => 
                    a.Title.Contains(searchTerm) || 
                    a.ShortDescription.Contains(searchTerm) || 
                    a.Content.Contains(searchTerm) ||
                    a.Tags.Contains(searchTerm))
                .Include(a => a.Category)
                .OrderByDescending(a => 
                    (a.Title.Contains(searchTerm) ? 3 : 0) +
                    (a.ShortDescription.Contains(searchTerm) ? 2 : 0) +
                    (a.Content.Contains(searchTerm) ? 1 : 0))
                .Take(20)
                .ToListAsync();
        }

        public async Task<List<GlossaryTerm>> GetGlossaryTermsAsync()
        {
            return await _context.GlossaryTerm
                .OrderBy(t => t.Term)
                .ToListAsync();
        }

        public async Task<GlossaryTerm> GetGlossaryTermByIdAsync(int id)
        {
            return await _context.GlossaryTerm
                .Include(t => t.RelatedTerms)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<GlossaryTerm>> SearchGlossaryAsync(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return new List<GlossaryTerm>();

            return await _context.GlossaryTerm
                .Where(t => 
                    t.Term.Contains(searchTerm) || 
                    t.Definition.Contains(searchTerm) ||
                    t.Example.Contains(searchTerm))
                .OrderByDescending(t => 
                    (t.Term.Contains(searchTerm) ? 3 : 0) +
                    (t.Definition.Contains(searchTerm) ? 2 : 0) +
                    (t.Example.Contains(searchTerm) ? 1 : 0))
                .Take(20)
                .ToListAsync();
        }

        public async Task<List<GlossaryTerm>> GetGlossaryTermsByCategoryAsync(string category)
        {
            return await _context.GlossaryTerm
                .Where(t => t.Category == category)
                .OrderBy(t => t.Term)
                .ToListAsync();
        }

        public async Task<List<string>> GetGlossaryCategoriesAsync()
        {
            return await _context.GlossaryTerm
                .Select(t => t.Category)
                .Distinct()
                .Where(c => c != null)
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task IncrementArticleViewCountAsync(int articleId)
        {
            var article = await _context.HelpArticle.FindAsync(articleId);
            if (article != null)
            {
                article.ViewCount++;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<HelpArticle> CreateArticleAsync(HelpArticle article)
        {
            _context.HelpArticle.Add(article);
            await _context.SaveChangesAsync();
            return article;
        }

        public async Task UpdateArticleAsync(HelpArticle article)
        {
            article.LastModifiedDate = DateTime.Now;
            _context.Entry(article).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteArticleAsync(int id)
        {
            var article = await _context.HelpArticle.FindAsync(id);
            if (article != null)
            {
                _context.HelpArticle.Remove(article);
                await _context.SaveChangesAsync();
            }
        }
    }
}