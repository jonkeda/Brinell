using Brinell.Core.Locators;
using OpenQA.Selenium;

namespace Brinell.Blazor.Controls;

/// <summary>
/// Converts Brinell Locator objects to Selenium By objects for web browsers.
/// </summary>
internal static class LocatorConverter
{
    /// <summary>
    /// Converts a Brinell Locator to a Selenium By locator.
    /// </summary>
    /// <param name="locator">The Brinell locator to convert.</param>
    /// <returns>A Selenium By locator.</returns>
    public static By ToBy(Locator locator)
    {
        return locator.Strategy switch
        {
            LocatorStrategy.Id => By.Id(locator.Value),
            LocatorStrategy.Name => By.Name(locator.Value),
            LocatorStrategy.ClassName => By.ClassName(locator.Value),
            LocatorStrategy.Css => By.CssSelector(locator.Value),
            LocatorStrategy.XPath => By.XPath(locator.Value),
            LocatorStrategy.TagName => By.TagName(locator.Value),
            LocatorStrategy.LinkText => By.LinkText(locator.Value),
            LocatorStrategy.PartialLinkText => By.PartialLinkText(locator.Value),
            LocatorStrategy.DataTestId => By.CssSelector($"[data-testid='{locator.Value}']"),
            LocatorStrategy.DataAutomationId => By.CssSelector($"[data-automation-id='{locator.Value}']"),
            LocatorStrategy.AutomationId => By.CssSelector($"[data-automation-id='{locator.Value}']"),
            LocatorStrategy.Text => By.XPath($"//*[text()='{EscapeXPath(locator.Value)}']"),
            LocatorStrategy.AccessibilityId => By.CssSelector($"[aria-label='{locator.Value}']"),
            _ => throw new NotSupportedException(
                $"Locator strategy '{locator.Strategy}' is not supported for Blazor platform.")
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

        // Handle strings with both quote types using concat
        var parts = value.Split('\'');
        return "concat('" + string.Join("',\"'\",'", parts) + "')";
    }
}
