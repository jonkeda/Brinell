using Brinell.Core.Interfaces;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.Interfaces;

/// <summary>
/// MAUI-specific element scope that provides access to the test context.
/// Extends the generic element scope with AppiumElement as the native element type.
/// </summary>
public interface IMauiElementScope : IElementScope<AppiumElement>
{
    /// <summary>
    /// Gets the MAUI test context for this scope.
    /// </summary>
    IMauiTestContext Context { get; }
}
