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

### 3.1 `ContainerBase` derives from `ControlBase` — the fluent return type is wrong

```csharp
public abstract class ContainerBase<TParent, TSelf> : ControlBase<TParent>, IMauiContainer<TParent, TSelf>
```

`ControlBase<TParent>` means every inherited action returns `TParent`, not `TSelf`. So:

```csharp
page.UserProfile.Click()      // returns ContainerDemoPage — jumps out of the container
     .NameEntry               // compile error: page has no NameEntry
```

A container's own actions eject you from the container. You must re-enter via `page.UserProfile`
every time. That defeats the point of a container being a scope you work *inside*.

### 3.2 `List<TScope,TItem>` items are not scoped to the item — they are scoped to the page

This is the most serious defect. In `List.Item(int)`:

```csharp
public TItem Item(int index)
{
    var scope = ContainingScope as IMauiScope<TScope> ...;
    return _itemFactory(scope, index);   // scope = the LIST'S parent, not the list, not the item
}
```

And the demo page confirms the consequence — the factory ignores its own `scope` parameter:

```csharp
TaskList = new List<ContainerDemoPage, TaskItemContainer>(
    this, "TaskListBorder", "Task_",
    (scope, index) => new TaskItemContainer(this, index));   // 'this' = the page
```

So `TaskItemContainer` searches the **whole page** and is only saved from collisions by every
item carrying a globally unique `AutomationId` (`Task_0`, `Task_1`, …). The container's promise —
"searching for controls happens inside this" — does not hold for list items.

The knock-on effects:

- `GetItemCount()` probes `Task_0, Task_1, …` against the **containing scope**, with a hard
  `maxItems = 100` safety limit, so it cannot count a list it is not scoped to and silently
  truncates at 100.
- Item template controls must have per-item-unique ids. A plain `<Label AutomationId="TaskName"/>`
  repeated per row — the normal MAUI item template — cannot be addressed at all.
- `TItem` is constrained only to `class`, so nothing guarantees an item is even a container.
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

`ObjectBase`
`├── PageObjectBase<TSelf>`          (unchanged)
`├── ContainerObjectBase<TParent,TSelf>`   **new** — not a ControlBase
`└── CollectionObjectBase<TParent,TSelf,TItem>` **new** — extends ContainerObjectBase

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
    public IReadOnlyList<TItem> Items { get; }
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

The existing types stay and keep working; new work moves to the new bases.

| Step | Change | Breaks callers? |
|---|---|---|
| 1 | Add `IMauiContainerObject`, `IMauiCollectionObject`, `IMauiItemContainer` | no |
| 2 | Add `ContainerObjectBase`, `CollectionObjectBase`, `ItemContainerBase` | no |
| 3 | `ContainerBase<TParent,TSelf>` → `[Obsolete]` shim over `ContainerObjectBase` | no (warning) |
| 4 | Add `IItemStrategy` + implementations; `List<TScope,TItem>` delegates to them | no |
| 5 | Give `Border`/`Frame`/`ContentView`/`ScrollView`/`SwipeView`/`RefreshView` scoping forms | no (additive) |
| 6 | Collections re-based on `CollectionObjectBase`; keep `List<…>` as `[Obsolete]` shim | no (warning) |
| 7 | Generate scope factories for both page and container | no |
| 8 | Port `UITests2` containers to the new bases as the reference example | test-only |
| 9 | Drop obsolete shims at the next major version, per [BREAKING-CHANGES-POLICY.md](../../BREAKING-CHANGES-POLICY.md) | yes, gated |

Step 3 note: `ContainerBase` currently returns `TParent` from inherited control actions; the shim
must keep that to stay source-compatible, so it is a genuinely separate type from
`ContainerObjectBase`, not a subclass.

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

## 8. Open questions

1. **Indexer vs. `Item(i)`** — the design offers both. If only one, `Item(i)` reads better in
   fluent chains and matches the existing API.
2. **`Items` eager list** — on a virtualized collection this materializes everything by scrolling.
   Should it be `IEnumerable<TItem>` with lazy scrolling instead, so `.First(…)` stops early?
3. **`ContainerObjectBase` and `IsLoaded`** — pages have `IsLoaded`/`WaitIdle`/`BusySentinel`.
   Should containers get a `WaitContentReady` hook for containers that load async?
4. **Should `PageObjectBase` and `ContainerObjectBase` share a `ScopeObjectBase`?** It would remove
   real duplication (ready-state, factories, logging identity) but adds a layer; the generated
   factory partial may be enough on its own.
