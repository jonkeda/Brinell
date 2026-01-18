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
    private readonly string _itemLocatorPattern;
    
    /// <summary>
    /// Creates a list control.
    /// </summary>
    /// <param name="scope">The containing scope.</param>
    /// <param name="listLocator">Locator for the list container itself.</param>
    /// <param name="itemLocatorPattern">XPath pattern for finding items (e.g., ".//Frame[@AutomationId='TaskItem']").</param>
    /// <param name="itemFactory">Factory to create item containers. Receives scope and 0-based index.</param>
    public MauiListControl(
        IMauiScope<TScope> scope, 
        Locator listLocator,
        string itemLocatorPattern,
        Func<IMauiScope<TScope>, int, TItem> itemFactory)
        : base(scope, listLocator)
    {
        _itemLocatorPattern = itemLocatorPattern ?? throw new ArgumentNullException(nameof(itemLocatorPattern));
        _itemFactory = itemFactory ?? throw new ArgumentNullException(nameof(itemFactory));
    }
    
    /// <summary>
    /// Creates a list control using automation ID.
    /// </summary>
    public MauiListControl(
        IMauiScope<TScope> scope, 
        string automationId,
        string itemLocatorPattern,
        Func<IMauiScope<TScope>, int, TItem> itemFactory)
        : base(scope, automationId)
    {
        _itemLocatorPattern = itemLocatorPattern ?? throw new ArgumentNullException(nameof(itemLocatorPattern));
        _itemFactory = itemFactory ?? throw new ArgumentNullException(nameof(itemFactory));
    }
    
    /// <summary>
    /// Gets the count of items in the list.
    /// </summary>
    public int GetItemCount()
    {
        var listElement = TryFindElement();
        if (listElement == null) return 0;
        
        var items = listElement.FindElements(OpenQA.Selenium.By.XPath(_itemLocatorPattern));
        return items.Count;
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
}
