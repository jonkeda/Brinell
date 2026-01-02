using Brinell.Core.Abstractions.Controls;

namespace Brinell.Maui.Abstractions.Controls;

/// <summary>
/// Platform-specific interface for MAUI/mobile controls.
/// Extends core control functionality with mobile gesture support.
/// </summary>
public interface IMobileControl : IControlObject
{
    /// <summary>
    /// Tap the control (single touch).
    /// </summary>
    void Tap();
    
    /// <summary>
    /// Double-tap the control.
    /// </summary>
    void DoubleTap();
    
    /// <summary>
    /// Long-press the control.
    /// </summary>
    void LongPress(int durationMs = 500);
    
    /// <summary>
    /// Swipe across the control.
    /// </summary>
    void Swipe(SwipeDirection direction, int distancePx = 100);
    
    /// <summary>
    /// Scroll within the control.
    /// </summary>
    void Scroll(ScrollDirection direction, int amountPx = 100);
    
    /// <summary>
    /// Pinch zoom operation.
    /// </summary>
    void PinchZoom(double scaleFactor);
}
