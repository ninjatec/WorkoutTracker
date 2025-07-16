using Microsoft.AspNetCore.Razor.TagHelpers;

namespace WorkoutTrackerWeb.TagHelpers
{
    /// <summary>
    /// TagHelper to automatically add nonce attribute to script tags for CSP compliance
    /// </summary>
    [HtmlTargetElement("script", Attributes = "add-nonce")]
    public class ScriptNonceTagHelper : TagHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ScriptNonceTagHelper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// When true, adds the CSP script nonce to the script tag
        /// </summary>
        [HtmlAttributeName("add-nonce")]
        public bool AddNonce { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (AddNonce && _httpContextAccessor.HttpContext != null)
            {
                var scriptNonce = _httpContextAccessor.HttpContext.Items["CSP-Script-Nonce"]?.ToString();
                if (!string.IsNullOrEmpty(scriptNonce))
                {
                    output.Attributes.SetAttribute("nonce", scriptNonce);
                }
            }

            // Remove the add-nonce attribute from the final output
            output.Attributes.RemoveAll("add-nonce");
        }
    }
}
