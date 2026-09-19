# Stale readiness: design

**This is the current design, and the only one.** Where a document in `background/` says
something different, this document wins. The build order and its status are in
[plan.md](plan.md). The decisions behind it are in plan.md section 3.

Status: accepted, 2026-09-19. Rewritten the same day. The rewrite makes **MAUI stand on its own
interfaces**:

- no bridges on Core's interfaces;
- no changes to Core;
- once MAUI is right, the proven shape moves down into Core (section 4.5).

Finding and decision codes point into `background/`:

| Code | Where |
| --- | --- |
| "original plan", S1-S5 | [background/original-plan.md](background/original-plan.md) |
| "rrE", S6 | [background/resolve-ready-element.md](background/resolve-ready-element.md) |
| F1-F9 | [background/find-element-trace.md](background/find-element-trace.md) |
| A-E, P1-P10, Q1-Q10 | [background/scopes-analysis.md](background/scopes-analysis.md) |
| The review | [background/review.md](background/review.md) |

---

## 1. Goal

A test's call on any MAUI object (control, page, container, collection or row) is one unit of
work. It:

- spends one budget;
- waits for the scopes it stands in, one link at a time;
- finds its element again on every attempt;
- recognizes a replaced element on every platform;
- never repeats an action;
- when it fails, says what it last saw;
- **never turns an app bug into a pass.**

## 2. Rules

| # | Rule | Replaces |
| --- | --- | --- |
| R0 | **App bugs stay failures.** The framework waits for *state*. It never waits out, retries through or swallows a *defect*. Whatever a call absorbs on its way to success is still reported (2.1) | "a flaky test" as the answer to an app bug. The Todo `CanExecuteChanged` bug was found *because* a test failed |
| R1 | **One unit per public call**: one log entry/exit pair, and one failure, around the whole call, including the action | readiness checked twice; log lines doubled; actions outside the log (original plan section 1, rrE 4.1) |
| R2 | **Only the call's poll waits.** Readiness checks, lookups, visibility, scrolling and settling are single attempts inside it | nested `EnsureVisible`; the `MauiTestContext.FindElement` loop; `Menu.OpenCore`; the collection settle polls; the `WaitReady` before the poll (F1, F6, D1, E1) |
| R3 | **Each phase has the caller's budget:** resolve with `timeoutMs`, then confirm with `timeoutMs` | S6, option B |
| R4 | **A call waits for its scope chain:** each scope reports itself and asks its parent | the page-only gate (A1-A5) |
| R5 | **A stale element is a signal** (`StaleElementException`) from both drivers. A poll re-resolves on it; a confirmation reports it and does not act again. No catch-alls | Selenium-only catches; catch-alls (original plan 3.3, D5) |
| R6 | **One check that a root is still alive**, used by every scope that caches a root | three validity rules (B1) |
| R7 | **Non-`Try` members wait; `Try*` members answer about now** | D6 |
| R8 | **The generator does not change**: same `Run*` names and signatures on both hierarchies | - |
| R9 | **MAUI owns the interfaces it changes. Core is not changed by this work.** MAUI stops implementing Core's element, driver, scope and page interfaces, and defines its own. It keeps the Core interfaces whose shape this work does not touch (4.1). The proven shape moves down to Core afterwards, as its own project (4.5) | the bridge approach, which was dropped |

### 2.1 R0 in practice: what a call may absorb, and what it must report

A call may wait for the app to *reach* a state. It must not turn a state the app *never reaches*
into a pass, and it must not hide how hard reaching the state was.

