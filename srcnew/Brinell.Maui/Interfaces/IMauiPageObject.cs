using Brinell.Core.Interfaces;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.Interfaces;

/// <summary>
/// MAUI-specific page object that narrows the generic TElement to AppiumElement.
/// Represents a page/screen in a MAUI application.
/// </summary>
public interface IMauiPageObject : IPageObject<AppiumElement>, IMauiElementScope
{
    // Inherits all functionality from IPageObject<AppiumElement> and IMauiElementScope
}
