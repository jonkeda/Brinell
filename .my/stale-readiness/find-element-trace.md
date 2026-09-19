# Finding elements: trace, gaps, and what to merge

This traces `FindElement`, `TryFindElement` and `FindElements` from a control down to the
driver. It then lists what should change because of the stale-readiness plan
([plan.md](plan.md)) and its companion note ([resolve-ready-element.md](resolve-ready-element.md)),
and which methods can be merged.

Status: analysis, 2026-09-19. Nothing is changed yet. Scope: `Brinell.Maui`,
`Brinell.Maui.FlaUI`, `Brinell.Maui.Appium`, and the `Brinell.Core` interfaces they implement.
`Brinell.NativeAndroid` and `Brinell.Html` have their own versions of all of this and are left
out.

---

## 1. The layers

| Layer | Members | Waits? |
| --- | --- | --- |
| **Driver**: `IDriver<T>` (`FlaUIMauiDriver`, `AppiumMauiDriver`) | `FindElement(locator, timeoutMs = 5000)`, `FindElements(locator, timeoutMs = 0)`, `TryFindElement(locator, out element, timeoutMs = 0)` | Yes, when given a timeout |
| **Element**: `IElement<T>` (`FlaUIMauiElement`, `FlaUIDeclaredElement`, `AppiumMauiElement`) | The same three members, searching descendants | Yes, when given a timeout. `FindElement` defaults to **5000** |
| **Scope**: `IElementScope<T>` (`MauiTestContext`, `AppRoot`, `DriverRootScope`, `RootedScopeBase` = pages, containers, rows, collections) | `TryFindElement(locator)`, `FindElement(locator)`, `FindElements(locator)` | Only `MauiTestContext.FindElement` does (section 3, F1) |
| **Control**: `ViewBase` | `TryFindElement()`, `TryFindElement(ScrollLookup)`, `Resolver(ScrollLookup)`, `FindElement()` | No, but `FindElement()` sweeps the scroller on every miss (F3) |

Controls call only the scope layer, and the scope layer calls the element layer. Of the driver
layer, only `FindElements` is used outside the drivers themselves (by `MauiTestContext`).

## 2. The trace

### A control on a page or in a container: the usual case

```text
ViewBase.FindElement()
└─ scope.FindElement(locator)                    RootedScopeBase
   ├─ CanResolveElements()                       page: is it loaded? (checks, does not wait)
   └─ TryFindElement(locator) ?? throw ENF
      ├─ ContainerRoot                           cached; IsCachedRootValid (page: HasUsableBounds,
      │                                          other: TagName read); miss -> FindContainerRootElement
      │    page:      Context.FindElements(pageLocator).First(HasUsableBounds)
      │    container: parentScope.FindElement(containerLocator)      (recurses up)
      │    row:       collection.TryGetItemRoot(index)
      ├─ root.FindElement(locator, timeoutMs: 0) FlaUI FindFirstDescendant / Appium element.FindElement
      └─ on StaleElementReferenceException: InvalidateCache, re-root, try once more
└─ on ENF, if scope.AllowsScrollLookup: ScrollingElement().TryFindByScrolling(locator)
       Windows: interface default, returns null at once.  Android: UiScrollable sweep.
```

One attempt, no waiting, which is what the plan wants. The retry belongs to the `RunPoll` around it.

### A control on `AppRoot` or a `DriverRootScope`: `ToolbarButton`, menus, native alerts

```text
ViewBase.FindElement()
└─ AppRoot.FindElement -> MauiTestContext.FindElement(locator)
   ├─ loop for Timeouts.ElementFind (3000 by default and in MauiTestFixtureBase; 1000/10000 in the presets):
   │     driver.FindElements(locator); pause 100 ms
   └─ driver.AppElement.TryFindByScrolling(locator)          sweep #1
└─ on ENF: ScrollingElement() = Context.AppElement -> TryFindByScrolling   sweep #2
```

### `TryFindElement()` (absence-tolerant members)

- Page or container scope: the same path as above. `ElementNotFoundException` becomes `null`.
  Nothing else is caught except the one stale retry.
- `AppRoot` / `DriverRootScope`: `MauiTestContext.TryFindElement` is a single
  `driver.FindElements`, with `catch (Exception) { return null; }`.
- `TryFindElement(ScrollLookup.Once)` / `Resolver(Once)`: a plain lookup, and a sweep only on the
  first call of a poll.

### `FindElements`

`FindElements` is used by `RootedScopeBase` (children), `ItemStrategy` (rows), `PageObjectBase`
(page root), and a few Core methods (`DatePicker`, `TimePicker`, `ContentDialog`, `Stepper`).
Every caller passes `timeoutMs: 0` or uses the scope overload, which has no timeout. None of them
waits.

## 3. Findings

### F1. `MauiTestContext.FindElement` has its own wait, not listed in the plan

It polls for `Timeouts.ElementFind` (3 s by default) on **every call**, and that call is inside
the control's `RunPoll`. This is the same kind of problem as the plan's nested `EnsureVisible`
budget, and it sits on the path of the control that started the plan: `ToolbarButton` is declared
on `AppRoot`.

