namespace Brinell.Core.Abstractions;

/// <summary>
/// Abstracts the underlying driver (FlaUI, Appium, Selenium).
/// </summary>
public interface IDriverAdapter : IDisposable
{
    /// <summary>
    /// Find element by AutomationId.
    /// </summary>
    IElementAdapter? FindElement(string automationId);
    
    /// <summary>
    /// Find element by XPath.
    /// </summary>
    IElementAdapter? FindElementByXPath(string xpath);
    
    /// <summary>
    /// Find all elements matching AutomationId.
    /// </summary>
    IReadOnlyCollection<IElementAdapter> FindElements(string automationId);
    
    /// <summary>
    /// Click an element.
    /// </summary>
    void Click(IElementAdapter element);
    
    /// <summary>
    /// Send keys/text to an element.
    /// </summary>
    void SendKeys(IElementAdapter element, string text);
    
    /// <summary>
    /// Clear an element's text.
    /// </summary>
    void Clear(IElementAdapter element);
    
    /// <summary>
    /// Get element's text content.
    /// </summary>
    string? GetText(IElementAdapter element);
    
    /// <summary>
    /// Get an attribute value from an element.
    /// </summary>
    string? GetAttribute(IElementAdapter element, string name);
    
    /// <summary>
    /// Check if element is displayed (not off-screen).
    /// </summary>
    bool IsDisplayed(IElementAdapter element);
    
    /// <summary>
    /// Check if element is enabled.
    /// </summary>
    bool IsEnabled(IElementAdapter element);
}
