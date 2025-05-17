using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services.Blog;

namespace WorkoutTrackerWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogApiController : ControllerBase
    {
        private readonly IBlogService _blogService;
        private readonly ILogger<BlogApiController> _logger;

        public BlogApiController(IBlogService blogService, ILogger<BlogApiController> logger)
        {
            _blogService = blogService;
            _logger = logger;
        }

        [HttpGet("categories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _blogService.GetAllCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching blog categories");
                return StatusCode(500, "An error occurred while fetching categories");
            }
        }

        [HttpGet("tags")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTags()
        {
            try
            {
                var tags = await _blogService.GetAllTagsAsync();
                return Ok(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching blog tags");
                return StatusCode(500, "An error occurred while fetching tags");
            }
        }

        [HttpGet("posts")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPosts(int page = 1, int pageSize = 10)
        {
            try
            {
                var posts = await _blogService.GetPublishedBlogPostsAsync(page, pageSize);
                var total = await _blogService.GetTotalPublishedBlogPostCountAsync();
                
                return Ok(new { 
                    Posts = posts,
                    Total = total,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (total + pageSize - 1) / pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching blog posts");
                return StatusCode(500, "An error occurred while fetching posts");
            }
        }
    }
}
