# Brinell.Html to the MAUI standard: design

**This is the proposed design.** It is not accepted; the plan is a draft (`plan.md` section 1).
Where a rule or shape here matches MAUI's, this document says so and points into
[`../stale-readiness/design.md`](../stale-readiness/design.md) instead of restating it. The
departures from MAUI live in [differences-from-maui.md](differences-from-maui.md) and are cited
inline.

## 1. Goal

Same as MAUI's design section 1, with "MAUI" swapped for "Html". A test's call on any Html
object is one unit of work; it spends one budget; it waits for its scope chain; it finds its
element again on every attempt; it recognizes a replaced or detached element; it never repeats
an action; when it fails, it says what it last saw; **app bugs stay failures.**

## 2. Rules

Same rules as MAUI, unchanged:

- **R0** App bugs stay failures.
- **R1** One unit per public call: one `LogEntry`/`LogExit` pair.
- **R2** Only the call's poll waits.
- **R3** Each phase has the caller's budget.
- **R4** A call waits for its scope chain.
- **R5** A stale element is a signal (no catch-alls).
- **R6** One check that a root is still alive.
- **R7** Non-`Try` members wait; `Try*` members answer about now.
- **R8** The generator does not change.
- **R9** Html owns the interfaces it changes. Core is not changed by this work.

Full text: `../stale-readiness/design.md` section 2. The R0 practice table (section 2.1)
applies unchanged.

Two entries from that table are worth naming for Html:

| Situation | The Html-shaped case |
| --- | --- |
| The action ran, but its effect never shows (`NotConfirmed`) | `Click` on a submit button that hits the server; the URL never changes and the success flag stays clear |
| The app process exits, or the driver session is lost (`AppUnavailableException`) | Playwright `Page.Close()` or a browser crash: `Locator` calls throw `TargetClosedError` / `PlaywrightException` |

**X2 is closed here**, not deferred. MAUI's design section 11 kept X2 open ("`Brinell.Html`'s
`RunDoWithElement` retries actions ... fix it when Html moves down"). That fix is R0 in step 4
of `plan.md`. The move-down draft note stays for record; this project is the fix.

## 3. The layers

Same shape as MAUI's section 3, with "MAUI" swapped for "Html":

```text
Brinell.Core          unchanged. Html still uses: Locator, BrinellException and its existing
                      subtypes, ITestLogger + LogResult, TimeoutSettings, non-generic
                      ITestContext, IElementObject / IControlObject / capability interfaces,
                      IDiagnosticDriver, IScreenshotService, generator attributes.
Brinell.Html          Interfaces   IHtmlElement · IHtmlDriver · IHtmlElementScope · IHtmlScope<T>
                                   IHtmlPage · IHtmlTestContext · container/list/item interfaces
                      Exceptions   StaleElementException · ElementNotReadyException
                                   ScopeNotReadyException · AppUnavailableException
                      Calls        ControlCall · Poller · Deadline · AttemptContext · Observation(Log)
                      Scopes       ScopeReadiness · AppRoot · HtmlPageObjectBase · RootedScopeBase
                                   ContainerObjectBase · CollectionObjectBase · ItemObjectBase
                                   FrameScope
                      Controls     ViewBase · the Run* helpers · Confirm
                      Helpers      HtmlElementExtensions
Brinell.Blazor        Reuses everything above. Adds Blazor-specific busy signals and a small
                      set of Blazor components.
Drivers               Brinell.Html.Playwright: PlaywrightHtmlElement · PlaywrightHtmlDriver
                      · PlaywrightTestContext · Live<T>(Func<T>) mapping
                      TargetClosedError -> AppUnavailableException and
                      element-detached errors -> StaleElementException
```

The call flow is the one from MAUI's design section 3:

```text
public member
└─ ControlCall.Run(caller)                    log pair; near-miss warning
   └─ Poller.Until(attempt, deadline)         the only loop
        attempt:
        1. scope.ProbeReadiness()             walks the chain
        2. locate                             one lookup; throttled sweep
        3. visible / ready                    one check
        4. body                               read · predicate · compare · materialize step
   └─ act once, then Confirm (actions only)
```

## 4. The Html boundary

### 4.1 What Html keeps from Core, and what it leaves

