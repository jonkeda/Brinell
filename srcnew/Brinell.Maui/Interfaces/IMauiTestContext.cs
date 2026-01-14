using Brinell.Core.Interfaces;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.Interfaces;

/// <summary>
/// MAUI test context interface with Appium driver access.
/// Combines test context capabilities with MAUI element scope.
/// </summary>
public interface IMauiTestContext : ITestContext<AppiumElement>, IMauiElementScope
{
    /// <summary>
    /// Gets the Appium driver for direct WebDriver operations.
    /// </summary>
    AppiumDriver Driver { get; }
    
    /// <summary>
    /// Gets this context as the element scope.
    /// Implementation should return 'this'.
    /// </summary>
    new IMauiTestContext Context { get; }
}
