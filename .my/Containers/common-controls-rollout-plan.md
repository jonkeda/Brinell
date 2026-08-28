# Plan: roll out all common container and collection controls

**Status:** Phases 0 and 1 complete and verified; Phases 2–6 not started
**Scope:** `Brinell.Maui` only — containers, collections, sample pages, UI tests
**Builds on:** [container-and-collection-design.md](container-and-collection-design.md) §6 steps 7–12,
which this plan expands into ordered, individually shippable work.

---

## 1. What is already done

The framework layer exists and is proven against the real Windows app:

- `ContainerObjectBase`, `CollectionObjectBase`, `ItemContainerBase`, `ItemStrategy`
  (`srcnew/Brinell.Maui/Containers/`)
- The generator resolves the fluent return type per class (`TScope` vs `TSelf`)
- One worked example end to end: `GridCollectionDemoView` + `ProductCollection` +
  `ProductRow`, with 23 passing UI tests

What is missing is **breadth**. Exactly one container control (`Grid`) and one
collection control have been exercised. Every other container in
`Controls/Container/` is still a bare `ControlBase<TScope>` that cannot scope its
children, and the three collection controls still derive from the doomed
`List<TScope,TItem>`.

## 2. Inventory and target state

### 2.1 Container controls — `srcnew/Brinell.Maui/Controls/Container/`

| Control | Today | Target | Why |
|---|---|---|---|
| `Grid` | `Grid<TScope>` **and** `Grid<TParent,TSelf>` on `ContainerBase` | single `Grid<TParent,TSelf>` on `ContainerObjectBase` | the pair only existed to work around the old fluent-return defect |
| `Border` | `ControlBase<TScope>` | + scoping form | very common wrapper |
| `Frame` | `ControlBase<TScope>` | + scoping form | legacy but still widespread |
| `ContentView` | `ControlBase<TScope>` | + scoping form | the base of every custom view |
| `ScrollView` | `ScrollableControlBase<TScope>` | + scoping form, **keeps** scroll members | scoping must not lose `ScrollableControlBase` |
| `SwipeView` | `ControlBase<TScope>` | + scoping form | items are the interesting part |
| `RefreshView` | `ControlBase<TScope>` | + scoping form | wraps a scrollable child |
| `BoxView` | `ControlBase<TScope>` | **unchanged** | has no children by definition |
| `IsoPaneView` | `ControlBase<TScope>` | **unchanged** — confirm | project-specific; check before touching |

Not in `Container/` but container-shaped, and worth adding because MAUI layouts
are what people actually nest:

| New control | Base | Note |
|---|---|---|
| `VerticalStackLayout` | `ContainerObjectBase` | |
| `HorizontalStackLayout` | `ContainerObjectBase` | |
| `StackLayout` | `ContainerObjectBase` | legacy |
| `FlexLayout` | `ContainerObjectBase` | |
| `AbsoluteLayout` | `ContainerObjectBase` | |

> **Windows caveat — MEASURED in Phase 0, see section 3.** Bare MAUI layouts do not
> expose `AutomationId` to UI Automation; they have no `AutomationPeer`. But this is
> fixable per base type with one handler registration, and the sample app already
> does it for `Layout` and `ContentView`. With those registered, **every layout in
> both tables above is addressable and usable as a scope** — no `AutomationContainer`
> wrapping required.
>
> The cost moves from the app author (wrap every container) to the app's
> `MauiProgram` (register three handlers). See the revised 6.4 for who should own
> those registrations.
>
> Two rows above are now known-bad and are struck from the work:
> **`Frame`** (deprecated in MAUI 10, no handler to hook, superseded by `Border`) and
> **`SwipeView`/`RefreshView`** (WinUI controls whose peers must not be overridden —
> doing so breaks the whole UIA tree).

### 2.2 Dialogs

| Control | Today | Target |
|---|---|---|
| `ContentDialog` | `ContainerBase<TParent, ContentDialog<TParent>>` | `ContainerObjectBase` |

### 2.3 Collection controls — `srcnew/Brinell.Maui/Controls/Collection/`

