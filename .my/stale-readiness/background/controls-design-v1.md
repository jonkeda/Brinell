# Stale readiness: class design

> **Background, superseded.** The current design is [../design.md](../design.md), and the plan is
> [../plan.md](../plan.md). Keep this only for history. What changed since:
>
> - Scopes, pages, containers and collections were added.
> - Section 2.2 (changing Core's `IElement` / `IDriver`) is replaced. Core is not changed; MAUI
>   gets its own interfaces (design R9, section 4), and the shape moves down later.
> - `Deadline` is internal.
> - Rule R0 (app bugs stay failures) and near-miss reporting were added.
> - The build order in section 8 is replaced by plan.md section 2.

The classes after all three stale-readiness documents are carried out:

- [original-plan.md](original-plan.md): stale elements in the readiness wait (phases 0-6, decisions S1-S5).
- [resolve-ready-element.md](resolve-ready-element.md): the resolve-then-act-once split, the log
  unit and the confirm budget (S6).
- [find-element-trace.md](find-element-trace.md): the lookup path (findings F1-F9).

Status: design, 2026-09-19. Nothing is implemented yet. Where this design departs from one of the
three documents, the change is marked **Change from ...**.

---

## 0. Rules the design implements

1. **One unit per public call.** Checking page readiness, the log entry/exit pair and any failure
   all belong to the public member, and happen once per call.
2. **Only the call's poll waits.** Lookups, visibility checks and readiness checks each make one
   attempt. Nothing below the poll starts a timer. This replaces the nested `EnsureVisible`
   budget, the `MauiTestContext.FindElement` loop (F1), `Menu.OpenCore`'s trigger wait (F6) and
   the timeouts on element and driver lookups (F6, F8).
3. **Each phase gets the caller's budget:** resolve with `timeoutMs`, then confirm with
   `timeoutMs` (S6, option B). Nothing uses `DefaultWait` unless the caller passed nothing.
4. **A stale element is a named signal** (`StaleElementException`), raised by both drivers. The
   poll re-resolves when it sees one. A confirmation that sees one fails without repeating the
   action.
5. **Every retry path goes through one loop.** Today there are four:
   - `ViewBase.RunPoll`
   - `RootedScopeBase.RunPoll`
   - `ObjectBase.Poll`
   - `WaitHelper.WaitFor` behind `Until`

   After the change there is one (`Poller`).
6. **The generator does not change.** The eight `Run*` helper names and signatures that
   `ActionGenerator` and `IsWaitAssertGenerator` emit stay the same. Only their bodies change.

---

## 1. The picture after

```text
public member (generated)                      e.g. Click(timeoutMs)
└─ Run*WithElement helper (ViewBase)           same name and signature as today
   └─ ControlCall.Run                          page gate once, log entry/exit once
      ├─ phase 1: Poller.Until(budget)         the only loop
      │    └─ each attempt (AttemptContext):
      │         Locate  -> TryFindElement()    one lookup (scope -> element -> driver, no waits)
      │                   + throttled sweep    at most once per Animation ms (F3)
      │         Visible -> one check, throttled scroll-into-view (plan 3.1)
      │         Ready   -> EnsureReadyForActionCore (actions only)
      │         Body    -> read / predicate / compare (queries only)
      │       every outcome is recorded as an Observation
      └─ phase 2 (actions only): coreOperation(element), exactly once
           └─ Confirm(...) -> Poller.Until(budget)   stale = "cannot confirm", never replays
   on failure: ObservationLog.ToException()    "found, then gone (replaced 3 times)", with the Locator
```

---

## 2. `Brinell.Core`

### 2.1 Exceptions (`Brinell.Core.Exceptions`)

```csharp
/// The element a handle referred to is no longer in the UI tree.
public class StaleElementException : BrinellException
{
    public StaleElementException(Locator? locator, Exception? platformError = null);
    public Locator? Locator { get; }
}

/// The element was found but is not ready for what the call wants.
public class ElementNotReadyException : BrinellException
{
    public ElementNotReadyException(Locator locator, NotReadyReason reason, string? detail = null);
    public Locator Locator { get; }
    public NotReadyReason Reason { get; }
}

public enum NotReadyReason { NotVisible, Disabled, Other }
```

**Change from plan 3.1:** there is one `ElementNotReadyException` with a reason, not a separate
`NotVisibleException`. `EnsureVisible` and `EnsureReadyForActionCore` both throw it. Today they
throw `TimeoutException` ("Element was not enabled"), which claims a timeout from a check that
never waited.

`ElementNotFoundException`, `AssertionException` and `PageLoadException` stay. `WaitTimeoutException`
is still unused by `Brinell.Maui`, and nothing here needs it.

### 2.2 Element and driver interfaces (F6, F8)

```csharp
public interface IElement<TSelf> where TSelf : class
{
    // ...state, location, interaction unchanged...

    /// One attempt; null when absent. Throws StaleElementException when this element is gone.
    TSelf? TryFindElement(Locator locator);

    /// One attempt; empty when absent.
    IReadOnlyList<TSelf> FindElements(Locator locator);

    /// One attempt; throws when absent.
    TSelf FindElement(Locator locator)
        => TryFindElement(locator) ?? throw new ElementNotFoundException(locator);
}

public interface IDriver<TElement> : IDisposable
{
    /// One attempt from the app root.
    IReadOnlyList<TElement> FindElements(Locator locator);
    // FindElement / TryFindElement removed: unused outside the drivers (F8).
}
```

- The `timeoutMs` parameters are removed, along with `TryFindElement`'s `out` shape. This
  follows the no-backward-compatibility rule.
- The latent FlaUI `while`-vs-`do` bug (F7) disappears with the member it lives in.
- `IElementScope<T>` keeps its three members. Its `FindElement` becomes a default member:

```csharp
public interface IElementScope<TElement> : IElementScope where TElement : class
{
    TElement? TryFindElement(Locator locator);
    IReadOnlyList<TElement> FindElements(Locator locator);

    TElement FindElement(Locator locator) => TryFindElement(locator) ?? throw DescribeMiss(locator);

    /// Why a lookup in this scope found nothing; a scope that can be "not ready" says so (F5).
    ElementNotFoundException DescribeMiss(Locator locator) => new(locator);
}
```

### 2.3 `TimeoutSettings`

`ElementFind` is no longer read by `Brinell.Maui`, but it stays in `Core`, because
`Brinell.Html.Playwright` and `Brinell.NativeAndroid` still read it (open point in the trace doc).
Its doc comment will say "not used by MAUI".

### 2.4 `Deadline` (`Brinell.Core.Utilities`)

```csharp
/// A point in time a phase must finish by. Created once per phase and passed down, never re-derived.
public readonly struct Deadline
{
    public static Deadline In(int milliseconds);
    public int RemainingMs { get; }
    public bool IsPassed { get; }
}
```

`Poller` uses it internally. It is public so that if option A (one budget for the whole call) is
ever wanted, the budget can be handed from phase 1 to phase 2 without changing any signature.

---

## 3. Drivers

### 3.1 Element identity

```csharp
public partial interface IMauiElement
{
    /// Identifies this element instance: the same value for the same UI element, a new value
    /// when the platform replaces it. FlaUI: the UIA runtime id; Appium: the WebDriver element id.
    string InstanceKey { get; }
}
```

This is what turns "found again" into "replaced 3 times" (plan section 2, item 4). The
`42.3410548.4.30` → `…4.65` trace in the plan is exactly this value.

### 3.2 `FlaUIMauiElement`

```csharp
/// Every member that touches UI Automation goes through this. UIA_E_ELEMENTNOTAVAILABLE
/// (FlaUI's exception, or a COMException with 0x80040201) becomes StaleElementException;
/// everything else keeps its type.
private T Live<T>(Func<T> read);
private void Live(Action act);
```

- It is a single helper, not a catch in each member (plan 3.3).
- `FlaUIDeclaredElement` never goes stale, because its answers come from the app bridge. Its
  `InstanceKey` is the declared id.
- `FindElement` / `FindElements` / `TryFindElement` make one `FindFirstDescendant` /
  `FindAllDescendants` call each.
- The chrome lookups (the navigation button and `LightDismiss`) use a private
  `FindChrome(locator, int timeoutMs)`. It stays inside the driver, because those lookups run
  inside driver actions, not control calls.

### 3.3 `AppiumMauiElement`

The same `Live` helper maps Selenium's `StaleElementReferenceException`. Once it is in,
Selenium's types appear only in `Brinell.Maui.Appium` (plan phase 1's done-check). The finds
lose their `WebDriverWait` branches, and the chrome lookup gets its own private `FindChrome`.

