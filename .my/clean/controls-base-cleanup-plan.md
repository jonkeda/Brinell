# Plan: clean up the base classes in `srcnew/Brinell.Maui/Controls`

**Status:** COMPLETE. All 8 steps done; the `Controls` root holds no loose files.
**Rule being applied:** every base class must either live in `Controls/Base`, be moved
there, or be deleted if unused.

---

## 1. The shape of the problem

`Controls/Base` **is** the hierarchy. Everything in it is generated (`.tpl.cs` +
`.gen.cs`) and rooted at `ViewBase`. The loose files in the `Controls` root are the **old**
hierarchy, rooted at `ControlBase`, and they are superseded rather than parallel:

| Old (root, hand-written) | Superseded by |
|---|---|
| `ControlBase<TScope>` | `Base/ViewBase<TScope>` |
| `ClickableControlBase<TScope>` | `Base/ClickableControlBase<TScope>` |
| `FocusableControlBase<TScope>` | `Base/FocusableControlBase<TScope>` |

The last two share a **name** with their replacements, differing only by namespace. Both
compile, and which one a control gets depends on a `using`. That is the single most
confusing thing in this folder and the main reason to finish the migration.

Both hierarchies descend from `Brinell.Core.Abstractions.ControlObjectBase<TScope>`, so
the migration is a reparent, not a rewrite.

### Current contents

| File | Lines | Kind | Real consumers |
|---|---|---|---|
| `ControlBase.cs` | 724 | superseded base | 23 in-project + 3 Extensions |
| `ClickableControlBase.cs` | — | superseded base | 23 |
| `FocusableControlBase.cs` | — | superseded base | **1** (the root `ClickableControlBase`) |
| `SwipeableControlBase.cs` | 174 | **capability** | 1 (`SwipeView`) |
| `RefreshableControlBase.cs` | 152 | **capability** | 1 (`RefreshView`) |
| `ExpandableControlBase.cs` | 193 | **capability** | 1 (`Extensions/Expander`) |
| `ScrollableControlBase.cs` | — | capability | 2 (both `.Basic` files) |
| `Control.cs` | — | abstraction | **0 — dead** |
| `List.cs` | 407 | collection base | 3 (owned by rollout Phase 3) |
| `ElementClicker.cs` | — | static helper | 10 files |
| `ElementSearch.cs` | — | static helper | 8 files |

23 controls still sit on the old hierarchy: all 7 Shapes, 4 Media, 5 Container
(`BoxView`, `Frame`, `IsoPaneView`, `RefreshView`, `SwipeView`), 4 Collection
(`IndicatorView`, `TableView`, and the two `.Basic` files), `GraphicsView`, plus
`Control.cs` and `List.cs`.

## 2. Disposition

| File | Action |
|---|---|
| `Control.cs` | **DELETE** — zero consumers |
| `ControlBase.cs` | **DELETE** after migrating its 26 consumers to `Base/ViewBase` |
| `ClickableControlBase.cs` | **DELETE** after migrating its 23 consumers to `Base/ClickableControlBase` |
| `FocusableControlBase.cs` | **DELETE** — falls out once the root `ClickableControlBase` goes |
| `ScrollableControlBase.cs` | **DELETE** after rollout Phase 3 removes the `.Basic` files |
| `List.cs` | **DELETE** after rollout Phase 3 |
| `SwipeableControlBase.cs` | **CONVERT to a capability** — see §4 |
| `RefreshableControlBase.cs` | **CONVERT to a capability** — see §4 |
| `ExpandableControlBase.cs` | **CONVERT to a capability** — see §4 |
| `ElementClicker.cs` | **MOVE** to `Controls/Internal` |
| `ElementSearch.cs` | **MOVE** to `Controls/Internal` |

Nothing is *moved into* `/Base` as-is. The superseded bases are deleted; the capabilities
become something other than a base class.

## 3. Migrating off `ControlBase` / `ClickableControlBase`

The bulk of the work: 23 controls, 724 lines of superseded base.

**These are not drop-in equivalents.** `ViewBase` gets its public members from the
generator; `ControlBase` hand-writes them, and the two have already drifted — the
`[AbsenceTolerant]` fix had to patch `ControlBase`'s hand-written `Visible` and `Exists`
regions separately from the generated ones (see
[`.my/fixes/waitexists-absence-assertions.md`](../fixes/waitexists-absence-assertions.md) §8).

Migrate one control group per commit:

