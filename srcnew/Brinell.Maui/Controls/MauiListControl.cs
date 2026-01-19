using OpenQA.Selenium.Interactions;

namespace Brinell.Maui.Controls;

/// <summary>
/// Generic list control that finds items and provides typed access to each item container.
/// TItem is a container type that represents each item in the list.
/// </summary>
/// <typeparam name="TScope">The containing scope (page or container).</typeparam>
/// <typeparam name="TItem">The item container type.</typeparam>
public class MauiListControl<TScope, TItem> : MauiControlBase<TScope>
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
    public MauiListControl(
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
    public MauiListControl(
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
        
        var unwrappedElement = listElement.UnwrapElement();
        var driver = Context.Driver.UnwrapDriver();
        
        try
        {
            // Get element bounds for scroll calculations
            var location = unwrappedElement.Location;
            var size = unwrappedElement.Size;
            var centerX = location.X + size.Width / 2;
            var startY = location.Y + size.Height - 20; // Near bottom
            var endY = location.Y + 20; // Near top
            
            // Create actions for scrolling
            var actions = new Actions(driver);
            
            // Scroll down first (swipe up gesture)
            actions.MoveToLocation(centerX, startY)
                   .ClickAndHold()
                   .MoveToLocation(centerX, endY)
                   .Release()
                   .Perform();
            
            // Small pause to allow UI to update
            Thread.Sleep(100);
            
            // Scroll back to top (swipe down gesture)
            actions = new Actions(driver);
            actions.MoveToLocation(centerX, endY)
                   .ClickAndHold()
                   .MoveToLocation(centerX, startY)
                   .Release()
                   .Perform();
            
            // Small pause to allow UI to stabilize
            Thread.Sleep(100);
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
        
        var unwrappedElement = listElement.UnwrapElement();
        var driver = Context.Driver.UnwrapDriver();
        
        try
        {
            var location = unwrappedElement.Location;
            var size = unwrappedElement.Size;
            var centerX = location.X + size.Width / 2;
            var startY = location.Y + 20; // Near top
            var endY = location.Y + size.Height - 20; // Near bottom
            
            // Swipe down to scroll to top
            var actions = new Actions(driver);
            actions.MoveToLocation(centerX, startY)
                   .ClickAndHold()
                   .MoveToLocation(centerX, endY)
                   .Release()
                   .Perform();
            
            Thread.Sleep(100);
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
        
        var unwrappedElement = listElement.UnwrapElement();
        var driver = Context.Driver.UnwrapDriver();
        
        try
        {
            var location = unwrappedElement.Location;
            var size = unwrappedElement.Size;
            var centerX = location.X + size.Width / 2;
            var startY = location.Y + size.Height - 20; // Near bottom
            var endY = location.Y + 20; // Near top
            
            // Swipe up to scroll to end
            var actions = new Actions(driver);
            actions.MoveToLocation(centerX, startY)
                   .ClickAndHold()
                   .MoveToLocation(centerX, endY)
                   .Release()
                   .Perform();
            
            Thread.Sleep(100);
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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // First check without scrolling
        if (GetItemCount() >= minimumCount)
            return true;
        
        // Try scrolling to render virtualized items
        ScrollToRenderItems();
        
        // Poll for items
        while (stopwatch.ElapsedMilliseconds < timeout)
        {
            if (GetItemCount() >= minimumCount)
                return true;
            
            Thread.Sleep(PollingIntervalMs);
        }
        
        return GetItemCount() >= minimumCount;
    }
    
    #endregion
}