| Control | Today | Target |
|---|---|---|
| `CollectionView` | `CollectionView<TScope,TItem> : List<>` + `CollectionView<TScope> : ScrollableControlBase` (`.Basic.cs`) | one `CollectionView<TParent,TSelf,TItem>` on `CollectionObjectBase`; delete `.Basic` |
| `ListView` | `List<>` | `CollectionObjectBase` |
| `CarouselView` | `List<>` + `.Basic` | `CollectionObjectBase`; delete `.Basic` |
| `IndicatorView` | `ControlBase<TScope>` | keep, add `GetCount` / `GetSelectedIndex` |
| `TableView` | `ControlBase<TScope>` | `CollectionObjectBase` with a section/cell item model — **see 6.2** |
| `Picker` (`Selection/`) | `SelectorControlBase` | **unchanged** — its items are not element-scoped |

`CollectionView`'s existing extras (`GetSelectionMode`, `IsMultiSelectEnabled`)
must survive the re-base; they are not on `CollectionObjectBase` today.

### 2.4 Deletions

- `srcnew/Brinell.Maui/Controls/ContainerBase.cs` (480 lines)
- `srcnew/Brinell.Maui/Controls/List.cs` (407 lines)
- `Collection/CollectionView.Basic.cs`, `Collection/CarouselView.Basic.cs`

Consumers to port first (the tree must compile at every commit):

- `testsnew/Brinell.Maui.Tests/FluentChainingTests.cs` — done in Phase 1
- `testsnew/Brinell.Maui.Tests/Semantic/SemanticControlTestsBase.cs` — done in Phase 1

> `Brinell.Maui.UITests2` and `Brinell.Samples.Maui.App2` **were deleted** after Phase 1.
> They were a stale parallel copy: never in the solution, unable to navigate (App2's
> shell had no Containers tab and no AutomationIds), with only 14 of ~250 `Tests2`
> discovered and 12 of those failing. Their container consumers are gone with them.

## 3. Ordering

Each phase compiles, ships, and is independently reviewable.

### Phase 0 — Settle the Windows automation question — **DONE**

**Result: 10 of 13 addressable, and the reason is not what this plan assumed.**

Implemented as `AutomationProbeView` / `AutomationProbePage` in the sample app plus
`AutomationProbeTests` in the UI test project, reachable from a "Probe" tab. Run:

```
dotnet test testsnew/Brinell.Maui.UITests --filter "FullyQualifiedName~AutomationProbeTests"
```

#### Measured result

| Layout | Root addressable | Usable as scope |
|---|---|---|
| `AutomationContainer` (control group) | yes | yes |
| `Grid` | yes | yes |
| `VerticalStackLayout` | yes | yes |
| `HorizontalStackLayout` | yes | yes |
| `StackLayout` | yes | yes |
| `FlexLayout` | yes | yes |
| `AbsoluteLayout` | yes | yes |
| `ContentView` | yes | yes |
| `ScrollView` | yes | yes |
| `Border` | yes* | yes* |
| `Frame` | **NO** | **NO** |
| `SwipeView` | **NO** | **NO** |
| `RefreshView` | **NO** | **NO** |

\* `Border` required a new handler added during this phase — see below.

Notably, **"root addressable" and "usable as scope" never diverged**. No layout was
findable but unusable as a scope. The `rootOnly` bucket was empty in every run, so
the plan can treat "addressable" as a single property.

#### The finding that changes the plan

The positives are **not** stock MAUI behaviour. They are produced by handlers the
sample app registers in `MauiProgram.cs`:

```csharp
handlers.AddHandler<ContentView, AutomationContentViewHandler>();
handlers.AddHandler<Layout, AutomationLayoutHandler>();
```

`AddHandler<Layout, …>` is registered against the **base** `Layout` type, so it
covers every layout subclass at once — `Grid`, all three stack layouts, `FlexLayout`,
`AbsoluteLayout`. Each returns a platform panel whose `OnCreateAutomationPeer`
supplies a peer that stock MAUI does not provide.

This reframes the question the phase was asked to settle. It is not
"which layouts does MAUI expose?" — the answer to that is essentially *none*. It is
**"which layouts can be made to expose their AutomationId, and at what cost?"** The
answer: any layout, cheaply, via one handler registration per base type.

So the third bullet of the original gate ("mixed → build the addressable ones") is
the outcome, but for a better reason than expected: the boundary is not a fixed
platform limit, it is a function of which handlers an app registers.

#### `Border`: fixed during this phase