Same table as MAUI's design 4.1, with `IHtmlElement` in place of `IMauiElement` and so on. All
seven "yes: leaves" rows apply to Html today: `IElement<IHtmlElement>`, `IDriver<IHtmlElement>`,
`IElementScope<IHtmlElement>` / `IElementScope<T>`, `IPageObject<IHtmlElement>`,
`ITestContext<IHtmlElement>`, `IContainerControl<IHtmlElement>`, `IContainerObject<IHtmlElement>`
and `ControlObjectBase<TScope>`.

### 4.2 The Html interfaces

Modelled on MAUI's 4.2 with only the Html-shaped members. Cited inline where a departure from
MAUI matters.

```csharp
namespace Brinell.Html.Interfaces;

/// A DOM element (or a Playwright ILocator standing in for one). No Core element interface
/// underneath.
public interface IHtmlElement
{
    // identity
    string InstanceKey { get; }            // see differences-from-maui.md item 2
    string? AutomationId { get; }          // typically the value of data-testid
    string? Name { get; }                  // aria-label, or the tag name

    // state, geometry, interaction (the members IHtmlElement contributes today, minus timeouts)
    bool Visible { get; }
    bool Enabled { get; }
    bool Selected { get; }                 // for a checkbox / radio / option
    string? Text { get; }
    string? TagName { get; }
    Point Location { get; }
    Size Size { get; }
    Rectangle Rect { get; }
    string? GetAttribute(string name);
    string? GetDomAttribute(string attributeName);
    string? GetDomProperty(string propertyName);
    string? GetCssValue(string propertyName);
    string InnerHtml { get; }
    string OuterHtml { get; }
    bool IsChecked { get; }
    string InputValue { get; }

    void Click();
    void DoubleClick();
    void RightClick();
    void Hover();
    void SendKeys(string text, TextInputMethod method = TextInputMethod.Keys);
    void Fill(string value);
    void Clear();
    void Check();
    void Uncheck();
    void SelectOption(string value);
    void SelectOption(string[] values);
    void Focus();
    void Blur();
    void Submit();
    void ScrollIntoView();                 // no default timeout; takes CallRemainingMs
    void Evaluate(string expression);
    T Evaluate<T>(string expression);

    // lookup: one attempt each, no timeouts (R2). The throwing form is the
    // extension HtmlElementExtensions.FindElement (Try ?? throw).
    IHtmlElement? TryFindElement(Locator locator);
    IReadOnlyList<IHtmlElement> FindElements(Locator locator);
}

/// The driver. No Core driver interface underneath. See ?A2 in plan.md.
public interface IHtmlDriver : IDiagnosticDriver, IDisposable
{
    IHtmlElement PageRoot { get; }                              // the current page or frame's body
    string CurrentUrl { get; }
    string PageTitle { get; }
    bool IsIdle();                                              // network + Blazor busy signal
    IReadOnlyList<IHtmlElement> FindElements(Locator locator);  // one attempt, from the page root
    void GoTo(string url);
    void GoBack();
    void GoForward();
    void Reload();
    // screenshots and diagnostics as today
}

/// Anything controls can be declared in.
public interface IHtmlElementScope
{
    IHtmlTestContext Context { get; }
    LocatorStrategy DefaultLocatorStrategy { get; }
    IHtmlPage? Page { get; }                                    // for naming only, never for gating

    ScopeReadiness ProbeReadiness();                            // one attempt
    bool IsReady() => ProbeReadiness().IsReady;
    bool WaitReady(int? timeoutMs = null);                      // a real wait

    IHtmlElement? TryFindElement(Locator locator);              // one attempt
    IReadOnlyList<IHtmlElement> FindElements(Locator locator);
    IHtmlElement FindElement(Locator locator) => TryFindElement(locator) ?? throw DescribeMiss(locator);
    ElementNotFoundException DescribeMiss(Locator locator) => new(locator);
}

public interface IHtmlScope<TScope> : IHtmlElementScope where TScope : IHtmlScope<TScope>
{
    TScope Self { get; }
}

/// A page.
public interface IHtmlPage : IHtmlElementScope
{
    string Name { get; }
    string? Url { get; }
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
public interface IHtmlPage<TSelf> : IHtmlPage, IHtmlScope<TSelf> where TSelf : IHtmlPage<TSelf>;

public interface IHtmlTestContext : ITestContext, IHtmlElementScope       // ITestContext: Core, non-generic, unchanged
{
    IHtmlDriver Driver { get; }
    string CurrentUrl { get; }
    string PageTitle { get; }
    bool IsIdle();
    IHtmlPage? CurrentPage { get; }                                       // the last known page object, for naming
}

// IHtmlContainer<TParent, TSelf>, IHtmlCollection<TParent, TSelf, TItem>,
// IHtmlItemObject<TCollection, TSelf>: as today, minus the Core IContainerControl bases.
```

