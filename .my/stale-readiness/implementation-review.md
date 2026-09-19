# Stale readiness: implementation review

Reviewed 2026-09-19, against [plan.md](plan.md) (status "done", steps 0-9) and [design.md](design.md).
The review covers the working tree: everything after step 0 (commit `4455f79`) is uncommitted.

## Outcome (2026-09-19)

All seven findings are fixed. Each fix, with its evidence, is in [plan.md](plan.md) 2.1 under
"Review fixes", and the decisions are in plan.md section 3.

| # | Outcome |
| --- | --- |
| 1 | Fixed: a cached row element must still hold the row's item. Two pins. |
| 2 | Fixed: `SelectItem` resolves by polling, then activates once. Two pins. |
| 3 | Fixed for scrolls: every scroll gets the call's remaining budget (`CallRemainingMs`). The driver settle waits are kept on purpose, and design 5 now says so. Two pins. |
| 4 | Fixed: after retries, the failure is a `WaitTimeoutException` naming the type and count, with the exception inside. One pin. |
| 5 | Fixed at the driver: a bridge answer of "nothing", when the app has exited, is `AppUnavailableException`. The tree-walk catches in `BrinellBridgeLookup` stay, because a vanished sibling must not end a walk. Not tested against a closed app. |
| 6 | Fixed: the unused timeouts are gone, and so is `ObjectBase.Poll`. Container `GetAttribute` keeps its timeout for Core's `IControlObject`. |
| 7 | Fixed: `NearMissSettings`, and `IMauiElementScope.DescribeMiss`. One pin. |
| Hygiene | Plan sections 3 and 4 updated. The commit is still to do. |

## Verdict

Most of the plan is built as described, and the claims I could check hold up. There is one real
defect: a row's own members can read or act on a recycled element. Three smaller gaps also
matter for R0 or R2/R3. None of them is caught by the pins, because the pins exercise a
different path or use mocks that return at once.

## What was checked, and holds

| Claim | How checked | Result |
| --- | --- | --- |
| Core untouched (R9) | `git diff --stat 64a9f0a -- srcnew/Brinell.Core` | empty |
| Step 1 grep: no Core element, scope, page, driver or `ControlObjectBase` types in `srcnew/Brinell.Maui*` | grep | 0 matches |
| No Selenium type in `Brinell.Maui` | grep | only reflection strings and comments |
| Removed: `CanResolveElements`, `EnsureLoaded`, `WaitContentReadyCore`, `IsParentReady`, `WaitParentReady`, `DriverRootScope`, `WaitVisibleCore`, `RunPoll`, `doEnsureVisible`, `Until`, `TryMaterializeMore`, `HasMoreThan`, `WaitForProgressThenSettle` | grep | all gone (`Poller.Until` is the new loop) |
| `PageLoadException` no longer thrown by MAUI | grep | only a doc comment |
| Calls layer (design 6) | read `Calls/*` | `Poller` is the one loop, makes at least one attempt, ends at once on `AppUnavailableException` and configuration errors; `ControlCall` writes one log pair plus the near-miss `Warning`; `Deadline` is internal |
| `ActOnce` contract: `ElementNotReadyException` only before acting | read every throw site | holds: `Click`, `Toggle`, `RadioButton`, range and `RatingView` guards, `Stepper`, `ClickableItemBase`, and the FlaUI toolbar and menu "not performed" answers |
| `Brinell.Maui.Tests` | `dotnet test` | **168 passed, 1 skipped, 0 failed**: every pin, including the step 8 pins |
| Decision records, skills, docs | read and grep | AD-004, AD-009 and AD-010 are there; no skill or doc still names a removed member |

Not rerun: the Windows, Todo and Android UI suites. The rule is one UI test process at a time,
and each run takes minutes. Their numbers in plan section 4 are taken as reported.

## Findings

Most severe first.

### 1. A row's own members read and act on a recycled element (confirmed)

**Where:** [ItemObjectBase.cs](../../srcnew/Brinell.Maui/Containers/ItemObjectBase.cs) `IsCachedRootValid`,
and [ContainerObjectBase.cs](../../srcnew/Brinell.Maui/Containers/ContainerObjectBase.cs) `RootAttempt` / `ProbeCallReadiness`.

- The cached-root check for a row is `IsUsable`: alive, and with a size. It does **not** check
  `Key.IsHeldBy(root)`.
- The key is checked in two places only. `FindContainerRootElement` checks it, but it runs only
  after the cache has been invalidated. `ProbeContentReadiness` also checks it.
- A container's own members run `RootAttempt`, which checks `ProbeCallReadiness()`. For a
  container that is the parent chain only, so the row's own `ProbeContentReadiness` never runs.
- Result: once a row has resolved its root, a recycled element passes as the row. Affected: the
  row's own `Click`, `GetText`, `Assert*` and `Wait*` (`ClickableItemBase`, `SelectableItemBase`,
  `TabItem`), and the container `WaitExists` / `AssertVisible` on rows.
- Not affected: controls inside the row (`row.Name.GetText()`). They go through
  `ScopeGate.Check(row)`, which is the full `ProbeReadiness`. That is the only path the
  `Row_FindsItsItemAgain_WhenItsElementIsRecycled` pin covers.

