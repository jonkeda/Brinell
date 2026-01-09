using Brinell.Core.Locators;
using Brinell.Maui.Interfaces;
using OpenQA.Selenium;

namespace Brinell.Maui.Controls;

/// <summary>
/// Converts Brinell Locator objects to Selenium By objects for Appium.
/// Handles platform-specific locator strategies.
/// </summary>
internal static class LocatorConverter
{
    /// <summary>
    /// Converts a Brinell Locator to a Selenium By locator.
    /// </summary>
    /// <param name="locator">The Brinell locator to convert.</param>
    /// <param name="platform">The target MAUI platform for platform-specific strategies.</param>
    /// <returns>A Selenium By locator.</returns>
    public static By ToBy(Locator locator, MauiPlatform platform)
    {
        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId => GetAutomationIdBy(locator.Value, platform),
            LocatorStrategy.Id => By.Id(locator.Value),
            LocatorStrategy.Name => By.Name(locator.Value),
            LocatorStrategy.ClassName => By.ClassName(locator.Value),
            LocatorStrategy.XPath => By.XPath(locator.Value),
            LocatorStrategy.AccessibilityId => GetAccessibilityIdBy(locator.Value, platform),
            LocatorStrategy.Text => GetTextBy(locator.Value, platform),
            _ => throw new NotSupportedException(
                $"Locator strategy '{locator.Strategy}' is not supported for MAUI platform.")
        };
    }

    /// <summary>
    /// Gets the appropriate By locator for AutomationId based on platform.
    /// </summary>
    private static By GetAutomationIdBy(string value, MauiPlatform platform)
    {
        // AutomationId in MAUI maps to accessibility-id in Appium
        return platform switch
        {
            MauiPlatform.Android => MobileBy.AccessibilityId(value),
            MauiPlatform.iOS => MobileBy.AccessibilityId(value),
            MauiPlatform.Windows => MobileBy.AccessibilityId(value),
            _ => MobileBy.AccessibilityId(value)
        };
    }

    /// <summary>
    /// Gets the appropriate By locator for AccessibilityId based on platform.
    /// </summary>
    private static By GetAccessibilityIdBy(string value, MauiPlatform platform)
    {
        return MobileBy.AccessibilityId(value);
    }

    /// <summary>
    /// Gets a By locator for finding elements by text content.
    /// </summary>
    private static By GetTextBy(string text, MauiPlatform platform)
    {
        return platform switch
        {
            MauiPlatform.Android => By.XPath($"//*[@text='{EscapeXPath(text)}']"),
            MauiPlatform.iOS => By.XPath($"//*[@label='{EscapeXPath(text)}' or @value='{EscapeXPath(text)}']"),
            MauiPlatform.Windows => By.XPath($"//*[@Name='{EscapeXPath(text)}']"),
            _ => By.XPath($"//*[text()='{EscapeXPath(text)}']")
        };
    }

    /// <summary>
    /// Escapes special characters for XPath string values.
    /// </summary>
    private static string EscapeXPath(string value)
    {
        if (!value.Contains('\''))
            return value;

        if (!value.Contains('"'))
            return value;

        // Handle strings with both quote types
        var parts = value.Split('\'');
        return "concat('" + string.Join("',\"'\",'", parts) + "')";
    }
}

/// <summary>
/// Mobile-specific By locator strategies for Appium.
/// </summary>
internal static class MobileBy
{
    /// <summary>
    /// Creates a By locator using accessibility ID.
    /// </summary>
    public static By AccessibilityId(string accessibilityId)
    {
        // Appium uses XPath with accessibility-id or the -android uiautomator
        // For cross-platform, we use the accessibility ID attribute
        return By.XPath($"//*[@content-desc='{accessibilityId}' or @accessibility-id='{accessibilityId}' or @AutomationId='{accessibilityId}']");
    }
}
