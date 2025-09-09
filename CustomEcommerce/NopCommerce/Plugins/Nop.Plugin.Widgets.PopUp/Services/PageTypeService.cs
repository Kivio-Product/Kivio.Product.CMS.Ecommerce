using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Nop.Plugin.Widgets.PopUp.Services;

/// <summary>
/// Represents a service for detecting page types
/// </summary>
public class PageTypeService : IPageTypeService
{
    /// <summary>
    /// Gets the current page type based on the request
    /// </summary>
    /// <param name="httpContext">HTTP context</param>
    /// <returns>The current page type</returns>
    public string GetCurrentPageType(HttpContext httpContext)
    {
        if (httpContext?.Request?.Path == null)
            return "unknown";

        var path = httpContext.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var routeData = httpContext.GetRouteData();

        if (routeData?.Values == null)
            return "unknown";

        var controller = routeData.Values["controller"]?.ToString()?.ToLowerInvariant();
        var action = routeData.Values["action"]?.ToString()?.ToLowerInvariant();

        // Home page
        if ((controller == "home" && action == "index") || path == "/" || path == "")
            return "home";

        // Category pages
        if (controller == "catalog" && action == "category")
            return "category";

        // Product pages
        if (controller == "product" && action == "productdetails")
            return "product";

        // Manufacturer pages
        if (controller == "catalog" && action == "manufacturer")
            return "manufacturer";

        // Topic pages
        if (controller == "topic" && action == "topicdetails")
            return "topic";

        // Blog pages
        if (controller == "blog")
            return "blog";

        // News pages
        if (controller == "news")
            return "newsitem";

        // Shopping cart
        if (controller == "shoppingcart")
            return "cart";

        // Checkout
        if (controller == "checkout")
            return "checkout";

        return "other";
    }

    /// <summary>
    /// Checks if the popup should be displayed on the current page
    /// </summary>
    /// <param name="httpContext">HTTP context</param>
    /// <param name="displayPages">Configured display pages</param>
    /// <returns>True if the popup should be displayed; otherwise, false</returns>
    public bool ShouldDisplayPopup(HttpContext httpContext, string displayPages)
    {
        if (string.IsNullOrWhiteSpace(displayPages))
            return false;

        var currentPageType = GetCurrentPageType(httpContext);
        var allowedPages = displayPages.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().ToLowerInvariant())
            .ToList();

        // If "all" is selected, show on all pages
        if (allowedPages.Contains("all"))
            return true;

        return allowedPages.Contains(currentPageType);
    }
}
