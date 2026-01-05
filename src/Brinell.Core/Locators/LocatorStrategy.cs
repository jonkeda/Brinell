namespace Brinell.Core.Locators;

/// <summary>
/// Specifies the strategy used to locate elements.
/// </summary>
public enum LocatorStrategy
{
    /// <summary>
    /// Locate by AutomationId (XAML) or data-automation-id (HTML).
    /// </summary>
    AutomationId,

    /// <summary>
    /// Locate by Name attribute.
    /// </summary>
    Name,

    /// <summary>
    /// Locate by Id attribute (HTML id or AccessibilityId).
    /// </summary>
    Id,

    /// <summary>
    /// Locate by class name.
    /// </summary>
    ClassName,

    /// <summary>
    /// Locate by XPath expression.
    /// </summary>
    XPath,

    /// <summary>
    /// Locate by CSS selector (HTML only).
    /// </summary>
    Css,

    /// <summary>
    /// Locate by exact text content.
    /// </summary>
    Text,

    /// <summary>
    /// Locate by partial text content.
    /// </summary>
    PartialText,

    /// <summary>
    /// Locate by data-testid attribute.
    /// </summary>
    TestId,

    /// <summary>
    /// Chained locator (parent -> child).
    /// </summary>
    Chained,

    /// <summary>
    /// Locate by tag name.
    /// </summary>
    TagName,

    /// <summary>
    /// Locate by accessibility label.
    /// </summary>
    AccessibilityLabel
}