| Order | Group | Controls | Existing tests |
|---|---|---|---|
| 1 | Shapes | 7 | **none** |
| 2 | Graphics | 1 | none |
| 3 | Media | 4 | none |
| 4 | Container leftovers | `BoxView`, `Frame`, `IsoPaneView` | none (unaddressable on Windows) |
| 5 | Collection leftovers | `IndicatorView`, `TableView` | none |
| 6 | Extensions | `EditableField`, `GenericBrowser`, `SelectionList` | 15 unit tests |

Shapes first: 7 controls, no `*Core` overrides of their own, so the generator produces
everything. But note the column — **groups 1–5 have no tests at all**. Their migration is
unverifiable beyond "it compiles". Either write existence tests first or state plainly
that the migration is unverified; do not let it look covered.

Group 6 is last precisely because it is the only one with real test coverage: if something
subtle breaks in the reparent, that is where it shows.

When the last consumer is gone, delete both root bases. The name collision disappears with
them.

## 4. The capability problem — and why inheritance cannot solve it

`Swipeable`, `Refreshable`, and `Expandable` are not hierarchy levels. Each adds one
orthogonal ability:

| Base | Adds |
|---|---|
| `SwipeableControlBase` | `SwipeLeft/Right/Up/Down`, `Swipe(x,y,x,y)` |
| `RefreshableControlBase` | `PullToRefresh`, `IsRefreshing`, `WaitRefreshing`, `AssertRefreshing` |
| `ExpandableControlBase` | `Expand`, `Collapse`, `ToggleExpanded`, `IsExpanded`, `WaitExpanded`, `AssertExpanded` |

C# gives one base class, so a control that is both scrollable and swipeable — a plausible
`CollectionView` on mobile — cannot inherit both. Moving them into `/Base` does not fix
that; it relocates the problem. **They need a different mechanism.**

### The precedent already in the codebase

This exact problem was solved once, for scrolling. `ScrollView` needed both container
scoping and scroll behaviour; the answer was
`Containers/ScrollHelper` — static methods over `IMauiElement` that the container
delegates to. `CollectionObjectBase` and `ScrollView` both use it, and neither inherits
from the other.

### Recommended: default interface methods

The interfaces already exist — `ISwipeableControlObject<TScope>`,
`IRefreshableControlObject<TScope>`, `IExpandableControlObject<TScope>` — and the project
is `net10.0` with `LangVersion latest`, so default interface implementations are available.

Move each base's body into its interface as default methods, over a small protected
surface the control already has:

```csharp
public interface ISwipeableControlObject<TScope> : IControlObject<TScope>
{
    TScope SwipeLeft(int? timeoutMs = null) => SwipeCore(Direction.Left, timeoutMs);
    // ... the other four, all defaulted
}
```

A control then declares the capabilities it has:

```csharp
public class SwipeView<TScope> : ViewBase<TScope>, ISwipeableControlObject<TScope>
public class RefreshView<TScope> : ViewBase<TScope>, IRefreshableControlObject<TScope>
```

Multiple capabilities compose freely, which inheritance cannot do.

**The catch, stated plainly:** default interface methods are not virtual in the usual way.
A control cannot `override` them — it must re-declare, and callers holding the concrete
type get the class member while callers holding the interface get the default. If the
`*Core` methods need per-control customisation, this is the wrong mechanism.

Check that before committing: `SwipeCore`, `PullToRefreshCore`, `IsRefreshingCore`,
`ExpandCore`, `CollapseCore`, and `IsExpandedCore` are all `protected virtual` today.
**Whether anything actually overrides them decides this question**, and it is a single
grep — do it first.

### Fallback: static helpers, as `ScrollHelper` does

If the `*Core` methods do need overriding, use the shape that already works here:
`Controls/Internal/SwipeHelper`, `RefreshHelper`, `ExpandHelper` — static methods over
`IMauiElement`. The control keeps a thin member that delegates:

```csharp
public TScope SwipeLeft(int? timeoutMs = null)
    => RunDoWithElement(e => SwipeHelper.SwipeLeft(e), timeoutMs);
```

More boilerplate per control, but it keeps virtual dispatch and matches existing practice.

**Decide between these two after the grep.** Both remove the base classes; they differ
only in how much per-control code remains.

### DECIDED: static helpers

The grep returned **nothing** — no control overrides any of the twelve `*Core` methods. That
alone would have allowed default interface methods, but a second check settled it against
them: every public member wraps `RunDoWithElement`, which is **`protected`** on `ViewBase`.
An interface cannot reach a protected member, so a default implementation cannot be written
at all.

The `*Core` bodies, by contrast, are pure `IMauiElement` geometry — rect arithmetic and one
`element.Swipe(...)` call. That is exactly the shape `ScrollHelper` already has.

