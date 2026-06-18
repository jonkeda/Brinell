namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for MAUI controls with pull-to-refresh capability.
/// Implements IRefreshableControlObject with PullToRefresh, IsRefreshing.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract class RefreshableControlBase<TScope> : ControlBase<TScope>, IRefreshableControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new refreshable control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the element.</param>
    public RefreshableControlBase(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new refreshable control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public RefreshableControlBase(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }
    
    #region IRefreshableControlObject<TScope> Implementation
    
    /// <inheritdoc />
    public TScope PullToRefresh(int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            PullToRefreshCore(element);
        }, timeoutMs);
    }
    
    #endregion
    
    #region Core Methods (Element-Aware, No Logging)
    
    /// <summary>
    /// Performs pull-to-refresh gesture.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void PullToRefreshCore(IMauiElement element)
    {
        var rect = element.Rect;
        var centerX = rect.X + rect.Width / 2;
        var startY = rect.Y + (int)(rect.Height * 0.2);
        var endY = rect.Y + (int)(rect.Height * 0.8);
        
        // Perform swipe down gesture
        element.Swipe(centerX, startY, centerX, endY);
    }
    
    /// <summary>
    /// Gets refreshing state from pre-found element.
    /// Reads from IsRefreshing attribute.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if refreshing, false if not, null if element is null.</returns>
    protected virtual bool? IsRefreshingCore(IMauiElement? element)
    {
        if (element == null) return null;
        
        // Try IsRefreshing attribute
        var isRefreshing = element.GetAttribute("IsRefreshing");
        if (!string.IsNullOrEmpty(isRefreshing))
        {
            return isRefreshing.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        
        // Try Refreshing attribute
        var refreshing = element.GetAttribute("Refreshing");
        if (!string.IsNullOrEmpty(refreshing))
        {
            return refreshing.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        
        // Default to false if no attribute found
        return false;
    }
    
    #endregion
    
    #region IsRefreshing
    
    /// <inheritdoc />
    public bool? IsRefreshing()
    {
        return IsRefreshingCore(TryFindElement());
    }
    
    #endregion
    
    #region WaitRefreshing
    
    /// <summary>
    /// Waits for refreshing state using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="expected">The expected refreshing state.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    protected bool WaitRefreshingCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(
            element,
            e => IsRefreshingCore(e) == expected,
            timeoutMs);
    }

    /// <inheritdoc />
    public bool WaitRefreshing(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        return RunCheckWithElement(
            element => IsRefreshingCore(element) == expected,
            timeoutMs);
    }
    
    #endregion
    
    #region AssertRefreshing
    
    /// <summary>
    /// Asserts the element is refreshing. Throws if it isn't.
    /// </summary>
    /// <param name="message">Optional custom message for the assertion failure.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertRefreshing(string? message = null, int? timeoutMs = null)
        => AssertRefreshing(true, message, timeoutMs);
    
    /// <inheritdoc />
    public TScope AssertRefreshing(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;
        
        return RunAssert(nameof(AssertRefreshing), expected, () =>
        {
            return WaitRefreshing(expected, timeoutMs);
        }, message ?? $"Expected element {(expected.Value ? "to be refreshing" : "not to be refreshing")}. Locator: {Locator}");
    }
    
    #endregion
}
