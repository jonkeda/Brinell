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
    // There is deliberately no platform-less ToBy overload. One existed and defaulted to
    // MauiPlatform.Windows, so every AutomationId on Android resolved as an AccessibilityId
    // (content-desc) instead of By.Id (resource-id) - which is how MAUI actually surfaces it
    // there. The result was that a control was found only when it happened to carry a
    // content-desc, and the caller had no way to see why. The platform is always known at the
    // call site; requiring it makes the mistake unrepresentable.

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
            // MAUI surfaces AutomationId differently on each mobile platform:
            // - Android: the resource-id attribute, so By.Id
            // - iOS: the accessibility identifier
            LocatorStrategy.AutomationId => platform switch
            {
                MauiPlatform.Android => By.Id(locator.Value),
                MauiPlatform.iOS => MobileBy.AccessibilityId(locator.Value),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(platform), platform,
                    "Brinell.Maui.Appium drives Android and iOS. Windows uses Brinell.Maui.FlaUI.")
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
            LocatorStrategy.ControlType => ToControlTypeBy(locator.Value, platform),
            _ => throw new ArgumentOutOfRangeException(
                nameof(locator), 
                locator.Strategy, 
                $"Locator strategy '{locator.Strategy}' is not supported for MAUI/Appium.")
        };
    }

    private static By ToControlTypeBy(string controlType, MauiPlatform platform)
    {
        var className = (platform, controlType.ToLowerInvariant()) switch
        {
            (MauiPlatform.Android, "entry") => "android.widget.EditText",
            (MauiPlatform.iOS, "entry") => "XCUIElementTypeTextField",
            // A MAUI Button renders as a MaterialButton, but Android reports the accessibility
            // class of its Button ancestor, which is what a class-name match sees.
            (MauiPlatform.Android, "button") => "android.widget.Button",
            (MauiPlatform.iOS, "button") => "XCUIElementTypeButton",
            _ => throw new ArgumentOutOfRangeException(
                nameof(controlType), controlType,
                $"Control type '{controlType}' is not supported on {platform}.")
        };

        return MobileBy.ClassName(className);
    }
}
