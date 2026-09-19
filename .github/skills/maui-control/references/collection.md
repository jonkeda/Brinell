# Collection

Repeating rows whose children are controls. Two types: the collection,
`CollectionObjectBase<TParent, TSelf, TItem>`, and its row, `ItemObjectBase<TCollection, TSelf>`,
both in `srcnew/Brinell.Maui/Containers/`. A collection is a container, so
[container.md](container.md) applies to it too.

Options that are only text (a Picker's items) are not a collection: use `SelectorControlBase`.

## Framework collection

The framework does not ship concrete rows. It ships an abstract, self-referencing collection
with protected constructors, and shows the derivation in its remarks:

```csharp
namespace Brinell.Maui.Controls.Collection;

/// <summary>MAUI CollectionView: a scrollable collection that hands out typed, scoped rows.</summary>
/// <remarks>
/// Derive from this rather than instantiating it:
/// <code>
/// public class ProductCollection : CollectionView&lt;ProductsPage, ProductCollection, ProductRow&gt;
/// {
///     public ProductCollection(IMauiScope&lt;ProductsPage&gt; scope)
///         : base(scope, "ProductList", ItemStrategy.ByAutomationId("ProductRow"),
///                (c, root, i) =&gt; new ProductRow(c, root, i)) { }
/// }
/// </code>
/// </remarks>
public abstract partial class CollectionView<TParent, TSelf, TItem>
    : CollectionObjectBase<TParent, TSelf, TItem>
    where TParent : IMauiScope<TParent>
    where TSelf : CollectionView<TParent, TSelf, TItem>
    where TItem : class, IMauiItemObject<TSelf, TItem>
{
    protected CollectionView(IMauiScope<TParent> parentScope, Locator locator,
        IItemStrategy itemStrategy, Func<TSelf, IMauiElement, int, TItem> itemFactory)
        : base(parentScope, locator, itemStrategy, itemFactory) { }

    protected CollectionView(IMauiScope<TParent> parentScope, string automationId,
        IItemStrategy itemStrategy, Func<TSelf, IMauiElement, int, TItem> itemFactory)
        : base(parentScope, automationId, itemStrategy, itemFactory) { }

    // No SelectionMode or MultiSelectEnabled: neither platform publishes them to automation.
}
```

## Item strategy, in order of preference

| Strategy | When |
| --- | --- |
| `ItemStrategy.ByAutomationId("Row")` / `ByLocator(locator)` | the row id repeats on every row: normal MAUI templating |
| `ItemStrategy.Within(hostLocator, inner)` | the collection root is not the element that holds the rows |
| `ItemStrategy.ByIndexedId(prefix)` | only when the app gives each row a unique id |
| a custom `IItemStrategy` | only for platform structure the built-ins cannot express |

## Rows

```csharp
/// <summary>One product row, scoped to its own item root.</summary>
/// <remarks>
/// Every automation id below repeats unchanged on every row. A row receives an
/// already-discovered root and its index; it never locates itself by a unique id.
/// </remarks>
public class ProductRow : ItemObjectBase<ProductCollection, ProductRow>
{
    public ProductRow(ProductCollection collection, IMauiElement itemRoot, int index)
        : base(collection, itemRoot, index) { }

    public CheckBox<ProductRow> Selected => new(this, "ProductSelectedCheckBox");
    public Label<ProductRow> Name => new(this, "ProductNameLabel");
    public Button<ProductRow> Delete => new(this, "ProductDeleteButton");
}
```

- A public `(collection, itemRoot, index)` constructor and named child controls.
- A row never locates itself by a unique id. It is handed its root, and re-resolves through
  the collection when virtualization recycles it.
- A row that is itself a leaf control (a tab, a menu entry) derives from
  `ClickableItemBase` or `SelectableItemBase` in `Controls/Base/`, with Core methods on its
  own root (`TabItem`).

## Looping and virtualized platform lists

A `CarouselView` loops by default, and on Windows a looping carousel publishes its items over
and over (5 cards became about 6,500 list items). Any tree search that walks it - including a
search for a control placed after it, and a raw probe script - does not finish, and can hang
the app. Before probing or testing a carousel, set `Loop="False"` in the sample, and say in the
control's remarks that a looping carousel cannot be searched on Windows.

On Windows `IsVisible` means "on screen now". A check that scrolls (`AssertVisibleAfterScroll`)
can move the carousel itself, so use it only to prove the current item is shown; prove the
others are off screen with `TryItem(i)?.Name.AssertVisible(false)`. `Item(i)` finds only
realized rows (by `PositionInSet`), and waits for one; an off-screen row may or may not be
realized depending on timing, so a test that demands it with `Item(i)` passes on one run and
fails on the next.

## Counts and indexes

- `GetItemCount` is the number of **realized** rows, not the data count. Under virtualization
  they differ. A logical count comes from what the app shows (a count label), as a domain
  helper on the app collection. Say in each member's remarks which one it means.
- The logical index comes from `PositionInSet` where the platform publishes it.
- **A row remembers which item it holds** (`Key`: the logical index, else a stable or unique
  automation id, else the position). A row whose element went away is found again by that key;
  a row whose element now shows another item reports `ItemChanged` rather than answering for it.
  The row's own members (`Click`, `GetText`) check the key too: a cached element that now holds
  another item is dropped and the item is found again. Only a position key cannot be checked,
  so prefer rows the platform numbers.

## Waiting and budgets

- Non-`Try` members wait within their `timeoutMs`: `Item(i)`, `this[i]`, `Item(key)`,
  `ItemWhere`, `SelectItem`, `WaitItemCount`, `WaitAnyItem`, `WaitForItems`, `Assert*`.
  `TryItem`, `TrySelectItem(i)`, `GetItemCount()` and `IsEmpty()` answer about now and take no
  timeout.
- `SelectItem` polls for the row, then activates it once. An `ActivateItemCore` override answers
  false only when it activated nothing (asked again within the budget); anything that throws
  ends the call, because the row may already be activated.
- A wait below a call - a scroll in a Core method or an override - takes `CallRemainingMs`, never a
  default of its own. `IMauiElement.ScrollIntoView(timeoutMs)` has no default for that reason.
- `FindItem(predicate, timeoutMs)` scrolls through the list once and answers null at the end.
  `ItemWhere(predicate, timeoutMs)` keeps scrolling and looking until the budget runs out.
- `ScrollToItem`, `ScrollToEnd`, `ScrollToTop`, `WaitForItems`, `FindItem` and `ItemWhere` take
  one scroll step per attempt, all on the one budget. A long list needs a budget to match
  (the 60-row product search takes about 12 s on Windows).

## What the base gives, and what to override

Given: `Item(i)`, `this[i]`, `TryItem(i)`, `Item(key)`, `ItemByAutomationId`, `ItemByText`,
`ItemByName`, `Items`, `ToList()`, `FindItem(predicate)`, `ItemWhere(predicate)`,
`GetItemCount`, `WaitItemCount`, `AssertItemCount`, `AssertEmpty`, `WaitForItems`,
`ScrollToItem`, `ScrollToTop`, `ScrollToEnd`, `SelectItem`, `TrySelectItem`.

| Override | When |
| --- | --- |
| `ScrollTarget` | the scrolling element is not the root (a wrapper around the CollectionView) |
| `ActivateItemCore(itemRoot)` | selecting a row is not a tap or invoke of its root. Return false only when nothing was activated: `SelectItem` asks again within its budget |
| `MatchesKey(itemRoot, key)` | rows are keyed another way than a descendant matching the locator |

Collection-level controls (title, empty view, count label, buttons) are named properties on
the collection, like any container's children.

## App collections (test projects)

In the test project's `Containers/` folder, a plain `.cs` deriving from `CollectionObjectBase`
or a framework collection, with domain helpers built from its children and items:

```csharp
public ProductRow ByName(string name) => ItemWhere(row => row.Name.GetText() == name);
```

## Examples to read

`srcnew/Brinell.Maui/Controls/Collection/CollectionView.tpl.cs`,
`srcnew/Brinell.Maui/Controls/Navigation/TabItem.tpl.cs`,
`testsnew/Brinell.Maui.UITests/Containers/ProductCollection.cs` and `ProductRow.cs`.
