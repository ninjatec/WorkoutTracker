using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace WorkoutTrackerWeb.TagHelpers
{
    /// <summary>
    /// Tag helper to automatically add CSP nonces to script and style tags
    /// </summary>
    [HtmlTargetElement("script", Attributes = "csp-nonce")]
    [HtmlTargetElement("style", Attributes = "csp-nonce")]
    public class CspTagHelper : TagHelper
    {
        [ViewContext]
        public ViewContext ViewContext { get; set; } = default!;

        /// <summary>
        /// When set to true, automatically adds the appropriate CSP nonce
        /// </summary>
        [HtmlAttributeName("csp-nonce")]
        public bool UseNonce { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (!UseNonce)
                return;

            var httpContext = ViewContext.HttpContext;
            string? nonce = null;

            // Determine which nonce to use based on the tag name
            if (string.Equals(context.TagName, "script", StringComparison.OrdinalIgnoreCase))
            {
                nonce = httpContext.Items["CSP-Script-Nonce"] as string;
            }
            else if (string.Equals(context.TagName, "style", StringComparison.OrdinalIgnoreCase))
            {
                nonce = httpContext.Items["CSP-Style-Nonce"] as string;
            }

            if (!string.IsNullOrEmpty(nonce))
            {
                output.Attributes.SetAttribute("nonce", nonce);
            }

            // Remove the csp-nonce attribute from the output
            output.Attributes.RemoveAll("csp-nonce");
        }
    }

    /// <summary>
    /// Extension methods for accessing CSP nonces in views
    /// </summary>
    public static class CspExtensions
    {
        public static string? GetStyleNonce(this ViewContext viewContext)
        {
            return viewContext.HttpContext.Items["CSP-Style-Nonce"] as string;
        }

        public static string? GetScriptNonce(this ViewContext viewContext)
        {
            return viewContext.HttpContext.Items["CSP-Script-Nonce"] as string;
        }

        public static IHtmlContent StyleNonceAttribute(this IHtmlHelper htmlHelper)
        {
            var nonce = htmlHelper.ViewContext.GetStyleNonce();
            if (string.IsNullOrEmpty(nonce))
                return HtmlString.Empty;
            
            return new HtmlString($"nonce=\"{nonce}\"");
        }

        public static IHtmlContent ScriptNonceAttribute(this IHtmlHelper htmlHelper)
        {
            var nonce = htmlHelper.ViewContext.GetScriptNonce();
            if (string.IsNullOrEmpty(nonce))
                return HtmlString.Empty;
            
            return new HtmlString($"nonce=\"{nonce}\"");
        }
    }
}
