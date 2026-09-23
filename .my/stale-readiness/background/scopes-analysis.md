# Scopes, pages, containers and collections: how they fit together

> **Background.** Sections 1-5 describe the code on 2026-09-19. **The section 6 proposals were
> accepted on 2026-09-19** (plan.md section 3), and are built into [../design.md](../design.md)
> section 7. What changed since:
>
> - Its "Where the answers go in the plan" table is replaced by plan.md steps.
> - The `ItemObjectBase` rename is decided.
> - "Proposal" in section 6 should now be read as "accepted".

This analysis looks at the parts a control depends on: the test context, the two root scopes,
pages, containers, rows and collections. For each it covers what it does, how the parts combine
at runtime, and where they disagree. The earlier documents
([original-plan.md](original-plan.md), [resolve-ready-element.md](resolve-ready-element.md),
[find-element-trace.md](find-element-trace.md), [controls-design-v1.md](controls-design-v1.md)) looked mainly at
controls. This one looks at the ground they stand on.

Status: analysis, 2026-09-19. Sections 1-5 describe the code as it is. Section 6 holds proposed
answers to the open decisions, for review. None of them is decided yet.

Sources read:

- `Context/MauiTestContext.cs`
- `Pages/PageObjectBase.cs`
- `Pages/AppRoot.cs`
- `Scopes/DriverRootScope.cs`
- `Interfaces/IMauiScope.cs` and `IMauiElementScope.cs`
- `Containers/*` (`RootedScopeBase` / `ContainerObjectBase`, `ComponentObjectBase`,
  `ItemContainerBase`, `CollectionObjectBase`, `ItemStrategy`, `ElementMatch`, `ScrollHelper`)
- the containers that override scope behaviour: `Popup`, `ContentDialog`, `MediaElement`,
  `StateContainer`, `ShellFlyout`, `ScrollView`
- how the Todo and `Brinell.Maui.UITests` pages use all of these

---

## 1. The cast

Every control is built on an `IMauiScope<TScope>`. There are seven kinds of scope:

| Scope | Root element | Root cached? | `Page` | `IsReady` / `WaitReady` | Scroll lookup | Fluent `Self` |
| --- | --- | --- | --- | --- | --- | --- |
| `MauiTestContext` | none: the driver tree | - | null | `!disposed` | default (on) | the context |
| `AppRoot` | none: forwards to the context | - | null | forwards to the context | default (on) | `AppRoot` |
| `DriverRootScope<T>` | none: forwards to the context | - | null | always `true` | default (on) | the **owner's** `Self` |
| `PageObjectBase` | `Context.FindElements(AutomationId = Name)`, first with usable bounds | yes; valid while it has usable bounds | itself | `ProbeReadiness`: root plus optional busy signal | default (on) | the page |
| `ContainerObjectBase` (and `ComponentObjectBase`) | `parent.FindElement(locator)` | yes; valid while `TagName` reads | parent's | parent ready, root exists, `WaitContentReadyCore` | inherits the parent's | the container |
| `ItemContainerBase` (a row) | handed in by the collection; re-resolved **by index** when unusable | its own `_itemRoot`, **and** `ContainerRoot` | parent's | as container | **off** (rows repeat ids) | the row |
| `CollectionObjectBase` | as container; rows through `IItemStrategy` | as container | parent's | as container | inherits | the collection |

Containers that change these defaults:

| Container | Override | Why (from its own comments) |
| --- | --- | --- |
| `Popup` | root from `AppElement`, not cached, `Page => null`, parent readiness `true` | Shown over the page; on Android it takes the page out of the tree |
| `ContentDialog` | root is `AppElement.TryFindActiveDialog()`, not cached | A WinUI dialog lives outside the page |
| `MediaElement` | root falls back to its play/pause button; children searched in the **parent** | WinUI flattens the transport controls beside the root |
| `StateContainer`, `ShellFlyout` | not cached | The content is swapped or recreated |
| `ScrollView` | `ScrollingRoot` is its own root | It is the scroller for its children |

`Brinell.Maui` has no scope of its own for "a place in the app that is not a page". The Todo
tests use `AppRoot` for that: toolbar buttons, the page heading (Shell draws it in the window
chrome) and `ContentDialog`. So in the Todo suite, a large share of the controls have **no page
at all**.

## 2. How they combine at runtime

