using Brinell.Core.Utilities;
using Brinell.Maui.Configuration;
using Brinell.Maui.Controls;

namespace Brinell.Maui.Containers;

/// <summary>
/// Base class for collection objects: a container that also hands out typed items.
/// </summary>
/// <remarks>
/// Being a container, a collection scopes its own non-item controls too - a title,
/// an empty view, a footer - alongside <see cref="Item"/>.
/// <para>
/// <typeparamref name="TItem"/> is constrained to <see cref="IMauiItemContainer{TCollection,
/// TSelf}"/> - the contract, not <see cref="ItemContainerBase{TCollection, TSelf}"/> - so
/// items are still structurally guaranteed to be scoped to this collection while an item type
/// stays free to arrive at that any way it likes. The base class supplies nothing this class
/// calls: items are built by the factory and handed straight back.
/// </para>
/// </remarks>
/// <typeparam name="TParent">The parent scope type.</typeparam>
/// <typeparam name="TSelf">The collection type itself (self-referencing).</typeparam>
/// <typeparam name="TItem">The item container type.</typeparam>
public abstract class CollectionObjectBase<TParent, TSelf, TItem>
    : ContainerObjectBase<TParent, TSelf>, IMauiCollectionObject<TParent, TSelf, TItem>, IItemRootProvider
    where TParent : IMauiScope<TParent>
    where TSelf : CollectionObjectBase<TParent, TSelf, TItem>
    where TItem : class, IMauiItemContainer<TSelf, TItem>
{
    private readonly IItemStrategy _itemStrategy;
    private readonly Func<TSelf, IMauiElement, int, TItem> _itemFactory;

    /// <summary>
    /// Creates a collection within the given parent scope.
    /// </summary>
    /// <param name="parentScope">The parent scope (page or container).</param>
    /// <param name="locator">The locator for the collection's root element.</param>
    /// <param name="itemStrategy">How item roots are discovered.</param>
    /// <param name="itemFactory">Builds an item from the collection, the item's root, and its index.</param>
    protected CollectionObjectBase(
        IMauiScope<TParent> parentScope,
        Locator locator,
        IItemStrategy itemStrategy,
        Func<TSelf, IMauiElement, int, TItem> itemFactory)
        : base(parentScope, locator)
    {
        _itemStrategy = itemStrategy ?? throw new ArgumentNullException(nameof(itemStrategy));
        _itemFactory = itemFactory ?? throw new ArgumentNullException(nameof(itemFactory));
    }

    /// <summary>
    /// Creates a collection using the parent scope's default locator strategy.
    /// </summary>
    protected CollectionObjectBase(
        IMauiScope<TParent> parentScope,
        string locatorValue,
        IItemStrategy itemStrategy,
        Func<TSelf, IMauiElement, int, TItem> itemFactory)
        : base(parentScope, locatorValue)
    {
        _itemStrategy = itemStrategy ?? throw new ArgumentNullException(nameof(itemStrategy));
        _itemFactory = itemFactory ?? throw new ArgumentNullException(nameof(itemFactory));
    }

    #region Item access

    /// <summary>
    /// Gets the item at <paramref name="index"/>. Equivalent to <see cref="Item"/>;
    /// the indexer reads better for a direct lookup, <c>Item(i)</c> mid-chain.
    /// </summary>
    /// <remarks>
    /// Renamed via <see cref="System.Runtime.CompilerServices.IndexerNameAttribute"/>
    /// because an indexer is otherwise emitted as a member called <c>Item</c>, which
    /// would collide with the <see cref="Item"/> method.
    /// </remarks>
    [System.Runtime.CompilerServices.IndexerName("ItemAt")]
    public TItem this[int index] => Item(index);

    /// <inheritdoc />
    public TItem Item(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        return TryItem(index)
            ?? throw new ElementNotFoundException(
                $"No item at index {index} in collection. Locator: {Locator}, materialized items: {GetItemCount()}.");
    }

    /// <inheritdoc />
    public TItem? TryItem(int index)
    {
        if (index < 0) return null;

        var itemRoot = TryGetItemRoot(index);
        return itemRoot == null ? null : _itemFactory(Self, itemRoot, index);
    }

    /// <inheritdoc />
    public IMauiElement? TryGetItemRoot(int index)
    {
        if (index < 0) return null;

        var root = TryGetContainerRoot();
        if (root == null) return null;

        try
        {
            return _itemStrategy.FindItemElement(root, index);
        }
        catch (StaleElementReferenceException)
        {
            InvalidateCache();

            root = TryGetContainerRoot();
            return root == null ? null : _itemStrategy.FindItemElement(root, index);
        }
    }

    /// <summary>
    /// Gets the item identified by <paramref name="key"/>: its automation id, or failing
    /// that its caption.
    /// </summary>
    /// <remarks>
    /// The id is tried first across every item before any caption is considered, because an
    /// id is the identifier the app author chose and a caption is what the platform happened
    /// to render. Say which one you mean with the <see cref="Locator"/> overload -
    /// <c>Toolbar[Locator.ByText("Save")]</c> - when a collection could answer to both.
    /// </remarks>
    [System.Runtime.CompilerServices.IndexerName("ItemAt")]
    public TItem this[string key] => Item(key);

    /// <summary>
    /// Gets the item matching <paramref name="key"/> - by automation id, caption, name or
    /// control type.
    /// </summary>
    /// <remarks>
    /// <c>Toolbar[Locator.ByAutomationId("ToolbarSaveButton")]</c>,
    /// <c>Toolbar[Locator.ByText("Save")]</c>,
    /// <c>Toolbar[Locator.ByControlType("Button")]</c>. See
    /// <see cref="ElementMatch"/> for how each is compared.
    /// </remarks>
    [System.Runtime.CompilerServices.IndexerName("ItemAt")]
    public TItem this[Locator key] => Item(key);

    /// <summary>
    /// Gets the item identified by <paramref name="key"/>, waiting for it to appear and
    /// throwing when it does not.
    /// </summary>
    /// <remarks>
    /// Waits, for the same reason <c>FindElement</c> waits and <c>TryFindElement</c> does not:
    /// naming an item is a search, and the thing being searched for often arrives a frame
    /// after whatever revealed it - <c>Menu.Open()["New"]</c> asks for an item the click has
    /// only just started rendering. Use <see cref="TryItem(string)"/> to ask about right now.
    /// </remarks>
    public TItem Item(string key, int? timeoutMs = null)
    {
        ArgumentNullException.ThrowIfNull(key);

        return WaitForItem(() => TryItem(key), timeoutMs)
            ?? throw new ElementNotFoundException(
                $"No item with the automation id or caption '{key}' in collection. " +
                $"Locator: {Locator}, materialized items: {GetItemCount()}.");
    }

    /// <summary>
    /// Gets the item matching <paramref name="key"/>, waiting for it to appear and throwing
    /// when it does not.
    /// </summary>
    /// <inheritdoc cref="Item(string, int?)" path="/remarks"/>
    public TItem Item(Locator key, int? timeoutMs = null)
    {
        ArgumentNullException.ThrowIfNull(key);

        return WaitForItem(() => TryItem(key), timeoutMs)
            ?? throw new ElementNotFoundException(
                $"No item matched {key} in collection. " +
                $"Locator: {Locator}, materialized items: {GetItemCount()}.");
    }

    /// <summary>
    /// Polls a keyed lookup until it finds something or the timeout runs out.
    /// </summary>
    private TItem? WaitForItem(Func<TItem?> lookup, int? timeoutMs)
    {
        TItem? match = null;
        Poll(() => (match = lookup()) != null, timeoutMs ?? DefaultTimeoutMs);

        return match;
    }

    /// <summary>
    /// Gets the item identified by <paramref name="key"/>, or null when none matches.
    /// </summary>
    /// <remarks>
    /// Three passes, each across every item before the next begins: the automation id the app
    /// author chose, then the caption the platform rendered, then the accessibility name.
    /// The last is not redundant - navigation chrome regularly carries a name and no text at
    /// all, which is how Android labels a tab.
    /// </remarks>
    public TItem? TryItem(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var itemRoots = TryGetItemRoots();

        return MatchItem(itemRoots, Locator.ByAutomationId(key))
            ?? MatchItem(itemRoots, Locator.ByText(key))
            ?? MatchItem(itemRoots, Locator.ByName(key));
    }

    /// <summary>
    /// Gets the item matching <paramref name="key"/>, or null when none matches.
    /// </summary>
    /// <remarks>
    /// Matching walks the materialized items and reads one property from each, so it costs a
    /// lookup per item. That suits a handful of navigation items; index the collection when
    /// walking a long list.
    /// </remarks>
    public TItem? TryItem(Locator key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return MatchItem(TryGetItemRoots(), key);
    }

    /// <summary>
    /// Gets the item whose automation id is <paramref name="automationId"/>.
    /// </summary>
    /// <remarks>
    /// This and the three below are named forms of <see cref="Item(Locator)"/>, for when the
    /// selector is fixed at the call site: <c>Toolbar.ItemByText("Save")</c> reads better than
    /// <c>Toolbar[Locator.ByText("Save")]</c>. Pass a <see cref="Locator"/> instead when the
    /// selector is chosen at run time.
    /// </remarks>
    public TItem ItemByAutomationId(string automationId) => Item(Locator.ByAutomationId(automationId));

    /// <summary>Gets the item whose automation id matches, or null when none does.</summary>
    public TItem? TryItemByAutomationId(string automationId) => TryItem(Locator.ByAutomationId(automationId));

    /// <summary>Gets the item whose caption is <paramref name="text"/>.</summary>
    public TItem ItemByText(string text) => Item(Locator.ByText(text));

    /// <summary>Gets the item whose caption matches, or null when none does.</summary>
    public TItem? TryItemByText(string text) => TryItem(Locator.ByText(text));

    /// <summary>Gets the item whose name is <paramref name="name"/>.</summary>
    public TItem ItemByName(string name) => Item(Locator.ByName(name));

    /// <summary>Gets the item whose name matches, or null when none does.</summary>
    public TItem? TryItemByName(string name) => TryItem(Locator.ByName(name));

    /// <summary>Gets the first item of control type <paramref name="controlType"/>.</summary>
    public TItem ItemByControlType(string controlType) => Item(Locator.ByControlType(controlType));

    /// <summary>Gets the first item of that control type, or null when there is none.</summary>
    public TItem? TryItemByControlType(string controlType) => TryItem(Locator.ByControlType(controlType));

    /// <summary>
    /// The first item whose root matches, built at the index it was found at.
    /// </summary>
    private TItem? MatchItem(IReadOnlyList<IMauiElement> itemRoots, Locator key)
    {
        for (var index = 0; index < itemRoots.Count; index++)
        {
            if (MatchesKey(itemRoots[index], key))
            {
                return _itemFactory(Self, itemRoots[index], index);
            }
        }

        return null;
    }

    /// <summary>
    /// Whether an item root answers to <paramref name="key"/>.
    /// </summary>
    /// <remarks>
    /// Override for a collection whose items are identified by something the element itself
    /// does not carry - a child label's text, say.
    /// </remarks>
    /// <param name="itemRoot">The element the item strategy found.</param>
    /// <param name="key">What is being looked for.</param>
    protected virtual bool MatchesKey(IMauiElement itemRoot, Locator key)
        => ElementMatch.Matches(itemRoot, key);

    /// <summary>
    /// Every materialized item root, or an empty list when the collection is absent.
    /// </summary>
    /// <remarks>
    /// Protected because a collection sometimes has to ask about the elements rather than the
    /// items - "is any of these visible", say - and building item objects to answer that would
    /// be paying for scoping nobody uses.
    /// </remarks>
    protected IReadOnlyList<IMauiElement> TryGetItemRoots()
    {
        var root = TryGetContainerRoot();
        if (root == null) return [];

        try
        {
            return _itemStrategy.FindItemElements(root);
        }
        catch (StaleElementReferenceException)
        {
            InvalidateCache();

            root = TryGetContainerRoot();
            return root == null ? [] : _itemStrategy.FindItemElements(root);
        }
    }

    /// <summary>
    /// The items, yielded lazily. A consumer that stops early - <c>Items.First(...)</c> -
    /// does not pay to materialize the rest.
    /// </summary>
    public IEnumerable<TItem> Items
    {
        get
        {
            var count = GetItemCount();
            for (var index = 0; index < count; index++)
            {
                var item = TryItem(index);
                if (item == null) yield break;

                yield return item;
            }
        }
    }

    /// <summary>
    /// Materializes every item. Prefer <see cref="Items"/> when you may stop early.
    /// </summary>
    public IReadOnlyList<TItem> ToList() => [.. Items];

    #endregion

    #region Counting

    /// <inheritdoc />
    public int GetItemCount(int? timeoutMs = null)
    {
        var root = TryGetContainerRoot();
        if (root == null) return 0;

        try
        {
            return _itemStrategy.FindItemElements(root).Count;
        }
        catch (StaleElementReferenceException)
        {
            InvalidateCache();

            root = TryGetContainerRoot();
            return root == null ? 0 : _itemStrategy.FindItemElements(root).Count;
        }
    }

    /// <summary>
    /// Whether the collection currently has no materialized items.
    /// </summary>
    public bool IsEmpty(int? timeoutMs = null) => GetItemCount(timeoutMs) == 0;

    /// <summary>
    /// Waits until the materialized item count equals <paramref name="expected"/>.
    /// </summary>
    public bool WaitItemCount(int expected, int? timeoutMs = null)
        => Poll(() => GetItemCount() == expected, timeoutMs ?? DefaultTimeoutMs);

    /// <summary>
    /// Waits until at least one item is materialized, scrolling if needed.
    /// </summary>
    public bool WaitAnyItem(int? timeoutMs = null)
    {
        if (GetItemCount() > 0) return true;

        return Poll(() => GetItemCount() > 0, timeoutMs ?? DefaultTimeoutMs);
    }

    /// <summary>
    /// Waits until at least <paramref name="minimumCount"/> items are materialized,
    /// scrolling to realize more if the count is short.
    /// </summary>
    /// <remarks>
    /// Prefer this to <see cref="WaitItemCount"/> on a virtualizing collection: the
    /// realized count is bounded by the viewport, so an exact match may never occur even
    /// though the data source holds more. Scrolling is attempted only when the count is
    /// short, and is best-effort - see <see cref="ScrollToEnd"/> for its limits.
    /// </remarks>
    public bool WaitForItems(int minimumCount = 1, int? timeoutMs = null)
    {
        if (GetItemCount() >= minimumCount) return true;

        // Realize more rows before polling, otherwise a short viewport guarantees a
        // timeout rather than a wait.
        TryMaterializeMore(GetItemCount());

        return Poll(() => GetItemCount() >= minimumCount, timeoutMs ?? DefaultTimeoutMs);
    }

    /// <summary>
    /// Asserts the materialized item count, returning the collection for chaining.
    /// </summary>
    /// <remarks>
    /// On a virtualizing collection this counts materialized rows, not the bound data
    /// source. Use <see cref="AssertEmpty"/> or <see cref="WaitAnyItem"/> when only
    /// presence matters.
    /// </remarks>
    public TSelf AssertItemCount(int expected, string? message = null, int? timeoutMs = null)
    {
        if (!WaitItemCount(expected, timeoutMs))
        {
            throw new AssertionException(
                message ?? $"Expected {expected} items but found {GetItemCount()}. Locator: {Locator}");
        }

        return Self;
    }

    /// <summary>
    /// Asserts whether the collection is empty, returning the collection for chaining.
    /// </summary>
    public TSelf AssertEmpty(bool? expected = true, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return Self;

        if (!Poll(() => IsEmpty() == expected.Value, timeoutMs ?? DefaultTimeoutMs))
        {
            throw new AssertionException(
                message ?? $"Expected collection {(expected.Value ? "to be empty" : "not to be empty")} " +
                           $"but found {GetItemCount()} items. Locator: {Locator}");
        }

        return Self;
    }

    #endregion

    #region Search by content

    /// <summary>
    /// Finds the first item matching <paramref name="predicate"/>, scrolling to
    /// materialize more rows if needed. Returns null when none matches.
    /// </summary>
    public TItem? FindItem(Func<TItem, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var match = Items.FirstOrDefault(predicate);
        if (match != null) return match;

        // The match may be off-screen in a virtualizing collection: scroll while the
        // materialized count keeps growing, checking only the newly-arrived rows.
        var seen = GetItemCount();
        while (TryMaterializeMore(seen))
        {
            var count = GetItemCount();
            for (var index = seen; index < count; index++)
            {
                var item = TryItem(index);
                if (item != null && predicate(item)) return item;
            }

            seen = count;
        }

        return null;
    }

    /// <summary>
    /// Finds the first item matching <paramref name="predicate"/>, throwing when none does.
    /// </summary>
    public TItem ItemWhere(Func<TItem, bool> predicate)
        => FindItem(predicate)
           ?? throw new ElementNotFoundException(
               $"No item matched the predicate in collection. Locator: {Locator}, materialized items: {GetItemCount()}.");

    #endregion

    #region Scrolling

    /// <summary>
    /// Scrolls until the item at <paramref name="index"/> is materialized, returning the
    /// collection for chaining.
    /// </summary>
    /// <remarks>
    /// Scrolls a step at a time and re-checks after each, stopping when the item
    /// resolves or when scrolling stops producing new rows. Waits on observed item
    /// state rather than a fixed delay.
    /// </remarks>
    public TSelf ScrollToItem(int index, int? timeoutMs = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        if (TryGetItemRoot(index) != null) return Self;

        // A virtualizing panel slides its realized window: rows drop off the top as new
        // ones appear below, so the materialized COUNT can plateau while scrolling is
        // still making progress. Track the furthest row actually reached instead, and
        // stop only when a scroll step fails to reach any further.
        var furthestReached = -1;

        while (true)
        {
            if (!TryMaterializeMore(GetItemCount())) break;

            if (TryGetItemRoot(index) != null) return Self;

            var reach = FurthestReachableIndex();
            if (reach <= furthestReached) break;

            furthestReached = reach;
        }

        if (TryGetItemRoot(index) == null)
        {
            throw new ElementNotFoundException(
                $"Could not scroll item {index} into view. Locator: {Locator}, " +
                $"materialized items: {GetItemCount()}, furthest reached: {furthestReached}.");
        }

        return Self;
    }

    /// <summary>
    /// The highest index currently resolvable, used to tell real scroll progress from a
    /// realized window that merely slid.
    /// </summary>
    private int FurthestReachableIndex()
    {
        var count = GetItemCount();
        return count == 0 ? -1 : count - 1;
    }

    /// <summary>
    /// Scrolls the collection to the top, returning the collection for chaining.
    /// </summary>
    /// <remarks>
    /// Uses the first realized row's scroll-into-view where available, falling back to
    /// a pointer swipe only where pointer input is permitted.
    /// </remarks>
    public TSelf ScrollToTop(int? timeoutMs = null)
    {
        if (TryScrollItemIntoView(0)) return Self;

        var target = ScrollTarget ?? TryGetContainerRoot();

        // Scroll pattern first; repeat until it stops making progress, since one step is
        // one viewport.
        while (ScrollHelper.TryScrollBack(target))
        {
        }

        ScrollHelper.TrySwipeBack(target);
        return Self;
    }

    /// <summary>
    /// Scrolls the collection to the end, returning the collection for chaining.
    /// </summary>
    public TSelf ScrollToEnd(int? timeoutMs = null)
    {
        var seen = -1;
        while (GetItemCount() != seen)
        {
            seen = GetItemCount();
            if (!TryMaterializeMore(seen)) break;
        }

        return Self;
    }

    /// <summary>
    /// Asks the row at the given index to scroll itself into view.
    /// </summary>
    private bool TryScrollItemIntoView(int index)
    {
        var itemRoot = TryGetItemRoot(index);
        if (itemRoot == null) return false;

        try
        {
            itemRoot.ScrollIntoView();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Scrolls one step toward the end and reports whether that materialized new rows.
    /// </summary>
    /// <remarks>
    /// Prefers the UI Automation route - asking the last realized row to scroll itself
    /// into view - over pointer input, which is gated on Windows and unavailable by
    /// default. A pointer swipe is attempted only when the automation route makes no
    /// progress, and a policy refusal there simply ends the scroll rather than failing
    /// the caller.
    /// </remarks>
    private bool TryMaterializeMore(int countBefore)
    {
        var root = TryGetContainerRoot();
        if (root == null) return false;

        // UI Automation first: pull the last realized row into view, which makes the
        // virtualizing panel realize the rows after it.
        if (TryScrollLastItemIntoView() && HasMoreThan(countBefore))
        {
            return true;
        }

        try
        {
            // The scrollable element is the item host, not this container's root, which
            // may be a non-scrolling wrapper around it.
            var target = ScrollTarget ?? root;

            // UI Automation scroll pattern first: it moves the scrolling container, so it
            // advances a virtualizing list past its realized window, and it is not gated
            // by the pointer-input policy.
            if (ScrollHelper.TryScrollForward(target))
            {
                return HasMoreThan(countBefore);
            }

            // Pointer fallback, for surfaces with no scroll pattern.
            if (!ScrollHelper.TrySwipeForward(target))
            {
                // Neither route made progress: this is as far as scrolling can go.
                return false;
            }
        }
        catch (StaleElementReferenceException)
        {
            InvalidateCache();
            return false;
        }

        return HasMoreThan(countBefore);
    }

    /// <summary>
    /// The element that actually scrolls, when it is not this container's own root.
    /// </summary>
    /// <remarks>
    /// A collection is often wrapped for automation purposes - a platform bridge that
    /// exposes an AutomationId - so the container root and the scrolling item host can
    /// be different elements. Override to name the scrolling one; returning null uses
    /// the container root.
    /// </remarks>
    protected virtual IMauiElement? ScrollTarget => null;

    /// <summary>
    /// Asks the last realized row to scroll itself into view.
    /// </summary>
    private bool TryScrollLastItemIntoView()
    {
        var count = GetItemCount();
        if (count == 0) return false;

        var last = TryGetItemRoot(count - 1);
        if (last == null) return false;

        return ScrollHelper.TryScrollIntoView(last);
    }

    /// <summary>
    /// Waits briefly for the materialized count to exceed a previous value, polling
    /// observed state rather than sleeping.
    /// </summary>
    private bool HasMoreThan(int countBefore)
        => Poll(() => GetItemCount() > countBefore, PollingIntervalMs * 5);

    #endregion

    #region Selection

    /// <summary>
    /// Selects the item at <paramref name="index"/>, returning the collection for chaining.
    /// </summary>
    public TSelf SelectItem(int index, int? timeoutMs = null)
    {
        if (!TrySelectItem(index, timeoutMs))
        {
            throw new ElementNotFoundException(
                $"Could not select item at index {index}. Locator: {Locator}");
        }

        return Self;
    }

    /// <summary>
    /// Attempts to select the item at <paramref name="index"/>.
    /// </summary>
    public bool TrySelectItem(int index, int? timeoutMs = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        var itemRoot = TryGetItemRoot(index);
        if (itemRoot == null) return false;

        return ActivateItemCore(itemRoot);
    }

    /// <summary>
    /// Activates an item, given the element the item strategy found for it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The element a strategy matches is usually inside the row rather than the row itself —
    /// a label or a cell — and on Windows selection responds to the <c>ListItem</c> that
    /// contains it, not to that inner element. So the containing row is tried first, then the
    /// element itself.
    /// </para>
    /// <para>
    /// This lives on the collection rather than in a shared helper because "the row that owns
    /// this element" is collection knowledge. A collection whose rows activate differently
    /// overrides this.
    /// </para>
    /// </remarks>
    /// <param name="itemRoot">The element found for the item.</param>
    /// <returns>True when the item was activated.</returns>
    protected virtual bool ActivateItemCore(IMauiElement itemRoot)
    {
        ArgumentNullException.ThrowIfNull(itemRoot);

        if (!itemRoot.HasUsableBounds())
        {
            return false;
        }

        foreach (var row in FindContainingRows(itemRoot))
        {
            if (TryActivate(row))
            {
                return true;
            }
        }

        return TryActivate(itemRoot);
    }

    /// <summary>
    /// The <c>ListItem</c> elements whose bounds contain the given element, tightest first.
    /// </summary>
    /// <remarks>
    /// Ordered by area so a nested row is preferred over the outer list that also contains it.
    /// </remarks>
    private IReadOnlyList<IMauiElement> FindContainingRows(IMauiElement element)
    {
        var center = ElementGeometryExtensions.CenterOf(element.Rect);

        return this.FindVisibleElements(Locator.ByControlType("ListItem"))
            .Where(item => item.Rect.Contains(center))
            .OrderBy(item => item.Area())
            .ToList();
    }

    /// <summary>
    /// Activates a candidate row, reporting failure rather than throwing.
    /// </summary>
    /// <remarks>
    /// Unlike a control's click, this walks a list of candidates and a given one may simply be
    /// the wrong element, so an unsuccessful pattern is an answer rather than a fault. A
    /// pointer-policy violation is still allowed to surface — that is a configuration error,
    /// not a mismatched candidate.
    /// </remarks>
    private static bool TryActivate(IMauiElement element)
    {
        if (!element.HasUsableBounds())
        {
            return false;
        }

        try
        {
            if (ActivationHelper.TryActivateByPattern(element))
            {
                return true;
            }

            element.Click();
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
