namespace Brinell.Core.Interfaces;

/// <summary>
/// Interface for controls that support swipe gestures.
/// Primarily used for mobile platforms.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public interface ISwipeableControlObject<TScope> : IControlObject<TScope>
{
    /// <summary>
    /// Performs a swipe left gesture.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    TScope SwipeLeft(int? timeoutMs = null);
    
    /// <summary>
    /// Performs a swipe right gesture.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    TScope SwipeRight(int? timeoutMs = null);
    
    /// <summary>
    /// Performs a swipe up gesture.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    TScope SwipeUp(int? timeoutMs = null);
    
    /// <summary>
    /// Performs a swipe down gesture.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    TScope SwipeDown(int? timeoutMs = null);
    
    /// <summary>
    /// Performs a swipe from one point to another within the control.
    /// </summary>
    /// <param name="startX">Starting X coordinate (relative to control).</param>
    /// <param name="startY">Starting Y coordinate (relative to control).</param>
    /// <param name="endX">Ending X coordinate (relative to control).</param>
    /// <param name="endY">Ending Y coordinate (relative to control).</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    TScope Swipe(int startX, int startY, int endX, int endY, int? timeoutMs = null);
}
