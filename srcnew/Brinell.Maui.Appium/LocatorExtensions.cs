using Brinell.Core.Locators;
using Brinell.Maui.Enums;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.Appium;

/// <summary>
/// Extension methods for converting Brinell Locator to Appium/Selenium By selectors.
/// Internal to the Appium driver implementation.
/// </summary>
internal static class LocatorExtensions
{
    /// <summary>
    /// Converts a Brinell Locator to an Appium/Selenium By selector.
    /// Uses Windows platform semantics (AutomationId maps to AccessibilityId).
    /// </summary>
    /// <param name="locator">The locator to convert.</param>
    /// <returns>A By selector that can be used with Appium WebDriver.</returns>
    /// <exception cref="ArgumentNullException">Thrown when locator is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when locator strategy is not supported.</exception>
    public static By ToBy(this Locator locator)
    {
        return locator.ToBy(MauiPlatform.Windows);
    }
    
    /// <summary>
    /// Converts a Brinell Locator to an Appium/Selenium By selector for a specific platform.
    /// </summary>
    /// <param name="locator">The locator to convert.</param>
    /// <param name="platform">The target platform.</param>
    /// <returns>A By selector that can be used with Appium WebDriver.</returns>
    /// <exception cref="ArgumentNullException">Thrown when locator is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when locator strategy is not supported.</exception>
    public static By ToBy(this Locator locator, MauiPlatform platform)
    {
        ArgumentNullException.ThrowIfNull(locator);
        
        return locator.Strategy switch
        {
            // MAUI AutomationId maps differently per platform:
            // - Windows: AccessibilityId in automation tree
            // - Android: resource-id attribute (use By.Id)
            // - iOS: AccessibilityId
            LocatorStrategy.AutomationId => platform switch
            {
                MauiPlatform.Android => By.Id(locator.Value),
                MauiPlatform.iOS => MobileBy.AccessibilityId(locator.Value),
                _ => MobileBy.AccessibilityId(locator.Value)
            },
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