---

## 4. Shared polling: `Brinell.Maui/Polling/`

Four new internal types. `ViewBase` and `RootedScopeBase` both use them, and they replace the
two `RunPoll` copies, the two copies of the logging identity and `ObjectBase.Poll`.

### 4.1 `Observation` and `ObservationLog`

```csharp
/// What one attempt saw.
internal readonly record struct Observation(
    ObservationKind Kind,        // Missing, NotReady, Stale, Mismatch, Failed, Done
    string? InstanceKey,         // the element seen, when one was found
    NotReadyReason? Reason,
    Exception? Error);           // the exception behind Failed / Mismatch (AssertionException)

/// Every observation of one phase, and the failure built from them.
internal sealed class ObservationLog
{
    public void Add(Observation observation);
    public int Replacements { get; }      // distinct InstanceKeys seen, minus one
    public Observation Last { get; }

    /// The exception a timed-out phase throws:
    ///   Missing  -> ElementNotFoundException  "not found within 5000 ms"
    ///   NotReady -> ElementNotReadyException  "found but disabled for 5000 ms"
    ///   Stale / replacements > 0 -> StaleElementException "found, then gone (replaced 3 times)"
    ///   Mismatch -> the AssertionException as-is (keeps expected/actual)
    ///   Failed   -> the last exception as-is
    /// Every message names the Locator and the phase.
    public Exception ToException(Locator locator, string phase, int budgetMs);
}
```

