namespace Brinell.Core.Locators;

/// <summary>
/// Static factory for creating ControlLocator instances.
/// Provides a fluent API for element location strategies.
/// </summary>
public static class By
{
    /// <summary>
    /// Create a locator using AutomationId.
    /// Maps to AutomationId in XAML or data-automation-id in HTML.
    /// </summary>
    /// <param name="value">The AutomationId value.</param>
    /// <returns>A new ControlLocator.</returns>
    public static ControlLocator AutomationId(string value)
        => new(LocatorStrategy.AutomationId, value);

    /// <summary>
    /// Create a locator using Name attribute.
    /// </summary>
    /// <param name="value">The Name value.</param>
    /// <returns>A new ControlLocator.</returns>
    public static ControlLocator Name(string value)
        => new(LocatorStrategy.Name, value);

    /// <summary>
    /// Create a locator using Id attribute.
    /// Maps to AccessibilityId in mobile or HTML id.
    /// </summary>
    /// <param name="value">The Id value.</param>
    /// <returns>A new ControlLocator.</returns>
    public static ControlLocator Id(string value)
        => new(LocatorStrategy.Id, value);

    /// <summary>
    /// Create a locator using class name.
    /// </summary>
    /// <param name="value">The class name.</param>
    /// <returns>A new ControlLocator.</returns>
    public static ControlLocator ClassName(string value)
        => new(LocatorStrategy.ClassName, value);

    /// <summary>
    /// Create a locator using XPath expression.
    /// </summary>
    /// <param name="value">The XPath expression.</param>
    /// <returns>A new ControlLocator.</returns>
    public static ControlLocator XPath(string value)
        => new(LocatorStrategy.XPath, value);

    /// <summary>
    /// Create a locator using CSS selector (HTML only).
    /// </summary>
    /// <param name="value">The CSS selector.</param>
    /// <returns>A new ControlLocator.</returns>
    public static ControlLocator Css(string value)
        => new(LocatorStrategy.Css, value);

    /// <summary>
    /// Create a locator using data-testid attribute.
    /// </summary>
    /// <param name="value">The testid value.</param>
    /// <returns>A new ControlLocator.</returns>
    public static ControlLocator TestId(string value)
        => new(LocatorStrategy.TestId, value);

    /// <summary>
    /// Create a locator using exact text content.
    /// </summary>
    /// <param name="value">The exact text to match.</param>
    /// <returns>A new ControlLocator.</returns>
    public static ControlLocator Text(string value)
        => new(LocatorStrategy.Text, value);

    /// <summary>
    /// Create a locator using partial text content.
    /// </summary>
    /// <param name="value">The partial text to match.</param>
    /// <returns>A new ControlLocator.</returns>
    public static ControlLocator PartialText(string value)
        => new(LocatorStrategy.PartialText, value);

    /// <summary>
    /// Create a locator using tag name.
    /// </summary>
    /// <param name="value">The tag name (e.g., "button", "input").</param>
    /// <returns>A new ControlLocator.</returns>
    public static ControlLocator TagName(string value)
        => new(LocatorStrategy.TagName, value);

    /// <summary>
    /// Create a locator using accessibility label.
    /// </summary>
    /// <param name="value">The accessibility label.</param>
    /// <returns>A new ControlLocator.</returns>
    public static ControlLocator AccessibilityLabel(string value)
        => new(LocatorStrategy.AccessibilityLabel, value);
}
