namespace Brinell.Maui.Controls;

using Brinell.Core.Utilities;

/// <summary>
/// Generic list control that finds items and provides typed access to each item container.
/// TItem is a container type that represents each item in the list.
/// </summary>
/// <typeparam name="TScope">The containing scope (page or container).</typeparam>
/// <typeparam name="TItem">The item container type.</typeparam>
public class List<TScope, TItem> : ControlBase<TScope>, IScrollableControlObject<TScope>
    where TScope : IMauiScope<TScope>
    where TItem : class
{
    private readonly Func<IMauiScope<TScope>, int, TItem> _itemFactory;
    private readonly string _itemAutomationIdPrefix;
    
    /// <summary>
    /// Creates a list control.
    /// </summary>
    /// <param name="scope">The containing scope.</param>
    /// <param name="listLocator">Locator for the list container itself.</param>
    /// <param name="itemAutomationIdPrefix">Prefix for item AutomationIds (e.g., "Task_" finds Task_0, Task_1, ...).</param>
    /// <param name="itemFactory">Factory to create item containers. Receives scope and 0-based index.</param>
    public List(
        IMauiScope<TScope> scope, 
        Locator listLocator,
        string itemAutomationIdPrefix,
        Func<IMauiScope<TScope>, int, TItem> itemFactory)
        : base(scope, listLocator)
    {
        _itemAutomationIdPrefix = itemAutomationIdPrefix ?? throw new ArgumentNullException(nameof(itemAutomationIdPrefix));
        _itemFactory = itemFactory ?? throw new ArgumentNullException(nameof(itemFactory));
    }
    
    /// <summary>
    /// Creates a list control using automation ID.
    /// </summary>
    public List(
        IMauiScope<TScope> scope, 
        string automationId,
        string itemAutomationIdPrefix,
        Func<IMauiScope<TScope>, int, TItem> itemFactory)
        : base(scope, automationId)
    {
        _itemAutomationIdPrefix = itemAutomationIdPrefix ?? throw new ArgumentNullException(nameof(itemAutomationIdPrefix));
        _itemFactory = itemFactory ?? throw new ArgumentNullException(nameof(itemFactory));
    }
    
    /// <summary>
    /// Gets the count of items in the list by iterating through indexed items.
    /// Uses AutomationId prefix pattern (e.g., "Task_" finds Task_0, Task_1, ...).
    /// </summary>
    public int GetItemCount()
    {
        // Count items by iterating through indices until no more are found
        // This is Windows-compatible (avoids XPath starts-with which may not be supported)
        var count = 0;
        const int maxItems = 100; // Safety limit
        
        while (count < maxItems)
        {
            var automationId = $"{_itemAutomationIdPrefix}{count}";
            var locator = new Locator(LocatorStrategy.AutomationId, automationId);
            var element = ContainingScope.TryFindElement(locator);
            
            if (element == null)
                break;
                
            count++;
        }
        
        return count;
    }
    
    /// <summary>
    /// Gets an item container by index (0-based).
    /// </summary>
    public TItem Item(int index)
    {
        var scope = ContainingScope as IMauiScope<TScope> 
            ?? throw new InvalidOperationException("Scope is not IMauiScope");
        return _itemFactory(scope, index);
    }
    
    /// <summary>
    /// Gets all item containers.
    /// </summary>
    public IReadOnlyList<TItem> GetAllItems()
    {
        var count = GetItemCount();
        var items = new List<TItem>(count);
        for (int i = 0; i < count; i++)
        {
            items.Add(Item(i));
        }
        return items;
    }
    
    /// <summary>
    /// Waits for a specific item count.
    /// </summary>
    public bool WaitItemCount(int expected, int? timeoutMs = null)
    {
        return Poll(() => GetItemCount() == expected, timeoutMs ?? DefaultTimeoutMs);
    }
    
    /// <summary>
    /// Asserts item count matches expected.
    /// </summary>
    public TScope AssertItemCount(int expected, string? message = null, int? timeoutMs = null)
    {
        if (!WaitItemCount(expected, timeoutMs))
        {
            var actual = GetItemCount();
            throw new AssertionException(
                message ?? $"Expected {expected} items but found {actual}. Locator: {Locator}");
        }
        return ContainingScope;
    }
    
    #region Scrolling
    
    /// <summary>
    /// Scrolls the list to ensure all items are rendered in the automation tree.
    /// CollectionView virtualizes items, so they may not be visible until scrolled.
    /// This performs a scroll down then back to top to trigger rendering.
    /// </summary>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope ScrollToRenderItems()
    {
        var listElement = TryFindElement();
        if (listElement == null) return ContainingScope;
        
        try
        {
            // Get element bounds for scroll calculations
            var rect = listElement.Rect;
            var centerX = rect.X + rect.Width / 2;
            var startY = rect.Y + rect.Height - 20; // Near bottom
            var endY = rect.Y + 20; // Near top
            
            // Scroll down first (swipe up gesture)
            listElement.Swipe(centerX, startY, centerX, endY);
            
            // Brief pause to allow UI to update
            WaitHelper.Pause(100);
            
            // Scroll back to top (swipe down gesture)
            listElement.Swipe(centerX, endY, centerX, startY);
            
            // Brief pause to allow UI to stabilize
            WaitHelper.Pause(100);
        }
        catch
        {
            // Scrolling may not be supported on all platforms
            // Continue without failing
        }
        
        return ContainingScope;
    }
    
    /// <summary>
    /// Scrolls the list to the top.
    /// </summary>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope ScrollToTop()
    {
        var listElement = TryFindElement();
        if (listElement == null) return ContainingScope;
        
        try
        {
            var rect = listElement.Rect;
            var centerX = rect.X + rect.Width / 2;
            var startY = rect.Y + 20; // Near top
            var endY = rect.Y + rect.Height - 20; // Near bottom
            
            // Swipe down to scroll to top
            listElement.Swipe(centerX, startY, centerX, endY);
            
            WaitHelper.Pause(100);
        }
        catch
        {
            // Continue without failing
        }
        
        return ContainingScope;
    }
    
    /// <summary>
    /// Scrolls the list to the bottom/end.
    /// </summary>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope ScrollToEnd()
    {
        var listElement = TryFindElement();
        if (listElement == null) return ContainingScope;
        
        try
        {
            var rect = listElement.Rect;
            var centerX = rect.X + rect.Width / 2;
            var startY = rect.Y + rect.Height - 20; // Near bottom
            var endY = rect.Y + 20; // Near top
            
            // Swipe up to scroll to end
            listElement.Swipe(centerX, startY, centerX, endY);
            
            WaitHelper.Pause(100);
        }
        catch
        {
            // Continue without failing
        }
        
        return ContainingScope;
    }
    
    /// <summary>
    /// Waits for items to appear in the list, scrolling if needed to trigger virtualization.
    /// </summary>
    /// <param name="minimumCount">Minimum number of items expected.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if at least minimumCount items are found.</returns>
    public bool WaitForItems(int minimumCount = 1, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        
        // First check without scrolling
        if (GetItemCount() >= minimumCount)
            return true;
        
        // Try scrolling to render virtualized items
        ScrollToRenderItems();
        
        // Poll for items
        return Poll(() => GetItemCount() >= minimumCount, timeout);
    }

    #endregion

    #region IScrollableControlObject<TScope> Implementation

    /// <inheritdoc />
    public TScope ScrollToTop(int? timeoutMs = null)
    {
        return ScrollToTop();
    }

    /// <inheritdoc />
    public TScope ScrollToEnd(int? timeoutMs = null)
    {
        return ScrollToEnd();
    }

    /// <inheritdoc />
    public TScope ScrollBy(int deltaX, int deltaY, int? timeoutMs = null)
    {
        return RunWithElement(nameof(ScrollBy), timeoutMs, element =>
        {
            var rect = element.Rect;
            var centerX = rect.X + (rect.Width / 2);
            var centerY = rect.Y + (rect.Height / 2);
            element.Swipe(centerX, centerY, centerX - deltaX, centerY - deltaY);
        });
    }

    /// <inheritdoc />
    public TScope ScrollTo(Locator locator, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var found = Poll(() => MauiScope.TryFindElement(locator)?.Visible == true, timeout);
        if (!found)
        {
            ScrollToRenderItems();
            found = Poll(() => MauiScope.TryFindElement(locator)?.Visible == true, timeout);
        }

        if (!found)
        {
            throw new ElementNotFoundException($"Could not scroll to element with locator: {locator}");
        }

        return ContainingScope;
    }

    /// <inheritdoc />
    public double? GetScrollPosition(int? timeoutMs = null)
    {
        var element = timeoutMs.HasValue ? FindElementWithWait(timeoutMs.Value) : TryFindElement();
        if (element == null) return null;

        var scrollPercent = element.GetAttribute("Scroll.VerticalScrollPercent");
        if (!string.IsNullOrEmpty(scrollPercent) && double.TryParse(scrollPercent, out var percent))
        {
            return percent;
        }

        return 0;
    }

    /// <inheritdoc />
    public TScope SetScrollPosition(double percent, int? timeoutMs = null)
    {
        if (percent <= 0)
        {
            return ScrollToTop(timeoutMs);
        }

        if (percent >= 100)
        {
            return ScrollToEnd(timeoutMs);
        }

        var current = GetScrollPosition(timeoutMs) ?? 0;
        var diff = percent - current;
        return ScrollBy(0, (int)diff, timeoutMs);
    }

    /// <inheritdoc />
    public bool? CanScrollDown(int? timeoutMs = null)
    {
        var position = GetScrollPosition(timeoutMs);
        return position == null ? null : position.Value < 100;
    }

    /// <inheritdoc />
    public bool? CanScrollUp(int? timeoutMs = null)
    {
        var position = GetScrollPosition(timeoutMs);
        return position == null ? null : position.Value > 0;
    }

    /// <inheritdoc />
    public bool WaitScrollPosition(double? expected, double tolerance = 1.0, int? timeoutMs = null)
    {
        if (expected == null) return true;

        return Poll(() =>
        {
            var actual = GetScrollPosition();
            if (actual == null) return false;
            return Math.Abs(actual.Value - expected.Value) <= tolerance;
        }, timeoutMs ?? DefaultTimeoutMs);
    }

    /// <inheritdoc />
    public TScope AssertScrollPosition(double? expected, double tolerance = 1.0, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        if (!WaitScrollPosition(expected, tolerance, timeoutMs))
        {
            var actual = GetScrollPosition(timeoutMs);
            throw new AssertionException(
                message ?? $"Expected scroll position '{expected}' (±{tolerance}) but got '{actual}'. Locator: {Locator}");
        }

        return ContainingScope;
    }
    
    #endregion
}