This carries out plan section 2, item 4. It also removes the plan's section 1 item 4 (the
misleading "not visible" message) by construction: the message comes from what was observed, not
from which helper ran out of time.

### 4.2 `Poller`

```csharp
/// The only retry loop. Writes no logs, checks no page readiness, starts no nested budget.
internal sealed class Poller(int pollingIntervalMs)
{
    /// Runs attempt until it reports Done or the deadline passes.
    /// attempt returns an Observation; Kind == Done ends the loop.
    public bool Until(Func<Observation> attempt, Deadline deadline, ObservationLog log);
}
```

- `attempt` catches its own expected failures and turns them into `Observation`s. `Poller`
  treats any exception that escapes as `Failed` and retries it. That keeps the "transient"
  meaning `RunPoll` has today, but the failure is now recorded rather than silently swallowed.
- `ObjectBase.Poll` becomes `Poller.Until(() => condition() ? Done : Mismatch, ...)`.
- `ViewBase.Until` / `RootedScopeBase.Until` use it too (4.5).

### 4.3 `ControlCall`

```csharp
/// One public call: the page gate and one log entry/exit pair around the whole body,
/// the action included (resolve-ready-element.md 4.1).
internal sealed class ControlCall(IMauiTestContext context, IPageObject? page, string controlId)
{
    public T Run<T>(string caller, string? value, int budgetMs, Func<T> body);
}
```

- The `PageLoadException` message builder moves here from the three places it is copied today:
  `ViewBase.RunPoll`, `ViewBase.Run` and `RootedScopeBase.RunDo` / `RunGetWithOptionalElement`.
- The page gate runs **once**. The per-tick `Page.IsReady()` that `RunPoll` checks today becomes
  part of each attempt: a page that is not ready is recorded as `NotReady(Other, "page busy")`.
  **Change from today:** the gate still waits, but it no longer runs again inside a nested poll,
  because there is none.

### 4.4 `AttemptContext`

```csharp
/// State that lives for one phase of one call: the scroll throttles and the observation log.
internal sealed class AttemptContext(Deadline deadline, int animationMs)
{
    public ObservationLog Log { get; }
    public bool MaySweep();                     // scroll-to-find: once per Animation ms (F3)
    public bool MayScrollIntoView(string key);  // scroll-into-view: once per instance, and once per
                                                // Animation ms for the locator (plan 3.1, S3)
}
```

Control objects are created fresh on every property access (`=> new(this, "Id")`), so throttle
state cannot live on the control. Per-call state is also the right unit.

### 4.5 Confirming an action