**Proof:** a throwaway unit test (since deleted) used a `ClickableItemBase` row. The test called
`row.GetText()` once, recycled the element to item 7, and moved item 1 to a new element.
`row.GetText()` then returned `"Item 7"` where `"Item 1 (moved)"` was expected. This breaks R0
("never fall back to another element (another row)") and design 9 ("It would never press another
item's delete button").

**Fix:** `IsCachedRootValid(root) => IsUsable(root) && Key.IsHeldBy(root)`. A recycled root is then
found again by key. Add a pin for a row's own member, both a read and a `Click`.

### 2. `SelectItem` can activate twice

**Where:** [CollectionObjectBase.cs](../../srcnew/Brinell.Maui/Containers/CollectionObjectBase.cs) `SelectItem`, `ActivateItemCore`, `TryActivate`.

The activation runs inside the poll. That is intended for a `false` answer ("nothing activated").
However, exceptions also reach the poll:

- A `StaleElementException` thrown after `Select()` has taken effect is recorded as `Stale` by
  `RootAttempt`, and the next attempt activates again.
- Any other exception is recorded as `Failed` by `Poller`, and the next attempt activates again.
- On Windows, `TryActivate` turns an `InvalidOperationException` into `false` and moves on to the
  next candidate row.

This departs from the `ActOnce` contract the plan adopted in step 8: "any other exception still
ends the call at once". A double tap can deselect a row or navigate twice.

**Fix:** use the `ActOnce` shape. Poll to resolve the row, then activate. Retry only on `false`,
and let any exception end the call.

### 3. Waits with their own budget are still below the call (R2, R3)

- `IMauiElement.ScrollIntoView(int timeoutMs = 5000)` is still called without a budget:
  - `CollectionObjectBase.TryScrollItemIntoView`, inside `ScrollToItem`, the materializing steps
    and `ItemWhere`;
  - `ScrollHelper.ScrollIntoView`;
  - `ViewBase.ScrollIntoViewCore`, used by the `VisibleAfterScroll` trio and by `ScrollIntoView()`.

  On Android each call can loop for up to 5 s. So the D1 pin ("`ScrollToItem` honours the
  budget") holds on mocks but not on a device. Step 3 found this exact problem and fixed it for
  `EnsureVisible` only.
- Driver-level `WaitHelper.WaitFor` loops keep fixed budgets:
  - Appium `SetRangeValue`: 1000 ms;
  - Appium flyout: the chrome timeout;
  - Appium `WaitUntilPositionSettles`;
  - FlaUI `OpenDropdown` and its close: 2000 ms each.

  These are act-then-confirm steps inside the driver, which may be acceptable. But design 4.1
  says "`WaitHelper`: no longer used by MAUI", and that is not true. Either pass the remaining
  budget down, or write the exception into the design.

### 4. The failure message for an unexpected exception is not built

Design 2.1 and 6.1 say a timed-out call whose last attempt threw "names its type and how many
attempts raised it". `ObservationLog.ToException` instead returns `last.Error` unchanged (its
`_` branch). `RunProbe` and `ObjectBase.Poll` also rethrow it unchanged. The failure is still
reported correctly, but the promised count is missing, and the plan does not record this as a
departure.

### 5. Catch-alls remain on the FlaUI bridge path

Step 3 says the catch-alls were removed. These remain:

- `BrinellBridgeLookup`: 6 × `catch (Exception)` → `null`.
- `FlaUIDeclaredElement.DeclaresStateReads`: `catch` → `false`.

Since step 8, "nothing on the bridge answered" becomes an `ElementNotReadyException`, and the call
retries it. So an app that exits during a toolbar or menu call can wait out the whole budget as
"not ready", instead of failing at once with `AppUnavailableException` (the R0 table). I found
this by reading the code and did not reproduce it. Letting `FlaUIErrors.IsElementGone` through
in these catches, as step 3 did elsewhere, would close the gap.

### 6. Leftover timeout parameters (R1, R7)

- `RootedScopeBase.IsExists(int? timeoutMs)` still polls when given a timeout. It does that
  outside any call unit: no log pair and no scope check.
- These ignore their timeout: `IsVisible(int?)`, `GetAttribute(name, timeoutMs)`,
  `GetItemCount(int?)` (whose documentation says "not used"), `IsEmpty(int?)` and
  `TrySelectItem(index, timeoutMs)`.
- `IMauiContainerObject.GetItemCount(int?)` keeps the parameter on the interface.

Under "no backward compatibility", remove them.

### 7. Small departures from the design, not recorded

- The near-miss thresholds are `const`s on `ControlCall`, but design X5 calls them settings.
- `IMauiElementScope` has no `DescribeMiss`. Instead, `ViewBase.NotFound()` makes a second real
  lookup through `scope.FindElement` when the call fails.

## Plan and document hygiene

- **Step 9's "Done when: Docs merged" is not met.** Nothing after step 0 is committed: 96 files
  are modified, plus the new `Calls/`, `Exceptions/`, `Scopes/` and test files. The untracked
  `.my/ClaudeSwitcher/` and `tools/ClaudeSwitcher/` are unrelated, so keep them out of that
  commit.
- **Section 3's "Still open" is stale.** Step 7 settled Q9. For X5, step 8 found 1 near-miss in
  4,833 log lines, which says the thresholds are not noisy. Record both decisions.
- **Section 4 states the Android baseline three ways:**
  - "Range / Picker 6/16 fail / 0/8 pass";
  - "Range 6 passed / 16 failed" (step 8);
  - "Range 6/22 passed" (the step 8 row).

  Use one form.
- **The step 8 evidence is sound but thin.** "Framework-race failures down" rests on 5 clean
  Todo runs without the override, against about 1 failure in 2 runs before. That is about a 3%
  chance at the old rate, so it is convincing but not conclusive. More runs would firm it up.

## Suggested order

1. Fix finding 1 and add the pin. It is a small change, and the failure is exactly the one the
   design exists to prevent.
2. Fix finding 2 (`SelectItem` on the `ActOnce` shape).
3. Fix finding 3's `ScrollIntoView` budget. Decide about the driver-level waits, and write the
   decision into the design.
4. Fix findings 4-7, update plan sections 3 and 4, then commit, which closes step 9.