- `Click(timeoutMs: 500)` on a toolbar item that is missing takes at least 3 s.
- A 5 s outer budget gets at most two lookups for an `AppRoot` control, not about 50.
- `TryGetContainerRoot()` on a container whose parent is `AppRoot` also waits up to 3 s before it
  answers "absent", including in `IsExists()`, which is documented as "without waiting".

**Change:** make it one attempt (`TryFindElement(locator) ?? throw`) and drop the sweep. The
control layer already does the sweep (F2). Add this row to the plan's section 1 table
"Where it happens".

### F2. Controls on `AppRoot` sweep twice on Android

Sweep #1 happens in `MauiTestContext.FindElement` and sweep #2 in `ViewBase.FindElement`. Both
scroll the app element, because `AppRoot.ScrollingRoot` is null. On Windows both return null at
once. On Android each is a UiScrollable search. F1's change removes sweep #1.

### F3. `ViewBase.FindElement()` sweeps on every miss, on every tick

`Resolver(ScrollLookup.Once)` sweeps once per call, but `FindElement()`, which the generated
`Get*`, `Wait*`, `Assert*` and every action call through `RunPoll`, sweeps each time it finds
nothing. On Android a control that is missing for 5 s costs one UiScrollable search every tick.
Plan 3.1 throttles **scroll-into-view** but not this **scroll-to-find**.

**Change:** one throttle for both: sweep at most once per `Timeouts.Animation` ms for a given
locator, as in plan decision S3.

### F4. `MauiTestContext.TryFindElement` swallows every exception

`catch (Exception) { return null; }` reports a driver failure, a lost session or (after phase 1)
a `StaleElementException` as "absent". `WaitExists(false)` and `AssertExists(false)` then pass
on an error. `RootedScopeBase.TryFindElement` catches only "not found" and stale.

**Change:** catch `ElementNotFoundException` only, and let staleness surface as the phase 1
signal. Do this in phase 1.

### F5. `FindElement()` and `TryFindElement()` can disagree, so controls override both

`IndicatorView`, `DrawingView` and `Stepper` each override **both**, and in each the override is
`FindElement() => TryFindElement() ?? throw new ElementNotFoundException(...)`. They have to: a
control that overrides only `TryFindElement()` finds its element through a different path in the
optional members than in `Get*`, `Wait*`, `Assert*` and actions, because the base `FindElement()`
goes through `scope.FindElement`, not through `TryFindElement()`.

After F1, `scope.FindElement` means `scope.TryFindElement ?? throw` at every scope. So the base
can say the same:

```csharp
/// The one override point for "where is this control". FindElement is derived from it.
protected virtual IMauiElement? TryFindElement(ScrollLookup lookup) { ... }

protected IMauiElement FindElement()
    => TryFindElement(SweepPolicy()) ?? throw NotFound();

/// The message when the control is absent; a control with a special lookup says what it tried.
protected virtual ElementNotFoundException NotFound() => new(Locator);
```

The three overrides then become a `TryFindElement` override plus a `NotFound()` override that
keeps their message. `FindElement()` stops being virtual.

Keep one difference: `RootedScopeBase.FindElement` throws `CreateScopeNotReadyException` when
`CanResolveElements()` is false, where `TryFindElement` returns null. That message tells the
user the page did not load, not that the control is missing. Keep it by having the scope report
why it returned null (see F9), not by keeping two lookup paths.

### F6. Element-level finds take a timeout, and it defaults to 5 s

`IElement.FindElement(locator, timeoutMs = 5000)` makes the unsafe default the easy one to reach.
In `Brinell.Maui`, every caller passes `0`, with these exceptions:

- `Menu.OpenCore`: `element.FindElement(_triggerLocator, timeoutMs ?? DefaultTimeoutMs)`. This is
  a wait inside a Core method, tied to the menu element: another row for the plan's table.
- The drivers' shell-chrome lookups (`ChromeFindTimeoutMs = 5000`, used to open the navigation
  drawer and to light-dismiss), which are internal to the drivers.

**Change:** element-level finds make one attempt and take no timeout. `Menu.OpenCore` looks the
trigger up once, and `RunDoWithElement` already retried until the menu was ready. The chrome
lookups get a private helper in each driver. Most of the 95 mocked find setups in `testsnew/`
change mechanically: they drop the `0` argument.

### F7. FlaUI driver: `FindElement(locator, 0)` never searches

`FlaUIMauiDriver.FindElement` uses `while (elapsed < timeout)`, so with `timeoutMs: 0` the body
never runs and it throws `ElementNotFoundException` straight away. `TryFindElement` defaults to 0,
so on this driver it **always returns false**. `FlaUIMauiElement.FindElement` uses `do … while`
and is correct. Nothing calls the driver-level `FindElement(…, 0)` or `TryFindElement` today, so
the bug is latent.

**Change:** F8 makes both members go away. If they stay, switch to `do … while` and add a unit
test.

### F8. The driver layer needs only `FindElements`