`Border` initially failed. It maps to `ContentPanel` on Windows — the same platform
view `ContentView` uses — but MAUI registers `BorderHandler` separately, so the
`ContentView` registration missed it. Adding

```csharp
handlers.AddHandler<Border, AutomationBorderHandler>();
```

(`Platforms/Windows/Handlers/AutomationBorderHandler.cs`, 4 lines of body) moved
`Border` to fully addressable. Re-probed and confirmed.

#### `SwipeView` / `RefreshView`: a real negative, and a trap

These map to the WinUI `SwipeControl` and `RefreshContainer`, which already have
their own automation peers. Overriding `OnCreateAutomationPeer` on them **broke the
entire UIA tree**: with those handlers registered, all 13 probe subjects went
unaddressable, including the `AutomationContainer` control group. The app still
launched and rendered; it simply exposed nothing to automation.

This was caught only because the probe has a control group. A probe that asserted
one layout at a time would have reported "SwipeView still fails" and hidden a
catastrophic regression in everything else.

The attempted handlers are kept, **unregistered**, in
`Platforms/Windows/Handlers/AutomationRemainingHandlers.cs` with a header explaining
why they must not be registered. If `SwipeView`/`RefreshView` scoping is ever needed,
the route is to wrap their *content* in an `AutomationContainer`, not to touch the
WinUI control's peer.

`Frame` has no `FrameHandler` in MAUI 10 (it is deprecated in favour of `Border`).
Not pursued.

#### Consequences for the rest of this plan

1. **§2.1 layout controls are all viable.** `Grid`, `VerticalStackLayout`,
   `HorizontalStackLayout`, `StackLayout`, `FlexLayout`, `AbsoluteLayout`,
   `ContentView`, `ScrollView`, and `Border` can all back a container object.
   Phase 2 proceeds as written.

2. **`Frame` should be dropped from §2.1.** It is deprecated in MAUI, has no
   handler to hook, and `Border` is its replacement — which now works. Giving
   `Frame` a scoping form would ship a container that cannot resolve.

3. **`SwipeView` and `RefreshView` must not get scoping forms** in Phase 1 step 3.
   Their roots are unaddressable and the obvious fix is actively harmful. Leave them
   as plain `ControlBase<TScope>`. This removes 2 of the 6 controls from that step.

4. **§6.4 is now the central open question, and its stakes are higher than
   written.** The container objects only work in apps that register these handlers.
   A consumer using stock MAUI gets containers that never resolve — with no
   diagnostic beyond `ElementNotFoundException`. The handler registrations, not just
   `AutomationContainer`, are the thing that may need to ship from `Brinell.Maui`.
   See the revised 6.4.

5. **Phase 0's probe page should be kept**, not deleted. It is the regression test
   for any future handler change, and it cost nothing to keep. It lives behind its
   own tab and shares no state with other pages.

#### Verification

- `Brinell.Maui.UITests`: **114 passed / 31 failed / 2 skipped**
- Baseline on a clean tree (probe stashed): **111 passed / 31 failed / 2 skipped**

The 31 failures are pre-existing and unrelated (DatePicker, TimePicker, Image,
ProgressBar, Stepper, Switch). Verified by stashing all Phase 0 changes, rebuilding,
and re-running. Phase 0 added 3 passing tests and **zero** regressions.

> Note: this 31-failure baseline in `Brinell.Maui.UITests` is separate from, and
> additional to, the 8 pre-existing `Brinell.Maui.Tests` unit-test failures §5
> already records. Both predate this work.

### Phase 1 — Container base migration — **DONE**

Design §6 steps 7 and 8, minus the new layouts.

| Step | Status |
|---|---|
| 1. Reparent `ContentDialog` to `ContainerObjectBase` | done |
| 2. Collapse the `Grid` pair | done |
| 3. Scoping forms for `Border`, `ContentView` | done |
| 4. `ScrollView` per 6.1(b) | done |
| 5. Port the consumers | done |
| 6. Delete `ContainerBase.cs` | done — 480 lines |
| 7. Un-skip `ListItems_AreIndependentlyScoped` | **blocked, see below** |

#### What was built

