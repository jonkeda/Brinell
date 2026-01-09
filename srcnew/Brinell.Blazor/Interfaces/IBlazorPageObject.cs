using Brinell.Core.Interfaces;
using OpenQA.Selenium;

namespace Brinell.Blazor.Interfaces;

/// <summary>
/// Blazor-specific page object that narrows the generic TElement to IWebElement.
/// Represents a page/component in a Blazor application.
/// </summary>
public interface IBlazorPageObject : IPageObject<IWebElement>, IBlazorElementScope
{
    /// <summary>
    /// Gets the URL path for this page (relative to base URL).
    /// </summary>
    string? Path { get; }

    /// <summary>
    /// Navigates directly to this page using its path.
    /// </summary>
    void NavigateTo();
}