| Situation | What the call does | How the app bug stays visible |
| --- | --- | --- |
| A command never re-enables (a lost `CanExecuteChanged`) | Waits within the budget for "enabled", then fails | `ElementNotReadyException`: "found but disabled for 5000 ms", with the `Locator` and the element last seen |
| The action was **not performed**: a Core guard refused before acting, or the driver reports that nothing answered (a toolbar item the app's bridge did not answer for yet) or that the item is disabled | Resolves and asks again within the budget (`ActOnce`, step 8), because nothing happened. A Core method throws `ElementNotReadyException` only before it acts | `ElementNotReadyException` naming what was missing, when the budget runs out |
| The action ran, but its effect never shows | Never acts again; `Confirm` reports `NotConfirmed` | "Toggle ran; the state stayed Off for 5000 ms". The action ran exactly once |
| The element was replaced after the action | Never acts again; `Confirm` reports `Replaced` | "Toggle ran, and the element was replaced before its effect could be read" |
| A row now shows a different item | Never acts on it; the attempt reports `ItemChanged` | "row 3 now shows another item", on timeout |
| A page or scope stays busy | Waits within the budget, then fails | `ScopeNotReadyException` names the scope and its state |
| The app process exits, or the driver session is lost | **Fails at once**, with no retries | `AppUnavailableException`, raised by the driver |
| A busy signal is missing or not a boolean | **Fails at once** (a configuration error) | `ScopeNotReadyException` naming the signal |
| An unexpected exception inside an attempt | Retried within the budget, then reported | A `WaitTimeoutException` names its type and how many attempts raised it, with the exception inside (after a single attempt, the exception itself); it is never reported as "not found" |
| **Success after trouble**: replaced 3 or more times, or more than half the budget used | Passes | **A near-miss warning**: `LogExit(..., LogResult.Warning, ...)` with the observations. A run can list its near-misses, so a UI that keeps re-rendering, or a page that is slow to become idle, surfaces as a finding |
| Anything below the poll | No catch-alls (R5) | A failure is either an answer (an absent element, an unknown candidate) or a signal |

What the framework **never** does to make a test pass:

- repeat an action that may have run (only one reported as not performed is asked again);
- extend a budget the caller set;
- fall back to another element (another row, another window);
- treat an error as absence.

---

## 3. The layers

```text
Brinell.Core          unchanged. MAUI still uses: Locator, BrinellException and its existing subtypes,
                      ITestLogger + LogResult, TimeoutSettings, non-generic ITestContext,
                      IElementObject / IControlObject / capability interfaces, IDiagnosticDriver,
                      IScreenshotService, generator attributes
Brinell.Maui          Interfaces   IMauiElement · IMauiDriver · IMauiElementScope · IMauiScope<T>
                                   IMauiPage · IMauiTestContext · container/collection/item interfaces
                      Exceptions   StaleElementException · ElementNotReadyException
                                   ScopeNotReadyException · AppUnavailableException
                      Calls        ControlCall · Poller · Deadline · AttemptContext · Observation(Log)
                      Scopes       ScopeReadiness · AppRoot · PageObjectBase · RootedScopeBase
                                   ContainerObjectBase · CollectionObjectBase · ItemObjectBase
                      Controls     ViewBase · the Run* helpers · Confirm
                      Helpers      MauiElementExtensions
Drivers (FlaUI, Appium)            Live(...) stale mapping · InstanceKey · single-attempt finds
```

Every public member of a scope or a control follows the same path:

```text
public member
└─ ControlCall.Run(caller)                    log pair; near-miss warning; failure from the ObservationLog
   └─ Poller.Until(attempt, deadline)         the only loop
        attempt:
        1. scope.ProbeReadiness()             walks the chain upward, one probe per link
        2. locate                             one lookup; throttled sweep
        3. visible / ready                    one check; throttled scroll-into-view
        4. body                               read · predicate · compare · materialize step
        -> Observation (Done, or why not)
   └─ act once, then Confirm (actions only)   second phase with its own budget (R3)
```

---

## 4. The MAUI boundary

### 4.1 What MAUI keeps from Core, and what it leaves

| Core type | Changed by this work? | MAUI |
| --- | --- | --- |
| `Locator`, `LocatorStrategy`, `BrinellException`, `ElementNotFoundException`, `AssertionException`, `WaitTimeoutException` | no | **keeps** |
| `ITestLogger`, `LogResult` (including `Warning`), `TimeoutSettings` | no | **keeps** |
| `ITestContext` (non-generic: timeouts, logger, navigation, screenshots) | no | **keeps** |
| `IElementObject<TScope>`, `IControlObject<TScope>`, the capability interfaces (`IClickableControlObject`, `IToggleControlObject`, `IRangeControlObject` and the others) | no: they are the control-facing API. UAT discovers controls through `IControlObject<>` | **keeps** |
| `IDiagnosticDriver`, `IScreenshotService`, the generator attributes (`AbsenceTolerant`, `SkipGeneration` and the others) | no | **keeps** |
| `IElement<T>` | yes: finds without timeouts, `InstanceKey` | **leaves**, replaced by `IMauiElement` |
| `IDriver<T>` | yes: `FindElements` only | **leaves**, replaced by `IMauiDriver` |
| `IElementScope`, `IElementScope<T>` | yes: `ProbeReadiness`, page typing, no `IsReady(timeout)` | **leaves**, replaced by `IMauiElementScope` |
| `IPageObject`, `IPageObject<T>`, `PageReadinessSnapshot`, `PageReadinessState`, `BusySignalPolicy` | yes: readiness model, signatures without timeouts | **leaves**, replaced by `IMauiPage`, `ScopeReadiness` and a MAUI `BusySignalPolicy` |
| `ITestContext<T>` | yes: it is an `IElementScope<T>` | **leaves**; `IMauiTestContext` = `ITestContext` + `IMauiElementScope` |
| `IContainerControl<T>`, `IContainerObject<T>` | yes: they are `IElementScope<T>` | **leaves** |
| `ControlObjectBase<TScope>` (typed on `IElementScope`) | yes | **leaves**; `ViewBase` holds its own `Locator` and scope |
| `ElementGeometryExtensions`, `ElementScopeExtensions` (typed on `IElement<T>` / `IElementScope<T>`) | yes, by typing | **leaves**; MAUI copies in `MauiElementExtensions` |
| `WaitHelper` | no, but its loops are what R2 removes | used by MAUI only for `Pause` between attempts, and by the drivers for the settle waits below an action (section 5) |

**Rule of thumb:** MAUI keeps a Core interface when this work does not change its shape, and
leaves it when this work would. Nothing is bridged. Nothing in Core is edited.

### 4.2 The MAUI interfaces

```csharp
namespace Brinell.Maui.Interfaces;

/// A UI element. No Core element interface underneath.
public interface IMauiElement
{
    // identity
    string InstanceKey { get; }            // stable per UI element, new when replaced (5)
    string? AutomationId { get; }
    string? Name { get; }
    // state, geometry, interaction: the members IElement<IMauiElement> contributed today,
    // plus today's IMauiElement members, declared here directly (Text, Enabled, Visible, Rect,
    // Invoke, Click, SendKeys, Toggle, ScrollIntoView, TryFindByScrolling, ReadState, ...)

    // lookup: one attempt each, no timeouts
    IMauiElement? TryFindElement(Locator locator);
    IReadOnlyList<IMauiElement> FindElements(Locator locator);
    // The throwing form is the extension MauiElementExtensions.FindElement (Try ?? throw), so a
    // mocked element needs only TryFindElement set up (plan 2.1, step 1).
}

/// The driver. No Core driver interface underneath.
public interface IMauiDriver : IDiagnosticDriver, IDisposable
{
    MauiPlatform Platform { get; }
    IMauiElement AppElement { get; }
    ShellChromeLocators ShellChrome { get; }
    IReadOnlyList<IMauiElement> FindElements(Locator locator);   // one attempt, from the app root
    // navigation, windows, screenshots, ResetAppState: as today
}

/// Anything controls can be declared in.
public interface IMauiElementScope
{
    IMauiTestContext Context { get; }
    LocatorStrategy DefaultLocatorStrategy { get; }
    IMauiPage? Page { get; }                                  // for naming only, never for gating
    IMauiElement? ScrollingRoot => null;
    bool AllowsScrollLookup => true;

    ScopeReadiness ProbeReadiness();                          // one attempt (7.1)
    bool IsReady() => ProbeReadiness().IsReady;
    bool WaitReady(int? timeoutMs = null);                    // a real wait

    IMauiElement? TryFindElement(Locator locator);           // one attempt
    IReadOnlyList<IMauiElement> FindElements(Locator locator);
    IMauiElement FindElement(Locator locator) => TryFindElement(locator) ?? throw DescribeMiss(locator);
    ElementNotFoundException DescribeMiss(Locator locator) => new(locator);
}

public interface IMauiScope<TScope> : IMauiElementScope where TScope : IMauiScope<TScope>
{
    TScope Self { get; }
}

/// A page. Non-generic for Page references; generic for fluent returns.
public interface IMauiPage : IMauiElementScope
{
    string Name { get; }
    bool IsLoaded();
    bool IsBusy();
    bool WaitBusy(bool? expected, int? timeoutMs = null);
    bool WaitLoaded(bool? expected, int? timeoutMs = null);
    void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null);
    string? GetTitle();
    bool WaitTitle(string? expected, int? timeoutMs = null);
    void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);
    void AssertIdle(string? message = null, int? timeoutMs = null);
    void TakeScreenshot(string? filename = null);
}
public interface IMauiPage<TSelf> : IMauiPage, IMauiScope<TSelf> where TSelf : IMauiPage<TSelf>;

public interface IMauiTestContext : ITestContext, IMauiElementScope   // ITestContext: Core, non-generic, unchanged
{
    IMauiDriver Driver { get; }
    MauiPlatform Platform { get; }
    IMauiElement AppElement { get; }
    ShellChromeLocators ShellChrome { get; }
}

// IMauiContainer<TParent, TSelf>, IMauiContainerObject<TParent, TSelf>,
// IMauiItemObject<TCollection, TSelf> (was IMauiItemContainer), IMauiCollectionObject<...>:
// as today, minus the Core IContainerControl / IContainerObject bases.
```

MAUI-owned helpers replace the Core extensions MAUI used, in one class,
`MauiElementExtensions`:

- **on `IMauiElement`:** `FindElement` (the throwing lookup), `HasUsableBounds`,
  `ContainsCenter`, `Area`, `IsControlType`, `FirstVisible`;
- **on `IMauiElementScope`:** `FindVisibleElements`, `FindVisibleByAutomationId`,
  `FindVisibleByName`.

The helpers keep Core's method names, so call sites did not change. `CenterOf(Rectangle)` stays
Core's, because it is typed on a rectangle, not on an interface.

These are copies today. They merge back into Core when the shape moves down (4.5).

### 4.3 MAUI exceptions (`Brinell.Maui.Exceptions`)

```csharp
/// The element a handle referred to is no longer in the UI tree.
public class StaleElementException(Locator? locator, Exception? platformError = null) : BrinellException;

/// Found, but not ready for what the call wants.
public class ElementNotReadyException(Locator locator, NotReadyReason reason, string? detail = null) : BrinellException;
public enum NotReadyReason { NotVisible, Disabled, Other }

/// A scope in the chain did not become ready: page, container, collection or row.
public class ScopeNotReadyException(ScopeReadiness readiness, int budgetMs) : BrinellException;

/// The app under test is gone: its process exited, or the driver session was lost. Never retried.
public class AppUnavailableException(string detail, Exception? platformError = null) : BrinellException;
```

- MAUI throws `ScopeNotReadyException` where it threw Core's `PageLoadException` before. The 4
  tests that catch `PageLoadException` in `Brinell.Maui.Tests` / `Brinell.Maui.UITests` change.
- `ElementNotFoundException` and `AssertionException` stay Core's.

### 4.4 Logging near-misses without changing Core

`ControlCall` writes the near-miss through the existing `ITestLogger.LogExit`, with
`LogResult.Warning` and a message that starts with `near-miss:`. For example: "near-miss:
replaced 3 times; 3200 of 5000 ms; last: NotReady(Disabled) at 1800 ms". So nothing is added to
`ITestLogger`.

### 4.5 Moving down later (a separate project)

When MAUI has passed plan step 8 (proved on Windows and Todo, and no worse on Android):

1. Core gets the proven shapes, **as replacements, not additions**: the element, driver, scope and
   page interfaces from 4.2 (minus the MAUI-specific members); the four exceptions;
   `ScopeReadiness`; the geometry and scope helpers; and, if another stack needs them, the Calls
   layer.
2. Each other stack moves when it is refactored: NativeAndroid (the Appium approach carries over),
   then WPF and WinForms (FlaUI), then Html (Playwright, Blazor), then Stride.
3. MAUI switches from its own interfaces to Core's, which is mostly a namespace change.
4. The old Core interfaces are deleted with the last stack that used them. UAT's page discovery
   switches from `IPageObject` to the new page interface.

Until then, **MAUI and Core disagree on purpose**, and each is internally consistent.

UAT note (corrected in step 1): MAUI UAT finds pages through Brinell.Core's `TestComposition`,
which recognizes Core's `[TestPage]` attribute or Core's page interface. It does **not** use
`UatDiscovery`'s name inference. `PageObjectBase` therefore carries `[TestPage]`. The attribute
is inherited and unchanged; abstract types are skipped. `Brinell.Maui.Uat.Tests` confirms the
same 20 pages and the same controls are found as before. When the shape moves down, composition
switches to the new page interface and the attribute can go.

---

## 5. Drivers

| Driver | Change |
| --- | --- |
| `FlaUIMauiElement` | Implements `IMauiElement` directly. `Live<T>(Func<T>)` and `Live(Action)` wrap every UIA touch. `UIA_E_ELEMENTNOTAVAILABLE` (FlaUI's exception, or a `COMException` with `0x80040201`) becomes `StaleElementException`; other errors keep their type. `InstanceKey` reads the runtime id through `Live`, so it doubles as the alive check (R6). Finds make one `FindFirstDescendant` / `FindAllDescendants` call. |
| `FlaUIDeclaredElement` | Never stale. `InstanceKey` is the declared id. |
| `FlaUIMauiDriver` | Implements `IMauiDriver`. `FindElements` makes one search; the timeout `FindElement` / `TryFindElement` are gone, and with them the `while`-loop bug (F7). An exited app process becomes `AppUnavailableException`. Chrome lookups use an internal `FindChrome(locator, timeoutMs)` (internal, because the element classes call it on their driver). |
| `AppiumMauiElement` | The same `Live` helper, mapping Selenium's `StaleElementReferenceException`. `InstanceKey` is the element id. Finds make one search, with no `WebDriverWait`. |
| `AppiumMauiDriver` | Implements `IMauiDriver`. An invalid or terminated session becomes `AppUnavailableException`. Chrome lookups use internal `FindChrome` / `FindAllChrome` (the drawer opener, a toolbar item raised by id, picker items). |
| `MauiTestContext` | `TryFindElement`: one `FindElements`, catching nothing (F4). `FindElement`: the interface default, with no `ElementFind` loop and no sweep (F1, F2). It no longer reads `TimeoutSettings.ElementFind`. |

**Selenium types appear only in `Brinell.Maui.Appium`.**

**Settle waits below an action.** A driver action may wait, with a short fixed bound, for the
effect of the step it just took: FlaUI's `OpenDropdown` and its close (2 s each), Appium's
`SetRangeValue` (1 s), the flyout, and `WaitUntilPositionSettles`. These are the action's own
confirmation (R3), not a second poll: they never repeat the action and never look for an element.
They stay, and each says what it waits for when it gives up. A scroll is not one of them: it
takes the caller's remaining budget (6.4).

**The bridge and a closed app.** The bridge lookup treats a target it cannot read as "not there".
When nothing answered because the launched app has exited, the FlaUI driver raises
`AppUnavailableException` (bridge verbs, state reads, gestures), so a toolbar or menu call on a
closed app fails at once instead of waiting out its budget as "not ready".

---

## 6. Calls (`Brinell.Maui/Calls/`, all internal)

These replace:

- `ViewBase.RunPoll` and `ViewBase.Run`;
- `RootedScopeBase.RunPoll`, `RunDo` and its page gate;
- `ObjectBase.Poll`;
- both `Until` implementations;
- the three copies of the page-gate message;
- MAUI's use of `WaitHelper`.

### 6.1 `Observation` and `ObservationLog`

```csharp
internal enum ObservationKind { Done, ScopeNotReady, Missing, NotReady, Stale, ItemChanged, Mismatch, Pending, Failed }

/// What one attempt saw.
internal readonly record struct Observation(
    ObservationKind Kind,
    string? InstanceKey = null,        // the element seen
    ScopeReadiness? Scope = null,      // for ScopeNotReady
    NotReadyReason? Reason = null,     // for NotReady
    string? Detail = null,             // for Pending ("rows still moving"), ItemChanged
    Exception? Error = null);          // for Mismatch (AssertionException), Failed

internal sealed class ObservationLog
{
    public void Add(Observation observation);
    public Observation Last { get; }
    public int Attempts { get; }
    public int Replacements { get; }   // distinct InstanceKeys seen - 1

    /// The exception a timed-out phase throws, built from Last:
    ///   ScopeNotReady -> ScopeNotReadyException (the scope that failed, its state)
    ///   Missing       -> ElementNotFoundException
    ///   NotReady      -> ElementNotReadyException
    ///   Stale, or Replacements > 0 -> StaleElementException ("found, then gone (replaced 3 times)")
    ///   ItemChanged   -> StaleElementException ("row 3 now shows another item")
    ///   Pending       -> WaitTimeoutException (what was still pending)
    ///   Mismatch      -> the AssertionException as-is
    ///   Failed        -> after one attempt, the exception as-is; after retries, a
    ///                    WaitTimeoutException naming its type and how many attempts raised it,
    ///                    with the exception as InnerException (a message cannot be added to it)
    public Exception ToException(Locator? locator, string caller, int budgetMs);

    /// "replaced 3 times; last: NotReady(Disabled) at 1800 ms", for messages and near-misses.
    public string Summary();
}
```

### 6.2 `Deadline` and `Poller`

```csharp
/// The point in time a phase must finish by. Created once per phase and passed down.
internal readonly struct Deadline
{
    public static Deadline In(int milliseconds);
    public int RemainingMs { get; }
    public bool IsPassed { get; }
}

/// The only retry loop. It logs nothing, checks no readiness itself, and starts no nested budget.
/// AppUnavailableException and configuration errors end it at once (R0). Any other exception
/// escaping the attempt is recorded as Failed and retried (D3).
internal sealed class Poller(int pollingIntervalMs)
{
    public bool Until(Func<AttemptContext, Observation> attempt, Deadline deadline, AttemptContext context);
}
```

### 6.3 `AttemptContext`

```csharp
/// The state of one phase of one call. Control and scope objects are often created fresh on
/// every property access, so this state cannot live on them.
internal sealed class AttemptContext(Deadline deadline, int animationMs)
{
    public ObservationLog Log { get; }
    public bool MaySweep();                        // scroll-to-find: once per Animation ms (F3)
    public bool MayScrollIntoView(string key);     // once per instance, once per Animation ms (S3)
}
```

State that one kind of attempt carries across attempts, such as the collection's settle state,
lives in a local captured by that attempt's closure (7.6).

### 6.4 `ControlCall`

```csharp
/// One public call: a log entry/exit pair around the whole body, action included. There is no
/// page gate here: readiness is the first step of every attempt (R4).
/// On success, it writes a near-miss (LogResult.Warning, 4.4) when the log shows
/// context.NearMiss.Replacements or more replacements, or more than context.NearMiss.BudgetShare
/// of the budget used (X5).
internal sealed class ControlCall(IMauiTestContext context, string scopeName, string controlId)
{
    public T Run<T>(string caller, string? value, int budgetMs, Func<AttemptContext, T> body);
}
```

While its body runs, the call's `AttemptContext` is `AttemptContext.Current`. A wait below the
call that has no parameter for a budget - a Core method's scroll, a collection's scroll step -
takes `CallRemainingMs` from it, so it never outlasts the call (R2, R3). `IMauiElement.ScrollIntoView`
has no default timeout for that reason.

### 6.5 `Confirm`

```csharp
/// Waits, inside a Core method, for the effect of an action already done. Never repeats
/// anything. A stale read means Replaced, not a retry.
protected Confirmation<T> Confirm<T>(Func<T?> read, Func<T?, bool> done, int? timeoutMs);

public readonly record struct Confirmation<T>(ConfirmationResult Result, T? LastValue, Exception? LastError);
public enum ConfirmationResult { Confirmed, NotConfirmed, Replaced }
```

It lives on `ViewBase` and `RootedScopeBase`, and replaces both `Until` implementations. The eight
act-then-confirm Core methods word their failure by `Result`.

---

## 7. Scopes

### 7.1 Readiness

```csharp
namespace Brinell.Maui.Scopes;

public enum ScopeReadinessState
{
    Ready, MissingRoot, StaleRoot, NotLoaded, Busy, ContentNotReady, ItemChanged,
    MissingBusySignal, InvalidBusySignal   // configuration errors: fail at once (R0)
}

/// Which scope answered, and why. When a parent is not ready, its answer is returned unchanged,
/// so the message names the scope that actually failed.
public readonly record struct ScopeReadiness(
    string ScopeName, ScopeReadinessState State, string? Detail = null, bool RootReacquired = false)
{
    public bool IsReady => State == ScopeReadinessState.Ready;
}
```

- Every scope implements `WaitReady(timeoutMs)` as a `ControlCall` around a `Poller` on
  `ProbeReadiness`. `IsReady()` is the interface default.
- `Page` is used for the page name in log lines only.

### 7.2 The chain

| Scope | `ProbeReadiness()` | Asks parent? |
| --- | --- | --- |
| `AppRoot` | `Ready`. **Virtual**, for an app-level busy signal later (Q4) | none |
| `PageObjectBase` | root present and shown → `IsLoaded()` → busy signal (if on) | none |
| `ContainerObjectBase` | parent → root alive → `ProbeContentReadiness(root)` | yes (`AsksParent => true`) |
| `Popup`, `ContentDialog` | root alive → content | **no** (`AsksParent => false`): shown over their parent (A2, A3) |
| `CollectionObjectBase` | as container | yes |
| `ItemObjectBase` | parent collection → root alive → **same item** (`ItemKey`, 7.6) | yes |

```csharp
public abstract partial class RootedScopeBase<TSelf, TSetResult>
{
    /// Whether readiness is inherited from the parent. False for scopes shown over it.
    protected virtual bool AsksParent => true;

    /// This scope's own readiness beyond "the root is alive". One attempt: for example "the
    /// spinner is gone" or "at least one row". Replaces WaitContentReadyCore.
    protected virtual ScopeReadiness ProbeContentReadiness(IMauiElement root) => Ready;
}
```

A probe costs one read per link (the alive check), plus the page's busy read when that is on.

### 7.3 Roots: finding, caching, liveness

```csharp
public abstract partial class RootedScopeBase<TSelf, TSetResult>
{
    protected abstract IMauiElement? TryFindRootElement();   // was FindContainerRootElement (throwing)
    protected virtual bool CacheRoot => true;               // was CacheContainerRoot

    /// Liveness for every scope (R6): InstanceKey read through the driver's Live helper.
    private bool IsRootAlive(IMauiElement root);             // StaleElementException -> false

    /// A second check that only pages use: shown, not only alive.
    protected virtual bool IsRootUsable(IMauiElement root) => true;

    public IMauiElement? TryGetRoot();       // cache -> alive && usable -> otherwise re-find once
    public IMauiElement Root { get; }        // TryGetRoot() ?? throw DescribeMiss(...)   (was ContainerRoot)

    /// Runs read against the root. On StaleElementException it invalidates, re-roots once and
    /// runs read again. The only stale catch in the scope layer (F9).
    protected T WithRoot<T>(Func<IMauiElement, T> read, T whenAbsent);
}
```

The root is cached for the object's lifetime and checked on every access (Q6). A property
declared as `=> new` or as `{ get; }` then differs in cost only. `ContainerRoot`'s catch-all is
gone.

### 7.4 Child lookup

```csharp
public virtual IMauiElement? TryFindElement(Locator l) => WithRoot(root => root.TryFindElement(l), null);
public virtual IReadOnlyList<IMauiElement> FindElements(Locator l) => WithRoot(root => root.FindElements(l), []);
public virtual ElementNotFoundException DescribeMiss(Locator l);   // "not found within <scope>"
```

`CanResolveElements`, `EnsureLoaded` and its re-entrancy guard are deleted (Q2). A page that is not
shown has no root, and the readiness step reports `NotLoaded` or `MissingRoot`.

### 7.5 The scope classes

| Class | Its part |
| --- | --- |
| `AppRoot` | The only whole-app scope (Q3). `AllowsScrollLookup => false`. Lookups go through `MauiTestContext`. `ProbeReadiness` is virtual. |
| `PageObjectBase` | Implements `IMauiPage<TSelf>`. Root: `Context.FindElements(Name)`, the first with usable bounds. `IsRootUsable` is `HasUsableBounds`. `IsLoaded()` (no parameter) is the override point and feeds `ProbeReadiness`. The busy signal is read as today, with a MAUI `BusySignalPolicy`. `WaitReady`, `WaitBusy`, `WaitLoaded`, `WaitTitle`, `AssertLoaded`, `AssertTitle` and `AssertIdle` are call units with `DefaultWait` (E1). |
| `ContainerObjectBase` | Root: `parent.TryFindElement(locator)`. `ScrollingRoot` and `AllowsScrollLookup` come from the parent. `WaitExists`, `WaitVisible`, `AssertExists` and `AssertVisible` are call units. The generator-facing `Run*` helpers mirror `ViewBase`'s (R8). |
| `ComponentObjectBase` | Unchanged. |
| `Popup` | `AsksParent => false`. Root from `AppElement`, not cached. `Page => null` is deleted. |
| `ContentDialog` | `AsksParent => false`. Root from `TryFindActiveDialog()`, not cached. |
| `MediaElement` | Root falls back to play/pause. Child lookups go through the parent, as today. |
| `StateContainer`, `ShellFlyout` | `CacheRoot => false`. `ShellFlyout` sets `ScrollingRoot` to its item host and turns `AllowsScrollLookup` back on. |
| `ScrollView` | `ScrollingRoot` is its root. |
| `DriverRootScope` | **Removed** (Q3). |
| `MauiTestContext` | Implements `IMauiTestContext`. It is the lookup service behind `AppRoot`, not a scope to declare controls on. |

### 7.6 Collections and `ItemObjectBase`

`ItemContainerBase` becomes `ItemObjectBase`, and `IMauiItemContainer` becomes `IMauiItemObject`
(plan step 0).

```csharp
/// Which item a row holds, so a re-resolved row can be checked (Q7).
public readonly record struct ItemKey(ItemKeyKind Kind, string Value);
public enum ItemKeyKind { Logical, AutomationId, Position }   // strongest the platform offers

public interface IItemRootProvider
{
    IMauiElement? TryGetItemRoot(ItemKey key);
    ItemKey KeyOf(IMauiElement itemRoot, int position);
}

public abstract class ItemObjectBase<TCollection, TSelf> : ContainerObjectBase<TCollection, TSelf>
{
    public int Index { get; }
    public ItemKey Key { get; }                        // recorded at creation
    public override bool AllowsScrollLookup => false;  // unchanged
    // root re-resolved by Key; a root whose KeyOf(...) differs -> ProbeReadiness = ItemChanged
    // a cached root counts only while it still holds Key (IsCachedRootValid), so the row's own
    // members - Click, GetText - never use a recycled element either (implementation review)
}
```

The materializing loop becomes attempts (Q8, Q9). One scroll step is a deliberate, repeatable
action, so repeating it across attempts is the intent.

```csharp
/// Builds the attempt for ScrollToItem / ScrollToEnd / ScrollToTop / FindItem / WaitForItems.
/// The returned closure owns the settle state (the previous realized indexes). Each attempt:
///   the target is present -> Done
///   still settling        -> Pending("rows still moving")   (was WaitForProgressThenSettle)
///   otherwise             -> one scroll step -> Pending("scrolled")
///   the end is reached, target absent -> Missing
///   stale root            -> Stale
private Func<AttemptContext, Observation> MaterializeAttempts(Func<IMauiElement?> target);
```

| Member | After |
| --- | --- |
| `Item(index)`, `this[int]`, `Item(key)`, `this[Locator]`, `ItemWhere`, `SelectItem` | Call units that wait (R7). `SelectItem` resolves by polling, then activates once; only an activation that answers "nothing activated" is asked again, and any exception ends the call (R0). |
| `TryItem`, `TrySelectItem`, `FindItem` | Answer now (`TrySelectItem` has no timeout). `FindItem`'s recycling guard becomes `ItemKey`. |
| `ScrollToItem`, `ScrollToEnd`, `ScrollToTop`, `WaitForItems` | Call units with one budget (`timeoutMs ?? DefaultWait`) on `MaterializeAttempts`. `ScrollToTop` is bounded. |
| `WaitItemCount`, `WaitAnyItem`, `AssertItemCount`, `AssertEmpty` | Call units. `GetItemCount()` and `IsEmpty()` are single reads, with no timeout. |
| `TryGetItemRoot`, `TryGetItemRoots`, `GetItemCount` internals | `WithRoot`. |
| `TryScrollItemIntoView`, `TryActivate`, `IsUsable` | Catch-alls go. A stale row is a signal. "Wrong candidate" is an answer only for the exceptions the drivers document for it. |

---

## 8. Controls: `ViewBase`

```csharp
public abstract partial class ViewBase<TScope> : IControlObject<TScope>   // Core control API, unchanged
{
    protected ViewBase(IMauiScope<TScope> scope, Locator locator);          // no Core ControlObjectBase
    protected Locator Locator { get; }
    protected IMauiScope<TScope> MauiScope { get; }

    // Lookup (F3, F5)
    protected virtual IMauiElement? TryFindElement() => MauiScope.TryFindElement(Locator);  // the one override
    protected virtual ElementNotFoundException NotFound() => MauiScope.DescribeMiss(Locator);
    protected IMauiElement FindElement();                  // non-virtual: Try ?? sweep once ?? throw NotFound()
    private IMauiElement? Locate(AttemptContext a);        // Try ?? throttled sweep

    // Readiness (original plan 3.1)
    private void EnsureVisible(IMauiElement e, AttemptContext a);        // one attempt; ElementNotReadyException
    protected virtual bool RequiresVisibilityForAction => true;
    protected virtual void EnsureReadyForActionCore(IMauiElement e);     // throws ElementNotReadyException

    // The attempt: scope first
    private Observation TryOnElement<T>(AttemptContext a, bool ensureVisible,
        Func<IMauiElement, (bool Done, T Value)> body, ref T result)
    {
        var scope = MauiScope.ProbeReadiness();
        if (!scope.IsReady) return Observation.ScopeNotReady(scope);
        var element = Locate(a);
        // ... visible, body; Stale / NotReady / Assertion -> Observation
    }

    // Run* helpers: same names and signatures (R8); doEnsureVisible removed
    protected T? RunGetWithElement<T>(...);        // TryOnElement(visible, read)
    protected bool RunWaitWithElement<T>(...);     // TryOnElement(visible, predicate)
    protected TScope RunAssertWithElement<T>(...); // S1: hold, re-read; re-locate on Stale / NotReady
    protected TScope RunDoWithElement(...);        // phase 1: ResolveReady; phase 2: act once
    protected TScope RunSetWithElement<T>(...);    // same as RunDoWithElement
    // Run*WithOptionalElement: scope step + Locate only. RunWait / RunDo / RunAssert / Run: ControlCall + Poller
    private IMauiElement ResolveReady(AttemptContext a);   // still needed (rrE); a thin use of TryOnElement

    protected Confirmation<T> Confirm<T>(...);     // replaces Until (6.5)
}
```

Removed from controls:

- `EnsureVisible(element, timeout)` and `WaitVisibleCore`;
- the `EnsureVisible` calls in `ToggleControlBase` and `RadioButton`;
- `EnsureClickableCore` in the clickable Cores;
- the `FindElement` overrides in `IndicatorView`, `DrawingView` and `Stepper` (their messages move
  to `NotFound()`);
- `Menu.OpenCore`'s trigger wait.

`ScrollLookup`, `TryFindElement(ScrollLookup)` and `Resolver` stay for the `VisibleAfterScroll`
trio, now built on `Locate`.

---

## 9. One call, end to end

`todoList.Rows[3].DeleteButton.Click(timeoutMs: 2000)`, where the button is on a row, the row is
in a collection, and the collection is on the page:

```text
Click -> RunDoWithElement -> ControlCall.Run("Click", budget 2000)      one log pair
 phase 1: Poller.Until(deadline)
   attempt 1: row -> collection -> page.ProbeReadiness() = Busy
              => ScopeNotReady(TodoListPage, Busy)
   attempt 2: chain Ready; KeyOf(row) == Key
              Locate DeleteButton under the row root -> found (…4.30)
              EnsureVisible: not visible -> scroll (allowed) -> still not
              => NotReady(NotVisible, …4.30)
   attempt 3: chain Ready; Locate -> found (…4.65); visible; enabled
              => Done(…4.65)
 phase 2: ClickCore(…4.65), once
 exit Success, 3 attempts, 1 replacement (below the near-miss threshold)
```

If the list had recycled row 3, `row.ProbeReadiness()` would report `ItemChanged`, and the call
would fail with "row 3 now shows another item". It would never press another item's delete
button. If the Delete command never re-enabled, it would fail with "found but disabled for
2000 ms" (R0).

---

## 10. What changes for whom

| Audience | Change |
| --- | --- |
| **Generated code** | None (R8). |
| **Control authors** | Override `TryFindElement()` + `NotFound()`, not `FindElement()`. Throw `ElementNotReadyException` from `EnsureReadyForActionCore`. Use `Confirm`. Never call `EnsureVisible` from a Core method. A wait below the call (a scroll) takes `CallRemainingMs`. |
| **Container authors** | Override `ProbeContentReadiness`, not `WaitContentReadyCore`. Use `AsksParent => false` for a scope shown over its parent. Implement `TryFindRootElement`, which returns null. |
| **Page authors** | `IsLoaded()` takes no parameter; the overrides in about 10 MAUI test pages change mechanically. It is part of readiness, not a gate on each lookup. |
| **Test authors** | App bugs still fail, now naming what was seen (R0). Near-misses show in the log. Budgets mean what they say. `Item(index)` and `SelectItem` wait. `ScrollToItem` has a budget. Use `AppRoot` (`DriverRootScope` is gone). `ItemObjectBase` replaces `ItemContainerBase`. `ScopeNotReadyException` replaces `PageLoadException` from MAUI. |
| **Driver authors** | Implement `IMauiElement` / `IMauiDriver` directly. `InstanceKey`. `Live` maps staleness. Finds make one attempt. Report an app that is gone as `AppUnavailableException`. `ScrollIntoView(timeoutMs)` has no default, and a stepping scroll makes at least one step. |
| **Other stacks** | Nothing. Core is unchanged (R9). |
| **Skills** | `maui-control`: R0-R2 wording, `ProbeContentReadiness`, `AsksParent`, `ItemObjectBase`, `IMauiElement` as the element type. `maui-ui-test`: `ItemObjectBase`, what waits and what does not, `ScopeNotReadyException`. |

Removed or renamed, all listed in `CHANGELOG.md` in the last step:

- **Core bases MAUI no longer implements:** `IElement<IMauiElement>`, `IDriver<IMauiElement>`,
  `IElementScope` / `IElementScope<IMauiElement>`, `IPageObject` / `IPageObject<IMauiElement>`,
  `ITestContext<IMauiElement>`, `IContainerControl<IMauiElement>`, `IContainerObject<IMauiElement>`,
  `ControlObjectBase<TScope>`.
- **Lookups:** the timeout finds on MAUI elements and drivers.
- **Waits:** `EnsureVisible(element, timeout)`, `WaitVisibleCore`, `doEnsureVisible`, both `Until`
  implementations, both `RunPoll` implementations, and `ObjectBase.Poll` (removed after the
  implementation review, when nothing called it).
- **Virtual `ViewBase.FindElement()`.**
- **Page gating:** `CanResolveElements`, `EnsureLoaded`, `WaitContentReadyCore`, `IsParentReady`,
  `WaitParentReady`. (`IsCachedRootValid` stays until the root renames.)
- **Renamed** (the first three **not done**; left for the move down, see [move-down.md](move-down.md) section 4):
  - `FindContainerRootElement` → `TryFindRootElement`;
  - `ContainerRoot` / `TryGetContainerRoot` → `Root` / `TryGetRoot`;
  - `CacheContainerRoot` → `CacheRoot`;
  - `ItemContainerBase` → `ItemObjectBase`, and `IMauiItemContainer` → `IMauiItemObject`;
  - `IItemRootProvider.TryGetItemRoot(int)` → `TryGetItemRoot(ItemKey)`;
  - `IsLoaded(int?)` → `IsLoaded()`, `GetTitle(int?)` → `GetTitle()`,
    `TakeScreenshot(string?, int?)` → `TakeScreenshot(string?)`.
- **Scopes:** `DriverRootScope`, and `Popup.Page => null`.
- **Exceptions:** `PageLoadException` is no longer thrown by MAUI.

## 11. Open decisions

| # | Question | Default |
| --- | --- | --- |
| S1 | Resolve once in `RunAssertWithElement`? | Resolve once; re-locate on `Stale` / `NotReady`. Revisit with step 2's numbers. |
| D3 | Should `Poller` retry unknown exceptions? | Yes, within the budget, and named in the final message. `AppUnavailableException` and configuration errors are never retried. |
| X5 | Near-miss thresholds | **Settled** (step 8, and the implementation review): 3 or more replacements, or more than 50% of the budget used. They are settings: `MauiTestContextOptions.NearMiss` (`NearMissSettings`). Step 8 saw 1 near-miss in 4,833 logged calls, so the defaults are not noisy. |
| Q6 | Root cache: the object's lifetime, with an alive check | Switch to per-call if step 2 shows the check costs more than a lookup. |
| Q9 | Default budget for materializing loops | `DefaultWait`. Step 2's list of long-list tests decides. |
| X1 | When to start moving down (4.5) | After plan step 9, as its own plan. |
| X2 | `Brinell.Html`'s `RunDoWithElement` retries actions | Fix it when Html moves down (4.5), not before. |