- **`ScrollHelper`** (`srcnew/Brinell.Maui/Containers/ScrollHelper.cs`) — 6.1(b) realised.
  Static UIA-first primitives over `IMauiElement` (`TryScrollIntoView`,
  `TrySwipeForward`, `TrySwipeBack`), each swallowing
  `WindowsInteractionPolicyException` and reporting progress rather than throwing.
  `CollectionObjectBase` was refactored onto it, so this removed duplication rather than
  adding a third copy. That refactor also fixed a latent inconsistency: `ScrollToTop`
  ignored `ScrollTarget` while `TryMaterializeMore` honoured it — both now agree.
- **One-parameter convenience forms.** Each container ships as
  `X<TParent, TSelf>` plus a sealed `X<TParent> : X<TParent, X<TParent>>`. This keeps
  the old `Grid<TScope>` ergonomics for the common "just scope this element" case
  without the broken semantics that made the original pair necessary.
- **`WaitForItems(minimumCount)`** added to `CollectionObjectBase`. The old `List<>` had
  it and the new base had only exact-count and any-item waits; "at least N" is the right
  shape under virtualization, where an exact match may never occur.
- Every new container's XML docs name the Windows handler requirement and point at
  `Brinell.Maui.AppSupport` (6.4).

#### Consumers ported

`FluentChainingTests.TestContainer`, `SemanticControlTestsBase` (now a real
`TestCollection`/`TestListItem` pair on the new bases), the five `UITests2` containers,
and `ContainerDemoPage` (`List<>` → new `TaskCollection`/`TaskRow`).

`TypedListControlTests` was rewritten rather than mechanically ported: it asserted the
old page-wide indexed lookup, which no longer exists. It now verifies rows resolve
*through the collection root* and adds a guard that the page is never asked for rows.
Both pass.

> The `UITests2` half of this porting was **discarded**: that project and `App2` were
> deleted immediately afterwards (see 2.4). Only the `Brinell.Maui.Tests` ports survive,
> and they are what `ContainerBase`'s deletion actually depended on.

#### Step 7 is moot — the test was deleted with its project

The plan said: un-skip `ListItems_AreIndependentlyScoped`, and "if it does not pass
after the port, the port is wrong."

It did not pass, but not because the port was wrong. The whole `UITests2` fixture could
not navigate: `App2/AppShell.xaml` declared seven tabs, none of them Containers, none
carrying an AutomationId, so all 9 tests in `ContainerScopingTests` failed in the
**constructor** before any test body ran. Measured by stashing all Phase 1 changes and
re-running: 8 failed / 1 skipped both before and after — exact parity.

`UITests2` and `App2` have since been deleted, so the test is gone. The behaviour it was
meant to prove — rows with repeating ids scoped independently — **is** covered, by
`ProductCollectionTests.Rows_WithRepeatingIds_AreIndependentlyScoped` in the live project,
which passes.

Worth recording, because it explains why that test could never have worked: App2's item
template gave each row a bare `Border` root with **no AutomationId at all**, while the old
`List<>` looked rows up page-wide by `Task_{index}`. Those ids existed only in
`ContainerDemoViewModel.ReindexTasks()`, which maintained them for nothing. The rebuilt
collection pages in Phase 5 must not repeat that: give item templates repeating ids and
let scoping separate the rows.

#### Verification

- `Brinell.Maui.UITests`: **114 passed / 31 failed / 2 skipped**
- Baseline on a clean tree (probe stashed): **111 passed / 31 failed / 2 skipped**

The 31 failures are pre-existing and unrelated (DatePicker, TimePicker, Image,
ProgressBar, Stepper, Switch). Verified by stashing all Phase 0 changes, rebuilding,
and re-running. Phase 0 added 3 passing tests and **zero** regressions.

> Note: this 31-failure baseline in `Brinell.Maui.UITests` is separate from, and
> additional to, the 8 pre-existing `Brinell.Maui.Tests` unit-test failures §5
> already records. Both predate this work.

### Phase 1 — Container base migration — **DONE**

Design §6 steps 7 and 8, minus the new layouts.

| Step | Status |
|---|---|
| 1. Reparent `ContentDialog` to `ContainerObjectBase` | done |
| 2. Collapse the `Grid` pair | done |
| 3. Scoping forms for `Border`, `ContentView` | done |
| 4. `ScrollView` per 6.1(b) | done |
| 5. Port the consumers | done |
| 6. Delete `ContainerBase.cs` | done — 480 lines |
| 7. Un-skip `ListItems_AreIndependentlyScoped` | **blocked, see below** |

