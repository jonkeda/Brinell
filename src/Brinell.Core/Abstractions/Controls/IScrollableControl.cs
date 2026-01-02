namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for controls that support scrolling.
/// Implements FR-002.7: Scroll-to-Element Support.
/// </summary>
public interface IScrollableControl : IControlObject
{
    /// <summary>
    /// Scroll until the element with the specified automation ID is visible.
    /// </summary>
    /// <param name="automationId">The automation ID of the element to scroll to.</param>
    void ScrollToElement(string automationId);
    
    /// <summary>
    /// Scroll to the top of the content.
    /// </summary>
    void ScrollToTop();
    
    /// <summary>
    /// Scroll to the bottom of the content.
    /// </summary>
    void ScrollToBottom();
    
    /// <summary>
    /// Scroll up by the specified distance.
    /// </summary>
    /// <param name="distance">The distance to scroll (platform-specific units). Default: 100.</param>
    void ScrollUp(int distance = 100);
    
    /// <summary>
    /// Scroll down by the specified distance.
    /// </summary>
    /// <param name="distance">The distance to scroll (platform-specific units). Default: 100.</param>
    void ScrollDown(int distance = 100);
    
    /// <summary>
    /// Scroll left by the specified distance.
    /// </summary>
    /// <param name="distance">The distance to scroll (platform-specific units). Default: 100.</param>
    void ScrollLeft(int distance = 100);
    
    /// <summary>
    /// Scroll right by the specified distance.
    /// </summary>
    /// <param name="distance">The distance to scroll (platform-specific units). Default: 100.</param>
    void ScrollRight(int distance = 100);
}
