using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace WorkoutTrackerWeb.TagHelpers
{
    /// <summary>
    /// Tag helper that automatically creates responsive image variants and adds srcset attributes
    /// </summary>
    [HtmlTargetElement("img", Attributes = "responsive")]
    public class ResponsiveImageTagHelper : TagHelper
    {
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;
        private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly int[] WidthBreakpoints = { 320, 640, 960, 1280 };

        public ResponsiveImageTagHelper(IWebHostEnvironment env, IMemoryCache cache)
        {
            _env = env;
            _cache = cache;
        }

        [HtmlAttributeName("responsive")]
        public bool IsResponsive { get; set; } = false;

        [HtmlAttributeName("src")]
        public string ImageSource { get; set; }

        [HtmlAttributeName("lazy")]
        public bool IsLazy { get; set; } = true;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (!IsResponsive || string.IsNullOrEmpty(ImageSource))
            {
                if (IsLazy)
                {
                    // Still add lazy loading even if not responsive
                    output.Attributes.SetAttribute("loading", "lazy");
                }
                return;
            }

            // Only process images from the wwwroot directory
            if (!ImageSource.StartsWith("~/") && !ImageSource.StartsWith("/"))
            {
                return;
            }

            string imagePath = ImageSource.Replace("~/", "/").TrimStart('/');
            string fullPath = Path.Combine(_env.WebRootPath, imagePath);

            // Check if the file exists
            if (!File.Exists(fullPath))
            {
                return;
            }

            // Check if this is a supported image format
            string extension = Path.GetExtension(fullPath).ToLowerInvariant();
            if (!Array.Exists(SupportedExtensions, ext => ext.Equals(extension)))
            {
                // Still add lazy loading even if not a supported format
                if (IsLazy)
                {
                    output.Attributes.SetAttribute("loading", "lazy");
                }
                return;
            }

            // Generate or retrieve responsive image variants and build srcset
            string srcset = await GenerateSrcSetAsync(fullPath, imagePath);

            // Add srcset attribute to the output
            output.Attributes.SetAttribute("srcset", srcset);
            
            // Add sizes attribute to help browser select the right image
            output.Attributes.SetAttribute("sizes", "(max-width: 320px) 320px, (max-width: 640px) 640px, (max-width: 960px) 960px, 1280px");
            
            // Add lazy loading attribute if requested
            if (IsLazy)
            {
                output.Attributes.SetAttribute("loading", "lazy");
            }
        }

        private async Task<string> GenerateSrcSetAsync(string fullPath, string imagePath)
        {
            string cacheKey = $"srcset_{imagePath}_{File.GetLastWriteTime(fullPath).Ticks}";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out string cachedSrcSet))
            {
                return cachedSrcSet;
            }

            // Get directory and filename parts
            string directory = Path.GetDirectoryName(imagePath);
            string filename = Path.GetFileNameWithoutExtension(imagePath);
            string extension = Path.GetExtension(imagePath);

            // Create responsive directory if it doesn't exist
            string responsiveDir = Path.Combine(_env.WebRootPath, directory, "responsive");
            if (!Directory.Exists(responsiveDir))
            {
                Directory.CreateDirectory(responsiveDir);
            }

            // Build srcset string
            var srcsetBuilder = new System.Text.StringBuilder();

            // Create different sized variants
            foreach (int width in WidthBreakpoints)
            {
                string responsiveFilename = $"{filename}-{width}w{extension}";
                string responsivePath = Path.Combine(directory, "responsive", responsiveFilename);
                string fullResponsivePath = Path.Combine(_env.WebRootPath, responsivePath);

                // Check if the variant already exists
                if (!File.Exists(fullResponsivePath))
                {
                    await CreateImageVariantAsync(fullPath, fullResponsivePath, width);
                }

                // Add to srcset
                if (srcsetBuilder.Length > 0)
                {
                    srcsetBuilder.Append(", ");
                }
                srcsetBuilder.Append($"/{responsivePath} {width}w");
            }

            string srcset = srcsetBuilder.ToString();
            
            // Store in cache
            _cache.Set(cacheKey, srcset, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(24)
            });

            return srcset;
        }

        private async Task CreateImageVariantAsync(string sourcePath, string targetPath, int targetWidth)
        {
            try
            {
                using var image = await Image.LoadAsync(sourcePath);
                
                // Calculate height to maintain aspect ratio
                int targetHeight = (int)((float)image.Height * targetWidth / image.Width);
                
                // Resize the image
                image.Mutate(x => x.Resize(targetWidth, targetHeight));
                
                // Save the resized image
                await image.SaveAsync(targetPath);
            }
            catch (Exception)
            {
                // Silently fail, original image will be used instead
            }
        }
    }
}