#### What was built

- **`ScrollHelper`** (`srcnew/Brinell.Maui/Containers/ScrollHelper.cs`) — 6.1(b) realised.
  Static UIA-first primitives over `IMauiElement` (`TryScrollIntoView`,
  `TrySwipeForward`, `TrySwipeBack`), each swallowing
  `WindowsInteractionPolicyException` and reporting progress rather than throwing.
  `CollectionObjectBase` was refactored onto it, so this removed duplication rather than
  adding a third copy. That refactor also fixed a latent inconsistency: `ScrollToTop`
  ignored `ScrollTarget` while `TryMaterializeMore` honoured it — both now agree.
- **One-parameter convenience forms.** Each container ships as
  `X<TParent, TSelf>` plus a sealed `X<TParent> : X<TParent, X<TParent>>`. This keeps
  the old `Grid<TScope>` ergonomics for the common "just scope this element" case
  without the broken semantics that made the original pair necessary.
- **`WaitForItems(minimumCount)`** added to `CollectionObjectBase`. The old `List<>` had
  it and the new base had only exact-count and any-item waits; "at least N" is the right
  shape under virtualization, where an exact match may never occur.
- Every new container's XML docs name the Windows handler requirement and point at
  `Brinell.Maui.AppSupport` (6.4).

#### Consumers ported

`FluentChainingTests.TestContainer`, `SemanticControlTestsBase` (now a real
`TestCollection`/`TestListItem` pair on the new bases), the five `UITests2` containers,
and `ContainerDemoPage` (`List<>` → new `TaskCollection`/`TaskRow`).

`TypedListControlTests` was rewritten rather than mechanically ported: it asserted the
old page-wide indexed lookup, which no longer exists. It now verifies rows resolve
*through the collection root* and adds a guard that the page is never asked for rows.
Both pass.

> The `UITests2` half of this porting was **discarded**: that project and `App2` were
> deleted immediately afterwards (see 2.4). Only the `Brinell.Maui.Tests` ports survive,
> and they are what `ContainerBase`'s deletion actually depended on.

#### Step 7 is blocked — and the blocker is not this work

The plan said: un-skip `ListItems_AreIndependentlyScoped`, and "if it does not pass
after the port, the port is wrong." It does not pass, but not for that reason.

**The whole `UITests2` fixture cannot navigate.** `MauiFixture.NavigateToContainerDemo`
clicks a `ContainersTab` that does not exist: `Brinell.Samples.Maui.App2/AppShell.xaml`
declares seven tabs, none of them Containers, none carrying an AutomationId. All 9 tests
in `ContainerScopingTests` fail in the **constructor**, before any test body runs.

Measured, by stashing all Phase 1 changes and re-running:

| | Failed | Skipped |
|---|---|---|
| Baseline (HEAD, no Phase 1) | 8 | 1 |
| After Phase 1 | 8 | 1 |

Exact parity. The test is left skipped with a rewritten reason recording that the
*original* skip cause — the sample's `Task_{index}` naming — is genuinely fixed, and
naming the real blocker. The original reason was itself accurate about a real defect:
the item template emits no row id at all, so the old `List<>` lookup could never have
worked, and `ContainerDemoViewModel.ReindexTasks()` maintained ids nothing consumed.

Un-skipping requires giving App2 a reachable Containers tab. That is app work, outside
this plan's scope, and worth doing before Phase 5 leans on `UITests2`.

#### Verification

| Suite | Result |
|---|---|
| `dotnet build Brinell.sln` | succeeded |
| UI tier 2 (Container + Collection) | **26 passed, 2 skipped, 0 failed** |
| `Brinell.Maui.Tests` | 62 passed, 8 failed, 1 skipped |
| `UITests2` `ContainerScopingTests` | 8 failed, 1 skipped — parity with baseline; project since deleted |

The 8 `Brinell.Maui.Tests` failures are the known pre-existing set (`ViewBase` `RunPoll`,
including `Enter_WithNullText`). Passing count rose 61 → 62 from the new scoping test.

#### Carried forward

`Controls/List.cs` (407 lines) is **not** deleted. Three types still derive from it:
`CollectionView`, `ListView`, and `CarouselView` — all in `Brinell.Maui`. It goes in
Phase 3, which re-bases them. Step 6's intent, removing the dead container base, is met.

