using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// MAUI Menu: a collection of <see cref="MenuItem{TParent}"/> with a trigger that reveals them.
/// </summary>
/// <remarks>
/// <para>
/// The menu's root is the menu as a whole, so the trigger, the items and any chrome are all
/// found inside it and a lookup never escapes to the page:
/// </para>
/// <code>
/// Menu.Open();
/// Menu.AssertOpen();
/// Menu["New"].Click();
/// </code>
/// <para>
/// A menu whose root <i>is</i> its trigger and item host - a bar that is always showing its
/// items - needs neither extra locator and behaves as it did before.
/// </para>
/// </remarks>
/// <typeparam name="TParent">The containing scope type.</typeparam>
public partial class Menu<TParent>
    : CollectionObjectBase<TParent, Menu<TParent>, MenuItem<TParent>>
    where TParent : IMauiScope<TParent>
{
    private readonly Locator? _triggerLocator;
    private readonly Locator? _itemsHostLocator;

    /// <summary>
    /// How a menu finds its items when the caller does not say: every button in the item host.
    /// </summary>
    public static IItemStrategy DefaultItemStrategy { get; } =
        ItemStrategy.ByLocator(Locator.ByControlType("Button"));

    /// <summary>
    /// Creates a Menu with a locator.
    /// </summary>
    /// <param name="scope">The parent scope.</param>
    /// <param name="locator">The locator for the menu as a whole.</param>
    /// <param name="triggerLocator">What <c>Open</c> clicks. Defaults to the menu's own root.</param>
    /// <param name="itemsHostLocator">
    /// The element inside the menu that holds its items. Defaults to the menu's own root, which
    /// would otherwise also offer up the trigger as an item.
    /// </param>
    /// <param name="itemStrategy">How items are found inside the host.</param>
    public Menu(
        IMauiScope<TParent> scope,
        Locator locator,
        Locator? triggerLocator = null,
        Locator? itemsHostLocator = null,
        IItemStrategy? itemStrategy = null)
        : base(scope, locator, ItemsIn(itemsHostLocator, itemStrategy), NewItem)
    {
        _triggerLocator = triggerLocator;
        _itemsHostLocator = itemsHostLocator;
    }

    /// <summary>
    /// Creates a Menu with an automation ID.
    /// </summary>
    /// <inheritdoc cref="Menu{TParent}(IMauiScope{TParent}, Locator, Locator, Locator, IItemStrategy)"/>
    public Menu(
        IMauiScope<TParent> scope,
        string automationId,
        Locator? triggerLocator = null,
        Locator? itemsHostLocator = null,
        IItemStrategy? itemStrategy = null)
        : base(scope, automationId, ItemsIn(itemsHostLocator, itemStrategy), NewItem)
    {
        _triggerLocator = triggerLocator;
        _itemsHostLocator = itemsHostLocator;
    }

    private static MenuItem<TParent> NewItem(Menu<TParent> menu, IMauiElement itemRoot, int index)
        => new(menu, itemRoot, index);

    private static IItemStrategy ItemsIn(Locator? itemsHostLocator, IItemStrategy? itemStrategy)
    {
        var items = itemStrategy ?? DefaultItemStrategy;

        return itemsHostLocator == null ? items : ItemStrategy.Within(itemsHostLocator, items);
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Opens the menu by clicking its trigger.
    /// </summary>
    /// <remarks>
    /// A disclosure menu's trigger toggles, so calling this on an open menu closes it. Ask
    /// <c>IsOpen</c> rather than assuming, and see the sample's menu tests for both.
    /// </remarks>
    /// <param name="element">The menu's own element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void OpenCore(IMauiElement element, int? timeoutMs = null)
    {
        var trigger = _triggerLocator == null
            ? element
            : element.FindElement(_triggerLocator, timeoutMs ?? DefaultTimeoutMs);

        if (ActivationHelper.TryActivateByPattern(trigger))
            return;

        trigger.Click();
    }

    /// <summary>
    /// Whether the menu is showing its items.
    /// </summary>
    /// <remarks>
    /// Read from the item host rather than from the menu itself: the menu's own element is
    /// there whether it is open or shut, which is why the previous <c>IsOpen</c> - the
    /// trigger's own visibility - answered true for a closed menu.
    /// </remarks>
    /// <param name="element">The menu's own element (may be null).</param>
    /// <returns>True when open, false when shut, null when the menu is not on the page.</returns>
    [AbsenceTolerant]
    protected virtual bool? IsOpenCore(IMauiElement? element)
    {
        if (element == null) return null;

        if (_itemsHostLocator == null) return element.Visible;

        return element.TryFindElement(_itemsHostLocator, out var host, 0)
               && host?.Visible == true;
    }

    #endregion
}
