---
title: Containers and Collections Design (Brinell.Maui)
description: ContainerObject as a first-class PageObject sibling, and Collection controls that hand out real scoped list items
status: design proposal
scope: Brinell.Maui (Core interface changes noted where required)
---

# Containers and Collections Design

## 1. Goal

Two things, stated as the request framed them:

1. A **container is a PageObject-like thing** — a `ContainerObject`. It holds `ControlObject`s
   and other `ContainerObject`s, and element searches happen *inside* it.
2. **Collection controls** work the same way: you can retrieve a list item, and that item is
   itself a `ContainerObject` scoped to the item's own subtree.

Focus is `Brinell.Maui`. Core interface changes are called out separately because they are
shared with WinForms/Wpf/Blazor/NativeAndroid.

> **Status: design only — not implemented.** Nothing in `srcnew/` or `testsnew/` has been
> changed. The sample app page, page objects, and tests under [samples/](samples/) are written
> against the proposed bases and are destined for the real MAUI codebase at the paths each file
> names, but they land **only on an explicit go-ahead**. See
> [README.md](README.md#destinations-when-implementing) for the full destination map.

## 2. Where the current code already gets it right

This is not a greenfield design. The scope abstraction is already in place and is sound:

| Piece | File | What it gives us |
|---|---|---|
| `IElementScope<TElement>` | [IElementScope.cs](../../srcnew/Brinell.Core/Interfaces/IElementScope.cs) | `TryFindElement` / `FindElement` / `FindElements` + `IsReady` / `WaitReady` |
| `IMauiScope<TScope>` | [IMauiScope.cs](../../srcnew/Brinell.Maui/Interfaces/IMauiScope.cs) | self-referencing scope for fluent returns |
| `IMauiPage<TSelf>` | [IMauiPage.cs](../../srcnew/Brinell.Maui/Interfaces/IMauiPage.cs) | page = root scope, no parent |
| `IMauiContainer<TParent,TSelf>` | [IMauiContainer.cs](../../srcnew/Brinell.Maui/Interfaces/IMauiContainer.cs) | container = scope + `Parent` |
| `ContainerBase<TParent,TSelf>` | [ContainerBase.cs](../../srcnew/Brinell.Maui/Controls/ContainerBase.cs) | root caching, stale-retry, scoped find, control factories |

**Page and container are already siblings under one scope interface.** A control does not know
or care whether its `TScope` is a page or a container — that is exactly the property the request
asks for, and it already holds. So this design is mostly about *closing gaps*, not rebuilding.

The gaps are what the rest of this document is about.

## 3. What is actually broken

> **Verified.** Every claim in this section is pinned by a runnable Moq test in
> [samples/VerifiedDefectRecordTests.cs](samples/VerifiedDefectRecordTests.cs), which passes
> against the code as it stands today (4/4). Two earlier drafts of this section overstated the
> problem; the scope below is what the tests actually show.

### 3.0 What already works — do not "fix" this

A **control** inside a container is already correct on both axes:

- it resolves **element-relative** to the container root, and
- it returns the **container**, not the page.

`Works_ControlInContainer_IsScopedAndReturnsContainer` asserts both. This is because
`Button<LegacyContainer>` binds the *control's* own `TScope` to the container, and
`ClickableControlBase<TScope>.Click()` returns `TScope`. Nothing here needs changing, and the
new bases must preserve it.

### 3.1 A container's own inherited actions return the parent, not the container

`ContainerBase<TParent, TSelf> : ControlBase<TParent>` — so members the container inherits *as a
control* return `TParent`:

```csharp
LegacyTestPage result = page.Container.AssertVisible(true);   // the PAGE, not the container
```

`Defect_ContainerOwnAction_ReturnsPage` confirms this. The consequence is narrower than "every
action ejects you" — it is specifically the container's own state members (`AssertVisible`,
`AssertExists`, `Click` on the container itself) that break a chain:

```csharp
page.UserProfile
    .AssertVisible(true)     // -> ContainerDemoPage
    .NameEntry               // compile error: page has no NameEntry
```

You must restart from `page.UserProfile`. `Grid<TParent,TSelf>` / `Grid<TScope>` exist as a
doubled type to work around exactly this.

### 3.2 List item roots are resolved page-wide, so rows need globally unique ids