> A fourth consumer, `Brinell.Maui.Extensions/Controls/Collection/PaginatedList.cs`, was
> missed in the original survey because it lives in a different project. It has since been
> **deleted** — see [uncovered-areas-plan.md](uncovered-areas-plan.md) Phase A — so Phase 3
> has three consumers to re-base, not four.

### Phase 2 — Layout controls

Whatever Phase 0 permits: `VerticalStackLayout`, `HorizontalStackLayout`,
`StackLayout`, `FlexLayout`, `AbsoluteLayout`.

These are near-identical thin subclasses. Write one by hand, confirm it, then
replicate — but do **not** introduce a code generator for them; five files of
eight lines each is not worth a template.

### Phase 3 — Collection migration

Design §6 step 9.

1. Re-base `CollectionView` on `CollectionObjectBase`, preserving
   `GetSelectionMode` / `IsMultiSelectEnabled`.
2. Re-base `ListView` and `CarouselView`.
3. Delete the `.Basic` files and `List.cs`. Its only remaining consumers are the three
   controls above; `PaginatedList` was removed in
   [uncovered-areas-plan.md](uncovered-areas-plan.md) Phase A.
4. `CarouselView` needs a `CurrentItem` / `GetPosition` concept that
   `CollectionObjectBase` does not have — see 6.3.

### Phase 4 — `TableView` (separable; defer if time-boxed)

Two-level model, unlike every other collection. See 6.2. This is the one item
worth cutting if the rollout needs to land sooner.

### Phase 5 — Samples and UI tests

Detailed in section 4.

### Phase 6 — Scope factories (§6 step 12, non-blocking)

Generator emits `Grid<TSelf> Grid(string id)` helpers on page and container so
call sites lose the `new X<Y>(this, "id")` ceremony. Pure ergonomics; ship last,
or not at all this round.

## 4. Samples and UI tests

> **Phase 5 now builds everything fresh.** The plan originally expected to port
> `UITests2`'s container tests as a starting point. That project and `App2` were deleted
> after Phase 1, so there is nothing to port — which is the better outcome: the App2
> markup carried real defects (item rows with no AutomationId, a `ReindexTasks()`
> maintaining ids nothing consumed) that a port would have inherited.
>
> Deleting them did lose coverage **areas** with no live equivalent: `CarouselView`,
> `ListView`, `TableView`, `PaginatedList`, `DataGrid`, plus Media and Navigation. Those
> tests were not running — only 14 of ~250 were even discovered, 12 of those failing — so
> nothing working was lost, but the areas are now genuinely uncovered until Phases 3–5
> rebuild them. `TableView` and the collection controls are already in scope below;
> Media, Navigation, and `DataGrid` are **not**, and need their own decision.

### 4.1 Sample pages

Follow the established convention exactly, as `GridCollectionDemoView` does:
files under `Views/` and `Pages/`, namespace `...Views2.TestViews`, `x:DataType`
on the view and on every `DataTemplate`, a `ShellContent` tab in `AppShell.xaml`
with an `AutomationId`, two `MauiXaml Update` entries in the `.csproj`.

| Page | Tab id | Demonstrates |
|---|---|---|
| `LayoutContainersPage` | `LayoutContainersTab` | every layout from 2.1, nested three deep, repeating child ids at each level |
| `NestedContainersPage` | `NestedContainersTab` | container-in-container-in-collection; the no-parent-fallback boundary |
| `CollectionControlsPage` | `CollectionControlsTab` | `ListView`, `CarouselView`, `IndicatorView` side by side |
| `TableViewPage` | `TableViewTab` | sections and cells (Phase 4) |
| `DialogPage` | `DialogTab` | `ContentDialog` open/scope/close |

Each page **must** carry the two affordances that made `GridCollectionDemoView`
testable, because their absence is what makes UI tests flaky:

- a **Reset** button restoring known state, wired into fixture navigation
- a **count / state label** giving tests a logical value to wait on rather than
  polling realized elements

And each **must** use repeating, non-unique `AutomationId`s in item templates
with no reindexing. Unique per-row ids would make the tests pass without testing
anything — scoping is the entire point.

### 4.2 UI tests

