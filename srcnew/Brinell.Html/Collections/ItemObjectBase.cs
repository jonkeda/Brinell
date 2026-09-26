using Brinell.Core.Calls;
using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Core.Scopes;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Collections;

/// <summary>
/// One row of a collection, as a scope for the controls inside it. The row stands for one item
/// (<see cref="Key"/>) and finds its element again by that key on every lookup (design 7.6).
/// </summary>
/// <remarks>
/// When its item is no longer in the list, the row reports <see cref="ScopeReadinessState.ItemChanged"/>:
/// a call on one of its controls waits for the item within its budget, then fails naming it. It
/// never finds another item's row in its place, which a row kept by position would (R0).
/// </remarks>
public abstract class ItemObjectBase<TSelf> : ObjectBase, IHtmlScope<TSelf>
    where TSelf : ItemObjectBase<TSelf>
{
    private readonly IHtmlCollection _collection;

    /// <summary>The row of <paramref name="key"/>, found at <paramref name="position"/> when it was made.</summary>
    protected ItemObjectBase(IHtmlCollection collection, ItemKey key, int position)
    {
        _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        Key = key;
        Position = position;
    }

    /// <summary>The item this row stands for.</summary>
    public ItemKey Key { get; }

    /// <summary>Where the row was when it was made. It may have moved since.</summary>
    public int Position { get; }

    public override IHtmlTestContext Context => _collection.Context;

    public abstract TSelf Self { get; }

    public IHtmlPage? Page => _collection.Page;

    public LocatorStrategy DefaultLocatorStrategy => _collection.DefaultLocatorStrategy;

    protected override string ScopeName => $"{GetType().Name}({Key})";

    /// <summary>The collection first, then the row of this item (R4).</summary>
    public ScopeReadiness ProbeReadiness()
    {
        if (_collection.ProbeReadiness() is { IsReady: false } collection)
        {
            return collection;
        }

        return _collection.TryGetItemRoot(Key) is null
            ? new ScopeReadiness(ScopeName, ScopeReadinessState.ItemChanged, $"{Key} is no longer in the list")
            : ScopeReadiness.Ready(ScopeName);
    }

    public bool IsReady() => ProbeReadiness().IsReady;

    public bool WaitReady(int? timeoutMs = null)
        => RunProbe(() => ScopeGate.Check(ProbeReadiness()) ?? Observation.Done(), timeoutMs ?? DefaultTimeoutMs);

    /// <summary>Whether the row's item is still in the list now.</summary>
    public bool IsExists() => _collection.TryGetItemRoot(Key) != null;

    public IHtmlElement? TryFindElement(Locator locator) => _collection.TryGetItemRoot(Key)?.TryFindElement(locator);

    public IHtmlElement FindElement(Locator locator) => TryFindElement(locator) ?? throw new ElementNotFoundException(locator);

    public IReadOnlyList<IHtmlElement> FindElements(Locator locator)
        => _collection.TryGetItemRoot(Key)?.FindElements(locator) ?? [];
}