```csharp
// On ViewBase and RootedScopeBase, replacing both Until overloads.
/// Waits, inside a Core method, for the effect of an action already performed.
/// Returns Confirmed, NotConfirmed (read the old value to the end) or Replaced (the element went
/// stale; its effect cannot be read on this instance). Never re-runs anything.
protected Confirmation Confirm<T>(Func<T?> read, Func<T?, bool> done, int? timeoutMs);

protected readonly record struct Confirmation(ConfirmationResult Result, T? LastValue, Exception? LastError);
```

- The eight act-then-confirm Core methods switch from `Until` to `Confirm` and word their failure
  by `Result`. For `Replaced`: "Toggle ran, and the element was replaced before its effect could
  be read" (plan 3.4).
- They keep throwing `InvalidOperationException`. Nothing in the three documents asks for a new
  type, and callers already catch this one.
- `timeoutMs` is the caller's value (rule 3), so the Core signatures do not change.

---

## 5. `ViewBase`

### 5.1 Finding the element (F3, F5)

```csharp
/// Where this control is: one plain lookup. The only lookup a control overrides
/// (IndicatorView, DrawingView, Stepper already override exactly this).
protected virtual IMauiElement? TryFindElement() => MauiScope.TryFindElement(Locator);

/// The message when TryFindElement found nothing; override to say what else was tried.
protected virtual ElementNotFoundException NotFound() => MauiScope.DescribeMiss(Locator);

/// The lookup inside a poll: plain, then a sweep when the context allows it.
private IMauiElement? Locate(AttemptContext attempt)
    => TryFindElement()
       ?? (MauiScope.AllowsScrollLookup && attempt.MaySweep()
           ? ScrollingElement()?.TryFindByScrolling(Locator)
           : null);

/// Outside a poll (Core methods, hand-written helpers): one attempt, no sweep throttle.
protected IMauiElement FindElement() => TryFindElement() ?? SweepOnce() ?? throw NotFound();
```

- `FindElement()` is no longer virtual. The `FindElement` overrides in `IndicatorView`,
  `DrawingView` and `Stepper` are deleted, and their messages move to `NotFound()`.
- `ScrollLookup`, `TryFindElement(ScrollLookup)` and `Resolver(ScrollLookup)` stay for the
  hand-written `VisibleAfterScroll` trio. `Resolver(Once)` becomes `Locate` with a throttle that
  allows one sweep, so both paths share one implementation.

### 5.2 Readiness checks (plan 3.1)

```csharp
/// One attempt: visible now, or scroll (throttled) and look again. Throws
/// ElementNotReadyException(NotVisible) when still not visible. Private: it is a step of the
/// poll, not something a Core method calls.
private void EnsureVisible(IMauiElement element, AttemptContext attempt);

/// Unchanged extension points.
protected virtual bool RequiresVisibilityForAction => true;
protected virtual void EnsureReadyForActionCore(IMauiElement element);   // throws ElementNotReadyException
```

- `EnsureVisible(element, int timeout)` is removed, and so is `WaitVisibleCore(element, expected,
  timeoutMs)`, which was only `EnsureVisible`'s inner poll.
- `ToggleControlBase.ToggleCore` and `RadioButton` drop their `EnsureVisible(element, ...)` calls.
  The wrapper has already made the element visible in phase 1.
- `ClickCore` and the other clickable Cores drop `EnsureClickableCore(element)`, a second
  readiness check outside the poll (resolve-ready-element.md, section 6). A change between
  "ready" and "act" now shows up as a `StaleElementException` from the action itself.

### 5.3 The attempt primitive (resolve-ready-element.md, section 3)

```csharp
/// One attempt: locate, optionally make visible, then run body on the element.
/// Records what it saw; returns Done with a value, or a retry observation.
private Observation TryOnElement<T>(AttemptContext attempt, bool ensureVisible,
    Func<IMauiElement, (bool Done, T Value)> body, ref T result)
{
    var element = Locate(attempt);
    if (element is null) return Observation.Missing;
    try
    {
        if (ensureVisible) EnsureVisible(element, attempt);
        var (done, value) = body(element);
        result = value;
        return done ? Observation.DoneWith(element) : Observation.Mismatch(element);
    }
    catch (StaleElementException ex)    { return Observation.Stale(element, ex); }
    catch (ElementNotReadyException ex) { return Observation.NotReady(element, ex); }
    catch (AssertionException ex)       { return Observation.Mismatch(element, ex); }
}
```

