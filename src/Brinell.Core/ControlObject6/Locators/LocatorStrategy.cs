namespace Brinell.Core.ControlObject6.Locators;

/// <summary>
/// Defines the strategy used to locate UI elements.
/// Different platforms support different subsets of these strategies.
/// </summary>
public enum LocatorStrategy
{
    /// <summary>
    /// Find by AutomationId (MAUI) or data-automation-id (Blazor).
    /// This is the default and most reliable strategy.
    /// </summary>
    AutomationId,

    /// <summary>
    /// Find by Name property (MAUI) or name attribute (Blazor).
    /// </summary>
    Name,

    /// <summary>
    /// Find by id attribute (Blazor only).
    /// </summary>
    Id,

    /// <summary>
    /// Find by class name (MAUI) or CSS class (Blazor).
    /// </summary>
    ClassName,

    /// <summary>
    /// Find by XPath expression.
    /// </summary>
    XPath,

    /// <summary>
    /// Find by CSS selector (Blazor only).
    /// </summary>
    Css,

    /// <summary>
    /// Find by exact text content.
    /// </summary>
    Text,

    /// <summary>
    /// Find by partial text content.
    /// </summary>
    PartialText,

    /// <summary>
    /// Find by accessibility ID (MAUI) or aria-label (Blazor).
    /// </summary>
    AccessibilityId,

    /// <summary>
    /// Find by tag name (Blazor) or control type (MAUI).
    /// </summary>
    TagName,

    /// <summary>
    /// Find by associated label.
    /// </summary>
    Label,

    /// <summary>
    /// Find by placeholder text.
    /// </summary>
    Placeholder,

    /// <summary>
    /// Find by title attribute.
    /// </summary>
    Title,

    /// <summary>
    /// Find by ARIA role.
    /// </summary>
    Role,

    /// <summary>
    /// Find by data-testid attribute.
    /// </summary>
    TestId,

    /// <summary>
    /// Find by custom data attribute (Blazor only).
    /// </summary>
    DataAttribute,

    /// <summary>
    /// Chained locator - find within parent element.
    /// </summary>
    Chained
}