A Todo edit-page test uses every kind at once:

```text
TodoEditPage (PageObjectBase)                  Page = itself, readiness = root visible (+ busy if enabled)
├─ Entry TitleEntry            -> page scope    gated by the page
├─ AppRoot _appRoot            -> no page       gated by nothing
│   ├─ ToolbarButton SaveButton / CancelButton
│   ├─ Label Heading
│   └─ ContentDialog Dialog    -> root from AppElement, not cached
└─ (list page) CollectionView rows -> ItemContainerBase, re-resolved by index
```

When a control on the page runs one `Get*`, each poll tick does this:

| Step | Work | Where |
| --- | --- | --- |
| Page gate (each tick) | `Page.IsReady()` → `ProbeReadiness` → `TryGetContainerRoot` → cache check (`HasUsableBounds`, a `Rect` read) → if busy is enabled: find `Busy` under the root, then read its `Text` | `ViewBase.RunPoll`, line 100 |
| Lookup | `page.FindElement` → `CanResolveElements` → `IsLoaded()` | `RootedScopeBase` |
| … `IsLoaded` | default: root `HasUsableBounds` (another `Rect` read). The test pages **override** it with `StatusLabel.IsExists()`, a full control lookup, which can include an Android sweep | `PageObjectBase`, test pages |
| … child lookup | `ContainerRoot` cache check (a third `Rect` read) → `root.FindElement(locator, 0)` | `RootedScopeBase` |
| Miss | a sweep of the scroller | `ViewBase.FindElement` |
| Visible | `EnsureVisible` (the plan's nested wait) | `ViewBase` |

A control on `AppRoot` skips the page gate, but its lookup is `MauiTestContext.FindElement`,
which waits up to 3 s and sweeps (trace F1, F2).

## 3. Findings

### A. "Ready" has three meanings, and controls consult only one

| Meaning | Defined by | Used by |
| --- | --- | --- |
| **Loaded**: may children be looked up? | `PageObjectBase.IsLoaded()`, which test pages override (for example "the status label exists"); reached through `CanResolveElements` | every child lookup in a page |
| **Ready**: may a control call proceed? | `PageObjectBase.ProbeReadiness()`: root present, plus the busy signal when `BusySignalPolicy` is on | the page gate in `ViewBase.RunPoll` / `Run` and `RootedScopeBase.RunPoll` / `RunDo` |
| **Scope ready**: is this container usable? | `RootedScopeBase.IsReady` / `WaitReady`: parent ready, root exists, `WaitContentReadyCore` | only a test that calls `container.WaitReady()` directly |

What follows from this:

- **A1. Controls never consult their own scope's readiness.** The gate asks `Page`, which is
  `Scope.Page`, not `Scope.IsReady()`. A container's `WaitContentReadyCore` (documented as "wait
  for a spinner clearing, a count becoming non-zero") is never on a control's path. Nothing
  overrides it today, so this is a hook that nothing reaches, not a bug in use.
- **A2. `Popup` had to turn readiness off twice:** `Page => null` for the control gate, and
  `IsParentReady` / `WaitParentReady => true` for the scope chain. That it needed both shows
  the two mechanisms are separate.
- **A3. `ContentDialog` scoped under a page is gated by that page's busy signal.** A dialog raised
  by a running command (a delete confirmation) appears while the page may be reporting busy. Its
  buttons would then wait for "not busy", which cannot happen until the dialog is answered. The
  Todo tests avoid this by putting the dialog on `AppRoot`. *Not measured; a scenario to verify.*
- **A4. "Loaded" and "ready" can disagree.** A test page's `IsLoaded` checks a label, while
  `ProbeReadiness` checks the root. Once a page's root is visible, the gate lets a call through
  even when `IsLoaded` is false. Every lookup then fails with "page not loaded" until either the
  lookup succeeds or the outer poll runs out. That is correct, but it happens by accident: the
  page gate's error message ("did not become ready … Last readiness state") never appears,
  because the gate passed.
- **A5. The root scopes have no readiness.** `MauiTestContext` is "not disposed", `AppRoot`
  forwards to it, and `DriverRootScope` is always ready. This is fine for chrome, but it means
  nothing waits while the app is busy for any control on `AppRoot`.
- **A6. `EnsureLoaded`'s re-entrancy guard lets a page's own `IsLoaded` override use its
  controls.** While the guard is set, a lookup inside `IsLoaded` treats the page as loaded. The
  `wait: true` branch of `EnsureLoaded` has no callers.

### B. Every scope checks its cached root differently

| Scope | Cache lifetime | "Is my root still valid?" | On failure |
| --- | --- | --- | --- |
| Page | object lifetime | `HasUsableBounds` | re-find by `Name` |
| Container | object lifetime | `TagName` reads | any exception → re-find in the parent |
| Row | object lifetime | `IsUsable`: `TagName` + non-zero `Rect`, catch-all | re-find **by index** in the collection |
| `Popup`, `ContentDialog`, `StateContainer`, `ShellFlyout` | none | - | - |

- **B1.** There are three validity rules for one question. The plan's phase 4 ("does `TagName`
  still read on a removed root under FlaUI?") covers only the container rule.
- **B2. The cache lasts as long as the object that holds it, and pages declare containers both
  ways:** `=> new(this, "Id")` (a fresh object, so a fresh cache, on every access) and
  `{ get; }` (one object, one cache for the whole test). The same container can therefore be
  re-found on every call, or held across page changes, depending on how the page author wrote the
  property.
- **B3. A row re-resolved by index can come back as a different item.** After recycling or a
  reorder, index *n* may hold another item. `FindItem` already guards against this with a
  logical-index re-read ("a row the list recycled while the predicate read it"). `ItemContainerBase`
  does not.
- **B4.** `IsCachedRootValid` runs on **every** `ContainerRoot` access. A page answers it with a
  `Rect` read, and one poll tick reads the page root's bounds up to three times (section 2).

### C. The three root scopes overlap

- **C1.** `MauiTestContext`, `AppRoot` and `DriverRootScope<T>` all mean "search the whole app".
  They differ in:
  - their `Self` (the context, `AppRoot`, or the owner's `Self`, so a chain returns to the page);
  - whether they are an `ObjectBase` (`AppRoot` is);
  - `IsReady` (it forwards, or is always `true`).
- **C2.** `DriverRootScope` is used by one unit test and by no control, page or sample.
  `AppRoot` is what the docs and the Todo suite use.
- **C3.** All three inherit trace findings F1 (the 3 s wait in `FindElement`), F2 (double sweep)
  and F4 (`TryFindElement` catches everything) from `MauiTestContext`.
- **C4.** None of them sets `ScrollingRoot` or `AllowsScrollLookup`. So chrome controls (toolbar
  and dialog buttons, which are never in a scroller) still sweep the app when missing.

### D. Containers and collections outside the call model

These repeat, in more detail, the answer given before this document.

- **D1. Nested budgets.**
  - `TryMaterializeMore` → `WaitForProgressThenSettle` runs two `Poll(DefaultWait)` calls.
  - `HasMoreThan` runs a `Poll(5 × interval)`.
  - All three run inside the loops of `ScrollToItem`, `ScrollToEnd`, `FindItem` and
    `WaitForItems`.
- **D2. `timeoutMs` accepted and ignored** by `ScrollToItem`, `ScrollToTop`, `ScrollToEnd`,
  `SelectItem`, `TrySelectItem` and the container's `IsVisible(timeoutMs)`. The loop in
  `ScrollToTop` has no limit.
- **D3. Public waits on `ObjectBase.Poll`, with no call unit** (no log pair, no page gate, no
  observation):
  - on the container: `WaitExists`, `WaitVisible`, `AssertExists`, `AssertVisible`;
  - on the collection: `Item(key)`, `WaitItemCount`, `WaitAnyItem`, `WaitForItems`,
    `AssertItemCount`, `AssertEmpty`;
  - on the page: `WaitReady`, `WaitBusy`, `WaitLoaded`, `WaitTitle`.

  The same object mixes this model with the `Run*` model its generated members use.
- **D4. `ObjectBase.Poll` quirk.** With a `null` timeout, `ms < null` is false, so it makes a
  single final check. With a timeout it retries every exception silently, then lets the final
  check throw.
- **D5. Catch-alls that will hide `StaleElementException`:** `ContainerRoot`, `IsUsable`,
  `TryScrollItemIntoView` and `TryActivate`. The last two turn a replaced row into
  "Could not select item at index N".
- **D6. Waiting is inconsistent.** `Item(key)` waits for the item, while `Item(index)` and
  `SelectItem(index)` do not.
- **D7. `ContainerObjectBase.RunDo` checks page readiness but does not poll, and it has no log
  pair.** Its `RunGetWithOptionalElement` has a third copy of the page-gate message.

### E. Pages outside the call model

- **E1.** `WaitReady` and `WaitBusy` default to `PageLoad` (10 s), while every control defaults
  to `DefaultWait` (5 s). The page gate inside a control call passes the **control's** budget,
  so how long the gate waits depends on which call hit it.
- **E2.** `GetTitle` returns `Name` and ignores `timeoutMs`, so `WaitTitle` compares the page's
  class name. This is harmless, but the timeout is another one accepted and ignored.
- **E3.** `ProbeReadinessCore` has the one well-built stale path in this layer: re-acquire the
  root once and report `StaleRoot`. It is what the other scopes' stale handling could copy.
  Its catches are Selenium's type, so on FlaUI this path is currently dead (plan section 1).

## 4. What "working together" would mean

These are the properties the pieces could share. Each row names where today's code differs.
**Nothing here is decided.**

| # | Property | Today |
| --- | --- | --- |
| P1 | A control's call waits for **its scope chain**, one link at a time up to the page, not for the page alone | A1, A2, A3 |
| P2 | "Loaded" and "ready" are one definition, or one is clearly part of the other | A4, A6 |
| P3 | Root scopes say whether they can be waited on, and whether they may sweep | A5, C4 |
| P4 | One rule for "is my cached root still valid", and one reaction to staleness (re-acquire once, then report) | B1, B4, E3 |
| P5 | A root's lifetime does not depend on how the page author declared the property | B2 |
| P6 | A row knows when its index points at a different item | B3 |
| P7 | One "search the whole app" scope, or a stated reason for each | C1, C2 |
| P8 | Every public member of a scope (page, container, collection) is a call unit, with one budget, a log pair and an observation, exactly like a control's | D1-D4, D7, E1, E2 |
| P9 | No catch-all below the poll; failures are answers or signals | D5 |
| P10 | Keyed, indexed and selecting access agree on whether they wait | D6 |

## 5. Decisions to take

| # | Question | Options |
| --- | --- | --- |
| Q1 | What does a control wait for before its call proceeds? | (a) the page, as today; (b) its scope chain: each scope's `IsReady`, up to the page; (c) its nearest scope only, which asks its own parent |
| Q2 | Merge "loaded" into "ready"? | (a) keep both; (b) `IsLoaded` becomes one input to `ProbeReadiness`; (c) drop the `IsLoaded` gate on child lookups and rely on the call's poll |
| Q3 | How many "whole app" scopes? | (a) keep all three; (b) `AppRoot` only (remove `DriverRootScope`, keep the context internal); (c) `AppRoot` plus a typed variant that returns to the owner, replacing `DriverRootScope` |
| Q4 | Can chrome scopes wait on anything? | (a) no; (b) an optional app-level busy signal, as pages have |
| Q5 | Root validity rule | (a) per scope, as today; (b) one rule (live property plus `StaleElementException`; the phase 4 measurement picks the property); (c) (b), plus pages keep their bounds check as a "shown" test that is separate from "alive" |
| Q6 | Root cache lifetime | (a) the object, as today; (b) one call; (c) the object, but checked with `InstanceKey` |
| Q7 | Row identity after re-resolve | (a) by index, as today; (b) by logical index (`PositionInSet`) where the platform has one; (c) (b), plus "the item changed" as an observation that fails the call |
| Q8 | Scope and collection members in the call model | (a) leave them on `ObjectBase.Poll`; (b) move them all onto `ControlCall` / `Poller` (design section 4); (c) (b) for public members only, keeping the private helpers (`HasMoreThan`, the settle wait) as single attempts inside the caller's poll |
| Q9 | Budget for the materializing loops (`ScrollToItem` and friends) | (a) as today; (b) the caller's `timeoutMs` for the whole loop; (c) (b), with a per-step cap so one slow step cannot use it all |
| Q10 | Should `Item(index)` / `SelectItem` wait like `Item(key)`? | (a) no; (b) yes, all keyed and indexed access waits; (c) only `Try*` variants skip waiting |

Once these are decided, [controls-design-v1.md](controls-design-v1.md) gets a "Scopes" section and the plan gets its
phases.

---

## 6. Proposed answers

Status: **proposal**, 2026-09-19, for review. Each answer says what it changes and which finding
or property it closes.

### The idea behind them

**Readiness moves into the scopes, and each scope answers only for itself and its parent.** A
control call no longer checks "the page" before it starts. Each poll attempt first asks the
control's own scope whether it is ready, and that scope asks its parent in turn, up to the page
or the app root. So the readiness check becomes the first step of the same attempt that finds
the element. That gives one loop and one budget, and a failure reports the first scope in the
chain that was not ready.

```csharp
public interface IMauiElementScope : IElementScope<IMauiElement>
{
    // ...
    /// One attempt, no waiting: this scope's readiness, having asked its parent first.
    ScopeReadiness ProbeReadiness();
}

public readonly record struct ScopeReadiness(
    bool IsReady, string ScopeName, ScopeReadinessState State, string? Detail);
// State: Ready, ParentNotReady, MissingRoot, StaleRoot, NotLoaded, Busy, InvalidBusySignal
```

`PageReadinessSnapshot` becomes the page's `ScopeReadiness`. `IPageObject.ProbeReadiness`
stays, because tests and `AssertIdle` use it.

### Answers

| # | Proposal | What changes | Closes |
| --- | --- | --- | --- |
| **Q1** | **(c), which gives the effect of (b):** a control asks its **nearest scope**, and each scope asks its own parent. | The control gate calls `Scope.ProbeReadiness()` instead of `Page.WaitReady` / `Page.IsReady`, as the first step of every attempt. The separate `WaitReady` before the poll is removed, because it was a second budget in front of the first (E1). `Popup` and `ContentDialog` override one thing only, "do not ask my parent", so `Popup.Page => null` goes. `WaitContentReadyCore` becomes the container's own contribution to its probe, and is finally on a control's path. | A1, A2, A3, E1, P1 |
| **Q2** | **(b) + (c):** "loaded" becomes one input to readiness (`Ready = Loaded && !Busy`), and child lookups stop checking it. | `CanResolveElements`, `EnsureLoaded` and its re-entrancy guard are deleted. `IsLoaded()` stays as the page's override point. A page root is found only when it is shown (usable bounds), so a lookup on a page that is not showing already returns null without the gate. | A4, A6, P2 |
| **Q3** | **(b):** `AppRoot` is the only whole-app scope. | `DriverRootScope` is removed (one unit test uses it, and nothing else). `MauiTestContext` stays an `IElementScope`, because Core's `ITestContext<T>` requires it, but it is documented as "the lookup service `AppRoot` uses", not a scope to declare controls on. No backward-compatibility shim. | C1, C2, P7 |
| **Q4** | **(a) for now, with the extension point in place:** `AppRoot.ProbeReadiness()` is `Ready`, and it is virtual. `AppRoot` sets `AllowsScrollLookup => false`. | No app today has an app-level busy signal, so an app that gets one later adds it with one override. Chrome does not scroll, so chrome controls stop sweeping the app (C4). `ShellFlyout`, whose items can scroll on Android, sets its own `ScrollingRoot` and turns the lookup back on. | A5, C4, P3 |
| **Q5** | **(c):** one "alive" rule for every scope: a live read (`InstanceKey` through the driver's `Live` helper) that raises `StaleElementException`. Pages add "shown" (bounds) as a **second** check, and failing it also means re-find. | `IsCachedRootValid` becomes `IsCachedRootAlive`, the same for every scope. Pages add `IsCachedRootShown`. When Shell keeps an old page instance alive off-screen and shows a new one, the page finds the visible one. `ContainerRoot`'s catch-all becomes `catch (StaleElementException)`. The phase 4 measurement then only confirms the live read, instead of choosing between rules. | B1, B4, E3, P4 |
| **Q6** | **(a), made safe by Q5:** the root lives as long as the object, and is checked with the Q5 rule on every access. | Declaring a property as `=> new` or as `{ get; }` then changes **cost only, not behaviour**. That is P5, without paying for a root lookup on every call. If phase 0 shows the check costs more than a lookup, switch to (b). | B2, P5 |
| **Q7** | **(c):** a row records its **item key** when it is created, and re-resolves by that key. | The key is the logical index (`PositionInSet`) where the platform has one. Otherwise it is the automation id for id-based strategies (`{prefix}{index}`, already stable), and otherwise the position. When a re-resolved row carries a different key, the attempt observes "the item changed" and the call fails, rather than acting on another item. `FindItem`'s logical-index re-read becomes the shared rule. | B3, P6 |
| **Q8** | **(c):** every **public** member of a page, container or collection runs on `ControlCall` / `Poller` (design section 4). Private helpers become single attempts inside the caller's poll. | Covers `WaitExists`, `WaitVisible` and `Assert*` on containers; `Item(key)`, `WaitItemCount`, `WaitAnyItem`, `WaitForItems`, `AssertItemCount` and `AssertEmpty` on collections; `WaitReady`, `WaitBusy`, `WaitLoaded`, `WaitTitle` and `AssertIdle` on pages. `HasMoreThan` and the settle waits in `WaitForProgressThenSettle` become attempt steps that report "not settled yet". `ObjectBase.Poll` is deleted. `ContainerObjectBase.RunDo` joins the call model (D7). | D1, D3, D4, D7, E1, P8 |
| **Q9** | **(b):** one budget, the caller's `timeoutMs`, for the whole materializing loop, with no per-step cap. | With Q8 the steps are attempts, not waits, so no single step can eat the budget on its own. If the list does not settle, the observation says so. `ScrollToTop` gets a limit. **Risk:** `ScrollToItem` has no limit today, so long-list tests may need an explicit `timeoutMs`. Phase 0 lists the collection tests that run longer than 5 s before this lands. | D1, D2 |
| **Q10** | **(c):** every non-`Try` access waits, and every `Try*` answers "now". | `Item(index)`, `this[int]`, `SelectItem` and `ItemWhere` wait like `Item(key)`. `TryItem`, `TrySelectItem` and `FindItem` do not wait. This matches AD-004 and the `Get`/`TryGet` split elsewhere. `TryScrollItemIntoView` and `TryActivate` lose their catch-alls: a stale row becomes an observation, and "wrong candidate" stays an answer, but only for the exceptions the drivers document for it. | D5, D6, P9, P10 |

### The rename: `ItemContainerBase` → `ItemObjectBase`

Agreed. A row is an object in a collection. That it is also a scope is how it works, not what it
is.

- `ItemContainerBase<TCollection, TSelf>` → `ItemObjectBase<TCollection, TSelf>`, still
  deriving from `ContainerObjectBase`, because a row is a scope for its children.
- For consistency, `IMauiItemContainer<TCollection, TSelf>` → `IMauiItemObject<TCollection, TSelf>`.
- The footprint:
  - 22 code references in `srcnew/Brinell.Maui`, `testsnew`, and `samples/Todo`;
  - the `maui-control` skill (`SKILL.md`, `references/collection.md`);
  - the `maui-ui-test` skill (`SKILL.md`, `references/forbidden-apis.md`);
  - the `.my/` notes;
  - none in the generator.
- **When:** as its own commit before phase 1. It is mechanical and has no behaviour change, and
  keeping it out of the stale-readiness diffs keeps those reviewable. The Q7 item key then lands on
  `ItemObjectBase` directly.

### Where the answers go in the plan

| Plan phase | Adds |
| --- | --- |
| Before 1 | Rename (above). |
| 0 | Failing tests: a dialog on a busy page (A3), a container's content readiness reaching a child call (A1), a row that re-resolves to another item (B3), `ScrollToItem(timeoutMs: 1000)` staying within its budget (D1), and `Item(index)` waiting (D6). Record which collection tests take more than 5 s (Q9 risk). |
| 1 | Q5 (the alive rule and the `StaleElementException` catch in `ContainerRoot`), and the catch-alls from Q10. |
| 2 | Q1, Q2, Q4 (`ProbeReadiness` on scopes, control gate per attempt), and Q8 and Q9 (scope members on the call model). |
| 2b | Q3 (`DriverRootScope` removed). |
| 3 | Q7 (item key and "item changed"). |
| 6 | AD-004 gains "a call waits for its scope chain, one link at a time". The skills describe `ProbeReadiness` overrides in place of `Page => null`. |

Once these are agreed, [controls-design-v1.md](controls-design-v1.md) gets section 6b, "Scopes", with the class shapes.