### 5.4 The `Run*` helpers

The signatures stay as generated, and every body is one `ControlCall.Run` around one or two
phases. The `doEnsureVisible` parameter is removed: the generator never emits it, and
`RequiresVisibilityForAction` covers the one control that needs it.

| Helper | Phase 1 (polled) | Phase 2 (once) | Change |
| --- | --- | --- | --- |
| `RunGetWithElement` | `TryOnElement(visible, e => (true, core(e)))` | | No nested budget |
| `RunWaitWithElement` | `TryOnElement(visible, e => (core(e), default))` | | No nested budget |
| `RunAssertWithElement` | Held element re-read each tick; re-locate on `Stale` or `NotReady` (S1 default) | | Re-resolves on the Brinell signal |
| `RunDoWithElement` | `ResolveReady`: `TryOnElement(RequiresVisibilityForAction, e => { EnsureReadyForActionCore(e); return (true, e); })` | `coreOperation(element)` | Action now inside the log unit |
| `RunSetWithElement` | same as `RunDoWithElement` | `coreOperation(element)` | same |
| `Run*WithOptionalElement` (3) | `Locate` only, no visibility | | Sweep throttled, otherwise as today |
| `RunWait` / `RunDo` / `RunAssert` / `Run` | unchanged meaning, now on `ControlCall` + `Poller` | | One loop |

```csharp
protected TScope RunDoWithElement(Action<IMauiElement> coreOperation,
    int? timeoutMs = null, [CallerMemberName] string? caller = null)
    => Call.Run(caller!, null, Budget(timeoutMs), () =>
    {
        var element = ResolveReady(Budget(timeoutMs));   // phase 1: polled, logless
        coreOperation(element);                          // phase 2: once; Confirm inside has its own budget
        return ContainingScope;
    });

/// Polls until the control can be acted on (resolve-ready-element.md: still needed, now a
/// thin use of the shared attempt primitive).
private IMauiElement ResolveReady(int budgetMs);
```

`RunAssertWithElement` keeps the "hold the element, re-read each tick" design pending the phase 0
measurement (S1). If a `FindElement` per poll costs little, it becomes
`TryOnElement(visible, e => (compare(get(e)), default))` like the rest, and the special case is
deleted.

---

## 6. Scopes

### 6.1 `MauiTestContext` (F1, F2, F4)

```csharp
public IMauiElement? TryFindElement(Locator locator)
    => _driver.FindElements(locator) is [var first, ..] ? first : null;   // no catch-all (F4)

public IReadOnlyList<IMauiElement> FindElements(Locator locator) => _driver.FindElements(locator);

// FindElement: the interface default. The ElementFind loop and the AppElement sweep go (F1, F2).
```

`AppRoot` and `DriverRootScope` forward as today and inherit the default `FindElement`.

### 6.2 `RootedScopeBase` (F9, plan 3.5)

```csharp
/// Runs read against the root. On StaleElementException it invalidates, re-roots once and
/// runs read again. Returns whenAbsent when there is no root. The only stale catch in the
/// scope layer.
protected T WithRoot<T>(Func<IMauiElement, T> read, T whenAbsent);

public virtual IMauiElement? TryFindElement(Locator l)
    => CanResolveElements() ? WithRoot(root => root.TryFindElement(l), null) : null;

public virtual IReadOnlyList<IMauiElement> FindElements(Locator l)
    => CanResolveElements() ? WithRoot(root => root.FindElements(l), []) : [];

public virtual ElementNotFoundException DescribeMiss(Locator l)
    => CanResolveElements() ? new(...not found within container...) : CreateScopeNotReadyException(l);

// FindElement: the interface default.
```

- `CollectionObjectBase` `TryGetItemRoot`, `TryGetItemRoots`, `GetItemCount` and
  `TryMaterializeMore` use `WithRoot`, which removes four more catches.
- `IsCachedRootValid` reads a property that is guaranteed live, by going through `Live` on
  FlaUI, so a removed root raises `StaleElementException` and invalidates. Which property it
  reads (`TagName` or `InstanceKey`) is decided by the phase 4 measurement. `ContainerRoot`'s
  bare `catch` narrows to `StaleElementException`.
