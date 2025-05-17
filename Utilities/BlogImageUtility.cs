using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace WorkoutTrackerWeb.Utilities
{
    public class BlogImageUtility
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _blogImagesFolder = "blog-images";

        public BlogImageUtility(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<string> UploadBlogImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return null;
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            
            if (!Array.Exists(allowedExtensions, ext => ext == fileExtension))
            {
                throw new InvalidOperationException("Invalid file type. Only JPG, PNG, and GIF images are allowed.");
            }

            // Create directory if it doesn't exist
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, _blogImagesFolder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            // Return relative path
            return $"/{_blogImagesFolder}/{uniqueFileName}";
        }

        public async Task<bool> DeleteBlogImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return false;
            }

            try
            {
                // Get file path from URL
                var fileName = Path.GetFileName(imageUrl);
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, _blogImagesFolder, fileName);

                // Delete file if it exists
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        // Legacy method kept for backward compatibility
        public async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            return await UploadBlogImageAsync(imageFile);
        }
        
        // Legacy method kept for backward compatibility
        public void DeleteImage(string imageUrl)
        {
            _ = DeleteBlogImageAsync(imageUrl).GetAwaiter().GetResult();
        }
    }
}