using System.Drawing;

namespace Brinell.Maui.Interfaces;

/// <summary>
/// Abstraction over AppiumElement to enable unit testing with Moq.
/// This interface can be mocked because it doesn't require an Appium connection.
/// </summary>
public interface IMauiElement
{
    #region State Properties
    
    /// <summary>
    /// Gets a value indicating whether the element is displayed.
    /// </summary>
    bool Displayed { get; }
    
    /// <summary>
    /// Gets a value indicating whether the element is enabled.
    /// </summary>
    bool Enabled { get; }
    
    /// <summary>
    /// Gets a value indicating whether the element is selected.
    /// </summary>
    bool Selected { get; }
    
    /// <summary>
    /// Gets the text content of the element.
    /// </summary>
    string Text { get; }
    
    /// <summary>
    /// Gets the tag name of the element.
    /// </summary>
    string TagName { get; }
    
    /// <summary>
    /// Gets the location of the element on screen.
    /// </summary>
    Point Location { get; }
    
    /// <summary>
    /// Gets the size of the element.
    /// </summary>
    Size Size { get; }
    
    #endregion
    
    #region Actions
    
    /// <summary>
    /// Clicks the element.
    /// </summary>
    void Click();
    
    /// <summary>
    /// Sends keystrokes to the element.
    /// </summary>
    /// <param name="text">The text to send.</param>
    void SendKeys(string text);
    
    /// <summary>
    /// Clears the content of the element.
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Submits a form.
    /// </summary>
    void Submit();
    
    /// <summary>
    /// Scrolls the element into the visible area of the viewport.
    /// Uses Selenium 4 ScrollToElement action.
    /// </summary>
    /// <param name="driver">The driver to use for creating the scroll action.</param>
    void ScrollIntoView(IMauiDriver driver);
    
    #endregion
    
    #region Attribute Access
    
    /// <summary>
    /// Gets the value of the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <returns>The attribute value, or null if not present.</returns>
    string? GetAttribute(string attributeName);
    
    /// <summary>
    /// Gets the value of a DOM attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <returns>The attribute value, or null if not present.</returns>
    string? GetDomAttribute(string attributeName);
    
    /// <summary>
    /// Gets the value of a DOM property.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The property value, or null if not present.</returns>
    string? GetDomProperty(string propertyName);
    
    /// <summary>
    /// Gets the value of a CSS property.
    /// </summary>
    /// <param name="propertyName">The name of the CSS property.</param>
    /// <returns>The CSS value.</returns>
    string GetCssValue(string propertyName);
    
    #endregion
    
    #region Child Element Finding
    
    /// <summary>
    /// Finds a child element matching the locator.
    /// </summary>
    /// <param name="by">The locator to use.</param>
    /// <returns>The matching element.</returns>
    IMauiElement FindElement(By by);
    
    /// <summary>
    /// Finds all child elements matching the locator.
    /// </summary>
    /// <param name="by">The locator to use.</param>
    /// <returns>A list of matching elements.</returns>
    IReadOnlyList<IMauiElement> FindElements(By by);
    
    #endregion
    
    #region Escape Hatch
    
    /// <summary>
    /// Gets the underlying AppiumElement for advanced scenarios.
    /// Use sparingly - prefer interface methods for testability.
    /// </summary>
    /// <returns>The wrapped AppiumElement.</returns>
    AppiumElement UnwrapElement();
    
    #endregion
}