`testsnew/Brinell.Maui.UITests/`, mirroring the existing layout:
`Containers/` for the container objects, `Tests/Container/` and
`Tests/Collection/` for the tests, one page object per sample page in `Pages/`,
navigation helpers on `MauiFixture`.

Per container control, at minimum:

1. the container root resolves
2. a child resolves **through** the container
3. a child of the *same id* in a sibling container does **not** leak
4. the container's own inherited members return `TSelf`, not `TParent`
5. a child action returns the container
6. nesting: container → container → control resolves, and cannot escape upward

Per collection control:

7. `Item(i)`, `this[i]`, `TryItem(i)` agree
8. out-of-range: `TryItem` → `null`, `Item` → `ElementNotFoundException`
9. rows with repeating ids are independently scoped
10. mutation (add/delete) keeps scoping correct with no reindexing
11. `FindItem` / `ItemWhere` search by content
12. empty state

### 4.3 Unit tests

Extend `testsnew/Brinell.Maui.Tests/ContainerCollectionTests.cs` with Moq-backed
coverage for each new base — these run without the app and catch regressions the
UI suite would only find slowly.

### 4.4 Honest expectations on the UI suite

Two constraints from the last round will bite again and should be planned for,
not discovered:

- **Deep virtualized scrolling does not work** (`.my/fixes/` and
  `sample-app-ui-tests-design.md` §8.1). `ListView` and `CarouselView` tests must
  stay within the realized window, or be written against short lists. Do not
  write a test that scrolls to index 60.
- **Absence cannot be asserted fluently**
  (`.my/fixes/waitexists-absence-assertions.md`). Empty-state tests need
  `IsExists()`, not `AssertExists(false)`, until that fix lands. Landing that fix
  first would make the empty-state tests in 4.2 item 12 read properly — worth
  sequencing before Phase 5 if convenient.

## 5. Verification gates

Run at every phase boundary:

```
dotnet build Brinell.sln
dotnet test testsnew/Brinell.Generator.Tests      # expect 96/96
dotnet test testsnew/Brinell.Maui.Tests           # 8 pre-existing failures, see below
dotnet test testsnew/Brinell.Maui.UITests
```

**Known baseline:** `Brinell.Maui.Tests` has 8 pre-existing failures in
`ViewBase.tpl.cs` `RunPoll`, unrelated to this work. Record the count before
starting; any *increase* is a regression introduced by this plan. These should
be fixed separately — deciding that is still pending.

After any change under `Controls/Base/*.tpl.cs`, run
`Tools/Scripts/CreateMaui.bat` and review the `.gen.cs` diff. Phases 1–4 should
produce **no** generated diff; if they do, something changed that was not
intended to.

## 6. Design questions — **ALL RESOLVED**

Answered by the user. Each subsection records the decision; nothing here blocks.

### 6.1 `ScrollView`, and multiple inheritance we cannot have

`ScrollView` needs both `ScrollableControlBase` (scroll members) and
`ContainerObjectBase` (child scoping). C# gives one base class.

Options: (a) duplicate the scroll members onto the container form; (b) extract
`IScrollable` + an extension-method or delegated helper; (c) `ContainerObjectBase`
grows optional scroll members. Same question applies to `RefreshView`.

**DECIDED: (b)** — extract `IScrollable` plus a shared delegated helper.

`CollectionObjectBase` already needs scrolling and already has the `ScrollTarget`
hook from the last round, so one helper serves the container form, the collection
base, and `ScrollView` alike. Note that Phase 0 struck `RefreshView` from the
scoping work, so (b) now has one fewer consumer than when the question was posed.

### 6.2 `TableView` two-level model

`TableView` is sections containing cells. `CollectionObjectBase` assumes one flat
item level.

**DECIDED: as recommended.** Model the section as an `ItemContainerBase` that is
itself a `CollectionObjectBase` of cells.

**Verify the generic constraints compose before building on it** — the
self-referencing CRTP constraints may not permit a type that is both. If they do
not, `TableView` gets a bespoke base and stays out of the common hierarchy. Phase 4
is separable precisely so this can fail without blocking anything else.

### 6.3 `CarouselView` position

Needs `CurrentItem`, `GetPosition`, `ScrollTo(position)` — a notion of "current"
that `CollectionObjectBase` lacks.

