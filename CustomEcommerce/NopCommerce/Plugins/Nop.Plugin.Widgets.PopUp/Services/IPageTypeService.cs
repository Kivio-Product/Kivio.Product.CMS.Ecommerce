using Microsoft.AspNetCore.Http;

namespace Nop.Plugin.Widgets.PopUp.Services;

/// <summary>
/// Represents a service for detecting page types
/// </summary>
public interface IPageTypeService
{
    /// <summary>
    /// Gets the current page type based on the request
    /// </summary>
    /// <param name="httpContext">HTTP context</param>
    /// <returns>The current page type</returns>
    string GetCurrentPageType(HttpContext httpContext);

    /// <summary>
    /// Checks if the popup should be displayed on the current page
    /// </summary>
    /// <param name="httpContext">HTTP context</param>
    /// <param name="displayPages">Configured display pages</param>
    /// <returns>True if the popup should be displayed; otherwise, false</returns>
    bool ShouldDisplayPopup(HttpContext httpContext, string displayPages);
}
