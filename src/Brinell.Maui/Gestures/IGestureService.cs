using Brinell.Maui.Controls.Base;

namespace Brinell.Maui.Gestures;

/// <summary>
/// Service for advanced multi-element and multi-touch gestures.
/// Single-element gestures like Tap, Swipe, LongPress are available
/// directly on controls. Use this service for complex gestures.
/// </summary>
public interface IGestureService
{
    #region Multi-Element Gestures
    
    /// <summary>
    /// Drag from one control to another.
    /// </summary>
    /// <param name="from">Source control to drag from.</param>
    /// <param name="to">Target control to drag to.</param>
    Task DragTo(ControlBase from, ControlBase to);
    
    /// <summary>
    /// Drag a control by an offset.
    /// </summary>
    /// <param name="control">Control to drag.</param>
    /// <param name="offsetX">Horizontal offset in pixels.</param>
    /// <param name="offsetY">Vertical offset in pixels.</param>
    Task DragByOffset(ControlBase control, int offsetX, int offsetY);
    
    #endregion
    
    #region Multi-Touch Gestures
    
    /// <summary>
    /// Perform pinch-to-zoom gesture on a control.
    /// </summary>
    /// <param name="control">Control to perform pinch on.</param>
    /// <param name="scale">Scale factor (greater than 1 to zoom in, less than 1 to zoom out).</param>
    Task PinchZoom(ControlBase control, double scale);
    
    /// <summary>
    /// Perform pinch-to-close gesture on a control.
    /// </summary>
    /// <param name="control">Control to perform pinch on.</param>
    /// <param name="scale">Scale factor for closing.</param>
    Task PinchClose(ControlBase control, double scale);
    
    /// <summary>
    /// Perform rotation gesture on a control.
    /// </summary>
    /// <param name="control">Control to rotate.</param>
    /// <param name="degrees">Degrees to rotate (positive for clockwise).</param>
    Task Rotate(ControlBase control, double degrees);
    
    #endregion
    
    #region Screen-Level Gestures
    
    /// <summary>
    /// Swipe across the entire screen.
    /// </summary>
    /// <param name="direction">Direction to swipe.</param>
    /// <param name="durationMs">Duration of swipe in milliseconds.</param>
    Task SwipeScreen(SwipeDirection direction, int durationMs = 500);
    
    /// <summary>
    /// Tap at specific screen coordinates.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    Task TapAtCoordinates(int x, int y);
    
    /// <summary>
    /// Scroll the screen in a direction.
    /// </summary>
    /// <param name="direction">Direction to scroll.</param>
    /// <param name="distance">Distance to scroll in pixels.</param>
    Task ScrollScreen(SwipeDirection direction, int distance = 500);
    
    #endregion
}
