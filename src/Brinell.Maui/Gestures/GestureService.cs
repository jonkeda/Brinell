using OpenQA.Selenium.Appium;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Gestures;

/// <summary>
/// Service for advanced multi-element and multi-touch gestures.
/// Single-element gestures like Tap, Swipe, LongPress are available
/// directly on controls. Use this service for complex gestures.
/// </summary>
public class GestureService : IGestureService
{
    private readonly AppiumTestContext _context;
    
    /// <summary>
    /// Creates a new gesture service.
    /// </summary>
    /// <param name="context">The Appium test context.</param>
    public GestureService(AppiumTestContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Multi-Element Gestures

    /// <inheritdoc />
    public Task DragTo(ControlBase from, ControlBase to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        
        var fromElement = WaitForElement(from);
        var toElement = WaitForElement(to);
        
        if (fromElement == null)
            throw new InvalidOperationException($"Source element '{from.AutomationId}' not visible for drag.");
        if (toElement == null)
            throw new InvalidOperationException($"Target element '{to.AutomationId}' not visible for drag.");
        
        var fromLocation = fromElement.Location;
        var fromSize = fromElement.Size;
        var toLocation = toElement.Location;
        var toSize = toElement.Size;
        
        var startX = fromLocation.X + fromSize.Width / 2;
        var startY = fromLocation.Y + fromSize.Height / 2;
        var endX = toLocation.X + toSize.Width / 2;
        var endY = toLocation.Y + toSize.Height / 2;
        
        _context.Driver.PerformDrag(startX, startY, endX, endY);
        _context.Log($"[GestureService] DragTo: '{from.AutomationId}' -> '{to.AutomationId}'");
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DragByOffset(ControlBase control, int offsetX, int offsetY)
    {
        ArgumentNullException.ThrowIfNull(control);
        
        var element = WaitForElement(control);
        if (element == null)
            throw new InvalidOperationException($"Element '{control.AutomationId}' not visible for drag.");
        
        var location = element.Location;
        var size = element.Size;
        
        var startX = location.X + size.Width / 2;
        var startY = location.Y + size.Height / 2;
        var endX = startX + offsetX;
        var endY = startY + offsetY;
        
        _context.Driver.PerformDrag(startX, startY, endX, endY);
        _context.Log($"[GestureService] DragByOffset: '{control.AutomationId}' by ({offsetX}, {offsetY})");
        
        return Task.CompletedTask;
    }

    #endregion

    #region Multi-Touch Gestures

    /// <inheritdoc />
    public Task PinchZoom(ControlBase control, double scale)
    {
        ArgumentNullException.ThrowIfNull(control);
        
        // Note: Multi-touch gestures are complex and platform-specific.
        // This is a placeholder implementation. Full implementation would
        // require using W3C Actions with two finger pointers.
        _context.Log($"[GestureService] PinchZoom: '{control.AutomationId}' scale={scale} (not fully implemented)");
        throw new NotImplementedException("Multi-touch pinch gestures require platform-specific implementation.");
    }

    /// <inheritdoc />
    public Task PinchClose(ControlBase control, double scale)
    {
        ArgumentNullException.ThrowIfNull(control);
        
        _context.Log($"[GestureService] PinchClose: '{control.AutomationId}' scale={scale} (not fully implemented)");
        throw new NotImplementedException("Multi-touch pinch gestures require platform-specific implementation.");
    }

    /// <inheritdoc />
    public Task Rotate(ControlBase control, double degrees)
    {
        ArgumentNullException.ThrowIfNull(control);
        
        _context.Log($"[GestureService] Rotate: '{control.AutomationId}' degrees={degrees} (not fully implemented)");
        throw new NotImplementedException("Multi-touch rotation gestures require platform-specific implementation.");
    }

    #endregion

    #region Screen-Level Gestures

    /// <inheritdoc />
    public Task SwipeScreen(SwipeDirection direction, int durationMs = 500)
    {
        _context.Driver.PerformScreenSwipe(direction, durationMs);
        _context.Log($"[GestureService] SwipeScreen: {direction}, {durationMs}ms");
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task TapAtCoordinates(int x, int y)
    {
        _context.Driver.TapAtCoordinates(x, y);
        _context.Log($"[GestureService] TapAtCoordinates: ({x}, {y})");
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ScrollScreen(SwipeDirection direction, int distance = 500)
    {
        // Scrolling is the opposite of swiping - swipe up to scroll down
        var swipeDirection = direction switch
        {
            SwipeDirection.Up => SwipeDirection.Down,
            SwipeDirection.Down => SwipeDirection.Up,
            SwipeDirection.Left => SwipeDirection.Right,
            SwipeDirection.Right => SwipeDirection.Left,
            _ => direction
        };
        
        _context.Driver.PerformScreenSwipe(swipeDirection, durationMs: 300);
        _context.Log($"[GestureService] ScrollScreen: {direction}, {distance}px");
        
        return Task.CompletedTask;
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Wait for element to be visible using the driver adapter.
    /// </summary>
    private AppiumElement? WaitForElement(ControlBase control)
    {
        AppiumElement? element = null;
        _context.WaitFor(() =>
        {
            element = _context.Driver.FindElementDirect(control.AutomationId);
            return element?.Displayed ?? false;
        }, description: $"element '{control.AutomationId}' visible");
        
        return element;
    }

    #endregion
}