Html-owned helpers (analogous to MAUI's `MauiElementExtensions`) live in `HtmlElementExtensions`:

- on `IHtmlElement`: `FindElement` (the throwing lookup), `HasUsableBounds`, `Area`;
- on `IHtmlElementScope`: `FindVisibleElements`, `FindVisibleByAutomationId`, `FindVisibleByCss`.

### 4.3 Html exceptions (`Brinell.Html.Exceptions`)

```csharp
public class StaleElementException(Locator? locator, Exception? platformError = null) : BrinellException;

public class ElementNotReadyException(Locator locator, NotReadyReason reason, string? detail = null) : BrinellException;
public enum NotReadyReason { NotVisible, Disabled, Other }

public class ScopeNotReadyException(ScopeReadiness readiness, int budgetMs) : BrinellException;

public class AppUnavailableException(string detail, Exception? platformError = null) : BrinellException;
```

Html **stops throwing Core's `PageLoadException`**. The tests and pages that catch it change,
same as MAUI's step 5.

### 4.4 Logging near-misses

Same mechanism as MAUI's 4.4. `ControlCall` writes the near-miss through
`ITestLogger.LogExit` with `LogResult.Warning` and a message starting with `near-miss:`. Nothing
is added to `ITestLogger`. Threshold defaults come from `HtmlTestContextOptions.NearMiss` (parallel
to `MauiTestContextOptions.NearMiss`).

### 4.5 Moving to Core later

This slice is one of the moves listed in `../stale-readiness/move-down.md` section 3. When Html
is done, the two interfaces and their exception set are ready to become the shape Core adopts
with the last stack.

### 4.6 The async facade

Html today declares `IAsyncHtmlElement`, `IHtmlAsyncControlObject<TScope>` and a handful of
capability shims that wrap sync methods in `Task.FromResult`. That is not the same as
Playwright's async model: the Playwright driver blocks on `.GetAwaiter().GetResult()` inside the
sync methods. The shim only papers over the calling style.

**Decision (?A1 in plan.md):** step 1 keeps the shims. Step 9 revisits: if no consumer needs
`IsExistsAsync` and its siblings, they are dropped from `Brinell.Html`. A true async surface is
a separate project (a `Brinell.Html.Async` that owns its own `IAsyncHtmlElement` and driver, and
does not block); this project does **not** build it.

## 5. Drivers

| Driver | Change |
| --- | --- |
| `PlaywrightHtmlElement` | Implements `IHtmlElement` directly. `Live<T>(Func<T>)` and `Live(Action)` wrap every Playwright call. Playwright's `TargetClosedError` (a `PlaywrightException` with `message` containing `Target page, context or browser has been closed`) becomes `AppUnavailableException`. An `ElementHandle` that is detached, or a `Locator` whose `Count` was positive and is now zero mid-call, becomes `StaleElementException`. `InstanceKey` is read as an element handle identity (see [differences-from-maui.md](differences-from-maui.md) item 2). Finds make one attempt: `Locator.CountAsync + Nth(i)`, with `Timeout = 0` on every Playwright call so no wait outlasts the caller (?A5). |
| `PlaywrightHtmlDriver` | New class (?A2). Implements `IHtmlDriver`. `FindElements` makes one search from the page root. A closed browser or page becomes `AppUnavailableException`. Owns the frame model (?A6): `FrameScope` returns from `PageRoot.Frame(pattern)`, not from a new context. |
| `PlaywrightTestContext` | Implements `IHtmlTestContext`. `TryFindElement` and `FindElement` are the interface defaults, backed by the driver's `FindElements` on the page root. It no longer catches every exception in `IsReady`. The frame-taking constructors go; a frame is a scope. |

**Selenium-style types**, if a Selenium driver is added later, appear only in a hypothetical
`Brinell.Html.Selenium`. Not in scope for this project.

**Settle waits below an action.** Playwright's own action auto-waits (`ClickAsync` waits for the
element to be visible, enabled, stable and receive events) are disabled for actions inside a
call: the call's `EnsureVisible` step already gated on visibility, and the auto-wait duplicates
budget. `LocatorClickOptions { Timeout = 1 }` (Playwright's minimum) with `Force = false` and
`Trial = false` is the concrete shape - the action fails at once if not ready, and the calls
layer decides whether that ended the call or the attempt.

**The bridge and a closed browser.** No bridge here (Html has no gesture bridge). The
equivalent is: a Playwright call raising `TargetClosedError` translates to
`AppUnavailableException`, which the poller does not retry (R0, 2.1).

## 6. Calls

Same shape as MAUI's design section 6. `ControlCall`, `Poller`, `Deadline`, `AttemptContext`,
`Observation`, `ObservationLog`, `Confirmer` and `Confirmation<T>` are copied verbatim into
`Brinell.Html/Calls/` and typed on `IHtmlTestContext`. Nothing here is Html-specific except the
namespace.

`ControlCall`'s log line format stays MAUI's, so a shared log analyser recognises both stacks.

## 7. Scopes

### 7.1 Readiness

`ScopeReadiness` and `ScopeReadinessState` copied from MAUI's 7.1, unchanged. Every Html scope
implements `WaitReady(timeoutMs)` as a `ControlCall` around a `Poller` on `ProbeReadiness`.

### 7.2 The chain

| Scope | `ProbeReadiness()` | Asks parent? |
| --- | --- | --- |
| `AppRoot` | `Ready`. Virtual, for an app-level busy signal | none |
| `HtmlPageObjectBase` | Url check → `IsLoaded()` → busy signal (see below) | none |
| `FrameScope` | Frame present and attached → passes `AsksParent` to the page | yes |
| `ContainerObjectBase` | parent → root alive → `ProbeContentReadiness(root)` | yes |
| `CollectionObjectBase` | as container | yes |
| `ItemObjectBase` | parent collection → row root alive → **same item** (`ItemKey`, 7.6) | yes |

**The busy signal for a Blazor page** is the value of `data-busy` on `<body>`. The sample host
injects one line of JavaScript (`sample-app.md` item 3) that flips it during any interactive
render or event handler. `HtmlPageObjectBase.BusySignalPolicy` reads it through the driver's
`PageRoot.GetAttribute("data-busy")` and treats `"true"` as busy, `"false"` or missing as idle,
anything else as `InvalidBusySignal` (fails at once). This mirrors MAUI's `BusySignalPolicy`
and is Html's answer to ?A8.

Popups/dialogs shown over their parent set `AsksParent => false`, same as MAUI.

### 7.3 Roots

Same shape as MAUI's 7.3, with the "alive" check reading `InstanceKey` through `Live`. The
`ContainerRoot` (`FindContainerRootElement`) rename to `Root` (`TryFindRootElement`) is done
here, not deferred, because Html has no external callers of the old names outside the framework
and the two sample projects.

### 7.4 Child lookup

Same shape as MAUI's 7.4. `WithRoot` runs a read against the root, and on
`StaleElementException` re-roots once and runs it again. `CanResolveElements`, `EnsureLoaded`
and its guard are deleted if any equivalent exists in Html today.

### 7.5 The scope classes

| Class | Its part |
| --- | --- |
| `AppRoot` | The only whole-app scope. `AllowsScrollLookup => false`. Lookups go through `HtmlTestContext`. |
| `HtmlPageObjectBase` | Implements `IHtmlPage<TSelf>`. Root: `Context.FindElements(Locator.ByCss("body"))`. `IsLoaded()` is the override point (URL check + `IsIdle`). The busy signal is `data-busy`. `WaitReady`, `WaitBusy`, `WaitLoaded`, `WaitTitle`, `AssertLoaded`, `AssertTitle` and `AssertIdle` are call units. |
| `FrameScope` | New. Wraps a Playwright `IFrame` as a scope. `Root` is the frame's body; child lookups scope to the frame. Handles frame detach as `StaleElementException`. |
| `ContainerObjectBase` | Root: `parent.TryFindElement(locator)`. `WaitExists`, `WaitVisible`, `AssertExists` and `AssertVisible` are call units. |
| `ComponentObjectBase` | Unchanged in shape; parts declared as named children. |
| `HtmlTestContext` | Implements `IHtmlTestContext`. It is the lookup service behind `AppRoot`. |

### 7.6 Collections and `ItemObjectBase`

Same shape as MAUI's 7.6. `ItemKey` for Html:

```csharp
public readonly record struct ItemKey(ItemKeyKind Kind, string Value);
public enum ItemKeyKind { Logical, AutomationId, DomNode, Position }
```

**Playwright ItemKey source of truth** ([differences-from-maui.md](differences-from-maui.md)
item 5):

1. If the row's root has a `data-item-key` attribute, use it (`Logical`).
2. Else if it has `data-testid`, use that (`AutomationId`).
3. Else use the DOM node identity from an `EvaluateHandle` (`DomNode`).
4. Else fall back to `Position` and record a `Warning` on the row's first use.

Blazor `<Virtualize>` recycles DOM nodes, so `DomNode` alone is not enough: the sample project
gets a `data-item-key` on virtualized rows (`sample-app.md` item 4).

## 8. Controls: `ViewBase`

Same shape as MAUI's design section 8. `ViewBase<TScope>` implements `IControlObject<TScope>`
(Core, unchanged), holds its own `Locator`, and no longer inherits `ControlObjectBase`. Override
points are `TryFindElement()` and `NotFound()`; the `Run*WithElement` helpers keep their names
and signatures (R8).

Removed from Html controls:

- `EnsureVisible(element, timeout)` and every call to it inside a `RunDoWithElement`;
- the `doEnsureVisible` parameter on `RunDoWithElement`;
- `ControlBase.RunPoll`, replaced by `ControlCall.Run` + `Poller.Until`;
- every `catch (Exception)` in the calls path;
- the sync/async double-wrapping in `RunWithElement` overloads.

`Confirm` replaces the "loop until the effect is seen" habit currently baked into `RunPoll`. It
lives on `ViewBase` and takes `(read, done, timeoutMs)` as in MAUI's 6.5.

## 9. One call, end to end

`loginPage.LoginButton.Click(timeoutMs: 2000)`, where the button becomes disabled while the
form is validating:

```text
Click -> RunDoWithElement -> ControlCall.Run("Click", budget 2000)
 phase 1: Poller.Until(deadline)
   attempt 1: page.ProbeReadiness() = Busy (data-busy = "true")
              => ScopeNotReady(LoginPage, Busy)
   attempt 2: chain Ready
              Locate LoginButton -> found; visible; disabled (aria-disabled)
              => NotReady(Disabled, "the LoginButton (data-testid='login-btn')")
   attempt 3: chain Ready; Locate -> found; visible; enabled
              => Done
 phase 2: Playwright ClickAsync once (Timeout=0)
 exit Success, 3 attempts (below the near-miss threshold)
```

If the login command never re-enables, the call fails within 2000 ms with
`ElementNotReadyException`: "found but disabled for 2000 ms" (R0). It never retries the click.

## 10. What changes for whom

| Audience | Change |
| --- | --- |
| **Generated code** | None (R8). |
| **Control authors** | Override `TryFindElement()` + `NotFound()`, not `FindElement()`. Throw `ElementNotReadyException` from `EnsureReadyForActionCore`. Use `Confirm`. Never call `EnsureVisible` from a Core method. A wait below the call (a scroll) takes `CallRemainingMs`. |
| **Container authors** | Override `ProbeContentReadiness`, not `WaitContentReadyCore`. Use `AsksParent => false` for a scope shown over its parent. |
| **Page authors** | `IsLoaded()` takes no parameter. The busy signal is `data-busy` on `<body>`; a Blazor page host injects one line of JavaScript once. Overrides in Html page objects change mechanically. |
| **Test authors** | App bugs fail with a message naming what was seen (R0). Near-misses show in the log. Budgets mean what they say. Item and select methods wait. `ScopeNotReadyException` replaces `PageLoadException` from Html. |
| **Driver authors** | Implement `IHtmlElement` / `IHtmlDriver` directly. `InstanceKey`. `Live` maps staleness. Finds make one attempt. Report a closed target as `AppUnavailableException`. |
| **Core** | Nothing. Core is unchanged (R9). |
| **Skills** | Two new skills: `html-control` and `html-ui-test`, modelled on the MAUI ones (see [skills.md](skills.md)). |

Renames and removals are itemized in `plan.md` step 9 and in `CHANGELOG.md` at the end.

## 11. Open decisions

The eight `?A1`-`?A8` questions in `plan.md` section 3. This document takes a default for each;
step 4 or step 5 revisits the ones that block the pinned tests.
