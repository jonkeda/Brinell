using Brinell.Core.Interfaces;
using OpenQA.Selenium;

namespace Brinell.Blazor.Interfaces;

/// <summary>
/// Blazor-specific element scope that narrows the generic TElement to IWebElement.
/// Provides typed access to Selenium element finding within the Blazor platform.
/// </summary>
public interface IBlazorElementScope : IElementScope<IWebElement>
{
    /// <summary>
    /// Gets the test context associated with this scope.
    /// Provides back-reference for accessing timeouts, logger, and driver.
    /// </summary>
    IBlazorTestContext Context { get; }
}
