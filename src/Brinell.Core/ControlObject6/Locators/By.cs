namespace Brinell.Core.ControlObject6.Locators;

/// <summary>
/// Static factory for creating ControlLocator instances.
/// Provides a fluent API for element location strategies.
/// </summary>
public static class By
{
    /// <summary>
    /// Find by AutomationId (MAUI) or data-automation-id (Blazor).
    /// This is the most reliable cross-platform strategy.
    /// </summary>
    public static ControlLocator AutomationId(string value)
        => new(LocatorStrategy.AutomationId, value);

    /// <summary>
    /// Find by Name property (MAUI) or name attribute (Blazor).
    /// </summary>
    public static ControlLocator Name(string value)
        => new(LocatorStrategy.Name, value);

    /// <summary>
    /// Find by id attribute (Blazor only).
    /// </summary>
    public static ControlLocator Id(string value)
        => new(LocatorStrategy.Id, value);

    /// <summary>
    /// Find by class name (MAUI) or CSS class (Blazor).
    /// </summary>
    public static ControlLocator ClassName(string value)
        => new(LocatorStrategy.ClassName, value);

    /// <summary>
    /// Find by XPath expression.
    /// </summary>
    public static ControlLocator XPath(string value)
        => new(LocatorStrategy.XPath, value);

    /// <summary>
    /// Find by CSS selector (Blazor only).
    /// </summary>
    public static ControlLocator Css(string value)
        => new(LocatorStrategy.Css, value);

    /// <summary>
    /// Find by exact text content.
    /// </summary>
    public static ControlLocator Text(string value)
        => new(LocatorStrategy.Text, value);

    /// <summary>
    /// Find by partial text content.
    /// </summary>
    public static ControlLocator PartialText(string value)
        => new(LocatorStrategy.PartialText, value);

    /// <summary>
    /// Find by accessibility ID (MAUI) or aria-label (Blazor).
    /// </summary>
    public static ControlLocator AccessibilityId(string value)
        => new(LocatorStrategy.AccessibilityId, value);

    /// <summary>
    /// Find by tag name (Blazor) or control type (MAUI).
    /// </summary>
    public static ControlLocator TagName(string value)
        => new(LocatorStrategy.TagName, value);

    /// <summary>
    /// Find by associated label.
    /// </summary>
    public static ControlLocator Label(string value)
        => new(LocatorStrategy.Label, value);

    /// <summary>
    /// Find by placeholder text.
    /// </summary>
    public static ControlLocator Placeholder(string value)
        => new(LocatorStrategy.Placeholder, value);

    /// <summary>
    /// Find by title attribute.
    /// </summary>
    public static ControlLocator Title(string value)
        => new(LocatorStrategy.Title, value);

    /// <summary>
    /// Find by ARIA role.
    /// </summary>
    public static ControlLocator Role(string value)
        => new(LocatorStrategy.Role, value);

    /// <summary>
    /// Find by data-testid attribute.
    /// </summary>
    public static ControlLocator TestId(string value)
        => new(LocatorStrategy.TestId, value);

    /// <summary>
    /// Find by custom data attribute (Blazor only).
    /// </summary>
    /// <param name="name">The data attribute name (without 'data-' prefix).</param>
    /// <param name="value">The attribute value.</param>
    public static ControlLocator DataAttribute(string name, string value)
        => new(LocatorStrategy.DataAttribute, value, dataAttributeName: name);
}
