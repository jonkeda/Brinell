using Brinell.Core.Locators;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.Extensions;

/// <summary>
/// Extension methods for converting Brinell Locator to Appium/Selenium By selectors.
/// </summary>
public static class LocatorExtensions
{
    /// <summary>
    /// Converts a Brinell Locator to an Appium/Selenium By selector.
    /// </summary>
    /// <param name="locator">The locator to convert.</param>
    /// <returns>A By selector that can be used with Appium WebDriver.</returns>
    /// <exception cref="ArgumentNullException">Thrown when locator is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when locator strategy is not supported.</exception>
    public static By ToBy(this Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        
        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId => MobileBy.Id(locator.Value),
            LocatorStrategy.AccessibilityId => MobileBy.AccessibilityId(locator.Value),
            LocatorStrategy.Id => By.Id(locator.Value),
            LocatorStrategy.Name => By.Name(locator.Value),
            LocatorStrategy.ClassName => By.ClassName(locator.Value),
            LocatorStrategy.XPath => By.XPath(locator.Value),
            LocatorStrategy.Css => By.CssSelector(locator.Value),
            LocatorStrategy.TagName => By.TagName(locator.Value),
            LocatorStrategy.LinkText => By.LinkText(locator.Value),
            LocatorStrategy.PartialLinkText => By.PartialLinkText(locator.Value),
            _ => throw new ArgumentOutOfRangeException(
                nameof(locator), 
                locator.Strategy, 
                $"Locator strategy '{locator.Strategy}' is not supported for MAUI/Appium.")
        };
    }
}
