using Brinell.Core.Interfaces;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.Interfaces;

/// <summary>
/// MAUI-specific element scope that narrows the generic TElement to AppiumElement.
/// Provides typed access to Appium element finding within the MAUI platform.
/// </summary>
public interface IMauiElementScope : IElementScope<AppiumElement>
{
    /// <summary>
    /// Gets the test context associated with this scope.
    /// Provides back-reference for accessing timeouts, logger, and driver.
    /// </summary>
    IMauiTestContext Context { get; }
}
