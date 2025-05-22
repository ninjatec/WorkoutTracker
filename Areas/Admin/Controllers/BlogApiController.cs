using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WorkoutTrackerWeb.Utilities;

namespace WorkoutTrackerWeb.Areas.Admin.Controllers
{
    [Route("api/blog")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class BlogApiController : ControllerBase
    {
        private readonly BlogImageUtility _imageUtility;

        public BlogApiController(BlogImageUtility imageUtility)
        {
            _imageUtility = imageUtility;
        }

        [HttpPost("upload-image")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest(new { success = false, message = "No image was uploaded" });
            }

            try
            {
                // Validate file type
                var fileExtension = System.IO.Path.GetExtension(image.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                
                if (!Array.Exists(allowedExtensions, ext => ext == fileExtension))
                {
                    return BadRequest(new { success = false, message = "Invalid file type. Only JPG, PNG, and GIF images are allowed." });
                }

                // Upload the image
                var imageUrl = await _imageUtility.UploadBlogImageAsync(image);

                if (string.IsNullOrEmpty(imageUrl))
                {
                    return BadRequest(new { success = false, message = "Failed to upload image" });
                }

                return Ok(new { success = true, imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
