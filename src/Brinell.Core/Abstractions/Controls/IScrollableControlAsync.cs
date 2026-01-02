namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Async interface for controls that support scrolling.
/// For platforms with async-native drivers (Playwright).
/// Implements FR-002.7: Scroll-to-Element Support and AD-009 v3.2.
/// </summary>
public interface IScrollableControlAsync : IControlObjectAsync
{
    /// <summary>
    /// Scroll until the element with the specified automation ID is visible.
    /// </summary>
    /// <param name="automationId">The automation ID of the element to scroll to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask ScrollToElementAsync(string automationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Scroll to the top of the content.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask ScrollToTopAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Scroll to the bottom of the content.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask ScrollToBottomAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Scroll up by the specified distance.
    /// </summary>
    /// <param name="distance">The distance to scroll (platform-specific units). Default: 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask ScrollUpAsync(int distance = 100, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Scroll down by the specified distance.
    /// </summary>
    /// <param name="distance">The distance to scroll (platform-specific units). Default: 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask ScrollDownAsync(int distance = 100, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Scroll left by the specified distance.
    /// </summary>
    /// <param name="distance">The distance to scroll (platform-specific units). Default: 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask ScrollLeftAsync(int distance = 100, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Scroll right by the specified distance.
    /// </summary>
    /// <param name="distance">The distance to scroll (platform-specific units). Default: 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask ScrollRightAsync(int distance = 100, CancellationToken cancellationToken = default);
}
