namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for MAUI controls with swipe gesture capability.
/// Implements ISwipeableControlObject with SwipeLeft, SwipeRight, SwipeUp, SwipeDown.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract class SwipeableControlBase<TScope> : ControlBase<TScope>, ISwipeableControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new swipeable control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the element.</param>
    public SwipeableControlBase(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new swipeable control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public SwipeableControlBase(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }
    
    #region ISwipeableControlObject<TScope> Implementation
    
    /// <inheritdoc />
    public TScope SwipeLeft(int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            SwipeLeftCore(element);
        }, timeoutMs);
    }
    
    /// <inheritdoc />
    public TScope SwipeRight(int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            SwipeRightCore(element);
        }, timeoutMs);
    }
    
    /// <inheritdoc />
    public TScope SwipeUp(int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            SwipeUpCore(element);
        }, timeoutMs);
    }
    
    /// <inheritdoc />
    public TScope SwipeDown(int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            SwipeDownCore(element);
        }, timeoutMs);
    }
    
    /// <inheritdoc />
    public TScope Swipe(int startX, int startY, int endX, int endY, int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            SwipeCore(element, startX, startY, endX, endY);
        }, timeoutMs);
    }
    
    #endregion
    
    #region Core Methods (Element-Aware, No Logging)
    
    /// <summary>
    /// Performs swipe left gesture.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void SwipeLeftCore(IMauiElement element)
    {
        var rect = element.Rect;
        var centerY = rect.Y + rect.Height / 2;
        var startX = rect.X + (int)(rect.Width * 0.8);
        var endX = rect.X + (int)(rect.Width * 0.2);
        
        PerformSwipe(element, startX, centerY, endX, centerY);
    }
    
    /// <summary>
    /// Performs swipe right gesture.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void SwipeRightCore(IMauiElement element)
    {
        var rect = element.Rect;
        var centerY = rect.Y + rect.Height / 2;
        var startX = rect.X + (int)(rect.Width * 0.2);
        var endX = rect.X + (int)(rect.Width * 0.8);
        
        PerformSwipe(element, startX, centerY, endX, centerY);
    }
    
    /// <summary>
    /// Performs swipe up gesture.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void SwipeUpCore(IMauiElement element)
    {
        var rect = element.Rect;
        var centerX = rect.X + rect.Width / 2;
        var startY = rect.Y + (int)(rect.Height * 0.8);
        var endY = rect.Y + (int)(rect.Height * 0.2);
        
        PerformSwipe(element, centerX, startY, centerX, endY);
    }
    
    /// <summary>
    /// Performs swipe down gesture.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void SwipeDownCore(IMauiElement element)
    {
        var rect = element.Rect;
        var centerX = rect.X + rect.Width / 2;
        var startY = rect.Y + (int)(rect.Height * 0.2);
        var endY = rect.Y + (int)(rect.Height * 0.8);
        
        PerformSwipe(element, centerX, startY, centerX, endY);
    }
    
    /// <summary>
    /// Performs custom swipe from relative coordinates.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="startX">Starting X coordinate (relative to element).</param>
    /// <param name="startY">Starting Y coordinate (relative to element).</param>
    /// <param name="endX">Ending X coordinate (relative to element).</param>
    /// <param name="endY">Ending Y coordinate (relative to element).</param>
    protected virtual void SwipeCore(IMauiElement element, int startX, int startY, int endX, int endY)
    {
        var rect = element.Rect;
        
        // Convert relative to absolute coordinates
        var absStartX = rect.X + startX;
        var absStartY = rect.Y + startY;
        var absEndX = rect.X + endX;
        var absEndY = rect.Y + endY;
        
        PerformSwipe(element, absStartX, absStartY, absEndX, absEndY);
    }
    
    /// <summary>
    /// Performs the actual swipe action using element's Swipe method.
    /// </summary>
    /// <param name="element">The element to swipe on.</param>
    /// <param name="startX">Absolute starting X coordinate.</param>
    /// <param name="startY">Absolute starting Y coordinate.</param>
    /// <param name="endX">Absolute ending X coordinate.</param>
    /// <param name="endY">Absolute ending Y coordinate.</param>
    protected virtual void PerformSwipe(IMauiElement element, int startX, int startY, int endX, int endY)
    {
        element.Swipe(startX, startY, endX, endY);
    }
    
    #endregion
}
