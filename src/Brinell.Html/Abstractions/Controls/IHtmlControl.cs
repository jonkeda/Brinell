namespace Brinell.Html.Abstractions.Controls;

using Brinell.Core.Abstractions.Controls;

/// <summary>
/// Platform-specific interface for HTML/web controls.
/// Extends core control functionality with HTML-specific operations.
/// </summary>
public interface IHtmlControl : IControlObject
{
    /// <summary>
    /// Get an HTML attribute value.
    /// </summary>
    string? GetAttribute(string name);
    
    /// <summary>
    /// Get a CSS property value.
    /// </summary>
    string? GetCssProperty(string name);
    
    /// <summary>
    /// Check if element has a specific CSS class.
    /// </summary>
    bool HasClass(string className);
    
    /// <summary>
    /// Assert element has the specified CSS class.
    /// </summary>
    void AssertHasClass(string className, string? message = null);
    
    /// <summary>
    /// Assert element does not have the specified CSS class.
    /// </summary>
    void AssertNotHasClass(string className, string? message = null);
    
    /// <summary>
    /// Get the inner HTML content.
    /// </summary>
    string GetInnerHtml();
    
    /// <summary>
    /// Get the outer HTML including the element itself.
    /// </summary>
    string GetOuterHtml();
}
