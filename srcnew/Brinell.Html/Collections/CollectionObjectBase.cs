using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Calls;
using Brinell.Html.Controls;
using Brinell.Html.Interfaces;
using Brinell.Html.Scopes;

namespace Brinell.Html.Collections;

/// <summary>
/// A container of item rows that hands out typed row objects (design 7.6).
/// </summary>
/// <remarks>
/// Rows are identified by <see cref="ItemKey"/>: their <c>data-item-key</c> when they have one, or
/// their position. Members without <c>Try</c> wait, as one call each; <c>Try*</c> members answer
/// about now (R7).
/// </remarks>
/// <typeparam name="TParent">The scope the collection is declared in.</typeparam>
/// <typeparam name="TSelf">The collection type, for fluent chains.</typeparam>
/// <typeparam name="TItem">The row type.</typeparam>
public abstract class CollectionObjectBase<TParent, TSelf, TItem> : ContainerBase<TParent, TSelf>, IHtmlCollection
    where TParent : IHtmlScope<TParent>
    where TSelf : CollectionObjectBase<TParent, TSelf, TItem>
    where TItem : ItemObjectBase<TItem>
{
    private readonly Locator _itemLocator;

    /// <summary>The collection found by <paramref name="locator"/>, whose rows match <paramref name="itemLocator"/> inside it.</summary>
    protected CollectionObjectBase(IHtmlScope<TParent> parent, Locator locator, Locator itemLocator)
        : base(parent, locator)
    {
        _itemLocator = itemLocator ?? throw new ArgumentNullException(nameof(itemLocator));
    }

    /// <summary>Makes the row object for <paramref name="key"/>, found at <paramref name="position"/>.</summary>
    protected abstract TItem CreateItem(ItemKey key, int position);

    /// <inheritdoc />
    public IHtmlElement? TryGetItemRoot(ItemKey key)
    {
        var rows = TryGetItemRoots();
        return key.Kind == ItemKeyKind.Logical
            ? rows.FirstOrDefault(row => row.GetAttribute(ItemKey.Attribute) == key.Value)
            : rows.ElementAtOrDefault(int.Parse(key.Value, System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>The rows now. One attempt.</summary>
    protected IReadOnlyList<IHtmlElement> TryGetItemRoots() => FindElements(_itemLocator);

    /// <summary>How many rows there are now.</summary>
    public int GetItemCount() => TryGetItemRoots().Count;

    /// <summary>The rows now, as row objects. One attempt.</summary>
    public IReadOnlyList<TItem> Items => [.. TryGetItemRoots().Select((row, position) => CreateItem(ItemKey.Of(row, position), position))];

    /// <summary>The row at <paramref name="index"/> now, or <c>null</c>.</summary>
    public TItem? TryItem(int index)
    {
        var rows = TryGetItemRoots();
        return index >= 0 && index < rows.Count ? CreateItem(ItemKey.Of(rows[index], index), index) : null;
    }

    /// <summary>The row whose <c>data-item-key</c> is <paramref name="key"/> now, or <c>null</c>.</summary>
    public TItem? TryItem(string key)
    {
        var rows = TryGetItemRoots();
        for (var position = 0; position < rows.Count; position++)
        {
            if (rows[position].GetAttribute(ItemKey.Attribute) == key)
            {
                return CreateItem(new ItemKey(ItemKeyKind.Logical, key), position);
            }
        }

        return null;
    }

    /// <summary>The row at <paramref name="index"/>, waiting for it.</summary>
    /// <exception cref="ElementNotFoundException">There was no such row within the budget.</exception>
    public TItem Item(int index, int? timeoutMs = null)
        => WaitFor(() => TryItem(index), $"row {index}", timeoutMs);

    /// <summary>The row of item <paramref name="key"/>, waiting for it.</summary>
    /// <exception cref="ElementNotFoundException">There was no such item within the budget.</exception>
    public TItem Item(string key, int? timeoutMs = null)
        => WaitFor(() => TryItem(key), $"item '{key}'", timeoutMs);

    /// <summary>The first row that matches <paramref name="predicate"/>, waiting for one.</summary>
    /// <exception cref="ElementNotFoundException">No row matched within the budget.</exception>
    public TItem ItemWhere(Func<TItem, bool> predicate, int? timeoutMs = null)
        => WaitFor(() => Items.FirstOrDefault(predicate), "a row that matches", timeoutMs);

    /// <summary>Waits until there are <paramref name="expected"/> rows.</summary>
    public bool WaitItemCount(int expected, int? timeoutMs = null)
        => RunProbe(() => ScopeGate.Check(ProbeReadiness())
            ?? (GetItemCount() == expected ? Observation.Done() : Observation.Pending($"{GetItemCount()} rows")),
            timeoutMs ?? DefaultTimeoutMs);

    /// <summary>Fails unless there are <paramref name="expected"/> rows within the budget.</summary>
    public TSelf AssertItemCount(int expected, string? message = null, int? timeoutMs = null)
    {
        RunProbe(
            () => ScopeGate.Check(ProbeReadiness())
                ?? (GetItemCount() == expected ? Observation.Done() : Observation.Pending($"{GetItemCount()} rows")),
            timeoutMs ?? DefaultTimeoutMs,
            _ =>
            {
                var actual = GetItemCount();
                return new AssertionException(message ?? $"Expected {expected} rows in {ScopeName}, but there are {actual}.", expected, actual);
            });
        return Self;
    }

    private TItem WaitFor(Func<TItem?> find, string what, int? timeoutMs)
    {
        TItem? found = null;
        var budget = timeoutMs ?? DefaultTimeoutMs;
        RunProbe(
            () => ScopeGate.Check(ProbeReadiness()) ?? ((found = find()) != null ? Observation.Done() : Observation.Missing(what)),
            budget,
            _ => new ElementNotFoundException($"{ScopeName} had no {what} within {budget} ms."));
        return found!;
    }
}
