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

        public async Task<List<GlossaryTerm>> GetAllGlossaryTermsAsync()
        {
            return await Task.FromResult(new List<GlossaryTerm>
            {
                // ...existing terms...
                
                new GlossaryTerm
                {
                    Term = "Coach",
                    Definition = "A certified fitness professional who can create and manage workout programs for clients through the platform.",
                    Category = "Coaching",
                    Example = "A coach can create workout templates and assign them to multiple clients."
                },
                new GlossaryTerm
                {
                    Term = "Client",
                    Definition = "A user who is being trained by a coach on the platform.",
                    Category = "Coaching",
                    Example = "As a client, you'll receive workout assignments from your coach and can track your progress together."
                },
                new GlossaryTerm
                {
                    Term = "Workout Template",
                    Definition = "A pre-designed workout plan that can be assigned to one or more clients by a coach.",
                    Category = "Coaching",
                    Example = "A coach might create a 'Beginner Upper Body' template to use with multiple clients."
                },
                new GlossaryTerm
                {
                    Term = "Share Token",
                    Definition = "A secure access token that allows others to view specific workout data without needing an account.",
                    Category = "Sharing",
                    Example = "You can generate a share token to show your workout progress to friends or family."
                },
                new GlossaryTerm
                {
                    Term = "Client Group",
                    Definition = "A collection of clients organized by a coach for easier management and workout assignment.",
                    Category = "Coaching",
                    Example = "A coach might create separate groups for 'Beginners' and 'Advanced' clients."
                },
                new GlossaryTerm
                {
                    Term = "Workout Assignment",
                    Definition = "A workout template or program that has been assigned to a specific client by their coach.",
                    Category = "Coaching",
                    Example = "Your coach can assign you specific workouts with target sets, reps, and weights."
                }
            });
        }

        public async Task<List<HelpArticle>> GetDefaultArticlesAsync()
        {
            return await Task.FromResult(new List<HelpArticle>
            {
                // ...existing articles...
                
                new HelpArticle
                {
                    Title = "Getting Started with Coaching",
                    ShortDescription = "Learn how to become a coach and start training clients",
                    Content = @"<h2>Becoming a Coach</h2>
                               <p>To become a coach on the platform, you'll need to:</p>
                               <ol>
                                   <li>Go to your profile settings</li>
                                   <li>Click on 'Become a Coach'</li>
                                   <li>Complete your coaching profile</li>
                                   <li>Start accepting client requests</li>
                               </ol>
                               
                               <h2>Managing Clients</h2>
                               <p>As a coach, you can:</p>
                               <ul>
                                   <li>Accept/reject client requests</li>
                                   <li>Create client groups</li>
                                   <li>Design workout templates</li>
                                   <li>Schedule workouts for clients</li>
                                   <li>Track client progress</li>
                               </ul>",
                    HelpCategoryId = 6, // Coaching category
                    IsFeatured = true,
                    DisplayOrder = 1,
                    Version = "1.0",
                    Tags = "coach,coaching,clients,training"
                },
                
                new HelpArticle
                {
                    Title = "Working with a Coach",
                    ShortDescription = "Guide for clients working with a coach",
                    Content = @"<h2>Finding a Coach</h2>
                               <p>To find and connect with a coach:</p>
                               <ol>
                                   <li>Go to the 'Find a Coach' section</li>
                                   <li>Browse available coaches</li>
                                   <li>Send a connection request</li>
                                   <li>Wait for coach acceptance</li>
                               </ol>
                               
                               <h2>Following Your Training Program</h2>
                               <p>Once connected with a coach:</p>
                               <ul>
                                   <li>View assigned workouts in your dashboard</li>
                                   <li>Complete scheduled sessions</li>
                                   <li>Track your progress</li>
                                   <li>Communicate with your coach</li>
                               </ul>",
                    HelpCategoryId = 6, // Coaching category
                    DisplayOrder = 2,
                    Version = "1.0",
                    Tags = "coach,client,training,workout"
                },
                
                new HelpArticle
                {
                    Title = "Sharing Your Workout Data",
                    ShortDescription = "Learn how to share your workout progress with others",
                    Content = @"<h2>Creating Share Links</h2>
                               <p>To share your workout data:</p>
                               <ol>
                                   <li>Go to your workout history</li>
                                   <li>Click the 'Share' button</li>
                                   <li>Configure sharing options</li>
                                   <li>Copy and share the link</li>
                               </ol>
                               
                               <h2>Managing Shared Access</h2>
                               <p>Control your shared data:</p>
                               <ul>
                                   <li>Set expiration dates</li>
                                   <li>Choose what data to share</li>
                                   <li>Revoke access anytime</li>
                                   <li>Track who views your data</li>
                               </ul>",
                    HelpCategoryId = 7, // Sharing category
                    IsFeatured = true,
                    DisplayOrder = 1,
                    Version = "1.0",
                    Tags = "share,sharing,privacy,data"
                }
            });
        }
    }
}