- The own copies of `RunPoll`, `Until`, the logging identity and the page-gate message are
  replaced by `ControlCall`, `Poller` and `Confirm`. `ResolveReadyRoot` becomes phase 1 of
  `RunDoWithElement` / `RunSetWithElement`, exactly as in `ViewBase`.

### 6.3 `PageObjectBase`, `TabMenuMarkup`, `ElementMatch`, `ItemContainerBase`

Their Selenium catches become `StaleElementException`. `PageObjectBase.ProbeReadinessCore` keeps
its logic ("stale root, re-acquire once, report `StaleRoot`") and only changes the catch type.

---

## 7. What changes for whom

| Audience | Change |
| --- | --- |
| **Generated code** | None. Same helper names and signatures. Regenerating produces the same text. |
| **Control authors** | Override `TryFindElement()` + `NotFound()`, not `FindElement()`. Throw `ElementNotReadyException` from `EnsureReadyForActionCore`. Use `Confirm` instead of `Until`. Do not call `EnsureVisible` from a Core method (it is private now). |
| **Test authors** | Failures name what was observed. `Click(timeoutMs: 500)` means about 500 ms to find a ready element. An exception from `Brinell.Maui` is never a Selenium type. |
| **Driver authors** | Element finds make one attempt with no timeout. Implement `InstanceKey`. Map platform staleness to `StaleElementException` in one place. |
| **Removed** | `EnsureVisible(element, timeout)`, `WaitVisibleCore`, `ViewBase.FindElement()` as virtual, `doEnsureVisible`, `Until` (both classes), `ObjectBase.Poll`'s loop, `IDriver.FindElement` / `TryFindElement`, timeout parameters on `IElement` finds, `MauiTestContext.FindElement`'s loop. All go into the `CHANGELOG.md` entry in phase 6. |

---

## 8. Build order

This follows the plan's phases. Each step leaves `Brinell.Maui.Tests` green.

| Step | Plan phase | Classes | Unit tests |
| --- | --- | --- | --- |
| 1 | 0 | none: failing tests only (a)-(h), from all three documents | `ReadinessTests`, `UntilTests`, a new `FindElementTests` |
| 2 | 1 | `StaleElementException`, `ElementNotReadyException`, `InstanceKey`, `Live` in both drivers; `WithRoot`; `MauiTestContext.TryFindElement` narrowed; all Selenium catches switched | Driver mapping (mocked `0x80040201`, Selenium's type); `WithRoot` re-roots once |
| 3 | 2 | `Poller`, `ObservationLog`, `AttemptContext`, `ControlCall`; `ViewBase` and `RootedScopeBase` moved onto them; `Locate` + `NotFound()`; `MauiTestContext.FindElement` loop gone; `Menu.OpenCore` one attempt | Phase 0 tests (a)-(f), (h) pass; one log pair per call; one sweep per `Animation` ms |
| 4 | 3 | `Confirm` replaces `Until`; the 8 Core methods; `EnsureClickableCore` calls removed from clickable Cores | Replaced element → new message, action `Times.Once` |
| 5 | 2b | `IElement` / `IDriver` surface (2.2), mocks updated | Existing tests, mechanical |
| 6 | 4 | `IsCachedRootValid` per measurement | Probe result recorded |
| 7 | 5-6 | Prove on Windows / Todo / Android; docs (AD-004, skill R2, `CHANGELOG.md`) | UI suites as in the plan |

## 9. Open decisions carried forward

| # | From | Question | Design default |
| --- | --- | --- | --- |
| S1 | plan | Resolve once in `RunAssertWithElement`? | Resolve once; re-locate on `Stale` / `NotReady` |
| S6 | resolve-ready-element | Budget for resolve + confirm | Each phase gets `timeoutMs` (B); `Deadline` keeps A possible |
| D1 | this design | One `ElementNotReadyException` with a reason, or the plan's `NotVisibleException` plus a disabled type? | One type with a reason |
| D2 | this design | `ElementFind` removed from `Core`, or kept for Html / NativeAndroid? | Kept, and documented as unused by MAUI |
| D3 | this design | Should `Poller` retry exceptions it does not know (`Failed`), as `RunPoll` does today? | Yes: record them, retry, and rethrow the last one on timeout. Narrow later only with evidence. |
