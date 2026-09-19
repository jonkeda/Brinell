using Brinell.Maui.Calls;
using Brinell.Maui.Configuration;
using Brinell.Maui.Controls;

namespace Brinell.Maui.Containers;

/// <summary>
/// Base class for collection objects: a container that also hands out typed items.
/// </summary>
/// <remarks>
/// <para>
/// Being a container, a collection scopes its own non-item controls too - a title,
/// an empty view, a footer - alongside <see cref="Item(int, int?)"/>.
/// </para>
/// <para>
/// Every non-<c>Try</c> member is one call that waits within its budget, and every <c>Try*</c>
/// member answers about now (<c>.my/stale-readiness/design.md</c>, section 7.6, Q10). Members that
/// scroll to realize rows (<see cref="ScrollToItem"/>, <see cref="ScrollToEnd"/>,
/// <see cref="WaitForItems"/>, <see cref="ItemWhere"/>, <see cref="FindItem"/>) take one scroll
/// step per attempt of the call's poll, so the caller's budget covers the whole loop (Q9).
/// </para>
/// </remarks>
/// <typeparam name="TParent">The parent scope type.</typeparam>
/// <typeparam name="TSelf">The collection type itself (self-referencing).</typeparam>
/// <typeparam name="TItem">The item container type.</typeparam>
public abstract class CollectionObjectBase<TParent, TSelf, TItem>
    : ContainerObjectBase<TParent, TSelf>, IMauiCollectionObject<TParent, TSelf, TItem>, IItemRootProvider
    where TParent : IMauiScope<TParent>
    where TSelf : CollectionObjectBase<TParent, TSelf, TItem>
    where TItem : class, IMauiItemObject<TSelf, TItem>
{
    /// <summary>The detail of a <c>Missing</c> observation that means "scrolled to the end".</summary>
    private const string EndOfList = "reached the end of the list";

    private readonly IItemStrategy _itemStrategy;
    private readonly Func<TSelf, IMauiElement, int, TItem> _itemFactory;

    /// <summary>
    /// Whether the last scroll this collection performed was a jump, or null before the first.
    /// </summary>
    private bool? _lastScrollJumped;

    /// <summary>The realized rows the items being created were found among, for their keys.</summary>
    private IReadOnlyList<IMauiElement>? _keyRoots;

    /// <summary>The automation ids more than one of <see cref="_keyRoots"/> carries; computed on demand.</summary>
    private HashSet<string>? _duplicateIds;

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

    #region Item keys

    /// <inheritdoc />
    /// <remarks>
    /// The strongest the platform offers: the logical index where the platform publishes one
    /// (<c>PositionInSet</c>); otherwise the automation id, where the item strategy says ids are
    /// stable or no other realized row carries the same one; otherwise the position.
    /// </remarks>
    public ItemKey KeyOf(IMauiElement itemRoot, int position)
    {
        ArgumentNullException.ThrowIfNull(itemRoot);

        if (ItemKey.LogicalIndexOf(itemRoot) is { } logical)
        {
            return ItemKey.Logical(logical);
        }

        var id = itemRoot.AutomationId;
        if (!string.IsNullOrEmpty(id) && (_itemStrategy.HasStableIds || IsUniqueAmongRealized(itemRoot, id)))
        {
            return ItemKey.AutomationId(id);
        }

        return ItemKey.Position(position);
    }

    /// <inheritdoc />
    public IMauiElement? TryGetItemRoot(ItemKey key) => key.Kind switch
    {
        ItemKeyKind.Logical => WithRoot(
            root => _itemStrategy.FindItemElements(root).FirstOrDefault(item => ItemKey.LogicalIndexOf(item) == key.Index),
            null),
        ItemKeyKind.AutomationId => WithRoot(
            root => _itemStrategy.FindItemElements(root).Where(item => item.AutomationId == key.Value).ToList() is [var only]
                ? only
                : null,
            null),
        _ => TryGetItemRoot(key.Index)
    };

    /// <summary>Whether no other realized row carries <paramref name="id"/>.</summary>
    private bool IsUniqueAmongRealized(IMauiElement itemRoot, string id)
    {
        if (_keyRoots == null || !_keyRoots.Any(root => ReferenceEquals(root, itemRoot)))
        {
            // Created outside a listing (a row re-found by its own lookup): list the rows once.
            return TryGetItemRoots().Count(root => root.AutomationId == id) == 1;
        }

        _duplicateIds ??= _keyRoots
            .Select(root => root.AutomationId)
            .Where(value => !string.IsNullOrEmpty(value))
            .GroupBy(value => value!)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();

        return !_duplicateIds.Contains(id);
    }

    /// <summary>Builds the item for a row found among <paramref name="roots"/>.</summary>
    private TItem CreateItem(IMauiElement itemRoot, int index, IReadOnlyList<IMauiElement> roots)
    {
        if (!ReferenceEquals(_keyRoots, roots))
        {
            _keyRoots = roots;
            _duplicateIds = null;
        }

        return _itemFactory(Self, itemRoot, index);
    }

    /// <summary>Builds the item for a row, at its logical index when it has one.</summary>
    private TItem CreateItemAt(IMauiElement itemRoot, int positionalIndex, IReadOnlyList<IMauiElement> roots)
        => CreateItem(itemRoot, ItemKey.LogicalIndexOf(itemRoot) ?? positionalIndex, roots);

    #endregion

    #region Item access

    /// <summary>
    /// Gets the item at <paramref name="index"/>, waiting for it to appear. Equivalent to
    /// <see cref="Item(int, int?)"/>; the indexer reads better for a direct lookup, <c>Item(i)</c>
    /// mid-chain.
    /// </summary>
    [System.Runtime.CompilerServices.IndexerName("ItemAt")]
    public TItem this[int index] => Item(index);

    /// <inheritdoc />
    /// <remarks>
    /// Waits, like <see cref="Item(string, int?)"/>: a row often appears a frame after whatever
    /// added it. Use <see cref="TryItem(int)"/> to ask about right now. Does not scroll; use
    /// <see cref="ScrollToItem"/> for a row outside the realized window.
    /// </remarks>
    public TItem Item(int index, int? timeoutMs = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        return RunFind(() => TryItem(index), timeoutMs,
            () => new ElementNotFoundException(
                $"No item at index {index} in collection. Locator: {Locator}, materialized items: {GetItemCount()}."));
    }

    /// <inheritdoc />
    public TItem? TryItem(int index)
    {
        if (index < 0) return null;

        return WithRoot(root =>
        {
            var roots = _itemStrategy.FindItemElements(root);
            var itemRoot = FindItemRoot(root, roots, index);
            return itemRoot == null ? null : CreateItem(itemRoot, index, roots);
        }, null);
    }

    /// <summary>
    /// Finds the root element of the item at <paramref name="index"/>, or null when there is none now.
    /// </summary>
    /// <remarks>The logical index where the platform publishes one, otherwise the position.</remarks>
    public IMauiElement? TryGetItemRoot(int index)
    {
        if (index < 0) return null;

        return WithRoot(root => FindItemRoot(root, _itemStrategy.FindItemElements(root), index), null);
    }

    private IMauiElement? FindItemRoot(IMauiElement collectionRoot, IReadOnlyList<IMauiElement> roots, int index)
    {
        if (roots.Any(item => ItemKey.LogicalIndexOf(item).HasValue))
        {
            return roots.FirstOrDefault(item => ItemKey.LogicalIndexOf(item) == index);
        }

        return index < roots.Count ? roots[index] : _itemStrategy.FindItemElement(collectionRoot, index);
    }

    /// <summary>The logical indexes of the realized rows, in tree order, as one comparable string.</summary>
    private string RealizedLogicalIndexes()
        => string.Join(",", TryGetItemRoots().Select(root => ItemKey.LogicalIndexOf(root)?.ToString() ?? "-"));

    /// <summary>
    /// Gets the item identified by <paramref name="key"/>: its automation id, or failing
    /// that its caption.
    /// </summary>
    /// <remarks>
    /// The id is tried across every item before any caption is considered. Use the
    /// <see cref="Locator"/> overload - <c>Toolbar[Locator.ByText("Save")]</c> - when a
    /// collection could answer to both.
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
    /// Waits because an item often appears a frame after whatever revealed it, as in
    /// <c>Menu.Open()["New"]</c>. Use <see cref="TryItem(string)"/> to ask about right now.
    /// </remarks>
    public TItem Item(string key, int? timeoutMs = null)
    {
        ArgumentNullException.ThrowIfNull(key);

        return RunFind(() => TryItem(key), timeoutMs,
            () => new ElementNotFoundException(
                $"No item with the automation id or caption '{key}' in collection. " +
                $"Locator: {Locator}, materialized items: {GetItemCount()}."));
    }

    /// <summary>
    /// Gets the item matching <paramref name="key"/>, waiting for it to appear and throwing
    /// when it does not.
    /// </summary>
    /// <inheritdoc cref="Item(string, int?)" path="/remarks"/>
    public TItem Item(Locator key, int? timeoutMs = null)
    {
        ArgumentNullException.ThrowIfNull(key);

        return RunFind(() => TryItem(key), timeoutMs,
            () => new ElementNotFoundException(
                $"No item matched {key} in collection. " +
                $"Locator: {Locator}, materialized items: {GetItemCount()}."));
    }

    /// <summary>
    /// A lookup as one call: each attempt checks the scope chain and the collection root, then
    /// looks once, until it finds something or the budget runs out.
    /// </summary>
    private TItem RunFind(Func<TItem?> lookup, int? timeoutMs, Func<ElementNotFoundException> notFound,
        [System.Runtime.CompilerServices.CallerMemberName] string? caller = null)
    {
        var budget = Budget(timeoutMs);
        caller ??= nameof(RunFind);
        return Call.Run(caller, null, budget, AnimationMs, context =>
        {
            TItem? found = null;
            if (Poll(context, _ => RootAttempt(_ => (found = lookup()) != null ? Observation.Done() : Observation.Missing())))
            {
                return found!;
            }

            throw context.Log.Last.Kind == ObservationKind.Missing ? notFound() : Failure(context, caller, budget);
        });
    }

    /// <summary>
    /// Gets the item identified by <paramref name="key"/>, or null when none matches.
    /// </summary>
    /// <remarks>
    /// Three passes, each across every item before the next begins: automation id, then
    /// caption, then accessibility name (how Android labels a tab).
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
    /// This and the three below are named forms of <see cref="Item(Locator, int?)"/>, for when the
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
                return CreateItemAt(itemRoots[index], index, itemRoots);
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
    protected IReadOnlyList<IMauiElement> TryGetItemRoots()
        => WithRoot(root => _itemStrategy.FindItemElements(root), []);

    /// <summary>
    /// The items, yielded lazily. A consumer that stops early - <c>Items.First(...)</c> -
    /// does not pay to materialize the rest.
    /// </summary>
    public IEnumerable<TItem> Items
    {
        get
        {
            var roots = TryGetItemRoots();
            for (var index = 0; index < roots.Count; index++)
            {
                yield return CreateItemAt(roots[index], index, roots);
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
    /// <remarks>One read of the realized rows.</remarks>
    public int GetItemCount()
        => WithRoot(root => _itemStrategy.FindItemElements(root).Count, 0);

    /// <summary>
    /// Whether the collection currently has no materialized items.
    /// </summary>
    public bool IsEmpty() => GetItemCount() == 0;

    /// <summary>
    /// Waits until the materialized item count equals <paramref name="expected"/>.
    /// </summary>
    public bool WaitItemCount(int expected, int? timeoutMs = null)
        => RunWait(() => GetItemCount() == expected, timeoutMs);

    /// <summary>
    /// Waits until at least one item is materialized.
    /// </summary>
    public bool WaitAnyItem(int? timeoutMs = null)
        => RunWait(() => GetItemCount() > 0, timeoutMs);

    /// <summary>
    /// Waits until at least <paramref name="minimumCount"/> items are materialized,
    /// scrolling to realize more if the count is short.
    /// </summary>
    /// <remarks>
    /// Prefer this to <see cref="WaitItemCount"/> on a virtualizing collection: the
    /// realized count is bounded by the viewport, so an exact match may never occur even
    /// though the data source holds more. Each attempt takes at most one scroll step, and only
    /// while the count is short; see <see cref="ScrollToEnd"/> for its limits.
    /// </remarks>
    public bool WaitForItems(int minimumCount = 1, int? timeoutMs = null)
    {
        var budget = Budget(timeoutMs);
        return Call.Run(nameof(WaitForItems), minimumCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            budget, AnimationMs, context =>
            {
                var materialize = MaterializeAttempts(
                    NextMaterializationIndex,
                    () => GetItemCount() >= minimumCount ? Observation.Done() : null);

                if (Poll(context, materialize))
                {
                    return true;
                }

                return context.Log.Last.Kind is ObservationKind.Missing or ObservationKind.Pending
                    ? false
                    : throw Failure(context, nameof(WaitForItems), budget);
            }, succeeded: met => met);
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
        => RunAssert<int?>(expected, () => GetItemCount(), (actual, wanted) => actual == wanted,
            message ?? $"Expected {expected} items. Locator: {Locator}", timeoutMs);

    /// <summary>
    /// Asserts whether the collection is empty, returning the collection for chaining.
    /// </summary>
    public TSelf AssertEmpty(bool? expected = true, string? message = null, int? timeoutMs = null)
        => RunAssert(expected, () => (bool?)IsEmpty(), (actual, wanted) => actual == wanted,
            message ?? $"Expected collection {(expected == true ? "to be empty" : "not to be empty")}. Locator: {Locator}",
            timeoutMs);

    #endregion

    #region Search by content

    /// <summary>
    /// Finds the first item matching <paramref name="predicate"/>, scrolling through the list once
    /// to realize more rows. Returns null when none matches by the end of the list.
    /// </summary>
    /// <remarks>
    /// Answers about the list as it is: it does not wait for an item to appear. The scroll through
    /// the list is one call on <paramref name="timeoutMs"/> (<c>DefaultWait</c> when null); a list
    /// that takes longer to scroll through needs a larger budget.
    /// </remarks>
    public TItem? FindItem(Func<TItem, bool> predicate, int? timeoutMs = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var budget = Budget(timeoutMs);
        return Call.Run(nameof(FindItem), null, budget, AnimationMs, context =>
        {
            TItem? match = null;
            var search = SearchAttempts(predicate, item => match = item);

            if (Poll(context, search, stop: IsEndOfList))
            {
                return match;
            }

            return IsEndOfList(context.Log.Last) ? null : throw Failure(context, nameof(FindItem), budget);
        });
    }

    /// <summary>
    /// Finds the first item matching <paramref name="predicate"/>, scrolling to realize more rows,
    /// and waiting for one to appear; throws when none does within the budget.
    /// </summary>
    public TItem ItemWhere(Func<TItem, bool> predicate, int? timeoutMs = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var budget = Budget(timeoutMs);
        return Call.Run(nameof(ItemWhere), null, budget, AnimationMs, context =>
        {
            TItem? match = null;
            if (Poll(context, SearchAttempts(predicate, item => match = item)))
            {
                return match!;
            }

            throw context.Log.Last.Kind is ObservationKind.Missing or ObservationKind.Pending
                ? new ElementNotFoundException(
                    $"No item matched the predicate in collection within {budget} ms. Locator: {Locator}, " +
                    $"materialized items: {GetItemCount()}. {context.Log.Summary()}.")
                : Failure(context, nameof(ItemWhere), budget);
        });
    }

    /// <summary>
    /// The attempts of a search: each looks at the realized rows not yet seen, then takes one
    /// scroll step.
    /// </summary>
    /// <remarks>
    /// A row is seen once, by its key. A row that holds another item after the predicate read it
    /// (the list recycled it) is left unseen for the next attempt: neither answer was about it.
    /// Rows keyed only by position cannot be told apart after a scroll, so they are read again.
    /// </remarks>
    private Func<AttemptContext, Observation> SearchAttempts(Func<TItem, bool> predicate, Action<TItem> found)
    {
        var seen = new HashSet<ItemKey>();
        return MaterializeAttempts(NextMaterializationIndex, () =>
        {
            var roots = TryGetItemRoots();
            for (var position = 0; position < roots.Count; position++)
            {
                var root = roots[position];
                if (ItemKey.LogicalIndexOf(root) is { } logical && seen.Contains(ItemKey.Logical(logical)))
                {
                    // Seen already: skip it without building the row object.
                    continue;
                }

                var item = CreateItemAt(root, position, roots);
                var key = item.Key;
                if (seen.Contains(key))
                {
                    continue;
                }

                var matches = predicate(item);
                if (!key.IsHeldBy(root))
                {
                    continue;
                }

                if (key.Kind != ItemKeyKind.Position)
                {
                    seen.Add(key);
                }

                if (matches)
                {
                    found(item);
                    return Observation.Done();
                }
            }

            return null;
        });
    }

    #endregion

    #region Scrolling

    /// <summary>
    /// Scrolls until the item at <paramref name="index"/> is materialized, returning the
    /// collection for chaining.
    /// </summary>
    /// <remarks>
    /// One call on <paramref name="timeoutMs"/> (<c>DefaultWait</c> when null): each attempt checks
    /// for the row, then takes one scroll step. Stops when the row resolves, and fails when the
    /// list reaches its end without it or the budget runs out. A long list needs a budget to match.
    /// </remarks>
    public TSelf ScrollToItem(int index, int? timeoutMs = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        var budget = Budget(timeoutMs);
        return Call.Run(nameof(ScrollToItem), index.ToString(System.Globalization.CultureInfo.InvariantCulture),
            budget, AnimationMs, context =>
            {
                ThrowIfPastTheEnd(index);

                var materialize = MaterializeAttempts(
                    () => index,
                    () => TryGetItemRoot(index) != null ? Observation.Done() : null);

                if (Poll(context, materialize, stop: IsEndOfList))
                {
                    return Self;
                }

                throw context.Log.Last.Kind is ObservationKind.Missing or ObservationKind.Pending
                    ? new ElementNotFoundException(
                        $"Could not scroll item {index} into view within {budget} ms. Locator: {Locator}, " +
                        $"materialized items: {GetItemCount()}, furthest reached: {FurthestReachableIndex()}. " +
                        $"{context.Log.Summary()}.")
                    : Failure(context, nameof(ScrollToItem), budget);
            });
    }

    /// <summary>Refuses an index past the end up front, rather than scrolling to the end first.</summary>
    private void ThrowIfPastTheEnd(int index)
    {
        var roots = TryGetItemRoots();
        var logicalCount = GetLogicalItemCount(roots);
        if (logicalCount.HasValue
            && index >= logicalCount.Value
            && roots.Any(item => ItemKey.LogicalIndexOf(item).HasValue))
        {
            throw new ElementNotFoundException(
                $"No logical item at index {index} in collection. Locator: {Locator}, "
                + $"item count: {logicalCount.Value}.");
        }
    }

    /// <summary>
    /// The highest index currently resolvable, used to tell real scroll progress from a
    /// realized window that merely slid.
    /// </summary>
    private int FurthestReachableIndex()
    {
        var roots = TryGetItemRoots();
        var logical = roots.Select(ItemKey.LogicalIndexOf).Where(index => index.HasValue)
            .Select(index => index!.Value).ToArray();
        return logical.Length > 0
            ? logical.Max()
            : roots.Count - 1;
    }

    private int? GetLogicalItemCount(IReadOnlyList<IMauiElement>? roots = null)
    {
        roots ??= TryGetItemRoots();
        var size = roots.Select(item => item.SizeOfSet).FirstOrDefault(value => value is > 0);
        if (size.HasValue)
        {
            return size.Value;
        }

        var semantic = ScrollTarget ?? TryGetContainerRoot();

        return int.TryParse(
            semantic?.ReadState("ItemCount"),
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out var count)
            ? count
            : null;
    }

    /// <summary>
    /// Scrolls the collection to the top, returning the collection for chaining.
    /// </summary>
    /// <remarks>
    /// Uses the first realized row's scroll-into-view where available; otherwise steps back by the
    /// container's one scroll route until it stops moving, or until the budget runs out.
    /// </remarks>
    public TSelf ScrollToTop(int? timeoutMs = null)
    {
        var budget = Budget(timeoutMs);
        return Call.Run(nameof(ScrollToTop), null, budget, AnimationMs, context =>
        {
            if (!Poll(context, _ => RootAttempt(root =>
                {
                    if (TryScrollItemIntoView(0))
                    {
                        return Observation.Done();
                    }

                    // Repeated only while the platform confirms movement. A swipe cannot confirm
                    // it, so where the element swipes one step is taken - repeating a swipe that
                    // cannot say it arrived would never end.
                    return (ScrollTarget ?? root).ScrollContent(-1) == ScrollStep.Moved
                        ? Observation.Pending("moved up")
                        : Observation.Done();
                })))
            {
                throw Failure(context, nameof(ScrollToTop), budget);
            }

            return Self;
        });
    }

    /// <summary>
    /// Scrolls the collection to the end, returning the collection for chaining.
    /// </summary>
    /// <remarks>
    /// One call on <paramref name="timeoutMs"/> (<c>DefaultWait</c> when null), one scroll step per
    /// attempt, until scrolling stops producing rows.
    /// </remarks>
    public TSelf ScrollToEnd(int? timeoutMs = null)
    {
        var budget = Budget(timeoutMs);
        return Call.Run(nameof(ScrollToEnd), null, budget, AnimationMs, context =>
        {
            var materialize = MaterializeAttempts(NextMaterializationIndex, () => null);

            Poll(context, materialize, stop: IsEndOfList);
            if (!IsEndOfList(context.Log.Last))
            {
                throw context.Log.Last.Kind == ObservationKind.Pending
                    ? new WaitTimeoutException(
                        $"'{Locator}' did not reach the end of its list within {budget} ms. {context.Log.Summary()}.",
                        budget)
                    : Failure(context, nameof(ScrollToEnd), budget);
            }

            return Self;
        });
    }

    private int NextMaterializationIndex() => FurthestReachableIndex() + 1;

    private static bool IsEndOfList(Observation observation)
        => observation is { Kind: ObservationKind.Missing, Detail: EndOfList };

    /// <summary>
    /// Asks the row at the given index to scroll itself into view.
    /// </summary>
    private bool TryScrollItemIntoView(int index)
    {
        var itemRoot = TryGetItemRoot(index);
        if (itemRoot == null) return false;

        try
        {
            itemRoot.ScrollIntoView(CallRemainingMs);
            return true;
        }
        catch (Exception error) when (error is NotSupportedException or InvalidOperationException)
        {
            // The platform could not scroll this row into view: an answer. A row that is gone is
            // not - it propagates as StaleElementException.
            return false;
        }
    }

    #endregion

    #region Materializing, one step per attempt

    private enum MaterializePhase
    {
        /// <summary>Look for the target; if absent, take a scroll step.</summary>
        Idle,

        /// <summary>A step was taken; wait for the rows to change.</summary>
        AwaitingProgress,

        /// <summary>A jump brought new rows; wait for them to stop changing.</summary>
        Settling
    }

    /// <summary>What a materializing loop carries from one attempt to the next.</summary>
    private sealed class Materialization
    {
        public MaterializePhase Phase = MaterializePhase.Idle;
        public string RealizedBefore = string.Empty;
        public int CountBefore;
        public long MovedAtMs;
        public bool Jumped;
        public bool UsedLastIntoView;
        public bool TriedLastIntoView;
    }

    /// <summary>
    /// The attempts of a loop that scrolls to realize rows: <see cref="ScrollToItem"/>,
    /// <see cref="ScrollToEnd"/>, <see cref="WaitForItems"/>, <see cref="FindItem"/> and
    /// <see cref="ItemWhere"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each attempt checks the scope chain and the collection root, then, by phase: waits for a
    /// step's rows to arrive ("waiting for new rows") or to settle ("rows still moving"); otherwise
    /// looks for the target, and when it is absent takes one scroll step ("scrolled"). A step that
    /// realizes nothing, or a list that cannot move, is the end of the list, observed as
    /// <c>Missing</c>. No step waits on its own: the call's poll and budget are the only ones
    /// (<c>.my/stale-readiness/design.md</c>, R2 and Q9).
    /// </para>
    /// <para>
    /// The closure owns the loop's state, so one is built per call.
    /// </para>
    /// </remarks>
    /// <param name="nextIndex">The index to scroll towards.</param>
    /// <param name="target">Done when the target is there; null when it is not.</param>
    private Func<AttemptContext, Observation> MaterializeAttempts(Func<int> nextIndex, Func<Observation?> target)
    {
        var state = new Materialization();
        return context => RootAttempt(root => MaterializeStep(context, state, root, nextIndex, target));
    }

    private Observation MaterializeStep(AttemptContext context, Materialization state, IMauiElement root,
        Func<int> nextIndex, Func<Observation?> target)
    {
        switch (state.Phase)
        {
            case MaterializePhase.AwaitingProgress:
            {
                var realized = RealizedLogicalIndexes();
                if (GetItemCount() > state.CountBefore || realized != state.RealizedBefore)
                {
                    state.TriedLastIntoView = false;
                    if (state.Jumped)
                    {
                        // Rows read while the list is still recycling mix up positions and
                        // content, so let them settle first.
                        state.Phase = MaterializePhase.Settling;
                        state.RealizedBefore = realized;
                        return Observation.Pending("rows still moving");
                    }

                    state.Phase = MaterializePhase.Idle;
                    break;
                }

                // A jump's rows reach the tree after the jump returns, however long that takes;
                // a step's rows arrive within a moment or not at all.
                if (state.Jumped || context.Deadline.ElapsedMs - state.MovedAtMs < ProgressWindowMs)
                {
                    return Observation.Pending("waiting for new rows");
                }

                state.Phase = MaterializePhase.Idle;
                if (state.UsedLastIntoView)
                {
                    // Pulling the last row into view realized nothing; take a real step next.
                    break;
                }

                return Observation.Missing(EndOfList);
            }

            case MaterializePhase.Settling:
            {
                var realized = RealizedLogicalIndexes();
                if (realized != state.RealizedBefore)
                {
                    state.RealizedBefore = realized;
                    return Observation.Pending("rows still moving");
                }

                state.Phase = MaterializePhase.Idle;
                break;
            }
        }

        if (target() is { } done)
        {
            return done;
        }

        state.RealizedBefore = RealizedLogicalIndexes();
        state.CountBefore = GetItemCount();
        if (!ScrollOnce(root, nextIndex(), state))
        {
            return Observation.Missing(EndOfList);
        }

        state.Phase = MaterializePhase.AwaitingProgress;
        state.MovedAtMs = context.Deadline.ElapsedMs;
        return Observation.Pending("scrolled");
    }

    /// <summary>How long a scroll step's new rows may take to appear before the step counts as the end.</summary>
    private int ProgressWindowMs => Math.Max(AnimationMs, PollingIntervalMs * 5);

    /// <summary>One scroll step toward <paramref name="nextIndex"/>; false when the list cannot move.</summary>
    private bool ScrollOnce(IMauiElement root, int nextIndex, Materialization state)
    {
        // On a collection known not to jump, pull the last realized row into view first, which
        // makes the virtualizing panel realize the rows after it.
        if (_lastScrollJumped == false && !state.TriedLastIntoView && TryScrollLastItemIntoView())
        {
            state.TriedLastIntoView = true;
            state.UsedLastIntoView = true;
            state.Jumped = false;
            return true;
        }

        state.UsedLastIntoView = false;

        // The scrollable element is the item host, not this container's root, which may be a
        // non-scrolling wrapper around it. The element jumps where the app declared a route to an
        // index, and otherwise moves one step.
        ScrollStep step;
        try
        {
            step = (ScrollTarget ?? root).ScrollTowards(nextIndex);
        }
        catch (ArgumentOutOfRangeException)
        {
            // Past the end.
            return false;
        }

        _lastScrollJumped = step == ScrollStep.Jumped;
        state.Jumped = step == ScrollStep.Jumped;
        return step != ScrollStep.NotMoved;
    }

    /// <summary>
    /// The element that actually scrolls, when it is not this container's own root.
    /// </summary>
    /// <remarks>
    /// Override when the container root wraps the scrolling item host; returning null uses the
    /// container root.
    /// </remarks>
    protected virtual IMauiElement? ScrollTarget => null;

    /// <summary>
    /// Asks the last realized row to scroll itself into view.
    /// </summary>
    private bool TryScrollLastItemIntoView()
    {
        var roots = TryGetItemRoots();
        if (roots.Count == 0) return false;

        return ScrollHelper.ScrollIntoView(roots[^1], CallRemainingMs);
    }

    #endregion

    #region Selection

    /// <summary>
    /// Selects the item at <paramref name="index"/>, waiting for it to appear, returning the
    /// collection for chaining.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The row is found by polling, then activated once, as every action is (the control base's
    /// <c>ActOnce</c>). Any exception from the activation ends the call at once: the row may
    /// already have been activated, and activating it again could deselect it or navigate twice
    /// (R0).
    /// </para>
    /// <para>
    /// An activation that answers false is asked again within the budget. That is not a repeated
    /// action: <see cref="ActivateItemCore"/> answers false only when nothing was activated (a row
    /// without a size, or a selection the platform refused), which is a row not ready yet - a list
    /// re-laying out after a change. A row that stays refused fails the call when the budget
    /// runs out, naming the refusal.
    /// </para>
    /// </remarks>
    public TSelf SelectItem(int index, int? timeoutMs = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        const string NotActivated = "found, and the platform did not activate it";
        var budget = Budget(timeoutMs);
        return Call.Run(nameof(SelectItem), index.ToString(System.Globalization.CultureInfo.InvariantCulture),
            budget, AnimationMs, context =>
            {
                while (true)
                {
                    IMauiElement? itemRoot = null;
                    if (!Poll(context, _ => RootAttempt(_ =>
                            (itemRoot = TryGetItemRoot(index)) != null ? Observation.Done() : Observation.Missing())))
                    {
                        throw context.Log.Last.Kind == ObservationKind.Missing
                            ? new ElementNotFoundException(
                                $"No item at index {index} to select within {budget} ms. Locator: {Locator}, " +
                                $"materialized items: {GetItemCount()}.")
                            : Failure(context, nameof(SelectItem), budget);
                    }

                    if (ActivateItemCore(itemRoot!))
                    {
                        return Self;
                    }

                    context.Log.Add(Observation.Pending(NotActivated), context.Deadline.ElapsedMs);
                    if (context.Deadline.RemainingMs <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Item {index} was found, and the platform did not activate it within {budget} ms. " +
                            $"Locator: {Locator}. {context.Log.Summary()}.");
                    }

                    Brinell.Core.Utilities.WaitHelper.Pause(
                        Math.Max(1, Math.Min(PollingIntervalMs, context.Deadline.RemainingMs)));
                }
            });
    }

    /// <summary>
    /// Attempts to select the item at <paramref name="index"/>, now.
    /// </summary>
    public bool TrySelectItem(int index)
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
    /// Windows: the containing <c>ListItem</c> row is tried first, then the element itself, because
    /// the element a strategy matches is usually inside the row, and the row is what carries the
    /// selection pattern.
    /// </para>
    /// <para>
    /// Android and iOS: the element itself, whose <c>Select</c> is a tap - what a finger does to a
    /// row. <c>ListItem</c> is a UI Automation control type with no Appium counterpart; asking for
    /// it there threw before any row was touched (found by the Todo sample's Android run).
    /// </para>
    /// <para>Override for rows that activate differently.</para>
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

        if (Context.Platform == MauiPlatform.Windows)
        {
            foreach (var row in FindContainingRows(itemRoot))
            {
                if (TryActivate(row))
                {
                    return true;
                }
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
    private static bool TryActivate(IMauiElement element)
    {
        if (!element.HasUsableBounds())
        {
            return false;
        }

        try
        {
            // A candidate may be the wrong element, so a failure here is an answer, not a fault:
            // the pattern is absent (NotSupported) or refused (InvalidOperation). A candidate that
            // is gone is not an answer - it propagates as StaleElementException.
            element.Select();
            return true;
        }
        catch (Exception error) when (error is NotSupportedException or InvalidOperationException)
        {
            return false;
        }
    }

    #endregion
}