Outside the drivers, the only driver-level find used is `FindElements`, by `MauiTestContext`.
`FindElement` is used only for the drivers' own chrome lookups (F6), and `TryFindElement` is not
used at all.

**Change:** `IDriver` keeps `FindElements(locator)`, with no timeout. The single-element members
become private to the drivers or are removed (no-backward-compatibility rule).

### F9. The "stale root: invalidate, re-root, retry once" block is written six times

It appears in `RootedScopeBase.TryFindElement` and `FindElements`, and in `CollectionObjectBase`
`TryGetItemRoot`, `TryGetItemRoots`, `GetItemCount` and `TryMaterializeMore`. That is 6 of the 11
catches that plan phase 1 has to switch from Selenium's type to `StaleElementException`.

**Change:** switch all six at once by making them one helper:

```csharp
/// Runs read against the root; on a stale root, re-roots once and runs it again.
/// Returns whenAbsent when there is no root.
protected T WithRoot<T>(Func<IMauiElement, T> read, T whenAbsent)
```

Phase 1 then touches one catch instead of six. The helper is also the natural place to record
"root was replaced" for the plan's "last observed" messages.

## 4. What each layer looks like after merging

| Layer | Today | After |
| --- | --- | --- |
| Driver | `FindElement(l, t)`, `FindElements(l, t)`, `TryFindElement(l, out e, t)` | `FindElements(l)` |
| Element | the same three, with timeouts | `TryFindElement(l) → IMauiElement?`, `FindElements(l)`; `FindElement(l)` as a default interface member `TryFindElement(l) ?? throw` |
| Scope | three members; the context's `FindElement` waits and sweeps | the same three; `FindElement` is always `TryFindElement ?? throw` (a scope may supply the message) |
| Control | `TryFindElement()`, `TryFindElement(lookup)`, `Resolver(lookup)`, virtual `FindElement()` | `TryFindElement(lookup)` is the only override point; `FindElement()` and `Resolver` are derived from it |

The rule behind every row: **lookups make one attempt. Waiting happens in exactly one place,
the public call's poll**, which is the plan's "one budget per public call" applied to lookups. It
also makes the element and scope layers the same shape: one nullable lookup, one list lookup, and
a throwing variant derived from the nullable one.

Combined with `TryOnElement` in [resolve-ready-element.md](resolve-ready-element.md) section 3:
if the poll resolves with `TryFindElement(lookup)`, it can record "not found" as the last
observation without catching `ElementNotFoundException` on every tick. That makes the
"found, then gone (replaced 3 times)" message cheap to build.

## 5. Where this fits in the plan

| Finding | Phase | Why there |
| --- | --- | --- |
| F4 (context swallows everything), F9 (six stale blocks become one helper) | **1. Stale signal** | Both are about how staleness is caught; F9 cuts phase 1's edit list from 11 sites to 6. |
| F1 (context wait), F3 (sweep throttle), F5 (`FindElement` derived from `TryFindElement`), `Menu.OpenCore` from F6 | **2. Single budget** | Each is a second budget, or repeated work inside the one budget. |
| F6 (element timeouts), F7 (FlaUI driver bug), F8 (driver surface) | **New phase 2b, "Lookup surface"**, after phase 2 is green | Interface changes across both drivers and about 95 mocks; mechanical, but big enough to review on its own. |
| F2 (double sweep) | Falls out of F1 | |

Phase 0 additions:

- (f) A control on `AppRoot` whose element is missing: `Click(timeoutMs: 300)` fails in under
  1 s. Today it takes at least 3 s, because of F1.
- (g) `WaitExists(false)` while `TryFindElement` throws a non-"not found" error: it must not
  pass. Today it does, because of F4.
- (h) A missing control with `AllowsScrollLookup`: at most one `TryFindByScrolling` per
  `Animation` ms across one `Get*` call. Today there is one per tick, because of F3.

## 6. Risks and open points

| Risk | Mitigation |
| --- | --- |
| Code outside a `Run*` poll relied on F1's hidden 3 s wait, for example a container on `AppRoot` reached through `ContainerObjectBase.RunDo`, which does not poll | Search for `AppRoot` and `DriverRootScope` users in `testsnew/` and `samples/`. Run the Todo suite (whose toolbar sits on `AppRoot`) and the Windows `Brinell.Maui.UITests` suite before and after phase 2, and compare against `timing-baseline.json`. |
| Sweeping less often (F3) misses a row that arrives below the fold late in a poll | The throttle still sweeps every `Animation` ms, so it never stops sweeping. Android baseline subset before and after. |
| `ElementFind` becomes an unused setting | Remove it from `TimeoutSettings` and its presets in phase 2 (no-backward-compatibility rule), and add it to the `CHANGELOG.md` entry in phase 6. `Brinell.Html.Playwright` and `NativeAndroid` still read it, so check them first: drop it there too, or keep it for them only. |
| Does `FindFirstDescendant` cost less than `FindAllDescendants` on FlaUI? That matters if the element layer's single lookup is ever built on `FindElements` | Keep `TryFindElement` as its own primitive on the element layer (as in the table in section 4). Measure alongside plan phase 0's `FindElement` cost. |