**Taking the fallback: static helpers in `Controls/Internal`.**

## 5. Ordering

| Step | Work | Risk | Verify |
|---|---|---|---|
| 1 | Delete `Control.cs` | none | solution builds |
| 2 | Move helpers to `Controls/Internal` | low — namespace only | 18 referencing files |
| 3 | Grep the `*Core` overrides; pick the §4 mechanism | none | — |
| 4 | Convert the three capabilities | medium | `Expander` unit tests |
| 5 | Migrate groups 1–5 off `ControlBase` | **high** | compiles only — see §3 |
| 6 | Migrate group 6 (Extensions) | high | 15 unit tests |
| 7 | Delete `ControlBase.cs`, `ClickableControlBase.cs`, `FocusableControlBase.cs` | — | full suite |
| 8 | After rollout Phase 3: delete `List.cs`, `ScrollableControlBase.cs` | low | full suite |

Steps 1–3 are independent and can land immediately. Step 3 is free and decides step 4.

## Implementation record

Steps 1–7 are complete. The `Controls` root went from **11 loose files to 3**, and both
duplicate type names are gone.

### What was done

| Step | Result |
|---|---|
| 1. Delete `Control.cs` | done — zero consumers, confirmed by sweep |
| 2. Move helpers to `Controls/Internal` | done — namespace change plus one global using per project |
| 3. Grep the `*Core` overrides | done — **nothing overrides any of the twelve** |
| 4. Convert the three capabilities | done — `GestureHelper`, `ExpandHelper` |
| 5. Migrate 17 controls off `ControlBase` | done — Shapes (7), Media (4), Container (3), Collection (2), Graphics (1) |
| 6. Migrate 6 Extensions controls | done |
| 7. Delete `ClickableControlBase`, `FocusableControlBase` | done |
| 8. Delete `ControlBase`, `List`, `ScrollableControlBase` | done — see below |

### The mechanism decision, settled by measurement

The grep in step 3 returned nothing — no control overrides any `*Core` method, which would
have allowed default interface methods. A second check ruled them out anyway: every public
member wraps `RunDoWithElement`, which is **`protected`** on `ViewBase`, and an interface
cannot reach a protected member.

The `*Core` bodies are pure `IMauiElement` geometry, so the `ScrollHelper` shape fit
exactly. Two helpers now live in `Controls/Internal`:

- **`GestureHelper`** — swipe directions, relative swipe, pull-to-refresh, refreshing state
- **`ExpandHelper`** — expand, collapse, toggle, expanded state

`SwipeView`, `RefreshView`, and `Expander` declare their capability interfaces and delegate.
A control can now hold several capabilities, which inheritance could not express.

### Two pieces of real drift found

The plan predicted `ViewBase` and `ControlBase` had diverged. They had, in two places:

1. **`Run<TValue,TResult>`** existed only on `ControlBase`. It is generic logging
   infrastructure with no element involvement, so it was **ported to `ViewBase`** — four
   Extensions controls depend on it.
2. **`GetText`** existed only on `ControlBase`. It is a *control* concern rather than a base
   one — Label and the input controls supply it — so it was **not** ported. `Link` now reads
   `element.Text` directly through `RunGetWithElement`.

A third consumer surfaced that the original survey missed: `TabViewControl` in
**`Brinell.Maui.CommunityToolkit`**, a project outside the greps. It needed the reparent
plus one call-site fix, because `ViewBase.RunWaitWithElement` takes the expectation as its
first argument where `ControlBase`'s did not.

### Verification

| Check | Result |
|---|---|
| `dotnet build Brinell.sln` | succeeded |
| `Brinell.Maui.Tests` | 62 passed, 8 failed — **identical to baseline** |
| UI: Navigation + Collection + Grid + probes | **47 passed, 2 skipped, 0 failed** |

The 8 unit failures are the known pre-existing `ViewBase`/`RunPoll` set, unchanged
throughout.

**Honest caveat on coverage.** Groups 1–5 of step 5 — Shapes, Media, Graphics, and the
leftover Container and Collection controls, 17 controls in total — have **no tests at all**.
Their migration is verified only by compilation. The Extensions group was migrated last
precisely because its 15 unit tests are the only real check, and they pass.

Likewise, `SwipeView` and `RefreshView` are unaddressable on Windows, so the capability
conversion could not be verified on this platform. The logic was preserved verbatim rather
than simplified, for that reason.

### Step 8: the collection re-base, and why it was smaller than planned

Step 8 needed rollout Phase 3 first — `List.cs` and `ScrollableControlBase.cs` were the last
two things holding `ControlBase` alive, and their consumers were the three collection
controls plus the two `.Basic` files.

