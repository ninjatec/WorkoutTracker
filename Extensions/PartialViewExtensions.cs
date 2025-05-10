// filepath: /Users/marccoxall/Documents/projects/WorkoutTracker/Extensions/PartialViewExtensions.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace WorkoutTrackerWeb.Extensions
{
    public static class PartialViewExtensions
    {
        public static async Task<string> RenderPartialToStringAsync<TModel>(
            this Controller controller,
            string partialName,
            TModel model,
            ViewDataDictionary viewData = null)
        {
            if (string.IsNullOrEmpty(partialName))
            {
                throw new ArgumentNullException(nameof(partialName));
            }

            var factory = controller.HttpContext.RequestServices.GetService<ITempDataDictionaryFactory>();
            var viewEngine = controller.HttpContext.RequestServices.GetService<ICompositeViewEngine>();

            if (viewEngine == null)
            {
                throw new InvalidOperationException("ICompositeViewEngine not found in services");
            }

            // Create view data if not provided
            ViewDataDictionary viewDataDict;
            if (viewData != null)
            {
                viewDataDict = viewData;
            }
            else
            {
                var metadataProvider = controller.HttpContext.RequestServices.GetService<IModelMetadataProvider>();
                viewDataDict = new ViewDataDictionary<TModel>(metadataProvider, controller.ModelState) { Model = model };
            }
            
            using var sw = new StringWriter();
            var viewContext = new ViewContext(
                controller.ControllerContext,
                new EmptyView(),
                viewDataDict,
                factory.GetTempData(controller.HttpContext),
                sw,
                new HtmlHelperOptions()
            );

            var result = viewEngine.FindView(controller.ControllerContext, partialName, false);

            if (!result.Success)
            {
                throw new InvalidOperationException($"Couldn't find view '{partialName}'");
            }

            await result.View.RenderAsync(viewContext);
            return sw.ToString();
        }

        public static async Task<string> RenderViewComponentToStringAsync(
            this Controller controller,
            string componentName,
            object arguments = null)
        {
            if (string.IsNullOrEmpty(componentName))
            {
                throw new ArgumentNullException(nameof(componentName));
            }

            var viewComponentHelper = controller.HttpContext.RequestServices.GetService<IViewComponentHelper>();

            if (viewComponentHelper != null)
            {
                // Set the view context
                var viewContext = new ViewContext(
                    controller.ControllerContext,
                    new EmptyView(),
                    controller.ViewData,
                    controller.TempData,
                    new StringWriter(),
                    new HtmlHelperOptions()
                );

                // Use reflection to set the view context if the helper has a Contextualize method
                var contextualizeMethod = viewComponentHelper.GetType().GetMethod("Contextualize");
                if (contextualizeMethod != null)
                {
                    contextualizeMethod.Invoke(viewComponentHelper, new object[] { viewContext });
                }
            }

            using var sw = new StringWriter();
            var result = arguments == null 
                ? await viewComponentHelper.InvokeAsync(componentName)
                : await viewComponentHelper.InvokeAsync(componentName, arguments);

            result.WriteTo(sw, System.Text.Encodings.Web.HtmlEncoder.Default);
            
            return sw.ToString();
        }

        private class EmptyView : IView
        {
            public string Path => string.Empty;
            public async Task RenderAsync(ViewContext context)
            {
                await Task.CompletedTask;
            }
        }
    }
}