The child controls of a row *are* scoped correctly — the mock trace shows
`rowRoot.FindElement(RowLabel)`, element-relative. The defect is one level up: **the row root
itself** is found by a page-wide locator.

[List.cs:82-87](../../srcnew/Brinell.Maui/Controls/List.cs#L82-L87) passes the list's *parent*
scope to the item factory:

```csharp
public TItem Item(int index)
{
    var scope = ContainingScope as IMauiScope<TScope> ...;
    return _itemFactory(scope, index);   // the LIST'S parent - not the list, not the item
}
```

and [ContainerDemoPage.cs:23-27](../../testsnew/Brinell.Maui.UITests2/Pages2/ContainerDemoPage.cs#L23-L27)
shows the factory ignoring its own `scope` parameter and passing the page:

```csharp
(scope, index) => new TaskItemContainer(this, index)   // 'this' = the page
```

So each row is located by `Locator.ByAutomationId($"Task_{index}")` against the whole page. That
only works because every row carries a globally unique id. Give rows a **repeating** id — the
normal MAUI item-template style — and every index collapses onto the same element:

```csharp
// Defect_RepeatingRowId_AllIndexesCollapseToSameRow
Assert.Equal("FIRST ROW ONLY", page.Rows.Item(0).RowLabel.GetText());
Assert.Equal("FIRST ROW ONLY", page.Rows.Item(1).RowLabel.GetText());   // same row
```

The sample app pays this tax today. `ContainerDemoViewModel.TaskItem` carries an
`AutomationId => $"Task_{_id}"` property and a `ReindexTasks()` call after every mutation, purely
so rows stay addressable. And
[ContainerScopingTests.ListItems_AreIndependentlyScoped](../../testsnew/Brinell.Maui.UITests2/Tests2/Container/ContainerScopingTests.cs)
is `[Fact(Skip = ...)]`, attributed to "sample app XAML naming inconsistency" — but the XAML uses
repeating `TaskNameLabel` ids on every row, so the skip is really this defect.

Two further consequences:

- `GetItemCount()` probes `Task_0, Task_1, …` one at a time against the containing scope and stops
  at a hardcoded `maxItems = 100`, silently truncating (`Defect_GetItemCount_CapsAt100`).
- `TItem` is constrained only to `class`, so nothing guarantees an item is a container.
  `TrySelectItem` has to feature-test with `if (item is not IContainerControl<IMauiElement>)`.

### 3.3 There is no `ContainerObject` concept, only a control that happens to scope

`ContainerBase` lives in `Controls/` and is documented as a control. `Grid<TParent,TSelf>` /
`Grid<TScope>` exist as a pair only to work around #3.1. Most of `Controls/Container/` —
`Border`, `Frame`, `ContentView`, `BoxView`, `IsoPaneView` — are `ControlBase` subclasses that
**cannot scope at all**, despite being containers by name. Only `Grid` has a scoping form.

### 3.4 Collections are `ControlBase`, so a collection cannot hold controls either

`CollectionView<TScope,TItem>` derives from `List<…>` derives from `ControlBase<TScope>`. A
collection is not a scope, so a header/footer/empty-view inside a `CollectionView` has no scoped
home. `CollectionView<TScope>` (the `.Basic` file) exists as a second type for the same reason
`Grid` is doubled.

### 3.5 Container factory methods are `protected` and hand-maintained

`ContainerBase` carries ~30 `protected` factory methods (`Label`, `Button`, `Entry`, …) — a
hand-maintained list that already has a `// Note: Picker control is not yet implemented`
placeholder and comments about controls that moved to `Brinell.Maui.Extensions`. `PageObjectBase`
needs the same list and does not have it. This duplication should be generated, not typed.

## 4. Design

### 4.1 The shape

```
IMauiScope<TSelf>                     "things you can search inside"
├── IMauiPage<TSelf>                  root scope, no parent
├── IMauiContainerObject<TParent,TSelf>   scope + Parent + ContainerRoot
└── IMauiCollectionObject<TParent,TSelf,TItem>   container + typed item access
```

```
ObjectBase
└── ScopeObjectBase<TSelf>                     new (8.4) — Run*, ready-state, factories
    ├── PageObjectBase<TSelf>                  driver-rooted, IsLoaded / WaitIdle
    └── ContainerObjectBase<TParent,TSelf>     new — element-rooted, not a ControlBase
        ├── CollectionObjectBase<TParent,TSelf,TItem>   new — container + typed items
        └── ItemContainerBase<TCollection,TSelf>        new — root is a supplied element
```

Controls stay exactly as they are: `ControlBase<TScope> where TScope : IMauiScope<TScope>`.
They already accept a page or a container interchangeably. **No control changes are needed.**

### 4.2 `ContainerObjectBase<TParent, TSelf>` — the ContainerObject

The critical change: **it does not derive from `ControlBase<TParent>`.** It is a peer of
`PageObjectBase`, holding a root element instead of a driver root.

```csharp
public abstract class ContainerObjectBase<TParent, TSelf>
    : ObjectBase, IMauiContainerObject<TParent, TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf   : ContainerObjectBase<TParent, TSelf>
{
    private readonly IMauiScope<TParent> _parentScope;
    private IMauiElement? _cachedRoot;

    protected ContainerObjectBase(IMauiScope<TParent> parentScope, Locator locator);

    public TSelf   Self   => (TSelf)this;      // actions return the CONTAINER
    public TParent Parent => _parentScope.Self; // explicit step out
    public override IMauiTestContext Context => _parentScope.Context;

    public IMauiElement ContainerRoot { get; }  // cached, stale-retried (as today)
    protected virtual IMauiElement FindContainerRootElement();
    public void InvalidateCache();

    // IElementScope — scoped to ContainerRoot, no parent fallback (as today)
    public IMauiElement? TryFindElement(Locator locator);
    public IMauiElement  FindElement(Locator locator);
    public IReadOnlyList<IMauiElement> FindElements(Locator locator);

    public bool IsReady(int? timeoutMs = null);   // parent ready && root exists
    public bool WaitReady(int? timeoutMs = null);

    // Container-as-a-thing state, previously inherited from ControlBase but
    // now returning TSelf so you stay inside
    public bool IsExists(int? timeoutMs = null);
    public bool IsVisible(int? timeoutMs = null);
    public bool WaitExists(bool? expected, int? timeoutMs = null);
    public bool WaitVisible(bool? expected, int? timeoutMs = null);
    public TSelf AssertExists(bool? expected, string? m = null, int? t = null);
    public TSelf AssertVisible(bool? expected, string? m = null, int? t = null);
}
```

Fluent behaviour becomes:

```csharp
page.UserProfile                       // enter container
    .AssertVisible(true)               // -> UserProfileContainer  (stays inside)
    .NameEntry.SetText("Ada")          // -> UserProfileContainer  (control returns its scope)
    .SaveButton.Click()                // -> UserProfileContainer
    .Parent                            // -> ContainerDemoPage     (explicit exit)
    .AssertIdle();
```

`Parent` is the single, explicit way out. That is the whole ergonomic story.

Note which line actually changes: the two control lines already behave this way today (3.0). Only `.AssertVisible(true)` — the container's own member — is fixed here, and that one line is what currently breaks the chain.

The root-caching, stale-element retry, and no-fallback-to-parent search semantics in today's
`ContainerBase` are correct and move over unchanged — including the `catch (ElementNotFoundException)
→ return null` behaviour with its "Container scoping means elements must be within the container"
comment, which is exactly right and must be preserved.

### 4.3 Containers that are also interactive controls

A `SwipeView` or `RefreshView` is a container *and* has actions. Rather than multiply base classes,
expose the control aspect as a scoped property on the container:

```csharp
public class RefreshViewContainer<TParent, TSelf> : ContainerObjectBase<TParent, TSelf>
{
    // The container's own root, addressed as a control, scoped to the PARENT
    protected RefreshView<TParent> AsControl => new(_parentScope, Locator);

    public TSelf Refresh(int? t = null) { AsControl.Refresh(t); return Self; }
    public bool  IsRefreshing(int? t = null) => AsControl.IsRefreshing(t);
}
```

This keeps `TSelf` returns while reusing the existing control implementation verbatim. Same
treatment for `ScrollView`, `SwipeView`, `Border`, `Frame`.

### 4.4 `CollectionObjectBase<TParent, TSelf, TItem>` — the Collection

A collection **is** a container (so it can hold header/footer/empty-view controls) that
additionally hands out typed items.

```csharp
public abstract class CollectionObjectBase<TParent, TSelf, TItem>
    : ContainerObjectBase<TParent, TSelf>, IMauiCollectionObject<TParent, TSelf, TItem>
    where TParent : IMauiScope<TParent>
    where TSelf   : CollectionObjectBase<TParent, TSelf, TItem>
    where TItem   : IMauiItemContainer<TSelf, TItem>   // items ARE containers — enforced
{
    private readonly IItemStrategy _items;
    private readonly Func<TSelf, IMauiElement, int, TItem> _itemFactory;

    // --- item access ---
    public TItem this[int index] { get; }          // indexer: page.Tasks[2]
    public TItem Item(int index);
    public TItem? TryItem(int index);
    public IEnumerable<TItem> Items { get; }        // lazy - see 8.2
    public IReadOnlyList<TItem> ToList();          // materializes everything
    public int GetItemCount(int? timeoutMs = null);

    // --- search by content ---
    public TItem? FindItem(Func<TItem, bool> predicate);
    public TItem  ItemWhere(Func<TItem, bool> predicate);   // throws with diagnostics

    // --- state ---
    public bool  WaitItemCount(int expected, int? t = null);
    public bool  WaitAnyItem(int? t = null);
    public bool  IsEmpty(int? t = null);
    public TSelf AssertItemCount(int expected, string? m = null, int? t = null);
    public TSelf AssertEmpty(bool? expected, string? m = null, int? t = null);

    // --- selection / scrolling, returning TSelf ---
    public TSelf SelectItem(int index, int? t = null);
    public TSelf ScrollToItem(int index, int? t = null);
    public TSelf ScrollToTop(int? t = null);
    public TSelf ScrollToEnd(int? t = null);
}
```

Two things changed versus today's `List<TScope,TItem>`:

- `TItem` is constrained to be a container whose **parent is the collection**. Items are
  structurally guaranteed to be scoped, so `TrySelectItem`'s `is not IContainerControl<…>`
  feature-test disappears.
- The factory receives the **item's own root element**, not a page. That is what makes item
  scoping real.

### 4.5 Item containers: scoped to an element, not to a locator

Items cannot be located by a page-level locator — that is the root cause of §3.2. Introduce a
container whose root is an *element that was already found*:

```csharp
public abstract class ItemContainerBase<TCollection, TSelf>
    : ContainerObjectBase<TCollection, TSelf>, IMauiItemContainer<TCollection, TSelf>
{
    private readonly IMauiElement _itemRoot;
    public int Index { get; }

    protected ItemContainerBase(TCollection collection, IMauiElement itemRoot, int index)
        : base(collection, Locator.ForElement(itemRoot, index))
    { _itemRoot = itemRoot; Index = index; }

    // The whole point: the root is given, not searched for.
    protected override IMauiElement FindContainerRootElement() => _itemRoot;
}
```

Now an item template can use **repeating, non-unique** automation ids, because each item searches
only its own subtree:

```csharp
public class TaskItem : ItemContainerBase<TaskCollection, TaskItem>
{
    public TaskItem(TaskCollection c, IMauiElement root, int i) : base(c, root, i) { }

    public CheckBox<TaskItem> Done   => new(this, "TaskCheckBox");    // same id every row
    public Label<TaskItem>    Name   => new(this, "TaskNameLabel");   // same id every row
    public Button<TaskItem>   Delete => new(this, "TaskDeleteButton");
}
```

Compare with today's `TaskItemContainer`, which needs `Task_{index}` on the row *and* globally
unique ids inside it. Repeating ids inside a template are the natural MAUI authoring style, so
this removes a real burden from app authors, not just from test authors.

Stale-element handling: an item's root is a captured element, so virtualization can invalidate it.
`ItemContainerBase` overrides root recovery — on `StaleElementReferenceException` it asks the
owning collection to re-resolve item `Index` once, then fails with a message naming the collection
and index.

### 4.6 Item discovery strategies

How items are found is a policy, not a fixed rule. `GetItemCount`'s `Task_0, Task_1, …` probe with
`maxItems = 100` is one such policy, and a poor default. Make it pluggable:

```csharp
public interface IItemStrategy
{
    IReadOnlyList<IMauiElement> FindItemElements(IMauiElement collectionRoot);
    IMauiElement? FindItemElement(IMauiElement collectionRoot, int index);
    int? TryGetCount(IMauiElement collectionRoot);   // null => must enumerate
}
```

| Strategy | Use when | How |
|---|---|---|
| `ChildElementStrategy` (**default**) | normal MAUI `CollectionView`/`ListView` | enumerate item-type children of the collection root |
| `IndexedIdStrategy(prefix)` | app tags rows `Task_0…` | today's probe — kept for back-compat, but no longer the default, and the cap is a named option |
| `ItemTypeStrategy(typeName)` | Windows `ListItem` / Android `ViewGroup` | filter children by control type |
| `LocatorStrategy(locator)` | app marks rows with a shared id/class | `FindElements` inside the collection root |

`ChildElementStrategy` as the default is what lets `GetItemCount()` return a true count in one
call instead of N probes — and it removes the silent truncation at 100.

### 4.7 Virtualization

`CollectionView` virtualizes: off-screen items are not in the automation tree. Today's
`ScrollToRenderItems()` (scroll down, scroll back, `Pause(100)` twice, `catch { }`) is a blunt
instrument that swallows every exception and violates the "no arbitrary sleeps" rule in
[AGENTS.md](../../AGENTS.md).

Replace with a scroll-and-observe loop:

- `ScrollToItem(index)` — scroll in steps, after each step re-run the item strategy and check
  whether `index` resolved. Stop when found, when the scroll position stops changing, or on
  timeout. Wait on **observed item state**, never a fixed sleep.
- `ItemWhere(predicate)` — same loop, evaluating the predicate against newly-materialized items.
- `GetItemCount()` on a virtualized collection reports **materialized** items. `AssertItemCount`
  documents this explicitly; use `AssertEmpty` or `WaitAnyItem` when you only care about presence.

The blanket `catch { }` blocks go away — a scroll failure should surface, not be swallowed.

### 4.8 Generated factory methods

The ~30 `protected` factories on `ContainerBase` are the same list a page needs. Emit them once
from the existing generator (the repo is already mid-migration to `.tpl.cs` / `.gen.cs` under
`Controls/Base/`, per the `convert-control` skill) into a shared partial:

- `ScopeFactories.gen.cs` — one generated partial applied to both `PageObjectBase<TSelf>` and
  `ContainerObjectBase<TParent,TSelf>`, generic over the scope type.
- Make them `public`, not `protected`, so tests can build ad-hoc controls without a subclass:
  `page.UserProfile.Label("SomeId").GetText()`.
- Extension packages (`Brinell.Maui.Extensions`, `Brinell.Maui.CommunityToolkit`) contribute
  extension methods on `IMauiScope<TScope>` instead of editing the base class — which is what the
  "moved to Brinell.Maui.Extensions" comments in the current file are groping toward.

## 5. Worked example

App side — item template uses plain repeating ids:

```xml
<CollectionView AutomationId="TaskList">
  <CollectionView.ItemTemplate>
    <DataTemplate>
      <Grid AutomationId="TaskRow">
        <CheckBox AutomationId="TaskCheckBox" />
        <Label    AutomationId="TaskNameLabel" />
        <Button   AutomationId="TaskDeleteButton" />
      </Grid>
    </DataTemplate>
  </CollectionView.ItemTemplate>
</CollectionView>
```

Test side:

```csharp
public class TaskCollection : CollectionObjectBase<TaskPage, TaskCollection, TaskItem>
{
    public TaskCollection(IMauiScope<TaskPage> scope)
        : base(scope, "TaskList",
               ItemStrategy.ByLocator(Locator.ByAutomationId("TaskRow")),
               (collection, root, index) => new TaskItem(collection, root, index)) { }

    public Label<TaskCollection> EmptyMessage => new(this, "TaskListEmpty");  // collection is a scope
}

public class TaskPage : PageObjectBase<TaskPage>
{
    public TaskPage(IMauiTestContext c) : base(c) { Tasks = new TaskCollection(this); }
    public TaskCollection Tasks { get; }
}
```

```csharp
page.Tasks
    .AssertItemCount(3)
    .Item(1)                                  // -> TaskItem, scoped to row 1
        .Name.AssertText("Write design")      // -> TaskItem
        .Done.Check()                         // -> TaskItem
        .Parent                               // -> TaskCollection
    .ItemWhere(i => i.Name.GetText() == "Ship")
        .Delete.Click()
        .Parent
    .AssertItemCount(2)
    .Parent                                   // -> TaskPage
    .AssertIdle();
```

Every search in that chain is scoped to exactly one subtree, and no id has to be unique across
the page.

## 6. Migration

**No shims, no `[Obsolete]` layer, no back-compat.** `ContainerBase` and `List<TScope,TItem>` are
replaced outright and deleted. Same rule as the generator (see
[generator-changes.md](generator-changes.md)): the old shapes are not a constraint to design
around.

This is affordable because the MAUI blast radius is small and entirely inside this repo —
every consumer was enumerated:

| Consumer | File |
|---|---|
| `ContainerBase` | [Grid.cs:10](../../srcnew/Brinell.Maui/Controls/Container/Grid.cs#L10) |
| `ContainerBase` | [ContentDialog.cs:10](../../srcnew/Brinell.Maui/Controls/Dialogs/ContentDialog.cs#L10) |
| `ContainerBase` | `testsnew/Brinell.Maui.UITests2/Containers2/` — 5 containers |
| `ContainerBase` | `testsnew/Brinell.Maui.Tests/FluentChainingTests.cs`, `Semantic/SemanticControlTestsBase.cs` |
| `List<T,T>` | [ContainerDemoPage.cs:20,80](../../testsnew/Brinell.Maui.UITests2/Pages2/ContainerDemoPage.cs#L20) |
| `List<T,T>` | `testsnew/Brinell.Maui.Tests/Semantic/SemanticControlTestsBase.cs:150` |

Two source files and a handful of test files. There are no external callers, so a shim would
carry cost for nobody.

`Brinell.Html` and `Brinell.NativeAndroid` have their *own* `ContainerBase` /
`AndroidContainerBase` types — they do not reference the MAUI one and are untouched by this work
(see 7).

### Steps

| Step | Change | Notes |
|---|---|---|
| 1 | Generator: G1 + G2 from [generator-changes.md](generator-changes.md) | prerequisite — nothing below compiles without it |
| 2 | Add `IMauiContainerObject`, `IMauiCollectionObject`, `IMauiItemContainer` | |
| 3 | Add `ScopeObjectBase<TSelf>`; reparent `PageObjectBase` onto it | carries the single `RunPoll` (G2) |
| 4 | Add `ContainerObjectBase`, `CollectionObjectBase`, `ItemContainerBase` | |
| 5 | Add `IItemStrategy` + the four implementations | `ChildElementStrategy` is the default |
| 6 | Regenerate: `tools\Scripts\CreateMaui.Bat` | all 30 templates |
| 7 | Reparent `Grid` and `ContentDialog`; collapse the `Grid<TScope>` / `Grid<TParent,TSelf>` pair | the pair only existed to work around 3.1 |
| 8 | Give `Border`/`Frame`/`ContentView`/`ScrollView`/`SwipeView`/`RefreshView` scoping forms | 4.3 |
| 9 | Re-base `CollectionView`/`ListView`/`CarouselView` on `CollectionObjectBase`; collapse the `.Basic` duplicates | |
| 10 | **Delete** `Controls/ContainerBase.cs` and `Controls/List.cs` | |
| 11 | Port `UITests2` containers, `ContainerDemoPage`, and the two `Maui.Tests` files | |
| 12 | Generate scope factories for page and container | 4.8, non-blocking |

Steps 10 and 11 swap order in practice — port the consumers, then delete, so the tree compiles
at each commit.

### Two things the port must fix, not preserve

- **`ContainerDemoPage`** currently passes the page into its item factory
  (`(scope, index) => new TaskItemContainer(this, index)`). The new factory receives the item's
  own root element; the `Task_{index}` ids and `ContainerDemoViewModel.ReindexTasks()` that
  existed to support the old lookup should go with it.
- **`ContainerScopingTests.ListItems_AreIndependentlyScoped`** is `[Fact(Skip = ...)]`. Un-skip
  it — it is a correct test that the old design could not satisfy (3.2). If it does not pass
  after the port, the port is wrong.

## 7. Cross-platform note

`IMauiContainerObject` / `IMauiCollectionObject` are MAUI-shaped, but the underlying concepts are
not. The Core-level additions worth making now, so WinForms/Wpf/Blazor/NativeAndroid can follow
the same shape later:

- `IContainerObject<TElement>` — `IElementScope<TElement>` + `TElement ContainerRoot` (generalizes
  the existing `IContainerControl<TElement>`).
- `ICollectionObject<TElement, TItem>` — item count, indexed access, item-by-predicate.
- `IItemContainer<TElement>` — a container whose root is a supplied element.

`Brinell.Html`, `Brinell.NativeAndroid`, and `Brinell.Stride` already have parallel
`Controls/Collection` and `Controls/Container` folders, so the same gaps very likely exist there.
This design deliberately does not touch them — that is a follow-up, and each platform's item
discovery differs enough (DOM query vs. `RecyclerView` vs. UIA `ListItem`) that `IItemStrategy`
is the right seam.

## 8. Resolved decisions

All four questions answered; the design above reflects them.

### 8.1 Indexer and `Item(i)` — both

`collection[i]` and `collection.Item(i)` both ship. The indexer reads well for a direct
lookup (`page.Products[2]`), `Item(i)` reads better mid-chain and matches the existing API.
The indexer delegates to `Item(i)` so there is exactly one implementation and one place that
throws.

### 8.2 `Items` is lazy — `IEnumerable<TItem>`

`Items` is `IEnumerable<TItem>`, yielding rows as the item strategy materializes them and
scrolling only when the consumer asks for more. `.First(…)` on a 500-row virtualized collection
stops at the first match instead of scrolling to the end.

Consequences to honour in the implementation:

- Add `IReadOnlyList<TItem> ToList()` for callers that genuinely want everything (and accept the
  full scroll). `Assert.Equal(n, c.Items.Count())` should be written `AssertItemCount(n)`.
- Enumerating twice re-materializes; that is correct for a live UI, but document it, because
  `Items.First()` followed by `Items.Count()` costs two passes.
- `ItemWhere` / `FindItem` are implemented on top of the lazy sequence, which is what lets them
  stop early (4.7).

### 8.3 Containers get a `WaitContentReady` hook — yes

`ContainerObjectBase` gains a virtual readiness hook alongside root existence:

```csharp
/// <summary>
/// Extra readiness beyond "root element exists". Override for containers whose content
/// loads asynchronously. Default returns true.
/// </summary>
protected virtual bool WaitContentReadyCore(int? timeoutMs = null) => true;

public bool WaitContentReady(int? timeoutMs = null);
```

`IsReady` / `WaitReady` become: parent ready → root exists → `WaitContentReadyCore`. The default
keeps today's behaviour, so nothing changes for containers that do not override it. A container
over an async-loading section overrides it to wait on a concrete signal (a spinner disappearing,
a row count becoming non-zero) — never a sleep, per AGENTS.md.

`CollectionObjectBase` overrides it to `WaitAnyItem() || IsEmpty()`, so a collection is "ready"
once it has settled either way.

### 8.4 Share a `ScopeObjectBase` — yes

`PageObjectBase<TSelf>` and `ContainerObjectBase<TParent,TSelf>` both derive from a new
`ScopeObjectBase<TSelf>`, which owns what is genuinely common:

- the `Run*` helper surface and the single `RunPoll` implementation (which G2 in
  [generator-changes.md](generator-changes.md) needs anyway — this is where it lives, so there is
  no second copy)
- ready-state plumbing (`IsReady` / `WaitReady` / `WaitContentReady`)
- logging identity (`TestName` / `PageName` / control id resolution)
- the generated control factories (4.8)

What stays split: pages own `IsLoaded` / `WaitIdle` / `BusySentinel` / `TakeScreenshot` and root
their finds at the driver; containers own `Parent` / `ContainerRoot` / `InvalidateCache` and root
their finds at an element.

This subsumes the "maybe the generated partial is enough" alternative — the factory partial alone
would not have solved the `RunPoll` duplication, and G2 makes that duplication concrete rather
than hypothetical. Adding the layer is the smaller cost.

Revised hierarchy:

```
ObjectBase
└── ScopeObjectBase<TSelf>                        Run*, ready-state, factories, logging
    ├── PageObjectBase<TSelf>                     driver-rooted, IsLoaded/WaitIdle
    └── ContainerObjectBase<TParent,TSelf>        element-rooted, Parent/ContainerRoot
        └── CollectionObjectBase<TParent,TSelf,TItem>
            (ItemContainerBase<TCollection,TSelf> derives from ContainerObjectBase)
```

Migration step 2 in 6 therefore adds `ScopeObjectBase` first; step 3's `[Obsolete]`
`ContainerBase` shim is unaffected, since it keeps deriving from `ControlBase` for source
compatibility.