**A survey changed the shape of the work.** `CollectionView`, `ListView`, and `CarouselView`
had **no consumers anywhere** — not in `Brinell.Maui`, its Extensions, the samples, or the
tests. The rollout plan had budgeted 2–3 days for a careful re-base of live types; in fact
nothing depended on them, so they could be rewritten outright.

All three were rewritten as abstract bases over `CollectionObjectBase<TParent, TSelf, TItem>`.
They are abstract because that base is self-referencing: a consumer subclasses to bind its
own type, exactly as `ProductCollection` already does. The members the rollout plan required
to survive were carried across:

- `CollectionView` — `GetSelectionMode`, `IsMultiSelectEnabled`
- `CarouselView` — `GetPosition`, `IsLoopEnabled`, `GetCurrentItem`, `SwipeNext`,
  `SwipePrevious`, `WaitPosition`, `AssertPosition`

`CarouselView`'s swipes now delegate to `GestureHelper` — the helper built in step 4, reused
rather than reimplemented. Position stays on `CarouselView` rather than moving to
`CollectionObjectBase`: only a carousel has a "current" item, and putting it on the shared
base would give every collection a member most cannot honour. That was decision 6.3 of the
rollout plan, now implemented.

Five files were then deleted together: `CollectionView.Basic.cs`, `CarouselView.Basic.cs`,
`List.cs` (407 lines), `ScrollableControlBase.cs`, and `ControlBase.cs` (724 lines).

### Final state

```
Controls/
├── Base/        the hierarchy — .tpl.cs + .gen.cs pairs, plus Run ported onto ViewBase
├── Internal/    ElementClicker, ElementSearch, GestureHelper, ExpandHelper
└── Buttons/ Collection/ Container/ DateTimes/ Dialogs/ Display/ Graphics/
    Media/ Navigation/ Range/ Selection/ Shapes/ Text/ Toggle/
```

**No loose files in the `Controls` root.** One hierarchy, no duplicate type names,
capabilities composed rather than inherited.

### Verification after step 8

| Check | Result |
|---|---|
| `dotnet build Brinell.sln` | succeeded |
| `Brinell.Generator.Tests` | 104 passed |
| `Brinell.Maui.Tests` | 62 passed, 8 failed — **baseline unchanged** |
| UI: Navigation + Collection + Grid + probes | **47 passed, 2 skipped, 0 failed** |

The rewritten collection controls have **no tests**, because they had no consumers to test
through. That is unchanged from before — they were untested as `List<>` subclasses too — but
it means the rewrite is verified only by compilation. The first consumer to subclass one of
them is where it will actually be exercised.

## 6. A note on the mobile phase

`SwipeView` and `RefreshView` are unaddressable on Windows — measured, and unfixable
(overriding their WinUI automation peers collapses the whole UIA tree). Their capability
code has therefore never run in a passing test.

That is an argument for care, not for deletion: swipe and pull-to-refresh are mobile
gestures, and the app is planned for Android and iOS, where these become testable. The
conversion in §4 should preserve behaviour exactly rather than simplify on the assumption
that nothing depends on it. **Nothing here can be verified on Windows** — that is the risk,
and it should be stated in the commit rather than discovered later.

## 7. Out of scope

- `Containers/` — `ContainerObjectBase`, `CollectionObjectBase`, `ItemContainerBase`, and
  `ScrollHelper` are correctly placed already.
- Other platforms' `ControlBase` — `Brinell.Html`, `Wpf`, `WinForms`, `Stride`, and
  `Blazor` each have their own unrelated class. A naive grep for "ControlBase" returns 52
  consumers; only 26 are in scope.
- `ObjectBase.cs` at the project root — shared by pages and containers, not just controls.

## 8. End state

```
Controls/
├── Base/                      the hierarchy: .tpl.cs + .gen.cs pairs only
│   ├── ViewBase
│   ├── FocusableControlBase
│   ├── ClickableControlBase
│   ├── RangeControlBase
│   ├── SelectorControlBase
│   └── ToggleControlBase
├── Internal/                  static helpers, not base classes
│   ├── ElementClicker
│   ├── ElementSearch
│   └── (SwipeHelper, RefreshHelper, ExpandHelper — only if §4 takes the fallback)
├── Buttons/  Collection/  Container/  DateTimes/  Dialogs/
├── Display/  Graphics/  Media/  Navigation/  Range/
└── Selection/  Shapes/  Text/  Toggle/
```

No loose files in the `Controls` root, one hierarchy, no duplicate type names, and
capabilities composable rather than inherited.
