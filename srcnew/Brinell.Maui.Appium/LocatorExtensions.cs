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
            // - Android: the resource-id attribute, so the id strategy
            // - iOS: the accessibility identifier
            LocatorStrategy.AutomationId => platform switch
            {
                MauiPlatform.Android => AndroidIdBy(locator.Value),
                MauiPlatform.iOS => MobileBy.AccessibilityId(locator.Value),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(platform), platform,
                    "Brinell.Maui.Appium drives Android and iOS. Windows uses Brinell.Maui.FlaUI.")
            },
            LocatorStrategy.AccessibilityId => MobileBy.AccessibilityId(locator.Value),
            LocatorStrategy.Id => platform == MauiPlatform.Android ? AndroidIdBy(locator.Value) : By.Id(locator.Value),
            LocatorStrategy.Name => platform == MauiPlatform.Android
                ? AndroidNameBy(locator.Value)
                : By.Name(locator.Value),
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

    /// <summary>
    /// Converts a locator for a search under an element, which must stay inside that element's
    /// subtree.
    /// </summary>
    /// <remarks>
    /// Differs from <see cref="ToBy"/> only for a name on Android, whose XPath is made relative: an
    /// absolute one searches the whole page from any element.
    /// </remarks>
    /// <param name="locator">The locator to convert.</param>
    /// <param name="platform">The target platform.</param>
    /// <returns>A By selector for <c>element.FindElement</c>.</returns>
    public static By ToChildBy(this Locator locator, MauiPlatform platform)
    {
        ArgumentNullException.ThrowIfNull(locator);

        return platform == MauiPlatform.Android && locator.Strategy == LocatorStrategy.Name
            ? By.XPath("." + AndroidNamePath(locator.Value))
            : locator.ToBy(platform);
    }

    /// <summary>
    /// A resource id on Android, sent as the W3C <c>id</c> strategy.
    /// </summary>
    /// <remarks>
    /// Not Selenium's <c>By.Id</c>, which the .NET client sends as the CSS selector <c>#name</c>.
    /// UiAutomator2 runs that from an element as a UiSelector that leaves the element's subtree:
    /// asked for <c>TodoRow_Due</c> under a Todo row that has none, it answered with another row's.
    /// The <c>id</c> strategy from the same row finds nothing, as it should (both probed
    /// 2026-09-19, Android 16). A bare name matches in the app's package, as <c>By.Id</c> did.
    /// </remarks>
    private static By AndroidIdBy(string id) => new ResourceIdBy(id);

    private sealed class ResourceIdBy(string id) : By("id", id);

    /// <summary>
    /// A name on Android: the element's accessible name, which is its <c>content-desc</c> when it
    /// has one and its <c>text</c> otherwise - what UI Automation's Name is on Windows.
    /// </summary>
    /// <remarks>
    /// Selenium's <c>By.Name</c> becomes a CSS <c>[name=...]</c> selector, which UiAutomator2 refuses
    /// outright ("'name' is not a valid attribute"), so every name locator failed on Android - a
    /// dialog's buttons among them (found by the Todo sample's delete confirmation).
    /// </remarks>
    /// <exception cref="ArgumentException">The name holds both quote characters, which no XPath 1.0 literal can.</exception>
    private static By AndroidNameBy(string name) => By.XPath(AndroidNamePath(name));

    private static string AndroidNamePath(string name)
    {
        var literal = XPathLiteral(name, name);
        return $"//*[@content-desc={literal} or @text={literal}]";
    }

    /// <exception cref="ArgumentException">The value holds both quote characters, which no XPath 1.0 literal can.</exception>
    private static string XPathLiteral(string value, string located)
        => !value.Contains('\'') ? $"'{value}'"
            : !value.Contains('"') ? $"\"{value}\""
            : throw new ArgumentException($"A value with both quote characters cannot be located on Android: {located}", nameof(located));

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
            // Display text and images, as Windows reads "text" (ControlType.Text) and "image". A
            // MAUI Label renders as a TextView and an Image as an ImageView.
            (MauiPlatform.Android, "text") => "android.widget.TextView",
            (MauiPlatform.iOS, "text") => "XCUIElementTypeStaticText",
            (MauiPlatform.Android, "image") => "android.widget.ImageView",
            (MauiPlatform.iOS, "image") => "XCUIElementTypeImage",
            _ => throw new ArgumentOutOfRangeException(
                nameof(controlType), controlType,
                $"Control type '{controlType}' is not supported on {platform}.")
        };

        return MobileBy.ClassName(className);
    }
}