**DECIDED: as recommended.** `CurrentItem`, `GetPosition`, and `ScrollTo(position)`
go on `CarouselView` itself, not on `CollectionObjectBase`. Only `CarouselView` and
`IndicatorView` have a current position, and `IndicatorView` is not a collection
object.

### 6.4 Who owns the automation handlers — **RESOLVED**

**Phase 0 raised the stakes here.** Brinell's container objects only resolve in an
app whose `MauiProgram` registers automation handlers:

```csharp
handlers.AddHandler<ContentView, AutomationContentViewHandler>();
handlers.AddHandler<Layout, AutomationLayoutHandler>();
handlers.AddHandler<Border, AutomationBorderHandler>();
```

Without them, every container object in this rollout fails to resolve on Windows,
and the only diagnostic is `ElementNotFoundException` — indistinguishable from a
wrong AutomationId.

**DECIDED.** These are **always added manually** to the system under test. Sometimes
that means referencing `Brinell.Maui.AppSupport`; sometimes it means copying the
source straight into the app. Both are first-class, expected paths.

This is a deliberate rejection of the "one supported package reference" framing in
the earlier recommendation. The app under test is not always a project Brinell can
add a dependency to — it may be a third-party or legacy app where dropping in a few
source files is the only option. The handlers are therefore designed to be
**copy-friendly**: self-contained, dependency-free beyond MAUI itself, and
namespaced so a copy does not collide.

**IMPLEMENTED** alongside Phase 0:

- `samples/Brinell.Maui.AppSupport/` — new project, `Microsoft.Maui.Controls` as its
  only dependency. Contains the two peers, two panels, three handlers, a
  `README.md` covering both routes, and one entry point:
  `handlers.AddBrinellAutomationHandlers()`.
- `samples/Brinell.Samples.Maui.App` now references it and calls that one line. The
  six duplicated handler/peer/panel files were deleted from the sample.
- Re-probed after the move: **identical results** (10 addressable, same 3 not), so
  the extraction is behaviour-preserving.

**Constraints to keep:**

1. AppSupport must stay copy-portable — no dependency on `Brinell.Core`,
   `Brinell.Maui`, or anything else in the repo. A single Brinell reference breaks
   the copy route silently. This is asserted in the `.csproj` comment and the README.
2. Every layout control object's XML docs must state the requirement and name both
   routes. A container that silently never resolves is worse than one that does not
   exist.
3. The sample keeps its own `AutomationContainer`; AppSupport supplies the handlers
   that make *ordinary* layouts addressable, which is the broader need.

### 6.5 `IsoPaneView`

Unknown provenance; not a standard MAUI control.

**DECIDED: leave it alone** until someone confirms what it is.

## 7. Sequencing recommendation

Phase 0 first and on its own — it is cheap and it changes the design.

Then Phases 1 → 3 → 5, treating 2 and 4 as optional. Rationale: Phase 1 removes
`ContainerBase` (real debt), Phase 3 removes `List.cs` (more debt), and Phase 5
proves both. Phase 2's layout controls are additive breadth with no debt payoff,
and Phase 4 is a genuinely different data model. If the rollout has to be cut
short, cutting 2 and 4 leaves a coherent result; cutting 1 or 3 does not.

Landing `.my/fixes/waitexists-absence-assertions.md` before Phase 5 would let the
empty-state tests be written properly rather than written twice.

## 8. Effort

Rough, assuming the framework layer stays as-is:

| Phase | Size | Note |
|---|---|---|
| 0 — automation probe | ~~half a day~~ **done** | 3 tests, 0 regressions; changed the shape of 2.1 and 6.4 |
| 1 — container migration | ~~2–3 days~~ **done** | `ContainerBase` deleted; `List.cs` deferred to Phase 3; step 7 moot (UITests2 deleted) |
| 2 — layout controls | 1 day | thin, repetitive; all 5 confirmed viable by Phase 0 |
| 3 — collection migration | 2–3 days | 6.3 is the unknown |
| 4 — `TableView` | 2 days | separable; may prove not to fit |
| 5 — samples + UI tests | 4–5 days | the largest, and the one that finds the real bugs |
| 6 — scope factories | 1–2 days | ergonomics only |

The estimate for Phase 5 is deliberately the largest. In the last round the UI
tests, not the framework, surfaced every genuine defect — stale elements,
scroll policy, virtualization limits, absence assertions. Expect the same.
