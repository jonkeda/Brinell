using System.Drawing;
using Brinell.Core;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;

namespace Brinell.Maui.Interfaces;

/// <summary>
/// MAUI-specific element interface extending <see cref="IElement{TSelf}"/>.
/// Adds DOM access methods for hybrid WebView apps.
/// This interface can be mocked for unit testing without requiring an Appium connection.
/// </summary>
public interface IMauiElement : IElement<IMauiElement>
{
    #region Identity

    /// <summary>
    /// The element's automation id - the identifier the app author set, in the app author's
    /// terms.
    /// </summary>
    /// <remarks>
    /// Each platform publishes a MAUI <c>AutomationId</c> differently: Windows as the UIA
    /// AutomationId, Android as the view's resource id, iOS as the accessibility identifier.
    /// This property is where that difference is answered, so code that compares ids does not
    /// have to know which platform it is on. Null when the element carries no id.
    /// </remarks>
    string? AutomationId { get; }

    /// <summary>
    /// The element's accessible name - what a screen reader would announce.
    /// </summary>
    /// <remarks>
    /// The other way an element is named, and the only one platform-drawn chrome usually
    /// carries: an Android tab has no text and no id, and answers only to this. Windows reports
    /// the UIA Name, Android the content description (falling back to its text), iOS the
    /// accessibility label. Null when the element has none.
    /// </remarks>
    string? Name { get; }

    #endregion

    #region DOM Access (Hybrid Apps)
    
    /// <summary>
    /// Gets a DOM attribute value (for WebView content).
    /// </summary>
    /// <param name="attributeName">The name of the DOM attribute.</param>
    /// <returns>The attribute value, or null if not present or not applicable.</returns>
    string? GetDomAttribute(string attributeName);
    
    /// <summary>
    /// Gets a DOM property value (for WebView content).
    /// </summary>
    /// <param name="propertyName">The name of the DOM property.</param>
    /// <returns>The property value, or null if not present or not applicable.</returns>
    string? GetDomProperty(string propertyName);
    
    /// <summary>
    /// Gets a computed CSS value (for WebView content).
    /// </summary>
    /// <param name="propertyName">The name of the CSS property.</param>
    /// <returns>The CSS value, or null if not applicable.</returns>
    string? GetCssValue(string propertyName);
    
    #endregion
    
    #region Form Actions
    
    /// <summary>
    /// Submits a form (if the element is within a form).
    /// </summary>
    void Submit();
    
    #endregion
}
